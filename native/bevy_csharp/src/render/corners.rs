//! Rounded corners on a camera's picture, taken off to nothing rather than painted over.
//!
//! A camera draws into a rectangle, and an interface wanting that rectangle rounded can lay the
//! background color over its corners, which works only while there is a background color. On a
//! see-through window the corners have to become clear, and nothing drawn over them can do that,
//! since drawing only adds. So this multiplies what the camera drew by how much of each pixel lies
//! inside a rounded rectangle the size of the camera's viewport, color and alpha alike, after the
//! camera's own passes and before its picture is put on the window. What is left outside is clear,
//! and the edge is antialiased because the coverage is worked out per pixel.

#![cfg(feature = "render")]

use std::collections::HashMap;
use std::sync::Mutex;

use bevy::app::App;
use bevy::asset::{Assets, Handle};
use bevy::camera::Camera;
use bevy::core_pipeline::schedule::{Core2d, Core2dSystems, Core3d, Core3dSystems};
use bevy::core_pipeline::upscaling::upscaling;
use bevy::ecs::component::Component;
use bevy::ecs::query::With;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::Res;
use bevy::render::camera::ExtractedCamera;
use bevy::render::extract_component::{ExtractComponent, ExtractComponentPlugin};
use bevy::render::render_resource::{
    BindGroupEntry, BindGroupLayoutDescriptor, BindGroupLayoutEntry, BindingType, BlendComponent,
    BlendFactor, BlendOperation, BlendState, BufferBindingType, BufferInitDescriptor, BufferUsages,
    CachedRenderPipelineId, ColorTargetState, ColorWrites, FragmentState, PipelineCache,
    RenderPassDescriptor, RenderPipelineDescriptor, ShaderStages, TextureFormat, VertexState,
};
use bevy::render::renderer::{RenderContext, ViewQuery};
use bevy::render::view::ViewTarget;
use bevy::render::RenderApp;
use bevy::shader::Shader;

/// How round a camera's picture is at its corners, in physical pixels. None is square.
#[derive(Component, Clone, Copy, ExtractComponent)]
#[extract_component_filter(With<Camera>)]
pub struct BcsRoundedCorners(pub f32);

/// The shader, made in the main world where shader assets live.
#[derive(Resource, Clone)]
struct CornerShader(Handle<Shader>);

/// A pipeline for each format a picture comes in, queued the first time one is met.
#[derive(Resource, Default)]
struct CornerPipelines(Mutex<HashMap<TextureFormat, CachedRenderPipelineId>>);

const SOURCE: &str = r#"
struct Params {
    origin: vec2<f32>,
    size: vec2<f32>,
    radius: f32,
    unused_a: f32,
    unused_b: vec2<f32>,
};

@group(0) @binding(0) var<uniform> params: Params;

@vertex
fn vertex(@builtin(vertex_index) index: u32) -> @builtin(position) vec4<f32> {
    let corner = vec2<f32>(f32((index << 1u) & 2u), f32(index & 2u));
    return vec4<f32>(corner * 2.0 - 1.0, 0.0, 1.0);
}

// How much of the pixel lies inside the rounded rectangle, from its signed distance to the edge,
// over one pixel either side of it.
@fragment
fn fragment(@builtin(position) position: vec4<f32>) -> @location(0) vec4<f32> {
    let half = params.size * 0.5;
    let local = position.xy - params.origin - half;
    let radius = min(params.radius, min(half.x, half.y));
    let q = abs(local) - (half - vec2<f32>(radius));
    let distance = length(max(q, vec2<f32>(0.0))) + min(max(q.x, q.y), 0.0) - radius;
    let inside = clamp(0.5 - distance, 0.0, 1.0);
    return vec4<f32>(inside);
}
"#;

fn layout() -> BindGroupLayoutDescriptor {
    BindGroupLayoutDescriptor::new(
        "bcs_corners",
        &[BindGroupLayoutEntry {
            binding: 0,
            visibility: ShaderStages::FRAGMENT,
            ty: BindingType::Buffer {
                ty: BufferBindingType::Uniform,
                has_dynamic_offset: false,
                min_binding_size: std::num::NonZeroU64::new(32),
            },
            count: None,
        }],
    )
}

/// Takes the corners off a view's picture, where the view asked for it.
fn draw_corners(
    view: ViewQuery<(&BcsRoundedCorners, &ViewTarget, &ExtractedCamera)>,
    shader: Res<CornerShader>,
    pipelines: Res<CornerPipelines>,
    cache: Res<PipelineCache>,
    mut ctx: RenderContext,
) {
    let (corners, target, camera) = view.into_inner();

    if corners.0 <= 0.0 {
        return;
    }

    let format = target.main_texture_format();

    let id = {
        let Ok(mut known) = pipelines.0.lock() else { return };

        *known.entry(format).or_insert_with(|| {
            // What the picture holds is multiplied by what the shader writes, color and alpha
            // alike, which leaves a premultiplied picture premultiplied and makes what is outside
            // the corner clear rather than dark.
            let multiply = BlendComponent {
                src_factor: BlendFactor::Zero,
                dst_factor: BlendFactor::SrcAlpha,
                operation: BlendOperation::Add,
            };

            cache.queue_render_pipeline(RenderPipelineDescriptor {
                label: Some("bcs_corners".into()),
                layout: vec![layout()],
                vertex: VertexState {
                    shader: shader.0.clone(),
                    shader_defs: Vec::new(),
                    entry_point: None,
                    buffers: Vec::new(),
                },
                fragment: Some(FragmentState {
                    shader: shader.0.clone(),
                    shader_defs: Vec::new(),
                    entry_point: None,
                    targets: vec![Some(ColorTargetState {
                        format,
                        blend: Some(BlendState {
                            color: multiply,
                            alpha: multiply,
                        }),
                        write_mask: ColorWrites::ALL,
                    })],
                }),
                ..Default::default()
            })
        })
    };

    // Still compiling, which is a frame or two with square corners.
    let Some(pipeline) = cache.get_render_pipeline(id) else {
        return;
    };

    // The viewport where there is one, the whole picture otherwise.
    let (origin, size) = match &camera.viewport {
        Some(viewport) => (viewport.physical_position, viewport.physical_size),
        None => match camera.physical_target_size {
            Some(size) => (bevy::math::UVec2::ZERO, size),
            None => return,
        },
    };

    if size.x == 0 || size.y == 0 {
        return;
    }

    let params: [f32; 8] = [
        origin.x as f32,
        origin.y as f32,
        size.x as f32,
        size.y as f32,
        corners.0,
        0.0,
        0.0,
        0.0,
    ];

    let buffer = ctx.render_device().create_buffer_with_data(&BufferInitDescriptor {
        label: Some("bcs_corners_params"),
        contents: bytemuck::cast_slice(&params),
        usage: BufferUsages::UNIFORM,
    });

    let group = ctx.render_device().create_bind_group(
        "bcs_corners",
        &cache.get_bind_group_layout(&layout()),
        &[BindGroupEntry {
            binding: 0,
            resource: buffer.as_entire_binding(),
        }],
    );

    let mut pass = ctx.command_encoder().begin_render_pass(&RenderPassDescriptor {
        label: Some("bcs_corners"),
        color_attachments: &[Some(target.get_unsampled_color_attachment())],
        depth_stencil_attachment: None,
        timestamp_writes: None,
        occlusion_query_set: None,
        multiview_mask: None,
    });

    // Inside the viewport only. Outside it is the rest of the window, which is another camera's
    // or the world's clear color, and none of this one's business.
    pass.set_scissor_rect(origin.x, origin.y, size.x, size.y);
    pass.set_pipeline(pipeline);
    pass.set_bind_group(0, &group, &[]);
    pass.draw(0..3, 0..1);
}

/// Adds what rounds a camera's corners.
pub fn install(app: &mut App) {
    app.add_plugins(ExtractComponentPlugin::<BcsRoundedCorners>::default());

    let Some(mut shaders) = app.world_mut().get_resource_mut::<Assets<Shader>>() else {
        return;
    };

    let shader = shaders.add(Shader::from_wgsl(SOURCE, "bcs_corners.wgsl"));

    // Kept in the main world as well, so the shader asset lives as long as the app.
    app.insert_resource(CornerShader(shader.clone()));

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    // After everything the camera does to its own picture, and before the picture is put on the
    // window, which is the last moment the picture is the camera's alone.
    render_app
        .insert_resource(CornerShader(shader))
        .init_resource::<CornerPipelines>()
        .add_systems(Core3d, draw_corners.after(Core3dSystems::PostProcess).before(upscaling))
        .add_systems(Core2d, draw_corners.after(Core2dSystems::PostProcess).before(upscaling));
}

/// Rounds a camera's corners, or with a radius of nothing squares them again.
pub fn set(world: &mut bevy::ecs::world::World, camera: bevy::ecs::entity::Entity, radius: f32) -> i32 {
    let Ok(mut entity) = world.get_entity_mut(camera) else {
        return crate::interop::status::NO_ENTITY;
    };

    if radius > 0.0 {
        entity.insert(BcsRoundedCorners(radius));
    } else {
        entity.remove::<BcsRoundedCorners>();
    }

    crate::interop::status::OK
}
