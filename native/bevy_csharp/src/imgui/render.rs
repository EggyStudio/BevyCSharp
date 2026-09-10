//! The pass that draws what ImGui asked for.
//!
//! One system in the view's schedule, after everything else the camera does and before the picture
//! reaches the window. It knows nothing about widgets: it uploads this frame's triangles, and for
//! each draw call sets a scissor rectangle and a texture and draws a run of indices. That is the
//! whole of an ImGui backend, and the same shape the reference engine's Vulkan one has.

use bevy::app::App;
use bevy::asset::{AssetId, AssetServer, Handle, RenderAssetUsages, load_embedded_asset};
use bevy::core_pipeline::schedule::{Core2d, Core2dSystems, Core3d, Core3dSystems};
use bevy::core_pipeline::upscaling::upscaling;
use bevy::image::Image;
use bevy::platform::collections::HashMap;
use bevy::prelude::*;
use bevy::mesh::VertexBufferLayout;
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::binding_types::{sampler, texture_2d, uniform_buffer};
use bevy::render::render_resource::*;
use bevy::render::renderer::{RenderContext, RenderDevice, RenderQueue, ViewQuery};
use bevy::render::texture::GpuImage;
use bevy::render::view::ViewTarget;
use bevy::render::{
    Extract, ExtractSchedule, Render, RenderApp, RenderStartup, RenderSystems,
};

use super::{BcsImGuiCommand, BcsImGuiVertex};

/// What the managed side asked to be drawn this frame.
///
/// Replaced wholesale once a frame. Immediate mode means there is no state to keep between frames:
/// what is on screen is whatever the last frame said, and nothing here has to work out what
/// changed.
#[derive(Resource, Default)]
pub struct Drawn {
    /// Every vertex of every window, in one run.
    pub vertices: Vec<BcsImGuiVertex>,

    /// Every index, in one run. Each command says where its own share begins.
    pub indices: Vec<u16>,

    /// What to draw, in the order it is to be drawn.
    pub commands: Vec<BcsImGuiCommand>,

    /// Where the interface's top left is, in logical pixels.
    pub display_position: [f32; 2],

    /// How large the interface is, in logical pixels.
    pub display_size: [f32; 2],

    /// How many physical pixels a logical one is.
    pub framebuffer_scale: [f32; 2],
}

/// The pictures the interface draws with, by the name it knows them under.
///
/// The font atlas is one of these and nothing about it is special: ImGui hands over pixels and asks
/// for a name to put in its draw calls.
#[derive(Resource, Default)]
pub struct Pictures {
    next: u64,
    images: HashMap<u64, Handle<Image>>,
}

impl Pictures {
    /// Remembers a picture and answers what to call it.
    pub fn add(&mut self, image: Handle<Image>) -> u64 {
        // From one rather than from zero, because zero is what a caller passes to mean "no
        // picture" and a name that means both is a name that cannot be checked.
        self.next += 1;

        let name = self.next;
        self.images.insert(name, image);
        name
    }

    /// Forgets one.
    pub fn remove(&mut self, name: u64) {
        self.images.remove(&name);
    }

    /// What a name stands for.
    pub fn get(&self, name: u64) -> Option<&Handle<Image>> {
        self.images.get(&name)
    }
}

/// Builds an image the interface can draw with, from pixels the managed side handed over.
pub fn picture(pixels: Vec<u8>, width: u32, height: u32) -> Image {
    Image::new(
        Extent3d {
            width,
            height,
            depth_or_array_layers: 1,
        },
        TextureDimension::D2,
        pixels,
        // The colours in an atlas are what somebody chose, so they are sRGB, and saying so is what
        // makes sampling give the linear values the shader expects.
        TextureFormat::Rgba8UnormSrgb,
        RenderAssetUsages::RENDER_WORLD,
    )
}

/// This frame's triangles, once they have crossed into the renderer.
#[derive(Resource, Default)]
struct Frame {
    vertices: Vec<BcsImGuiVertex>,
    indices: Vec<u16>,
    calls: Vec<Call>,

    /// Screen space to clip space, built from the display size.
    projection: Mat4,

    /// How many physical pixels a logical one is, which is what a clip rectangle is measured in.
    scale: Vec2,

    /// Whether the pass has already run this frame.
    ///
    /// The view schedule runs once per camera, and an interface drawn once per camera is an
    /// interface drawn twice.
    done: bool,
}

/// One draw call, with its picture resolved to something the renderer holds.
struct Call {
    image: AssetId<Image>,
    clip: [f32; 4],
    index: u32,
    vertex: u32,
    elements: u32,
}

/// What the pass draws with.
#[derive(Resource)]
struct Pipeline {
    view_layout: BindGroupLayoutDescriptor,
    picture_layout: BindGroupLayoutDescriptor,
    shader: Handle<Shader>,
    sampler: Sampler,
}

/// What the pass draws from.
#[derive(Resource)]
struct Buffers {
    vertices: RawBufferVec<BcsImGuiVertex>,
    indices: RawBufferVec<u16>,
    projection: UniformBuffer<Mat4>,
    view: Option<BindGroup>,
    pictures: HashMap<AssetId<Image>, BindGroup>,
}

/// Which pipeline was built for which target.
#[derive(Resource, Default)]
struct Built(HashMap<TextureFormat, CachedRenderPipelineId>);

/// Puts the interface into an app.
pub fn install(app: &mut App) {
    app.init_resource::<Drawn>();
    app.init_resource::<Pictures>();

    bevy::asset::embedded_asset!(app, "imgui.wgsl");

    let Some(renderer) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    renderer
        .init_resource::<Frame>()
        .init_resource::<Built>()
        .add_systems(RenderStartup, start)
        .add_systems(ExtractSchedule, extract)
        .add_systems(Render, prepare.in_set(RenderSystems::PrepareBindGroups))
        // After what the camera does and before the picture is handed to the window, which is
        // where an overlay belongs: over the scene, under nothing.
        .add_systems(
            Core2d,
            draw.after(Core2dSystems::PostProcess).before(upscaling),
        )
        .add_systems(
            Core3d,
            draw.after(Core3dSystems::PostProcess).before(upscaling),
        );
}

/// Builds what the pass needs once the device is there.
fn start(mut commands: Commands, assets: Res<AssetServer>, device: Res<RenderDevice>) {
    let view_layout = BindGroupLayoutDescriptor::new(
        "bcs_imgui_view",
        &BindGroupLayoutEntries::single(ShaderStages::VERTEX, uniform_buffer::<Mat4>(false)),
    );

    let picture_layout = BindGroupLayoutDescriptor::new(
        "bcs_imgui_picture",
        &BindGroupLayoutEntries::sequential(
            ShaderStages::FRAGMENT,
            (
                texture_2d(TextureSampleType::Float { filterable: true }),
                sampler(SamplerBindingType::Filtering),
            ),
        ),
    );

    commands.insert_resource(Pipeline {
        view_layout,
        picture_layout,
        shader: load_embedded_asset!(assets.as_ref(), "imgui.wgsl"),
        sampler: device.create_sampler(&SamplerDescriptor {
            label: Some("bcs_imgui_sampler"),
            address_mode_u: AddressMode::ClampToEdge,
            address_mode_v: AddressMode::ClampToEdge,
            mag_filter: FilterMode::Linear,
            min_filter: FilterMode::Linear,
            ..default()
        }),
    });

    commands.insert_resource(Buffers {
        vertices: RawBufferVec::new(BufferUsages::VERTEX),
        indices: RawBufferVec::new(BufferUsages::INDEX),
        projection: UniformBuffer::default(),
        view: None,
        pictures: HashMap::default(),
    });
}

/// Carries this frame's triangles into the renderer.
fn extract(
    mut frame: ResMut<Frame>,
    drawn: Extract<Res<Drawn>>,
    pictures: Extract<Res<Pictures>>,
) {
    frame.done = false;
    frame.calls.clear();

    frame.vertices.clear();
    frame.vertices.extend_from_slice(&drawn.vertices);

    frame.indices.clear();
    frame.indices.extend_from_slice(&drawn.indices);

    for command in &drawn.commands {
        // A call naming a picture that is not there draws nothing rather than something wrong.
        let Some(image) = pictures.get(command.texture) else {
            continue;
        };

        frame.calls.push(Call {
            image: image.id(),
            clip: command.clip,
            index: command.index,
            vertex: command.vertex,
            elements: command.elements,
        });
    }

    let [width, height] = drawn.display_size;
    let [left, top] = drawn.display_position;

    // Screen space, y downwards, straight to clip space. What ImGui's own backends build.
    frame.projection = Mat4::orthographic_rh(left, left + width, top + height, top, -1.0, 1.0);
    frame.scale = Vec2::new(drawn.framebuffer_scale[0], drawn.framebuffer_scale[1]);
}

/// Puts this frame's triangles and pictures where the GPU can reach them.
fn prepare(
    mut buffers: ResMut<Buffers>,
    frame: Res<Frame>,
    pipeline: Res<Pipeline>,
    cache: Res<PipelineCache>,
    images: Res<RenderAssets<GpuImage>>,
    device: Res<RenderDevice>,
    queue: Res<RenderQueue>,
) {
    if frame.calls.is_empty() {
        return;
    }

    buffers.vertices.clear();
    buffers.vertices.extend(frame.vertices.iter().copied());
    buffers.vertices.write_buffer(&device, &queue);

    buffers.indices.clear();
    buffers.indices.extend(frame.indices.iter().copied());
    buffers.indices.write_buffer(&device, &queue);

    buffers.projection.set(frame.projection);
    buffers.projection.write_buffer(&device, &queue);

    if let Some(binding) = buffers.projection.binding() {
        buffers.view = Some(device.create_bind_group(
            "bcs_imgui_view",
            &cache.get_bind_group_layout(&pipeline.view_layout),
            &BindGroupEntries::single(binding),
        ));
    }

    // A picture's bind group is built once and kept for as long as the picture is: the atlas is
    // the same atlas every frame, and rebuilding its binding sixty times a second is work for
    // nothing.
    for call in &frame.calls {
        if buffers.pictures.contains_key(&call.image) {
            continue;
        }

        let Some(image) = images.get(call.image) else {
            continue;
        };

        let group = device.create_bind_group(
            "bcs_imgui_picture",
            &cache.get_bind_group_layout(&pipeline.picture_layout),
            &BindGroupEntries::sequential((&image.texture_view, &pipeline.sampler)),
        );

        buffers.pictures.insert(call.image, group);
    }
}

/// Draws the interface over what the camera drew.
fn draw(
    view: ViewQuery<&ViewTarget>,
    mut frame: ResMut<Frame>,
    mut built: ResMut<Built>,
    buffers: Res<Buffers>,
    pipeline: Res<Pipeline>,
    cache: Res<PipelineCache>,
    mut ctx: RenderContext,
) {
    if frame.done || frame.calls.is_empty() {
        return;
    }

    let target = view.into_inner();
    let format = target.main_texture_format();

    // Built for the format of whatever it is drawing into, and kept: a pipeline is compiled once
    // and there is one format in an ordinary run.
    let id = *built.0.entry(format).or_insert_with(|| {
        cache.queue_render_pipeline(descriptor(&pipeline, format))
    });

    let Some(built) = cache.get_render_pipeline(id) else {
        // Still compiling. Nothing is drawn this frame, which is a frame or two at startup.
        return;
    };

    let (Some(view_group), Some(vertices), Some(indices)) = (
        buffers.view.as_ref(),
        buffers.vertices.buffer(),
        buffers.indices.buffer(),
    ) else {
        return;
    };

    frame.done = true;

    let scale = frame.scale;
    let width = (frame.projection.x_axis.x.abs().recip() * 2.0 * scale.x).round() as u32;
    let height = (frame.projection.y_axis.y.abs().recip() * 2.0 * scale.y).round() as u32;

    let mut pass = ctx.begin_tracked_render_pass(RenderPassDescriptor {
        label: Some("bcs_imgui"),
        color_attachments: &[Some(target.get_unsampled_color_attachment())],
        depth_stencil_attachment: None,
        timestamp_writes: None,
        occlusion_query_set: None,
        multiview_mask: None,
    });

    pass.set_render_pipeline(built);
    pass.set_bind_group(0, view_group, &[]);
    pass.set_vertex_buffer(0, vertices.slice(..));
    pass.set_index_buffer(indices.slice(..), IndexFormat::Uint16);

    for call in &frame.calls {
        let Some(picture) = buffers.pictures.get(&call.image) else {
            continue;
        };

        // In physical pixels, and inside the target: a scissor rectangle that reaches past the
        // edge is a validation error rather than a clamp.
        let left = (call.clip[0] * scale.x).max(0.0).round() as u32;
        let top = (call.clip[1] * scale.y).max(0.0).round() as u32;
        let right = (call.clip[2] * scale.x).round() as u32;
        let bottom = (call.clip[3] * scale.y).round() as u32;

        let right = right.min(width);
        let bottom = bottom.min(height);

        if right <= left || bottom <= top {
            continue;
        }

        pass.set_scissor_rect(left, top, right - left, bottom - top);
        pass.set_bind_group(1, picture, &[]);

        let start = call.index;
        pass.draw_indexed(start..start + call.elements, call.vertex as i32, 0..1);
    }
}

/// What the pipeline for a target format looks like.
fn descriptor(pipeline: &Pipeline, format: TextureFormat) -> RenderPipelineDescriptor {
    RenderPipelineDescriptor {
        label: Some("bcs_imgui_pipeline".into()),
        layout: vec![
            pipeline.view_layout.clone(),
            pipeline.picture_layout.clone(),
        ],
        vertex: VertexState {
            shader: pipeline.shader.clone(),
            entry_point: Some("vertex".into()),
            buffers: vec![VertexBufferLayout::from_vertex_formats(
                VertexStepMode::Vertex,
                vec![
                    // Position, in logical pixels from the top left.
                    VertexFormat::Float32x2,
                    // Where it reads from its picture.
                    VertexFormat::Float32x2,
                    // Its colour, as ImGui packs one.
                    VertexFormat::Unorm8x4,
                ],
            )],
            ..default()
        },
        fragment: Some(FragmentState {
            shader: pipeline.shader.clone(),
            entry_point: Some("fragment".into()),
            targets: vec![Some(ColorTargetState {
                format,
                // Straight alpha, which is what ImGui writes. Premultiplied would draw every
                // panel's edges twice as dark as they should be.
                blend: Some(BlendState {
                    color: BlendComponent {
                        src_factor: BlendFactor::SrcAlpha,
                        dst_factor: BlendFactor::OneMinusSrcAlpha,
                        operation: BlendOperation::Add,
                    },
                    alpha: BlendComponent {
                        src_factor: BlendFactor::One,
                        dst_factor: BlendFactor::OneMinusSrcAlpha,
                        operation: BlendOperation::Add,
                    },
                }),
                write_mask: ColorWrites::ALL,
            })],
            ..default()
        }),
        ..default()
    }
}
