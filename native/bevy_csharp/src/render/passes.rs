//! Shader passes: full-screen fragment shaders the game wrote, run over what a camera drew.
//!
//! A pass is a program (see [`super::programs`]) whose fragment shader is run once per pixel of
//! the camera's picture, reading the picture so far and writing the next one. A camera takes any
//! number of them, run in order, either before tonemapping, where the picture is still linear and
//! may be brighter than white, or after it, where it is what the screen will show. Every pass is
//! compiled and reloaded the same way a material's shaders are, so a Slang or WGSL file edited
//! while the game runs reaches the screen the same way.
//!
//! **What a pass reads.** Group zero, laid out for every pass alike:
//!
//! | binding | holds |
//! |---|---|
//! | 0, 1 | the picture so far, and a linear sampler clamped at its edges |
//! | 2 | sixty-four floats, as sixteen `vec4` in a uniform |
//! | 3 | a read-only storage buffer of any size: the pass's own bytes, or a shared buffer |
//! | 4 | Bevy's globals, which is where time is |
//! | 5 | Bevy's view uniform, with the camera's matrices and viewport |
//! | 6 to 13 | four textures, each followed by its sampler |
//! | 14 | the camera's depth, as a depth texture |
//! | 15 | the camera's normals, as a texture of floats |
//!
//! Depth and normals come from the prepass, which a camera draws only when asked, through
//! `bcs_render_set_prepass`. A camera that draws neither, or draws them multisampled, which a
//! plain texture binding cannot take, binds a stand-in: depth zero, which is the far plane in
//! Bevy's reversed depth, and white normals.
//!
//! The vertex shader is Bevy's full-screen triangle, whose output is the position and a `uv` at
//! location zero, running from the top left.

#![cfg(feature = "render")]

use std::collections::HashMap;
use std::num::NonZeroU64;
use std::sync::Arc;

use bevy::app::App;
use bevy::asset::Handle;
use bevy::camera::Camera;
use bevy::core_pipeline::prepass::ViewPrepassTextures;
use bevy::core_pipeline::tonemapping::tonemapping;
use bevy::core_pipeline::{Core2d, Core2dSystems, Core3d, Core3dSystems, FullscreenShader};
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::{With, Without};
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query, Res, ResMut};
use bevy::image::Image;
use bevy::render::extract_component::{ExtractComponent, ExtractComponentPlugin};
use bevy::render::globals::{GlobalsBuffer, GlobalsUniform};
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    BindGroupEntry, BindGroupLayoutDescriptor, BindGroupLayoutEntry, BindingResource,
    BindingType, Buffer, BufferBindingType, BufferInitDescriptor, BufferUsages,
    CachedRenderPipelineId, ColorTargetState, ColorWrites, FilterMode, FragmentState, Operations,
    PipelineCache, RenderPassColorAttachment, RenderPassDescriptor, RenderPipelineDescriptor,
    Sampler, SamplerBindingType, SamplerDescriptor, ShaderStages, ShaderType, TextureFormat,
    TextureSampleType, TextureView, TextureViewDimension,
};
use bevy::render::renderer::{RenderContext, RenderDevice, RenderQueue, ViewQuery};
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::view::{ViewTarget, ViewUniform, ViewUniformOffset, ViewUniforms};
use bevy::render::{Render, RenderApp, RenderStartup, RenderSystems};

use super::material::PARAMETER_COUNT;
use super::programs::{self, Role};

/// How many textures a pass carries besides the picture.
pub const PASS_TEXTURE_COUNT: usize = 4;

/// One pass as the camera holds it.
#[derive(Clone)]
pub struct ShaderPass {
    pub program: u32,
    pub parameters: [f32; PARAMETER_COUNT],
    /// Shared, and replaced rather than changed, so the render side can tell new data from old
    /// by the pointer rather than by comparing the bytes.
    pub data: Arc<[u8]>,
    /// A buffer bound at the data's binding in place of the data.
    pub buffer: Option<Handle<bevy::render::storage::ShaderBuffer>>,
    pub textures: [Option<Handle<Image>>; PASS_TEXTURE_COUNT],
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
    layout: BindGroupLayoutDescriptor,
    sampler: Sampler,
    fullscreen: FullscreenShader,
    /// Bound at the depth binding when the camera has no depth to give.
    empty_depth: TextureView,
    /// One pipeline per program and picture format, because a pipeline names the format it
    /// writes and a camera drawing in HDR writes a different one before tonemapping than after.
    pipelines: HashMap<(u32, TextureFormat), CachedRenderPipelineId>,
}

/// A pass as the render side keeps it between frames.
struct PreparedPass {
    pipeline: Option<CachedRenderPipelineId>,
    parameters: Buffer,
    data: Buffer,
    /// Where the data came from, so it is uploaded again only when it is replaced.
    data_source: (usize, usize),
    textures: [(TextureView, Sampler); PASS_TEXTURE_COUNT],
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
            (prepare_passes, forget_passes).in_set(RenderSystems::PrepareResources),
        )
        .add_systems(
            Core3d,
            (
                run_passes::<false>
                    .before(tonemapping)
                    .in_set(Core3dSystems::PostProcess),
                run_passes::<true>
                    .after(tonemapping)
                    .in_set(Core3dSystems::PostProcess),
            ),
        )
        .add_systems(
            Core2d,
            (
                run_passes::<false>
                    .before(tonemapping)
                    .in_set(Core2dSystems::PostProcess),
                run_passes::<true>
                    .after(tonemapping)
                    .in_set(Core2dSystems::PostProcess),
            ),
        );
}

fn init_pipelines(
    mut commands: Commands,
    render_device: Res<RenderDevice>,
    fullscreen: Res<FullscreenShader>,
) {
    let entry = |binding: u32, ty: BindingType| BindGroupLayoutEntry {
        binding,
        visibility: ShaderStages::FRAGMENT,
        ty,
        count: None,
    };

    let texture = BindingType::Texture {
        sample_type: TextureSampleType::Float { filterable: true },
        view_dimension: TextureViewDimension::D2,
        multisampled: false,
    };

    let sampler = BindingType::Sampler(SamplerBindingType::Filtering);

    let mut entries = vec![
        entry(0, texture),
        entry(1, sampler),
        entry(
            2,
            BindingType::Buffer {
                ty: BufferBindingType::Uniform,
                has_dynamic_offset: false,
                min_binding_size: NonZeroU64::new((PARAMETER_COUNT * 4) as u64),
            },
        ),
        entry(
            3,
            BindingType::Buffer {
                ty: BufferBindingType::Storage { read_only: true },
                has_dynamic_offset: false,
                min_binding_size: None,
            },
        ),
        entry(
            4,
            BindingType::Buffer {
                ty: BufferBindingType::Uniform,
                has_dynamic_offset: false,
                min_binding_size: Some(GlobalsUniform::min_size()),
            },
        ),
        entry(
            5,
            BindingType::Buffer {
                ty: BufferBindingType::Uniform,
                has_dynamic_offset: true,
                min_binding_size: Some(ViewUniform::min_size()),
            },
        ),
    ];

    for index in 0..PASS_TEXTURE_COUNT as u32 {
        entries.push(entry(6 + index * 2, texture));
        entries.push(entry(7 + index * 2, sampler));
    }

    entries.push(entry(
        14,
        BindingType::Texture {
            sample_type: TextureSampleType::Depth,
            view_dimension: TextureViewDimension::D2,
            multisampled: false,
        },
    ));
    entries.push(entry(15, texture));

    let empty_depth = render_device
        .create_texture(&bevy::render::render_resource::TextureDescriptor {
            label: Some("bcs_shader_pass_empty_depth"),
            size: bevy::render::render_resource::Extent3d {
                width: 1,
                height: 1,
                depth_or_array_layers: 1,
            },
            mip_level_count: 1,
            sample_count: 1,
            dimension: bevy::render::render_resource::TextureDimension::D2,
            format: TextureFormat::Depth32Float,
            usage: bevy::render::render_resource::TextureUsages::TEXTURE_BINDING,
            view_formats: &[],
        })
        .create_view(&bevy::render::render_resource::TextureViewDescriptor::default());

    commands.insert_resource(ShaderPassPipelines {
        layout: BindGroupLayoutDescriptor::new("bcs_shader_pass_layout", &entries),
        sampler: render_device.create_sampler(&SamplerDescriptor {
            label: Some("bcs_shader_pass_sampler"),
            mag_filter: FilterMode::Linear,
            min_filter: FilterMode::Linear,
            ..Default::default()
        }),
        fullscreen: fullscreen.clone(),
        empty_depth,
        pipelines: HashMap::new(),
    });
}

/// The pipeline for a program writing a picture of `format`, queued the first time it is asked
/// for.
fn pipeline_for(
    pipelines: &mut ShaderPassPipelines,
    cache: &PipelineCache,
    program: u32,
    format: TextureFormat,
) -> Option<CachedRenderPipelineId> {
    if let Some(id) = pipelines.pipelines.get(&(program, format)) {
        return Some(*id);
    }

    let found = programs::lookup(program)?;
    let stage = found.stages[Role::Fragment as usize].clone()?;

    let id = cache.queue_render_pipeline(RenderPipelineDescriptor {
        label: Some("bcs_shader_pass".into()),
        layout: vec![pipelines.layout.clone()],
        vertex: pipelines.fullscreen.to_vertex_state(),
        fragment: Some(FragmentState {
            shader: stage.shader,
            shader_defs: found.defs.clone(),
            entry_point: Some(stage.entry),
            targets: vec![Some(ColorTargetState {
                format,
                blend: None,
                write_mask: ColorWrites::ALL,
            })],
        }),
        ..Default::default()
    });

    pipelines.pipelines.insert((program, format), id);
    Some(id)
}

/// The texture and sampler for one of a pass's slots, or the fallback's.
///
/// `None` where the image is still on its way, which holds the pass back for a frame rather than
/// running it once with a white square where a picture belongs.
fn pass_texture(
    images: &RenderAssets<GpuImage>,
    fallback: &FallbackImage,
    handle: &Option<Handle<Image>>,
) -> Option<(TextureView, Sampler)> {
    let Some(handle) = handle else {
        return Some((fallback.d2.texture_view.clone(), fallback.d2.sampler.clone()));
    };

    let image = images.get(handle)?;

    let flat = image.texture_descriptor.size.depth_or_array_layers == 1
        && image.texture_descriptor.dimension == bevy::render::render_resource::TextureDimension::D2
        && image.texture_descriptor.format.sample_type(None, None)
            == Some(TextureSampleType::Float { filterable: true });

    if !flat {
        bevy::log::warn_once!(
            "A shader pass was given a texture that is not a flat picture that can be filtered, \
             so the slot holds the fallback instead."
        );
        return Some((fallback.d2.texture_view.clone(), fallback.d2.sampler.clone()));
    }

    Some((image.texture_view.clone(), image.sampler.clone()))
}

/// Makes each view's buffers and pipelines match what its camera asked for.
fn prepare_passes(
    mut commands: Commands,
    mut pipelines: ResMut<ShaderPassPipelines>,
    cache: Res<PipelineCache>,
    render_device: Res<RenderDevice>,
    render_queue: Res<RenderQueue>,
    images: Res<RenderAssets<GpuImage>>,
    fallback: Res<FallbackImage>,
    buffers: Res<RenderAssets<bevy::render::storage::GpuShaderBuffer>>,
    views: Query<(
        Entity,
        &ViewTarget,
        &BcsShaderPasses,
        Option<&PreparedShaderPasses>,
    )>,
) {
    for (entity, target, asked, prepared) in &views {
        let format = target.main_texture_format();

        // The buffers from the frame before are kept where they still fit. The numbers are
        // written into the old uniform, which is every frame, and the data is uploaded again only
        // when it was replaced, since it may be large.
        let mut built = Vec::with_capacity(asked.passes.len());

        for (index, pass) in asked.passes.iter().enumerate() {
            let pipeline = pipeline_for(&mut pipelines, &cache, pass.program, format);

            let mut textures = Vec::with_capacity(PASS_TEXTURE_COUNT);
            for handle in &pass.textures {
                match pass_texture(&images, &fallback, handle) {
                    Some(bound) => textures.push(bound),
                    None => break,
                }
            }

            let Ok(textures) = <[(TextureView, Sampler); PASS_TEXTURE_COUNT]>::try_from(textures)
            else {
                // An image has not arrived. The pass is left out of this frame rather than run
                // without it, and the ones after it still run.
                continue;
            };

            let parameters: &[u8] = bytemuck::cast_slice(&pass.parameters);
            let source = (pass.data.as_ptr() as usize, pass.data.len());

            let reused = prepared.and_then(|prepared| prepared.0.get(index));

            let parameters_buffer = match &reused {
                Some(old) => {
                    render_queue.write_buffer(&old.parameters, 0, parameters);
                    old.parameters.clone()
                }
                None => render_device.create_buffer_with_data(&BufferInitDescriptor {
                    label: Some("bcs_shader_pass_parameters"),
                    contents: parameters,
                    usage: BufferUsages::UNIFORM | BufferUsages::COPY_DST,
                }),
            };

            let shared = match &pass.buffer {
                Some(handle) => match buffers.get(handle) {
                    Some(gpu) => Some(gpu.buffer.clone()),
                    // Not on the GPU yet, which holds the pass back the way a picture does.
                    None => continue,
                },
                None => None,
            };

            let data_buffer = match (&reused, shared) {
                (_, Some(buffer)) => buffer,
                (Some(old), None) if old.data_source == source => old.data.clone(),
                _ => {
                    let mut bytes = pass.data.to_vec();
                    bytes.resize(bytes.len().max(16).next_multiple_of(4), 0);

                    render_device.create_buffer_with_data(&BufferInitDescriptor {
                        label: Some("bcs_shader_pass_data"),
                        contents: &bytes,
                        usage: BufferUsages::STORAGE | BufferUsages::COPY_DST,
                    })
                }
            };

            built.push(PreparedPass {
                pipeline,
                parameters: parameters_buffer,
                data: data_buffer,
                // A shared buffer is never taken for the pass's own bytes on a later frame, which
                // an empty slice's pointer could otherwise be mistaken for.
                data_source: if pass.buffer.is_some() {
                    (0, usize::MAX)
                } else {
                    source
                },
                textures,
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
fn run_passes<const AFTER_TONEMAPPING: bool>(
    view: ViewQuery<(
        &ViewTarget,
        &ViewUniformOffset,
        &PreparedShaderPasses,
        Option<&ViewPrepassTextures>,
    )>,
    pipelines: Res<ShaderPassPipelines>,
    fallback: Res<FallbackImage>,
    cache: Res<PipelineCache>,
    globals: Res<GlobalsBuffer>,
    view_uniforms: Res<ViewUniforms>,
    mut ctx: RenderContext,
) {
    let (target, offset, prepared, prepass) = view.into_inner();

    // Only a texture drawn once a pixel can be bound as a plain texture. A multisampled camera's
    // prepass draws several samples a pixel, and it is left out rather than failing the pipeline.
    let single = |texture: &bevy::render::render_resource::Texture| texture.sample_count() == 1;

    let depth = prepass
        .and_then(|prepass| prepass.depth.as_ref())
        .filter(|depth| single(&depth.texture.texture))
        .map(|depth| depth.texture.default_view.clone())
        .unwrap_or_else(|| pipelines.empty_depth.clone());

    let normals = prepass
        .and_then(|prepass| prepass.normal.as_ref())
        .filter(|normal| single(&normal.texture.texture))
        .map(|normal| normal.texture.default_view.clone())
        .unwrap_or_else(|| fallback.d2.texture_view.clone());

    let (Some(globals), Some(view_binding)) =
        (globals.buffer.binding(), view_uniforms.uniforms.binding())
    else {
        return;
    };

    for pass in &prepared.0 {
        if pass.after_tonemapping != AFTER_TONEMAPPING {
            continue;
        }

        // Still compiling, or failed with nothing to fall back on. Skipped, so the picture goes on
        // as though the pass were not there.
        let Some(pipeline) = pass
            .pipeline
            .and_then(|id| cache.get_render_pipeline(id))
        else {
            continue;
        };

        let post = target.post_process_write();

        let mut entries = vec![
            BindGroupEntry {
                binding: 0,
                resource: BindingResource::TextureView(post.source),
            },
            BindGroupEntry {
                binding: 1,
                resource: BindingResource::Sampler(&pipelines.sampler),
            },
            BindGroupEntry {
                binding: 2,
                resource: pass.parameters.as_entire_binding(),
            },
            BindGroupEntry {
                binding: 3,
                resource: pass.data.as_entire_binding(),
            },
            BindGroupEntry {
                binding: 4,
                resource: globals.clone(),
            },
            BindGroupEntry {
                binding: 5,
                resource: view_binding.clone(),
            },
        ];

        for (index, (texture, sampler)) in pass.textures.iter().enumerate() {
            entries.push(BindGroupEntry {
                binding: 6 + index as u32 * 2,
                resource: BindingResource::TextureView(texture),
            });
            entries.push(BindGroupEntry {
                binding: 7 + index as u32 * 2,
                resource: BindingResource::Sampler(sampler),
            });
        }

        entries.push(BindGroupEntry {
            binding: 14,
            resource: BindingResource::TextureView(&depth),
        });
        entries.push(BindGroupEntry {
            binding: 15,
            resource: BindingResource::TextureView(&normals),
        });

        let bind_group = ctx.render_device().create_bind_group(
            "bcs_shader_pass",
            &cache.get_bind_group_layout(&pipelines.layout),
            &entries,
        );

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
        render_pass.set_bind_group(0, &bind_group, &[offset.offset]);
        render_pass.draw(0..3, 0..1);
    }
}
