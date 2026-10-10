//! Compute a camera runs at a point of its frame.

use std::collections::HashMap;

use bevy::asset::Handle;
use bevy::camera::Camera;
use bevy::core_pipeline::prepass::{
    PreviousViewUniformOffset, PreviousViewUniforms, ViewPrepassTextures,
};
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::{With, Without};
use bevy::ecs::resource::Resource;
use bevy::ecs::system::{Commands, Query, Res, ResMut};
use bevy::pbr::{ScreenSpaceAmbientOcclusionResources, ViewLightsUniformOffset, ViewShadowBindings};
use bevy::render::diagnostic::RecordDiagnostics;
use bevy::render::extract_component::ExtractComponent;
use bevy::render::globals::GlobalsBuffer;
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    BindGroup, BindGroupLayoutDescriptor, Buffer, CachedComputePipelineId, ComputePassDescriptor,
    ComputePipelineDescriptor, PipelineCache, ShaderStages,
};
use bevy::render::renderer::{RenderContext, RenderDevice, ViewQuery};
use bevy::render::storage::{GpuShaderBuffer, ShaderBuffer};
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::view::{ViewTarget, ViewUniformOffset, ViewUniforms};

use crate::render::material::say_once;
use crate::render::programs::{self, Role};
use crate::render::values::{PackContext, PackError, Stand, Values, pack};
use super::{
    SceneLights, ViewImageTextures, ViewInputSources, ViewInputs, ViewLights, scaled, view_names,
};

/// Where in a camera's frame a dispatch runs.
#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub enum FramePoint {
    /// Once depth, normals, motion, shadows and Bevy's own ambient occlusion are drawn, before
    /// anything is lit.
    AfterPrepass = 0,
    /// Once opaque geometry is drawn, before transparent geometry.
    AfterOpaque = 1,
    /// On the linear picture, before the passes that run before tonemapping.
    BeforeTonemapping = 2,
    /// On the picture as the screen will show it, before the passes that run after tonemapping.
    AfterTonemapping = 3,
    /// Inside the prepass, once Bevy's geometry has drawn depth, normals, motion and the deferred
    /// buffers, so a draw here is part of what everything after the prepass reads. Numbered last
    /// because it was added last, though it comes first in the frame.
    InPrepass = 4,
}

impl FramePoint {
    pub fn from_number(number: i32) -> Option<Self> {
        Some(match number {
            0 => Self::AfterPrepass,
            1 => Self::AfterOpaque,
            2 => Self::BeforeTonemapping,
            3 => Self::AfterTonemapping,
            4 => Self::InPrepass,
            _ => return None,
        })
    }
}

/// How many workgroups a dispatch on a camera runs.
#[derive(Clone, Debug)]
pub enum Workgroups {
    /// Enough of `size` to cover `scale` of the picture, for a shader working a pixel at a time.
    PerPixel { size: [u32; 2], scale: f32 },
    /// Exactly these.
    Fixed([u32; 3]),
    /// Three numbers at `offset` in a buffer, written on the GPU.
    Indirect {
        buffer: Handle<ShaderBuffer>,
        offset: u64,
    },
}

/// One dispatch a camera runs every frame.
#[derive(Clone, Debug)]
pub struct ViewDispatch {
    pub program: u32,
    pub values: Values,
    pub point: FramePoint,
    pub workgroups: Workgroups,
}

/// The dispatches a camera runs every frame, in order.
#[derive(Component, Clone, ExtractComponent)]
#[extract_app(bevy::render::RenderApp)]
#[extract_component_filter(With<Camera>)]
pub struct BcsViewDispatches(pub Vec<ViewDispatch>);

/// A dispatch ready to run on a view.
struct PreparedViewDispatch {
    point: FramePoint,
    pipeline: crate::render::spirv_compute::ComputePipelineRef,
    own: BindGroup,
    workgroups: PreparedWorkgroups,
    /// Whether it binds Solari's scene as group two, taken when it runs.
    traces_scene: bool,
    label: std::borrow::Cow<'static, str>,
}

enum PreparedWorkgroups {
    Direct([u32; 3]),
    Indirect(Buffer, u64),
}

/// A view's dispatches, ready to run.
#[derive(Component)]
pub struct PreparedViewDispatches(Vec<PreparedViewDispatch>);

/// One compute pipeline per program and version of it, for dispatches on a camera.
#[derive(Resource, Default)]
pub(super) struct ViewComputePipelines(HashMap<(u32, u32), crate::render::spirv_compute::ComputePipelineRef>);

/// The layout of a program's own group for a dispatch on a camera.
fn own_layout(layout: &crate::render::reflect::Layout) -> BindGroupLayoutDescriptor {
    BindGroupLayoutDescriptor::new("bcs_view_compute_own", &layout.entries(ShaderStages::COMPUTE))
}

/// The pipeline for a program's current version dispatched on a camera, queued the first time it
/// is asked for.
fn view_pipeline_for(
    pipelines: &mut ViewComputePipelines,
    inputs: &ViewInputs,
    cache: &PipelineCache,
    device: &RenderDevice,
    id: u32,
) -> Option<(crate::render::spirv_compute::ComputePipelineRef, programs::PipelineProgram)> {
    let program = programs::lookup(id)?;
    let layout = program.compute.clone()?;
    let stage = program.stages[Role::Compute as usize].clone()?;

    let key = (id, program.generation);

    if let Some(pipeline) = pipelines.0.get(&key) {
        return Some((pipeline.clone(), program));
    }

    let mut groups = vec![own_layout(&layout), inputs.layout.clone()];

    // Solari's scene, for a shader tracing rays through `bcs_ray`, where this app runs Solari.
    if layout.traces_scene {
        groups.push(crate::render::solari::scene_layout()?);
    }

    // SPIR-V built here, as `crate::render::compute` builds it, and anything else queued in Bevy's cache.
    use crate::render::spirv_compute::ComputePipelineRef;
    let pipeline = match &stage.spirv {
        Some(spirv) => crate::render::spirv_compute::build(device, cache, "bcs_view_compute", &groups, spirv, &stage.entry)
            .map_or(ComputePipelineRef::Cached(CachedComputePipelineId::INVALID), ComputePipelineRef::Own),
        None => ComputePipelineRef::Cached(cache.queue_compute_pipeline(ComputePipelineDescriptor {
            label: Some("bcs_view_compute".into()),
            layout: groups,
            immediate_size: 0,
            shader: stage.shader,
            shader_defs: Vec::new(),
            entry_point: None,
            zero_initialize_workgroup_memory: true,
            constants: Vec::new(),
        })),
    };

    pipelines.0.insert(key, pipeline.clone());
    Some((pipeline, program))
}

#[allow(clippy::too_many_arguments)]
pub(super) fn prepare_view_dispatches(
    mut commands: Commands,
    mut pipelines: ResMut<ViewComputePipelines>,
    inputs: Res<ViewInputs>,
    cache: Res<PipelineCache>,
    render_device: Res<RenderDevice>,
    images: Res<RenderAssets<GpuImage>>,
    buffers: Res<RenderAssets<GpuShaderBuffer>>,
    fallback: Res<FallbackImage>,
    stand: Option<Res<Stand>>,
    views: Query<(
        Entity,
        &ViewTarget,
        &BcsViewDispatches,
        Option<&ViewImageTextures>,
        (Option<&ScreenSpaceAmbientOcclusionResources>, Option<&bevy::core_pipeline::mip_generation::experimental::depth::ViewDepthPyramid>),
        Option<&ViewPrepassTextures>,
    )>,
) {
    let Some(stand) = stand else {
        return;
    };

    // A program that reads a camera's inputs runs only on a camera, so its pipeline is built here
    // as soon as it exists, which is also what says it is ready. One that does not is built by
    // `crate::render::compute` against time alone, and built here too only once a camera runs it.
    for id in 0..programs::table_len() {
        let reads_view = programs::lookup(id)
            .and_then(|program| program.compute)
            .is_some_and(|layout| layout.reads_view);

        if reads_view
            && let Some((pipeline, program)) = view_pipeline_for(&mut pipelines, &inputs, &cache, &render_device, id)
            && pipeline.get(&cache).is_some()
        {
            programs::mark_compute_ready(id, program.generation);
        }
    }

    for (entity, target, asked, owned, occlusion, prepass) in &views {
        let names = view_names(owned, occlusion, prepass);
        let mut prepared = Vec::with_capacity(asked.0.len());

        for dispatch in &asked.0 {
            let Some((pipeline, program)) =
                view_pipeline_for(&mut pipelines, &inputs, &cache, &render_device, dispatch.program)
            else {
                let found = programs::lookup(dispatch.program).filter(|program| program.generation > 0);

                if found.as_ref().is_some_and(|program| program.stages[Role::Compute as usize].is_none()) {
                    say_once(format!(
                        "A camera runs shader program {}, which has no compute stage, so it does \
                         nothing.",
                        dispatch.program
                    ));
                } else if found
                    .as_ref()
                    .and_then(|program| program.compute.as_ref())
                    .is_some_and(|layout| layout.traces_scene)
                {
                    say_once(crate::render::compute::no_scene(dispatch.program));
                }
                continue;
            };

            let layout = program.compute.clone().expect("a compute stage has a layout");

            let context = PackContext {
                device: &render_device,
                images: &images,
                buffers: &buffers,
                fallback: &fallback,
                stand: &stand,
                view: names.as_deref(),
            };

            let packed = match pack(&layout, &dispatch.values, &context) {
                Ok(packed) => packed,
                Err(PackError::NotReady) => continue,
                Err(PackError::Missing(message)) => {
                    say_once(format!(
                        "A dispatch on a camera of shader program {}: {message}",
                        dispatch.program
                    ));
                    continue;
                }
            };

            for problem in &packed.problems {
                say_once(format!(
                    "A dispatch on a camera of shader program {}: {problem}",
                    dispatch.program
                ));
            }

            let workgroups = match &dispatch.workgroups {
                Workgroups::PerPixel { size, scale } => {
                    let picture = target.main_texture();
                    let (width, height) = scaled(picture.width(), picture.height(), *scale);
                    PreparedWorkgroups::Direct([
                        width.div_ceil(size[0].max(1)),
                        height.div_ceil(size[1].max(1)),
                        1,
                    ])
                }
                Workgroups::Fixed(groups) => PreparedWorkgroups::Direct(*groups),
                Workgroups::Indirect { buffer, offset } => match buffers.get(buffer) {
                    Some(gpu) => PreparedWorkgroups::Indirect(gpu.buffer.clone(), *offset),
                    None => continue,
                },
            };

            prepared.push(PreparedViewDispatch {
                point: dispatch.point,
                pipeline,
                own: packed.bind_group(
                    &render_device,
                    "bcs_view_compute_own",
                    &cache.get_bind_group_layout(&own_layout(&layout)),
                ),
                workgroups,
                traces_scene: layout.traces_scene,
                label: programs::label(dispatch.program),
            });
        }

        commands.entity(entity).insert(PreparedViewDispatches(prepared));
    }
}

/// Drops what a view kept for dispatches its camera no longer has.
pub(super) fn forget_view_dispatches(
    mut commands: Commands,
    views: Query<Entity, (With<PreparedViewDispatches>, Without<BcsViewDispatches>)>,
) {
    for entity in &views {
        commands.entity(entity).remove::<PreparedViewDispatches>();
    }
}

/// Runs a view's dispatches for one point of its frame.
#[allow(clippy::too_many_arguments)]
pub(super) fn run_view_dispatches<const POINT: u8>(
    view: ViewQuery<(
        &ViewTarget,
        &ViewUniformOffset,
        &PreparedViewDispatches,
        Option<&ViewPrepassTextures>,
        Option<&PreviousViewUniformOffset>,
        Option<&ViewLightsUniformOffset>,
        Option<&ViewShadowBindings>,
        Option<&super::ViewEnvironmentTextures>,
    )>,
    inputs: Res<ViewInputs>,
    fallback: Res<FallbackImage>,
    cache: Res<PipelineCache>,
    globals: Res<GlobalsBuffer>,
    view_uniforms: Res<ViewUniforms>,
    previous_uniforms: Option<Res<PreviousViewUniforms>>,
    scene: SceneLights,
    #[cfg(feature = "solari")] traced: Option<Res<crate::render::solari::SceneBindings>>,
    mut ctx: RenderContext,
) {
    let (target, offset, prepared, prepass, previous, light_offset, shadows, environment) = view.into_inner();

    #[cfg(feature = "solari")]
    let traced = crate::render::compute::scene_group(&traced);
    #[cfg(not(feature = "solari"))]
    let traced: Option<BindGroup> = None;

    if !prepared.0.iter().any(|dispatch| dispatch.point as u8 == POINT) {
        return;
    }

    let (Some(globals), Some(view_binding)) =
        (globals.buffer.binding(), view_uniforms.uniforms.binding())
    else {
        return;
    };

    let sources = ViewInputSources::gather(
        &inputs,
        &fallback,
        prepass,
        previous_uniforms.as_deref().zip(previous),
        environment,
    );

    let Some((group, offsets)) = sources.bind(
        ctx.render_device(),
        &cache,
        &inputs,
        target.main_texture_view(),
        globals,
        view_binding,
        offset.offset,
        &scene,
        &ViewLights {
            offset: light_offset,
            shadows,
        },
    ) else {
        return;
    };

    for dispatch in &prepared.0 {
        if dispatch.point as u8 != POINT {
            continue;
        }

        // Still compiling. Left out of this frame, as a pass still compiling is.
        let Some(pipeline) = dispatch.pipeline.get(&cache) else {
            continue;
        };

        // Solari has not built its scene's bind group yet, which it does once a mesh is traced
        // and a light shines.
        if dispatch.traces_scene && traced.is_none() {
            continue;
        }

        let diagnostics = ctx.diagnostic_recorder();
        let diagnostics = diagnostics.as_deref();

        let mut pass = ctx
            .command_encoder()
            .begin_compute_pass(&ComputePassDescriptor {
                label: Some("bcs_view_compute"),
                timestamp_writes: None,
            });

        let span = diagnostics.pass_span(&mut pass, dispatch.label.clone());

        pass.set_pipeline(pipeline);
        pass.set_bind_group(0, &dispatch.own, &[]);
        pass.set_bind_group(1, &group, &offsets);

        if dispatch.traces_scene
            && let Some(traced) = &traced
        {
            pass.set_bind_group(crate::render::reflect::SCENE_GROUP, traced, &[]);
        }

        match &dispatch.workgroups {
            PreparedWorkgroups::Direct([x, y, z]) => pass.dispatch_workgroups(*x, *y, *z),
            PreparedWorkgroups::Indirect(buffer, offset) => {
                pass.dispatch_workgroups_indirect(buffer, *offset)
            }
        }

        span.end(&mut pass);
    }
}
