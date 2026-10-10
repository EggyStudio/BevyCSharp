//! Geometry a camera draws every frame out of buffers its vertex shader reads, into its picture,
//! its images, its prepass and deferred buffers or its shadow maps.

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
    BindGroup, BindGroupLayoutDescriptor, Buffer, PipelineCache, RenderPipeline, ShaderStages,
    TextureFormat, TextureSampleType, TextureView,
};
use bevy::render::renderer::{RenderContext, RenderDevice, ViewQuery};
use bevy::render::storage::{GpuShaderBuffer, ShaderBuffer};
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::view::{ViewTarget, ViewUniformOffset, ViewUniforms};

use crate::render::material::say_once;
use crate::render::programs::{self, Role};
use crate::render::values::{PackContext, PackError, Stand, Values, pack};
use super::images::level_view;
use super::{
    FramePoint, SceneLights, ViewEnvironmentTextures, ViewImageTextures, ViewInputSources,
    ViewInputs, ViewLights, view_names,
};

/// How what a draw writes combines with what is already in the picture.
#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
pub enum DrawBlend {
    /// Replaces it.
    Opaque,
    /// Over it, by the fragment's alpha.
    Alpha,
    /// Added to it, for anything glowing.
    Add,
}

/// How many vertices and instances a draw on a camera draws, or how many workgroups of its task
/// or mesh shader run, for a program drawing with mesh shaders.
#[derive(Clone, Debug)]
pub enum DrawCount {
    Fixed { vertices: u32, instances: u32 },
    /// Workgroups of the task shader, or of the mesh shader where there is no task shader.
    Groups { x: u32, y: u32, z: u32 },
    /// Unsigned integers at `offset` in a buffer, written on the GPU: four for vertices,
    /// instances, the first vertex and the first instance, or three workgroup counts for a program
    /// drawing with mesh shaders.
    Indirect {
        buffer: Handle<ShaderBuffer>,
        offset: u64,
    },
}

/// Geometry a program draws on a camera every frame, out of buffers its vertex shader reads.
#[derive(Clone, Debug)]
pub struct ViewDraw {
    pub program: u32,
    pub values: Values,
    pub point: FramePoint,
    pub count: DrawCount,
    pub blend: DrawBlend,
    /// Whether it writes depth, as opaque geometry does, or only tests against it, as anything
    /// see-through does.
    pub depth_write: bool,
    /// Drawn into the camera's directional shadow maps as well, depth alone, so it casts shadows.
    pub casts_shadows: bool,
    /// The camera's own images to draw into instead of the picture, one a fragment shader output
    /// in order, or none for the picture. The prepass's `normals` and `motion` and the deferred
    /// buffers' `gbuffer` and `lighting_pass` are named among them the same way.
    pub targets: Vec<String>,
}

/// The draws a camera makes every frame, in order.
#[derive(Component, Clone, ExtractComponent)]
#[extract_app(bevy::render::RenderApp)]
#[extract_component_filter(With<Camera>)]
pub struct BcsViewDraws(pub Vec<ViewDraw>);

/// What decides a draw's pipeline: program, version, target formats, sample count, blend, depth
/// write and whether there is depth at all, since a pipeline names all of them.
type PipelineKey = (u32, u32, Vec<TextureFormat>, u32, DrawBlend, bool, bool);

/// One render pipeline for each [`PipelineKey`], queued in Bevy's cache, or built by the bridge for
/// a program drawing with mesh shaders, where one that could not be built is kept as nothing so it
/// is not tried every frame.
#[derive(Resource, Default)]
pub(super) struct ViewDrawPipelines {
    cached: HashMap<PipelineKey, bevy::render::render_resource::CachedRenderPipelineId>,
    meshes: HashMap<PipelineKey, Option<RenderPipeline>>,
}

/// A draw's pipeline, queued in Bevy's cache or built by the bridge.
pub(super) enum DrawPipeline {
    Cached(bevy::render::render_resource::CachedRenderPipelineId),
    Own(RenderPipeline),
}

/// A draw ready to run on a view.
pub(super) struct PreparedViewDraw {
    point: FramePoint,
    pipeline: DrawPipeline,
    pub(super) own: BindGroup,
    pub(super) count: PreparedDrawCount,
    /// The camera images it draws into instead of the picture, and whether it is tested against
    /// the camera's depth there.
    target: Option<(Vec<TextureView>, bool)>,
    /// Which prepass inputs it writes, normals in the first bit and motion in the second, which
    /// its inputs bind a stand-in for, since a pass cannot read what it draws into.
    writes_prepass: u8,
    /// Its pipeline for drawing depth alone into a shadow map, where it casts shadows.
    pub(super) shadow_pipeline: Option<bevy::render::render_resource::CachedRenderPipelineId>,
    /// Whether it writes the camera's depth, which inside the prepass is copied into the
    /// prepass's once the draws there are done.
    writes_depth: bool,
    label: std::borrow::Cow<'static, str>,
}

pub(super) enum PreparedDrawCount {
    Direct(u32, u32),
    Indirect(Buffer, u64),
    Groups(u32, u32, u32),
    GroupsIndirect(Buffer, u64),
}

impl PreparedDrawCount {
    /// Draws as counted, on a pass with the draw's pipeline and groups set.
    pub(super) fn draw(&self, pass: &mut wgpu::RenderPass<'_>) {
        match self {
            PreparedDrawCount::Direct(vertices, instances) => pass.draw(0..*vertices, 0..*instances),
            PreparedDrawCount::Indirect(buffer, offset) => pass.draw_indirect(buffer, *offset),
            PreparedDrawCount::Groups(x, y, z) => pass.draw_mesh_tasks(*x, *y, *z),
            PreparedDrawCount::GroupsIndirect(buffer, offset) => pass.draw_mesh_tasks_indirect(buffer, *offset),
        }
    }
}

/// A view's draws, ready to run.
#[derive(Component)]
pub struct PreparedViewDraws(pub(super) Vec<PreparedViewDraw>);

/// The layout of a program's own group for a draw on a camera, seen by its task and mesh stages
/// where it draws with mesh shaders.
fn draw_layout(layout: &crate::render::reflect::Layout, meshes: bool) -> BindGroupLayoutDescriptor {
    let stages = if meshes {
        ShaderStages::TASK | ShaderStages::MESH | ShaderStages::FRAGMENT
    } else {
        ShaderStages::VERTEX_FRAGMENT
    };

    BindGroupLayoutDescriptor::new("bcs_view_draw_own", &layout.entries(stages))
}

#[allow(clippy::too_many_arguments)]
pub(super) fn prepare_view_draws(
    mut commands: Commands,
    mut pipelines: ResMut<ViewDrawPipelines>,
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
        &BcsViewDraws,
        Option<&bevy::render::view::Msaa>,
        Option<&ViewImageTextures>,
        (Option<&ScreenSpaceAmbientOcclusionResources>, Option<&bevy::core_pipeline::mip_generation::experimental::depth::ViewDepthPyramid>),
        Option<&ViewPrepassTextures>,
    )>,
) {
    use bevy::render::render_resource::{
        BlendComponent, BlendFactor, BlendOperation, BlendState, ColorTargetState, ColorWrites,
        CompareFunction, DepthStencilState, FragmentState, MultisampleState,
        RenderPipelineDescriptor, VertexState,
    };

    let Some(stand) = stand else {
        return;
    };

    for (entity, target, asked, msaa, owned, occlusion, prepass) in &views {
        let names = view_names(owned, occlusion, prepass);
        let format = target.main_texture_format();
        let samples = msaa.map_or(1, |msaa| msaa.samples());
        let mut prepared = Vec::with_capacity(asked.0.len());

        for draw in &asked.0 {
            let Some(program) = programs::lookup(draw.program) else {
                continue;
            };

            // A vertex stage, or a mesh stage with the task stage before it where there is one.
            let meshes = super::mesh_draws::draws_meshes(&program);
            let vertex = program.stages[Role::DrawVertex as usize].clone();

            let (Some(layout), Some(fragment), true) = (
                program.draw.clone(),
                program.stages[Role::DrawFragment as usize].clone(),
                meshes || vertex.is_some(),
            ) else {
                if program.generation > 0 {
                    say_once(format!(
                        "A camera draws shader program {}, which needs a draw fragment stage and \
                         a draw vertex or draw mesh stage before it, so it draws nothing.",
                        draw.program
                    ));
                }
                continue;
            };

            if meshes && draw.casts_shadows {
                say_once(format!(
                    "A draw on a camera of shader program {} draws with mesh shaders, which are \
                     not drawn into shadow maps, so it casts no shadow.",
                    draw.program
                ));
            }

            // The deferred buffers are drawn into inside the prepass and nowhere else, since by
            // the next point Bevy has copied out of them which pixels its deferred lighting lights,
            // and a draw inside the prepass goes into targets, since the main pass clears the
            // picture after it.
            let deferred = draw.targets.iter().any(|name| name == "gbuffer" || name == "lighting_pass");

            if deferred && draw.point != FramePoint::InPrepass {
                say_once(format!(
                    "A draw on a camera of shader program {} targets the deferred buffers at {:?}, \
                     where Bevy's deferred lighting has taken which pixels to light, so it \
                     draws nothing. It belongs at FramePoint.InPrepass.",
                    draw.program, draw.point
                ));
                continue;
            }

            if draw.point == FramePoint::InPrepass && draw.targets.is_empty() {
                say_once(format!(
                    "A draw on a camera of shader program {} draws into the picture inside the \
                     prepass, which the main pass clears afterward, so it draws nothing. Inside \
                     the prepass a draw goes into targets.",
                    draw.program
                ));
                continue;
            }

            if draw.targets.iter().any(|name| name == "gbuffer")
                && layout.bindings.values().any(|binding| binding.name == "gbuffer")
            {
                say_once(format!(
                    "A draw on a camera of shader program {} reads the G-buffer it draws into, \
                     which a render pass cannot do, so it draws nothing.",
                    draw.program
                ));
                continue;
            }

            // Camera images as the targets are drawn once a pixel, and against the camera's depth
            // only where they are all the picture's size and the camera draws once a pixel too,
            // since a depth attachment has to match every target in both. The prepass's motion
            // and normals are targets as well, which is how geometry drawn out of buffers gives
            // Bevy's temporal effects the motion they read.
            let mut slots: Vec<(TextureView, TextureFormat, (u32, u32))> = Vec::with_capacity(draw.targets.len());

            for name in &draw.targets {
                let own = owned.and_then(|owned| owned.slots.iter().find(|slot| &slot.spec.name == name)).map(|slot| {
                    (level_view(&slot.textures[slot.current], 0), slot.spec.format, slot.size)
                });

                let engine = || {
                    let attachment = match name.as_str() {
                        "motion" => prepass.and_then(|prepass| prepass.motion_vectors.as_ref()),
                        "normals" => prepass.and_then(|prepass| prepass.normal.as_ref()),
                        "gbuffer" => prepass.and_then(|prepass| prepass.deferred.as_ref()),
                        "lighting_pass" => {
                            prepass.and_then(|prepass| prepass.deferred_lighting_pass_id.as_ref())
                        }
                        _ => None,
                    }?;

                    let texture = &attachment.texture.texture;
                    (texture.sample_count() == 1).then(|| {
                        (attachment.texture.default_view.clone(), texture.format(), (texture.width(), texture.height()))
                    })
                };

                match own.or_else(engine) {
                    Some(found) => slots.push(found),
                    None => {
                        say_once(format!(
                            "A draw on a camera of shader program {} targets {name}, which is \
                             neither one of the camera's images nor a buffer of its prepass it \
                             draws once a pixel, so it draws nothing. The deferred buffers are \
                             there on a camera drawing deferred.",
                            draw.program
                        ));
                    }
                }
            }

            if slots.len() != draw.targets.len() {
                continue;
            }

            let picture = target.main_texture();
            let (formats, samples, has_depth) = if slots.is_empty() {
                (vec![format], samples, true)
            } else {
                let depth = samples == 1
                    && slots.iter().all(|slot| slot.2 == (picture.width(), picture.height()));
                (slots.iter().map(|slot| slot.1).collect::<Vec<_>>(), 1, depth)
            };

            // An integer target cannot be blended, which wgpu refuses as a pipeline rather than
            // ignores, so a draw into one replaces what is there however it was asked to blend.
            let integer = |format: &TextureFormat| {
                format.sample_type(None, None).is_some_and(|sample| {
                    matches!(sample, TextureSampleType::Uint | TextureSampleType::Sint)
                })
            };

            let blend = if formats.iter().any(integer) { DrawBlend::Opaque } else { draw.blend };

            let key = (
                draw.program,
                program.generation,
                formats.clone(),
                samples,
                blend,
                draw.depth_write && has_depth,
                has_depth,
            );

            let add = BlendComponent {
                src_factor: BlendFactor::One,
                dst_factor: BlendFactor::One,
                operation: BlendOperation::Add,
            };

            let targets: Vec<_> = formats
                .iter()
                .map(|format| {
                    Some(ColorTargetState {
                        format: *format,
                        blend: match blend {
                            DrawBlend::Opaque => None,
                            DrawBlend::Alpha => Some(BlendState::ALPHA_BLENDING),
                            DrawBlend::Add => Some(BlendState { color: add, alpha: add }),
                        },
                        write_mask: ColorWrites::ALL,
                    })
                })
                .collect();

            let depth_stencil = has_depth.then(|| DepthStencilState {
                format: bevy::core_pipeline::core_3d::CORE_3D_DEPTH_FORMAT,
                depth_write_enabled: Some(draw.depth_write),
                // Bevy's depth runs backwards, so nearer is greater.
                depth_compare: Some(CompareFunction::GreaterEqual),
                stencil: Default::default(),
                bias: Default::default(),
            });

            let pipeline = match &vertex {
                _ if meshes => {
                    let built = pipelines.meshes.entry(key).or_insert_with(|| {
                        super::mesh_draws::build(
                            &render_device,
                            &cache,
                            &program,
                            &programs::label(draw.program),
                            super::mesh_draws::MeshPipeline {
                                groups: &[draw_layout(&layout, true), inputs.layout.clone()],
                                targets,
                                depth: depth_stencil,
                                samples,
                            },
                        )
                    });

                    match built {
                        Some(pipeline) => DrawPipeline::Own(pipeline.clone()),
                        None => continue,
                    }
                }
                Some(vertex) => DrawPipeline::Cached(*pipelines.cached.entry(key).or_insert_with(|| {
                    cache.queue_render_pipeline(RenderPipelineDescriptor {
                        label: Some("bcs_view_draw".into()),
                        layout: vec![draw_layout(&layout, false), inputs.layout.clone()],
                        vertex: VertexState {
                            shader: vertex.shader.clone(),
                            shader_defs: Vec::new(),
                            entry_point: None,
                            buffers: Vec::new(),
                            constants: Vec::new(),
                        },
                        fragment: Some(FragmentState {
                            shader: fragment.shader,
                            shader_defs: Vec::new(),
                            entry_point: None,
                            targets,
                            constants: Vec::new(),
                        }),
                        depth_stencil,
                        multisample: MultisampleState {
                            count: samples,
                            ..Default::default()
                        },
                        ..Default::default()
                    })
                })),
                None => continue,
            };

            // Depth alone into a shadow map, from the vertex stage or from the program's draw
            // shadow stage where it writes its own, keyed apart from every draw into a picture by
            // having no color targets. Unclipped where the adapter allows, since a directional
            // light's cascades are boxes and a caster in front of one still shadows what is inside
            // it, as Bevy's own shadow pipelines do.
            let shadow_fragment = program.stages[Role::DrawShadow as usize].clone();
            let shadow_pipeline = vertex.filter(|_| draw.casts_shadows).map(|vertex| {
                let key = (draw.program, program.generation, Vec::new(), 1, DrawBlend::Opaque, true, true);
                let unclipped = render_device
                    .features()
                    .contains(bevy::render::settings::WgpuFeatures::DEPTH_CLIP_CONTROL);

                *pipelines.cached.entry(key).or_insert_with(|| {
                    cache.queue_render_pipeline(RenderPipelineDescriptor {
                        label: Some("bcs_view_draw_shadow".into()),
                        layout: vec![draw_layout(&layout, false), inputs.layout.clone()],
                        vertex: VertexState {
                            shader: vertex.shader,
                            shader_defs: Vec::new(),
                            entry_point: None,
                            buffers: Vec::new(),
                            constants: Vec::new(),
                        },
                        fragment: shadow_fragment.map(|stage| FragmentState {
                            shader: stage.shader,
                            shader_defs: Vec::new(),
                            entry_point: None,
                            targets: Vec::new(),
                            constants: Vec::new(),
                        }),
                        primitive: bevy::render::render_resource::PrimitiveState {
                            unclipped_depth: unclipped,
                            ..Default::default()
                        },
                        depth_stencil: Some(DepthStencilState {
                            format: bevy::core_pipeline::core_3d::CORE_3D_DEPTH_FORMAT,
                            depth_write_enabled: Some(true),
                            depth_compare: Some(CompareFunction::GreaterEqual),
                            stencil: Default::default(),
                            bias: Default::default(),
                        }),
                        ..Default::default()
                    })
                })
            });

            let context = PackContext {
                device: &render_device,
                images: &images,
                buffers: &buffers,
                fallback: &fallback,
                stand: &stand,
                view: names.as_deref(),
                constants: false,
            };

            let packed = match pack(&layout, &draw.values, &context) {
                Ok(packed) => packed,
                Err(PackError::NotReady) => continue,
                Err(PackError::Missing(message)) => {
                    say_once(format!(
                        "A draw on a camera of shader program {}: {message}",
                        draw.program
                    ));
                    continue;
                }
            };

            for problem in &packed.problems {
                say_once(format!(
                    "A draw on a camera of shader program {}: {problem}",
                    draw.program
                ));
            }

            let count = match (&draw.count, meshes) {
                (DrawCount::Fixed { vertices, instances }, false) => {
                    PreparedDrawCount::Direct(*vertices, *instances)
                }
                (DrawCount::Groups { x, y, z }, true) => PreparedDrawCount::Groups(*x, *y, *z),
                (DrawCount::Indirect { buffer, offset }, meshes) => match buffers.get(buffer) {
                    Some(gpu) if meshes => PreparedDrawCount::GroupsIndirect(gpu.buffer.clone(), *offset),
                    Some(gpu) => PreparedDrawCount::Indirect(gpu.buffer.clone(), *offset),
                    None => continue,
                },
                (_, meshes) => {
                    say_once(format!(
                        "A draw on a camera of shader program {} is counted in {}, and the program \
                         draws with {}, so it draws nothing. ViewDraw.{} counts it as it draws.",
                        draw.program,
                        if meshes { "vertices" } else { "workgroups" },
                        if meshes { "mesh shaders, which run workgroups" } else { "a vertex shader, which runs on vertices" },
                        if meshes { "Meshes" } else { "Fixed" },
                    ));
                    continue;
                }
            };

            prepared.push(PreparedViewDraw {
                point: draw.point,
                pipeline,
                own: packed.bind_group(
                    &render_device,
                    "bcs_view_draw_own",
                    &cache.get_bind_group_layout(&draw_layout(&layout, meshes)),
                ),
                count,
                target: (!slots.is_empty()).then(|| (slots.iter().map(|slot| slot.0.clone()).collect(), has_depth)),
                shadow_pipeline,
                writes_depth: draw.depth_write && has_depth,
                writes_prepass: draw.targets.iter().fold(0, |bits, name| match name.as_str() {
                    "normals" => bits | 1,
                    "motion" => bits | 2,
                    _ => bits,
                }),
                label: programs::label(draw.program),
            });
        }

        commands.entity(entity).insert(PreparedViewDraws(prepared));
    }
}

/// Drops what a view kept for draws its camera no longer has.
pub(super) fn forget_view_draws(
    mut commands: Commands,
    views: Query<Entity, (With<PreparedViewDraws>, Without<BcsViewDraws>)>,
) {
    for entity in &views {
        commands.entity(entity).remove::<PreparedViewDraws>();
    }
}

/// Runs a view's draws for one point of its frame, into its picture and against its depth.
#[allow(clippy::too_many_arguments)]
pub(super) fn run_view_draws<const POINT: u8>(
    view: ViewQuery<(
        &ViewTarget,
        &ViewUniformOffset,
        &PreparedViewDraws,
        &bevy::render::view::ViewDepthStencilTexture,
        Option<&ViewPrepassTextures>,
        Option<&PreviousViewUniformOffset>,
        Option<&ViewLightsUniformOffset>,
        Option<&ViewShadowBindings>,
        Option<&ViewEnvironmentTextures>,
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
    use bevy::render::render_resource::{RenderPassDescriptor, StoreOp};

    let (target, offset, prepared, depth, prepass, previous, light_offset, shadows, environment) =
        view.into_inner();

    if !prepared.0.iter().any(|draw| draw.point as u8 == POINT) {
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

    // The draw writes into the picture, so it cannot be read in the same pass, and a stand-in is
    // bound where it would be. The same goes for a prepass input a draw writes, which gets a group
    // of its own with that input stood in for.
    let lights = ViewLights {
        offset: light_offset,
        shadows,
    };

    let bind = |ctx: &RenderContext, writes_prepass: u8| {
        let mut sources = ViewInputSources {
            depth: sources.depth.clone(),
            normals: sources.normals.clone(),
            motion: sources.motion.clone(),
            previous: sources.previous,
            environment: sources.environment,
        };

        if writes_prepass & 1 != 0 {
            sources.normals = fallback.d2.texture_view.clone();
        }

        if writes_prepass & 2 != 0 {
            sources.motion = inputs.empty_motion.clone();
        }

        sources.bind(
            ctx.render_device(),
            &cache,
            &inputs,
            &fallback.d2.texture_view,
            globals.clone(),
            view_binding.clone(),
            offset.offset,
            &scene,
            &lights,
        )
    };

    let Some((group, offsets)) = bind(&ctx, 0) else {
        return;
    };

    let here: Vec<&PreparedViewDraw> = prepared.0.iter().filter(|draw| draw.point as u8 == POINT).collect();
    let same_target = |a: &PreparedViewDraw, b: &PreparedViewDraw| match (&a.target, &b.target) {
        (None, None) => true,
        (Some((a, _)), Some((b, _))) => {
            a.len() == b.len() && a.iter().zip(b).all(|(a, b)| a.id() == b.id())
        }
        _ => false,
    };

    // One render pass for each run of draws into the same target, in the order they were asked
    // for, so a draw into an image between two into the picture keeps its place.
    let mut start = 0;

    while start < here.len() {
        let end = (start..here.len())
            .find(|&index| !same_target(here[start], here[index]))
            .unwrap_or(here.len());

        let (color, depth_attachment): (Vec<_>, _) = match &here[start].target {
            None => (
                vec![Some(target.get_color_attachment())],
                Some(depth.get_attachment(StoreOp::Store)),
            ),
            Some((views, tested)) => (
                views
                    .iter()
                    .map(|view| {
                        Some(bevy::render::render_resource::RenderPassColorAttachment {
                            view,
                            depth_slice: None,
                            resolve_target: None,
                            ops: bevy::render::render_resource::Operations {
                                load: bevy::render::render_resource::LoadOp::Load,
                                store: StoreOp::Store,
                            },
                        })
                    })
                    .collect(),
                tested.then(|| depth.get_attachment(StoreOp::Store)),
            ),
        };

        let written_group = match here[start].writes_prepass {
            0 => None,
            bits => bind(&ctx, bits).map(|(group, _)| group),
        };
        let group = written_group.as_ref().unwrap_or(&group);

        let diagnostics = ctx.diagnostic_recorder();
        let diagnostics = diagnostics.as_deref();

        let mut pass = ctx.command_encoder().begin_render_pass(&RenderPassDescriptor {
            label: Some("bcs_view_draw"),
            color_attachments: &color,
            depth_stencil_attachment: depth_attachment,
            timestamp_writes: None,
            occlusion_query_set: None,
            multiview_mask: None,
        });

        // One span for the run, named after its first draw, since draws sharing a pass share its
        // timing.
        let span = diagnostics.pass_span(&mut pass, here[start].label.clone());

        for draw in &here[start..end] {
            let pipeline = match &draw.pipeline {
                DrawPipeline::Cached(id) => cache.get_render_pipeline(*id),
                DrawPipeline::Own(pipeline) => Some(pipeline),
            };
            let Some(pipeline) = pipeline else {
                continue;
            };

            pass.set_pipeline(pipeline);
            pass.set_bind_group(0, &draw.own, &[]);
            pass.set_bind_group(1, group, &offsets);

            draw.count.draw(&mut pass);
        }

        span.end(&mut pass);
        start = end;
    }
}

/// Copies the camera's depth into its prepass's once the draws inside the prepass are done, where
/// one of them wrote it, as Bevy's prepass copies it once its own geometry is drawn. Everything
/// after the prepass that reads depth reads the copy, Bevy's deferred lighting finding where each
/// pixel is in the world among them, so without it a draw there would be lit at the depth of
/// whatever was behind it.
pub(super) fn copy_prepass_depth(
    view: ViewQuery<(
        &PreparedViewDraws,
        &bevy::render::view::ViewDepthStencilTexture,
        Option<&ViewPrepassTextures>,
    )>,
    mut ctx: RenderContext,
) {
    let (prepared, depth, prepass) = view.into_inner();

    let wrote = prepared.0.iter().any(|draw| draw.point == FramePoint::InPrepass && draw.writes_depth);
    let Some((prepass, copy)) = prepass.and_then(|prepass| Some((prepass, prepass.depth.as_ref()?))) else {
        return;
    };

    if wrote {
        ctx.command_encoder().copy_texture_to_texture(
            depth.texture().as_image_copy(),
            copy.texture.texture.as_image_copy(),
            prepass.size,
        );
    }
}
