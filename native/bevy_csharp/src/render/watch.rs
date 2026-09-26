//! Watching what a camera's shaders keep: a copy of one of its images, made visible, written into
//! an ordinary image every frame so an interface can show it.
//!
//! A screen-space technique is a chain of images nobody looks at, and when the result is wrong the
//! question is which link broke. The images a camera owns live only in the render world, in formats
//! a picture cannot show (one channel of floats, integers, depth), so a watch draws one of them into
//! an eight-bit image asset, scaled and offset into view, after the camera's frame is done. The
//! editor shows that image, and so can a game's own debug overlay.
//!
//! Anything a shader on the camera can read by name can be watched: the camera's own images, their
//! `_previous` and `_mip` names, `ambient_occlusion` where Bevy's is on, and the prepass's `depth`,
//! `normals` and `motion`. A single channel shows as gray, two as red and green, and more as color.

#![cfg(feature = "render")]

use std::collections::HashMap;

use bevy::app::App;
use bevy::asset::{Assets, Handle};
use bevy::camera::Camera;
use bevy::core_pipeline::prepass::ViewPrepassTextures;
use bevy::core_pipeline::{Core2d, Core2dSystems, Core3d, Core3dSystems};
use bevy::ecs::component::Component;
use bevy::ecs::query::With;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Res};
use bevy::image::Image;
use bevy::pbr::ScreenSpaceAmbientOcclusionResources;
use bevy::render::extract_component::{ExtractComponent, ExtractComponentPlugin};
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    BindGroupEntry, BindGroupLayoutDescriptor, BindGroupLayoutEntry, BindingResource, BindingType,
    BufferBindingType, BufferInitDescriptor, BufferUsages, CachedRenderPipelineId,
    ColorTargetState, ColorWrites, FragmentState, Operations, PipelineCache,
    RenderPassColorAttachment, RenderPassDescriptor, RenderPipelineDescriptor, ShaderStages,
    TextureFormat, TextureSampleType, TextureView, TextureViewDimension, VertexState,
};
use bevy::render::renderer::{RenderContext, ViewQuery};
use bevy::render::texture::GpuImage;
use bevy::render::{RenderApp, RenderStartup};
use bevy::shader::Shader;

use super::views::{ViewImageTextures, view_names};

/// The format of the image a watch writes into, which any interface can draw.
pub const WATCH_FORMAT: TextureFormat = TextureFormat::Rgba8Unorm;

/// One image of a camera being watched.
#[derive(Clone, Debug)]
pub struct Watch {
    pub name: String,
    pub image: Handle<Image>,
    /// What each value is multiplied by before it is shown, then what is added.
    pub scale: f32,
    pub offset: f32,
}

/// The images of a camera being watched.
#[derive(Component, Clone, ExtractComponent, Default)]
#[extract_component_filter(With<Camera>)]
pub struct BcsWatches(pub Vec<Watch>);

/// How a watched texture is read, which is what decides the shader and its binding.
#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
enum Reading {
    Float,
    Unsigned,
    Signed,
    Depth,
}

impl Reading {
    fn of(format: TextureFormat) -> Option<Self> {
        Some(match format.sample_type(None, None)? {
            TextureSampleType::Float { .. } => Reading::Float,
            TextureSampleType::Uint => Reading::Unsigned,
            TextureSampleType::Sint => Reading::Signed,
            TextureSampleType::Depth => Reading::Depth,
        })
    }

    fn sample_type(self) -> TextureSampleType {
        match self {
            Reading::Float => TextureSampleType::Float { filterable: false },
            Reading::Unsigned => TextureSampleType::Uint,
            Reading::Signed => TextureSampleType::Sint,
            Reading::Depth => TextureSampleType::Depth,
        }
    }

    /// The shader that draws a texture read this way.
    fn source(self) -> String {
        let (texture, load) = match self {
            Reading::Float => ("texture_2d<f32>", "vec4<f32>(textureLoad(source, texel, 0))"),
            Reading::Unsigned => ("texture_2d<u32>", "vec4<f32>(textureLoad(source, texel, 0))"),
            Reading::Signed => ("texture_2d<i32>", "vec4<f32>(textureLoad(source, texel, 0))"),
            Reading::Depth => ("texture_depth_2d", "vec4<f32>(textureLoad(source, texel, 0))"),
        };

        format!(
            r#"struct Params {{
    scale: f32,
    offset: f32,
    channels: u32,
    unused: u32,
    picture_size: vec2<f32>,
    unused_b: vec2<f32>,
}};

@group(0) @binding(0) var source: {texture};
@group(0) @binding(1) var<uniform> params: Params;

@vertex
fn vertex(@builtin(vertex_index) index: u32) -> @builtin(position) vec4<f32> {{
    let corner = vec2<f32>(f32((index << 1u) & 2u), f32(index & 2u));
    return vec4<f32>(corner * 2.0 - 1.0, 0.0, 1.0);
}}

@fragment
fn fragment(@builtin(position) position: vec4<f32>) -> @location(0) vec4<f32> {{
    let size = vec2<f32>(textureDimensions(source));
    let texel = vec2<i32>(min(position.xy / params.picture_size * size, size - 1.0));
    let value = {load} * params.scale + params.offset;

    if (params.channels == 1u) {{
        return vec4<f32>(value.rrr, 1.0);
    }}

    if (params.channels == 2u) {{
        return vec4<f32>(value.rg, 0.0, 1.0);
    }}

    return vec4<f32>(value.rgb, 1.0);
}}
"#
        )
    }
}

/// The pipelines watches draw with, one for each way of reading.
#[derive(Resource)]
struct WatchPipelines {
    pipelines: HashMap<Reading, (BindGroupLayoutDescriptor, CachedRenderPipelineId)>,
}

/// The shaders, made in the main world where shader assets live.
#[derive(Resource, Clone)]
struct WatchShaders(HashMap<Reading, Handle<Shader>>);

fn init_pipelines(mut commands: Commands, shaders: Res<WatchShaders>, cache: Res<PipelineCache>) {
    let mut pipelines = HashMap::new();

    for (reading, shader) in &shaders.0 {
        let layout = BindGroupLayoutDescriptor::new(
            "bcs_watch",
            &[
                BindGroupLayoutEntry {
                    binding: 0,
                    visibility: ShaderStages::FRAGMENT,
                    ty: BindingType::Texture {
                        sample_type: reading.sample_type(),
                        view_dimension: TextureViewDimension::D2,
                        multisampled: false,
                    },
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 1,
                    visibility: ShaderStages::FRAGMENT,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Uniform,
                        has_dynamic_offset: false,
                        min_binding_size: std::num::NonZeroU64::new(32),
                    },
                    count: None,
                },
            ],
        );

        let pipeline = cache.queue_render_pipeline(RenderPipelineDescriptor {
            label: Some("bcs_watch".into()),
            layout: vec![layout.clone()],
            vertex: VertexState {
                shader: shader.clone(),
                shader_defs: Vec::new(),
                entry_point: None,
                buffers: Vec::new(),
            },
            fragment: Some(FragmentState {
                shader: shader.clone(),
                shader_defs: Vec::new(),
                entry_point: None,
                targets: vec![Some(ColorTargetState {
                    format: WATCH_FORMAT,
                    blend: None,
                    write_mask: ColorWrites::ALL,
                })],
            }),
            ..Default::default()
        });

        pipelines.insert(*reading, (layout, pipeline));
    }

    commands.insert_resource(WatchPipelines { pipelines });
}

/// Draws every watched image of a view into its watch, once the view's frame is done.
#[allow(clippy::too_many_arguments)]
fn draw_watches(
    view: ViewQuery<(
        &BcsWatches,
        Option<&ViewImageTextures>,
        Option<&ScreenSpaceAmbientOcclusionResources>,
        Option<&ViewPrepassTextures>,
    )>,
    pipelines: Option<Res<WatchPipelines>>,
    cache: Res<PipelineCache>,
    images: Res<RenderAssets<GpuImage>>,
    mut ctx: RenderContext,
) {
    let Some(pipelines) = pipelines else {
        return;
    };

    let (watches, owned, occlusion, prepass) = view.into_inner();
    let names = view_names(owned, occlusion);

    let single = |texture: &bevy::render::render_resource::Texture| texture.sample_count() == 1;

    for watch in &watches.0 {
        // The camera's own names first, then the prepass's, which only a watch reads by name.
        let found: Option<(TextureView, TextureFormat)> = names
            .as_deref()
            .and_then(|names| names.get(&watch.name))
            .map(|texture| (texture.level.clone(), texture.format))
            .or_else(|| {
                let prepass = prepass?;
                let attachment = match watch.name.as_str() {
                    "depth" => prepass.depth.as_ref(),
                    "normals" => prepass.normal.as_ref(),
                    "motion" => prepass.motion_vectors.as_ref(),
                    _ => None,
                }?;

                single(&attachment.texture.texture).then(|| {
                    (
                        attachment.texture.default_view.clone(),
                        attachment.texture.texture.format(),
                    )
                })
            });

        let (Some((source, format)), Some(target)) = (found, images.get(&watch.image)) else {
            continue;
        };

        let Some(reading) = Reading::of(format) else {
            continue;
        };

        let Some((layout, pipeline)) = pipelines.pipelines.get(&reading) else {
            continue;
        };

        let Some(pipeline) = cache.get_render_pipeline(*pipeline) else {
            continue;
        };

        let size = target.texture.size();
        let params: [u32; 8] = [
            watch.scale.to_bits(),
            watch.offset.to_bits(),
            u32::from(format.components()),
            0,
            (size.width as f32).to_bits(),
            (size.height as f32).to_bits(),
            0,
            0,
        ];

        let buffer = ctx
            .render_device()
            .create_buffer_with_data(&BufferInitDescriptor {
                label: Some("bcs_watch_params"),
                contents: bytemuck::cast_slice(&params),
                usage: BufferUsages::UNIFORM,
            });

        let group = ctx.render_device().create_bind_group(
            "bcs_watch",
            &cache.get_bind_group_layout(layout),
            &[
                BindGroupEntry {
                    binding: 0,
                    resource: BindingResource::TextureView(&source),
                },
                BindGroupEntry {
                    binding: 1,
                    resource: buffer.as_entire_binding(),
                },
            ],
        );

        let mut pass = ctx
            .command_encoder()
            .begin_render_pass(&RenderPassDescriptor {
                label: Some("bcs_watch"),
                color_attachments: &[Some(RenderPassColorAttachment {
                    view: &target.texture_view,
                    depth_slice: None,
                    resolve_target: None,
                    ops: Operations::default(),
                })],
                depth_stencil_attachment: None,
                timestamp_writes: None,
                occlusion_query_set: None,
                multiview_mask: None,
            });

        pass.set_pipeline(pipeline);
        pass.set_bind_group(0, &group, &[]);
        pass.draw(0..3, 0..1);
    }
}

/// Adds what draws watched images.
pub fn install(app: &mut App) {
    app.add_plugins(ExtractComponentPlugin::<BcsWatches>::default());

    let Some(mut shaders) = app.world_mut().get_resource_mut::<Assets<Shader>>() else {
        return;
    };

    let made: HashMap<Reading, Handle<Shader>> = [
        Reading::Float,
        Reading::Unsigned,
        Reading::Signed,
        Reading::Depth,
    ]
    .into_iter()
    .map(|reading| {
        (
            reading,
            shaders.add(Shader::from_wgsl(
                reading.source(),
                format!("bcs_watch_{reading:?}.wgsl"),
            )),
        )
    })
    .collect();

    // Kept in the main world as well, so the shader assets live as long as the app.
    app.insert_resource(WatchShaders(made.clone()));

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app
        .insert_resource(WatchShaders(made))
        .add_systems(RenderStartup, init_pipelines)
        .add_systems(
            Core3d,
            draw_watches
                .after(super::passes::AfterTonemappingPasses)
                .in_set(Core3dSystems::PostProcess),
        )
        .add_systems(
            Core2d,
            draw_watches
                .after(super::passes::AfterTonemappingPasses)
                .in_set(Core2dSystems::PostProcess),
        );
}

/// Makes the image a watch draws into, and puts the watch on a camera, replacing one of the same
/// name. Answers the image's asset key.
pub fn watch(
    world: &mut bevy::ecs::world::World,
    camera: bevy::ecs::entity::Entity,
    name: String,
    width: u32,
    height: u32,
    scale: f32,
    offset: f32,
) -> i32 {
    use bevy::render::render_resource::TextureUsages;

    let mut image = super::assets::target_image(width, height);
    image.texture_descriptor.format = WATCH_FORMAT;
    image.texture_descriptor.usage =
        TextureUsages::TEXTURE_BINDING | TextureUsages::RENDER_ATTACHMENT | TextureUsages::COPY_SRC;

    let Some(mut images) = world.get_resource_mut::<Assets<Image>>() else {
        return crate::interop::status::UNSUPPORTED;
    };

    let handle = images.add(image);
    let key = crate::assets::insert_handle(world, handle.clone().untyped());

    let mut entity = world.entity_mut(camera);
    let mut watches = entity.get::<BcsWatches>().cloned().unwrap_or_default();
    watches.0.retain(|watch| watch.name != name);
    watches.0.push(Watch {
        name,
        image: handle,
        scale,
        offset,
    });
    entity.insert(watches);

    key
}

/// Stops watching an image of a camera.
pub fn unwatch(world: &mut bevy::ecs::world::World, camera: bevy::ecs::entity::Entity, name: &str) {
    let mut entity = world.entity_mut(camera);

    if let Some(mut watches) = entity.get::<BcsWatches>().cloned() {
        watches.0.retain(|watch| watch.name != name);

        if watches.0.is_empty() {
            entity.remove::<BcsWatches>();
        } else {
            entity.insert(watches);
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    /// Every way of reading makes a shader naga accepts, which is what a watch would otherwise
    /// only find out when a camera first shows one.
    #[test]
    fn every_watch_shader_is_valid() {
        use naga::valid::{Capabilities, ValidationFlags, Validator};

        for reading in [Reading::Float, Reading::Unsigned, Reading::Signed, Reading::Depth] {
            let module = naga::front::wgsl::parse_str(&reading.source())
                .unwrap_or_else(|error| panic!("{reading:?}: {error}"));

            Validator::new(ValidationFlags::all(), Capabilities::all())
                .validate(&module)
                .unwrap_or_else(|error| panic!("{reading:?}: {error:?}"));
        }
    }
}
