//! Shader passes, full-screen Slang fragment shaders the game wrote, run over what a camera drew.
//!
//! A pass is a program (see [`super::programs`]) whose pass stage is run once per pixel of the
//! camera's picture, reading the picture so far and writing the next one. A camera takes any number
//! of them, run in order, either before tonemapping, where the picture is still linear and may be
//! brighter than white, or after it, where it matches what the screen will show.
//!
//! **What a pass reads.** Two groups. Group zero is whatever the shader declares, laid out from its
//! reflection and filled by name, the way a material's own group is, with any image the camera owns
//! bound under its own name (see [`super::views`]). Every shader running on a camera reads group
//! one, the picture, time, the view, depth, normals, motion and the previous view, which
//! [`super::views`] describes and `bcs_pass` declares.
//!
//! The vertex shader is Bevy's full-screen triangle, whose output is the position and a `uv` at
//! location zero, running from the top left.

#![cfg(feature = "render")]

use std::collections::HashMap;

use bevy::app::App;
use bevy::camera::Camera;
use bevy::core_pipeline::prepass::{
    PreviousViewUniformOffset, PreviousViewUniforms, ViewPrepassTextures,
};
use bevy::anti_alias::contrast_adaptive_sharpening::cas;
use bevy::anti_alias::fxaa::fxaa;
use bevy::anti_alias::smaa::smaa;
use bevy::core_pipeline::tonemapping::{Tonemapping, tonemapping};
use bevy::core_pipeline::{Core2d, Core2dSystems, Core3d, Core3dSystems, FullscreenShader};
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::{With, Without};
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::{IntoScheduleConfigs, SystemSet};
use bevy::ecs::system::{Commands, Query, Res, ResMut};
use bevy::render::extract_component::{ExtractComponent, ExtractComponentPlugin};
use bevy::render::globals::GlobalsBuffer;
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    BindGroup, BindGroupLayoutDescriptor, CachedRenderPipelineId, ColorTargetState, ColorWrites,
    FragmentState, Operations, PipelineCache, RenderPassColorAttachment, RenderPassDescriptor,
    RenderPipelineDescriptor, ShaderStages, TextureFormat,
};
use bevy::render::diagnostic::RecordDiagnostics;
use bevy::render::renderer::{RenderContext, RenderDevice, ViewQuery};
use bevy::render::storage::GpuShaderBuffer;
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::view::{ViewTarget, ViewUniformOffset, ViewUniforms};
use bevy::render::{Render, RenderApp, RenderStartup, RenderSystems};

use super::material::say_once;
use super::programs::{self, Role};
use super::values::{PackContext, PackError, Stand, Values, pack};
use super::views::{SceneLights, ViewImageTextures, ViewInputSources, ViewInputs, ViewLights};

/// Where the passes that run before tonemapping are, so a dispatch on a camera can run before them.
#[derive(SystemSet, Debug, Clone, PartialEq, Eq, Hash)]
pub struct BeforeTonemappingPasses;

/// Where the passes that run after tonemapping are.
#[derive(SystemSet, Debug, Clone, PartialEq, Eq, Hash)]
pub struct AfterTonemappingPasses;

/// Where the passes that run between opaque and transparent geometry are.
#[derive(SystemSet, Debug, Clone, PartialEq, Eq, Hash)]
pub struct AfterOpaquePasses;

/// Where a look runs in the tonemapper's place, first after it and before Bevy's antialiasing.
#[derive(SystemSet, Debug, Clone, PartialEq, Eq, Hash)]
pub struct LookPasses;

/// Where in a camera's frame a pass runs.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum PassPlace {
    /// On the linear picture, before tonemapping.
    BeforeTonemapping = 0,
    /// On the picture as the screen will show it.
    AfterTonemapping = 1,
    /// On the lit opaque geometry, before transparent geometry is drawn over it.
    AfterOpaque = 2,
    /// In the tonemapper's place, on the linear picture once everything Bevy does to it before
    /// tonemapping is done, turning it into the one the screen shows, which the library's look
    /// is. Bevy's tonemapper is left out on a camera with one.
    Look = 3,
}

impl PassPlace {
    pub fn from_number(number: i32) -> Option<Self> {
        Some(match number {
            0 => Self::BeforeTonemapping,
            1 => Self::AfterTonemapping,
            2 => Self::AfterOpaque,
            3 => Self::Look,
            _ => return None,
        })
    }
}

/// One pass as the camera holds it.
#[derive(Clone, Debug)]
pub struct ShaderPass {
    pub program: u32,
    pub values: Values,
    /// Moves on whenever a value does, so the render side knows when to build the pass's own bind
    /// group again rather than every frame.
    pub version: u64,
    pub place: PassPlace,
}

/// The passes a camera runs over its picture, in order.
#[derive(Component, Clone, ExtractComponent)]
#[extract_app(bevy::render::RenderApp)]
#[extract_component_filter(With<Camera>)]
pub struct BcsShaderPasses {
    pub passes: Vec<ShaderPass>,
}

/// What every pass is built from.
#[derive(Resource)]
pub struct ShaderPassPipelines {
    fullscreen: FullscreenShader,
    /// One pipeline per program, version of it and picture format, because a pipeline names the
    /// format it writes and a camera drawing in HDR writes a different one before tonemapping than
    /// after.
    pipelines: HashMap<(u32, u32, TextureFormat), CachedRenderPipelineId>,
}

/// A pass as the render side keeps it between frames.
struct PreparedPass {
    pipeline: CachedRenderPipelineId,
    own: BindGroup,
    /// What the bind group was built from, which says whether it can be kept.
    built_from: (u32, u64),
    place: PassPlace,
    label: std::borrow::Cow<'static, str>,
}

/// The passes of one view, ready to run.
#[derive(Component)]
pub struct PreparedShaderPasses(Vec<PreparedPass>);

/// Adds what runs shader passes.
pub fn install(app: &mut App) {
    app.add_plugins(ExtractComponentPlugin::<BcsShaderPasses>::default());

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app
        .add_systems(RenderStartup, init_pipelines)
        .add_systems(
            Render,
            (prepare_passes, forget_passes).in_set(RenderSystems::PrepareBindGroups),
        )
        // Before the tonemapper's pipeline is prepared for the view, which the view's tonemapper,
        // extracted again every frame, decides.
        .add_systems(Render, leave_out_tonemapper.in_set(RenderSystems::CreateViews))
        .add_systems(
            Core3d,
            (
                // After Bevy's antialiasing and before its motion blur, depth of field, bloom and
                // effect stack, each of which takes the picture into a fresh texture and, unordered
                // against a pass, could carry the picture on from before it.
                run_passes::<0>
                    .after(Core3dSystems::EarlyPostProcess)
                    .before(Core3dSystems::PostProcess)
                    .in_set(BeforeTonemappingPasses),
                // Bevy's tonemapper does nothing on a camera with a look, which runs here in its
                // place, before Bevy's antialiasing, which smooths the picture as the screen shows it.
                run_passes::<3>
                    .after(tonemapping)
                    .before(fxaa)
                    .before(smaa)
                    .before(cas)
                    .in_set(LookPasses)
                    .in_set(Core3dSystems::PostProcess),
                // After the antialiasing, each pass of which takes the picture into a fresh texture
                // and, unordered against a pass, could carry the picture on from before it.
                run_passes::<1>
                    .after(tonemapping)
                    .after(LookPasses)
                    .after(fxaa)
                    .after(smaa)
                    .after(cas)
                    .in_set(AfterTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
                // Between opaque and transparent geometry, so glass is drawn over what a pass did to
                // the lit opaque picture, and the tonemapper sees it as light.
                run_passes::<2>
                    .after(bevy::core_pipeline::core_3d::main_opaque_pass_3d)
                    .before(bevy::core_pipeline::core_3d::main_transparent_pass_3d)
                    .in_set(AfterOpaquePasses)
                    .in_set(Core3dSystems::MainPass),
            ),
        )
        .add_systems(
            Core2d,
            (
                run_passes::<0>
                    .after(Core2dSystems::EarlyPostProcess)
                    .before(Core2dSystems::PostProcess)
                    .in_set(BeforeTonemappingPasses),
                run_passes::<3>
                    .after(tonemapping)
                    .before(fxaa)
                    .before(smaa)
                    .before(cas)
                    .in_set(LookPasses)
                    .in_set(Core2dSystems::PostProcess),
                run_passes::<1>
                    .after(tonemapping)
                    .after(LookPasses)
                    .after(fxaa)
                    .after(smaa)
                    .after(cas)
                    .in_set(AfterTonemappingPasses)
                    .in_set(Core2dSystems::PostProcess),
            ),
        );
}

fn init_pipelines(mut commands: Commands, fullscreen: Res<FullscreenShader>) {
    commands.insert_resource(ShaderPassPipelines {
        fullscreen: fullscreen.clone(),
        pipelines: HashMap::new(),
    });
}

/// The layout of a program's own group for a pass.
fn own_layout(layout: &super::reflect::Layout) -> BindGroupLayoutDescriptor {
    BindGroupLayoutDescriptor::new("bcs_shader_pass_own", &layout.entries(ShaderStages::FRAGMENT))
}

/// The pipeline for a program writing a picture of `format`, queued the first time it is asked
/// for.
fn pipeline_for(
    pipelines: &mut ShaderPassPipelines,
    inputs: &ViewInputs,
    cache: &PipelineCache,
    program: u32,
    format: TextureFormat,
) -> Option<(CachedRenderPipelineId, programs::PipelineProgram)> {
    let found = programs::lookup(program)?;
    let layout = found.pass.clone()?;
    let stage = found.stages[Role::Pass as usize].clone()?;

    let key = (program, found.generation, format);

    if let Some(id) = pipelines.pipelines.get(&key) {
        return Some((*id, found));
    }

    let id = cache.queue_render_pipeline(RenderPipelineDescriptor {
        label: Some("bcs_shader_pass".into()),
        layout: vec![own_layout(&layout), inputs.layout.clone()],
        vertex: pipelines.fullscreen.to_vertex_state(),
        fragment: Some(FragmentState {
            shader: stage.shader,
            shader_defs: Vec::new(),
            entry_point: None,
            targets: vec![Some(ColorTargetState {
                format,
                blend: None,
                write_mask: ColorWrites::ALL,
            })],
            constants: Vec::new(),
        }),
        ..Default::default()
    });

    pipelines.pipelines.insert(key, id);
    Some((id, found))
}

/// Makes each view's pipelines and own bind groups match what its camera asked for.
#[allow(clippy::too_many_arguments)]
fn prepare_passes(
    mut commands: Commands,
    mut pipelines: ResMut<ShaderPassPipelines>,
    inputs: Res<ViewInputs>,
    cache: Res<PipelineCache>,
    render_device: Res<RenderDevice>,
    images: Res<RenderAssets<GpuImage>>,
    buffers: Res<RenderAssets<GpuShaderBuffer>>,
    fallback: Res<FallbackImage>,
    stand: Option<Res<Stand>>,
    mut views: Query<(
        Entity,
        &ViewTarget,
        &BcsShaderPasses,
        Option<&mut PreparedShaderPasses>,
        Option<&ViewImageTextures>,
        (Option<&bevy::pbr::ScreenSpaceAmbientOcclusionResources>, Option<&bevy::core_pipeline::mip_generation::experimental::depth::ViewDepthPyramid>),
        Option<&bevy::core_pipeline::prepass::ViewPrepassTextures>,
    )>,
) {
    let Some(stand) = stand else {
        return;
    };

    for (entity, target, asked, prepared, owned, occlusion, prepass) in &mut views {
        let names = super::views::view_names(owned, occlusion, prepass);
        let format = target.main_texture_format();

        let kept: Vec<PreparedPass> = prepared
            .map(|mut prepared| std::mem::take(&mut prepared.0))
            .unwrap_or_default();

        let mut built = Vec::with_capacity(asked.passes.len());

        for (index, pass) in asked.passes.iter().enumerate() {
            // Still compiling, or no pass stage. Left out of this frame, so the picture goes on
            // as though the pass were not there.
            let Some((pipeline, program)) =
                pipeline_for(&mut pipelines, &inputs, &cache, pass.program, format)
            else {
                continue;
            };

            let built_from = (program.generation, pass.version);

            // Built again every frame on a camera that owns images, because a history image is a
            // different texture from one frame to the next and a resized one is a new texture.
            let reusable = kept
                .get(index)
                .filter(|old| {
                    names.is_none() && old.built_from == built_from && old.pipeline == pipeline
                })
                .map(|old| old.own.clone());

            let own = match reusable {
                Some(own) => own,
                None => {
                    let layout = program.pass.clone().expect("a pass stage has a layout");

                    let context = PackContext {
                        device: &render_device,
                        images: &images,
                        buffers: &buffers,
                        fallback: &fallback,
                        stand: &stand,
                        view: names.as_deref(),
                        constants: false,
                    };

                    let packed = match pack(&layout, &pass.values, &context) {
                        Ok(packed) => packed,
                        Err(PackError::NotReady) => continue,
                        Err(PackError::Missing(message)) => {
                            say_once(format!(
                                "A pass of shader program {}: {message}",
                                pass.program
                            ));
                            continue;
                        }
                    };

                    for problem in &packed.problems {
                        say_once(format!(
                            "A pass of shader program {}: {problem}",
                            pass.program
                        ));
                    }

                    packed.bind_group(
                        &render_device,
                        "bcs_shader_pass_own",
                        &cache.get_bind_group_layout(&own_layout(&layout)),
                    )
                }
            };

            built.push(PreparedPass {
                pipeline,
                own,
                built_from,
                place: pass.place,
                label: super::programs::label(pass.program),
            });
        }

        commands.entity(entity).insert(PreparedShaderPasses(built));
    }
}

/// Leaves Bevy's tonemapper out of a view with a look, which takes its place, setting the view's
/// extracted tonemapper to none for the frame, which leaves the tonemapper's system doing nothing.
///
/// The view's own setting is the camera's, extracted again every frame, so the tonemapper it names
/// comes back the frame the look goes. A view drawing without a high dynamic range keeps Bevy's,
/// which its materials apply as they draw.
fn leave_out_tonemapper(
    mut views: Query<(&BcsShaderPasses, &bevy::render::camera::ExtractedCamera, &mut Tonemapping)>,
) {
    for (passes, camera, mut tonemapper) in &mut views {
        if camera.hdr && passes.passes.iter().any(|pass| pass.place == PassPlace::Look) {
            *tonemapper = Tonemapping::None;
        }
    }
}

/// Drops what a view kept for passes its camera no longer has.
fn forget_passes(
    mut commands: Commands,
    views: Query<Entity, (With<PreparedShaderPasses>, Without<BcsShaderPasses>)>,
) {
    for entity in &views {
        commands.entity(entity).remove::<PreparedShaderPasses>();
    }
}

/// Runs a view's passes on one side of tonemapping.
#[allow(clippy::too_many_arguments)]
fn run_passes<const PLACE: u8>(
    view: ViewQuery<(
        &ViewTarget,
        &ViewUniformOffset,
        &PreparedShaderPasses,
        Option<&ViewPrepassTextures>,
        Option<&PreviousViewUniformOffset>,
        Option<&bevy::pbr::ViewLightsUniformOffset>,
        Option<&bevy::pbr::ViewShadowBindings>,
        (Option<&super::views::ViewEnvironmentTextures>, Option<&bevy::pbr::resources::GpuAtmosphere>, Option<&bevy::pbr::resources::AtmosphereTextures>),
    )>,
    inputs: Res<ViewInputs>,
    fallback: Res<FallbackImage>,
    cache: Res<PipelineCache>,
    globals: Res<GlobalsBuffer>,
    view_uniforms: Res<ViewUniforms>,
    previous_uniforms: Option<Res<PreviousViewUniforms>>,
    scene: SceneLights,
    mut ctx: RenderContext,
) {
    let (target, offset, prepared, prepass, previous, light_offset, shadows, (environment, atmosphere, atmosphere_textures)) = view.into_inner();

    if !prepared
        .0
        .iter()
        .any(|pass| pass.place as u8 == PLACE)
    {
        return;
    }

    // Between opaque and transparent geometry a multisampled camera's picture is still its
    // samples, resolved only once transparent geometry is drawn, which would draw over whatever a
    // pass did to the picture beside it.
    if PLACE == PassPlace::AfterOpaque as u8 && target.sampled_main_texture().is_some() {
        super::material::say_once(
            "A camera drawing several samples a pixel runs passes after opaque geometry, which \
             needs one sample a pixel (PostSettings.Msaa of one), so they do not run."
                .to_string(),
        );
        return;
    }

    // A camera drawing to a picture of eight bits a channel tonemaps in its materials' shaders, so
    // there is no linear picture past white left for a look to bring under it.
    if PLACE == PassPlace::Look as u8 && target.main_texture_format().is_srgb() {
        super::material::say_once(
            "A camera drawing without a high dynamic range has a look, which needs one \
             (PostSettings.Hdr), so Bevy's tonemapper runs in its place."
                .to_string(),
        );
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
        super::views::ViewAtmosphere::of(atmosphere, atmosphere_textures),
    );

    for pass in &prepared.0 {
        if pass.place as u8 != PLACE {
            continue;
        }

        let Some(pipeline) = cache.get_render_pipeline(pass.pipeline) else {
            continue;
        };

        let post = target.post_process_write();

        let Some((group, offsets)) = sources.bind(
            ctx.render_device(),
            &cache,
            &inputs,
            post.source,
            globals.clone(),
            view_binding.clone(),
            offset.offset,
            &scene,
            &ViewLights {
                offset: light_offset,
                shadows,
            },
        ) else {
            return;
        };

        let descriptor = RenderPassDescriptor {
            label: Some("bcs_shader_pass"),
            color_attachments: &[Some(RenderPassColorAttachment {
                view: post.destination,
                depth_slice: None,
                resolve_target: None,
                ops: Operations::default(),
            })],
            depth_stencil_attachment: None,
            timestamp_writes: None,
            occlusion_query_set: None,
            multiview_mask: None,
        };

        let diagnostics = ctx.diagnostic_recorder();
        let diagnostics = diagnostics.as_deref();

        let mut render_pass = ctx.command_encoder().begin_render_pass(&descriptor);
        let span = diagnostics.pass_span(&mut render_pass, pass.label.clone());
        render_pass.set_pipeline(pipeline);
        render_pass.set_bind_group(0, &pass.own, &[]);
        render_pass.set_bind_group(1, &group, &offsets);
        render_pass.draw(0..3, 0..1);
        span.end(&mut render_pass);
    }
}
