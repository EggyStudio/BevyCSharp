//! Shader passes: full-screen Slang fragment shaders the game wrote, run over what a camera drew.
//!
//! A pass is a program (see [`super::programs`]) whose pass stage is run once per pixel of the
//! camera's picture, reading the picture so far and writing the next one. A camera takes any number
//! of them, run in order, either before tonemapping, where the picture is still linear and may be
//! brighter than white, or after it, where it is what the screen will show.
//!
//! **What a pass reads.** Two groups. Group zero is whatever the shader declares, laid out from its
//! reflection and filled by name, the way a material's own group is, with any image the camera owns
//! bound under its own name (see [`super::views`]). Group one is what every shader running on a
//! camera reads, the picture, time, the view, depth, normals, motion and the previous view, which
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
use bevy::core_pipeline::tonemapping::tonemapping;
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

/// One pass as the camera holds it.
#[derive(Clone, Debug)]
pub struct ShaderPass {
    pub program: u32,
    pub values: Values,
    /// Moves on whenever a value does, so the render side knows when to build the pass's own bind
    /// group again rather than every frame.
    pub version: u64,
    pub after_tonemapping: bool,
}

/// The passes a camera runs over its picture, in order.
#[derive(Component, Clone, ExtractComponent)]
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
    /// What the bind group was built from, which is what says it can be kept.
    built_from: (u32, u64),
    after_tonemapping: bool,
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
        .add_systems(
            Core3d,
            (
                run_passes::<false>
                    .before(tonemapping)
                    .in_set(BeforeTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
                run_passes::<true>
                    .after(tonemapping)
                    .in_set(AfterTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
            ),
        )
        .add_systems(
            Core2d,
            (
                run_passes::<false>
                    .before(tonemapping)
                    .in_set(BeforeTonemappingPasses)
                    .in_set(Core2dSystems::PostProcess),
                run_passes::<true>
                    .after(tonemapping)
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
        Option<&bevy::pbr::ScreenSpaceAmbientOcclusionResources>,
    )>,
) {
    let Some(stand) = stand else {
        return;
    };

    for (entity, target, asked, prepared, owned, occlusion) in &mut views {
        let names = super::views::view_names(owned, occlusion);
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
                after_tonemapping: pass.after_tonemapping,
            });
        }

        commands.entity(entity).insert(PreparedShaderPasses(built));
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
fn run_passes<const AFTER_TONEMAPPING: bool>(
    view: ViewQuery<(
        &ViewTarget,
        &ViewUniformOffset,
        &PreparedShaderPasses,
        Option<&ViewPrepassTextures>,
        Option<&PreviousViewUniformOffset>,
        Option<&bevy::pbr::ViewLightsUniformOffset>,
        Option<&bevy::pbr::ViewShadowBindings>,
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
    let (target, offset, prepared, prepass, previous, light_offset, shadows) = view.into_inner();

    if !prepared
        .0
        .iter()
        .any(|pass| pass.after_tonemapping == AFTER_TONEMAPPING)
    {
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
    );

    for pass in &prepared.0 {
        if pass.after_tonemapping != AFTER_TONEMAPPING {
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

        let mut render_pass = ctx.command_encoder().begin_render_pass(&descriptor);
        render_pass.set_pipeline(pipeline);
        render_pass.set_bind_group(0, &pass.own, &[]);
        render_pass.set_bind_group(1, &group, &offsets);
        render_pass.draw(0..3, 0..1);
    }
}
