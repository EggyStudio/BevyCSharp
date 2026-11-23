//! The copy that brings a window's picture to it at the frame's end, where the picture differs
//! from the window, which [`super::picture`] works out and the cameras draw.
//!
//! Bevy's own copy to the window clips to the camera's viewport in the target's pixels, which here
//! are not the window's, and samples the nearest pixel, so it is left out for a scaled camera's
//! frame, and this copy draws the whole target over the window with the filter the settings chose,
//! clipped to where the picture stands. A picture brought down to the window is copied smoothly
//! whatever the filter. The interface ImGui draws, and Bevy's where the window's picture is its
//! own camera's, is on a camera that is not scaled, so it is drawn after the copy at the window's
//! size.

#![cfg(feature = "render")]

use bevy::app::App;
use bevy::asset::{AssetServer, Handle, embedded_asset, load_embedded_asset};
use bevy::camera::{Camera2d, CameraOutputMode, CompositingSpace};
use bevy::color::LinearRgba;
use bevy::core_pipeline::core_2d::prepare_core_2d_depth_textures;
use bevy::core_pipeline::upscaling::upscaling;
use bevy::core_pipeline::{Core2d, Core2dSystems, Core3d, Core3dSystems, FullscreenShader};
use bevy::ecs::prelude::*;
use bevy::math::UVec2;
use bevy::platform::collections::HashMap;
use bevy::render::camera::ExtractedCamera;
use bevy::render::diagnostic::RecordDiagnostics;
use bevy::render::render_resource::binding_types::{sampler, texture_2d};
use bevy::render::render_resource::{
    BindGroup, BindGroupEntries, BindGroupLayoutDescriptor, BindGroupLayoutEntries, BlendState,
    CachedRenderPipelineId, ColorTargetState, ColorWrites, Extent3d, FilterMode, FragmentState, PipelineCache,
    RenderPassDescriptor, RenderPipelineDescriptor, Sampler, SamplerBindingType, SamplerDescriptor,
    ShaderStages, SpecializedRenderPipeline, SpecializedRenderPipelines, TextureDescriptor, TextureDimension,
    TextureFormat, TextureSampleType, TextureViewId,
};
use bevy::render::renderer::{RenderContext, RenderDevice, ViewQuery};
use bevy::render::sync_world::RenderEntity;
use bevy::render::texture::TextureCache;
use bevy::render::view::{ResolvedCompositingSpace, ViewDepthStencilTexture, ViewTarget};
use bevy::render::{Extract, ExtractSchedule, Render, RenderApp, RenderStartup, RenderSystems};
use bevy::shader::Shader;
use bevy::ui_render::ui_pass;

use super::picture::{ScaledCamera, filter};
use crate::render::corners::DrawCorners;

/// What the frame's end copies of a scaled camera's picture, the part of the window from its
/// corner the whole target is drawn over, the part of that the picture stands in, the filter, one
/// of [`filter`]'s, and whether the picture is brought down along either side, which is copied
/// smoothly whatever the filter.
#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub struct WindowCopy {
    pub over: UVec2,
    pub at: UVec2,
    pub size: UVec2,
    pub filter: u32,
    pub down: bool,
}

/// Adds the copy that brings a scaled camera's picture to its window.
pub fn install(app: &mut App) {
    embedded_asset!(app, "scaled.wgsl");

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app
        .init_resource::<ScaledViews>()
        .init_resource::<ScalePipelines>()
        .init_resource::<SpecializedRenderPipelines<ScalePipeline>>()
        .add_systems(RenderStartup, init_pipeline)
        .add_systems(ExtractSchedule, extract_scaled)
        .add_systems(Render, prepare_scaled.in_set(RenderSystems::Prepare))
        .add_systems(
            Render,
            own_depth.after(prepare_core_2d_depth_textures).in_set(RenderSystems::PrepareResources),
        )
        // In the place of Bevy's copy to the window, after everything the camera does. Bevy's
        // interface and the rounded corners draw into the camera's picture in the same place,
        // after its effects and before the copy to the window, and nothing else orders them
        // against this copy, which would take the picture without them and leave them undrawn.
        .add_systems(
            Core3d,
            copy_scaled.after(Core3dSystems::PostProcess).after(ui_pass).after(DrawCorners).before(upscaling),
        )
        .add_systems(
            Core2d,
            copy_scaled.after(Core2dSystems::PostProcess).after(ui_pass).after(DrawCorners).before(upscaling),
        );
}

/// The copies the render world makes this frame, by the view of each scaled camera.
#[derive(Resource, Default)]
struct ScaledViews(HashMap<Entity, WindowCopy>);

/// The copy's pipeline for each scaled view, for its window's format and its blending.
#[derive(Resource, Default)]
struct ScalePipelines(HashMap<Entity, CachedRenderPipelineId>);

/// Gives each 2D view a depth texture of its own size where Bevy gave it one of another.
///
/// Bevy makes one depth texture for all the 2D cameras on a target, at the size of the first,
/// which holds while every camera on a window draws at the window's size. A scaled camera draws at
/// its picture's, and the interface's camera on the same window at the window's, and wgpu refuses
/// a pass whose depth and color differ in size. Each 2D pass clears its depth, so a texture of its
/// own carries nothing another camera wrote, and a view whose depth is its size keeps Bevy's.
fn own_depth(
    mut commands: Commands,
    mut textures: ResMut<TextureCache>,
    device: Res<RenderDevice>,
    views: Query<(Entity, &ExtractedCamera, &ViewDepthStencilTexture), With<Camera2d>>,
) {
    for (view, camera, depth) in &views {
        let Some(size) = camera.physical_target_size else {
            continue;
        };
        let shared = depth.texture();
        let wanted = Extent3d { width: size.x, height: size.y, depth_or_array_layers: 1 };
        if shared.size() == wanted {
            continue;
        }
        let own = textures.get(
            &device,
            TextureDescriptor {
                label: Some("bcs_picture_depth"),
                size: wanted,
                mip_level_count: 1,
                sample_count: shared.sample_count(),
                dimension: TextureDimension::D2,
                format: shared.format(),
                usage: shared.usage(),
                view_formats: &[],
            },
        );
        commands.entity(view).insert(ViewDepthStencilTexture::new(own, Some(0.0), None));
    }
}

fn extract_scaled(mut views: ResMut<ScaledViews>, cameras: Extract<Query<(&RenderEntity, &ScaledCamera)>>) {
    views.0.clear();
    for (view, scaled) in &cameras {
        views.0.insert(view.id(), scaled.copy);
    }
}

#[derive(Resource)]
struct ScalePipeline {
    layout: BindGroupLayoutDescriptor,
    linear: Sampler,
    nearest: Sampler,
    fullscreen_shader: FullscreenShader,
    shader: Handle<Shader>,
}

#[derive(PartialEq, Eq, Hash, Clone, Copy)]
struct ScaleKey {
    format: TextureFormat,
    blend: Option<BlendState>,
    space: Option<CompositingSpace>,
    copy: Copied,
}

/// Which of the shader's copies a pipeline draws with, the sampler's filter alone, the sharp
/// scaling's blend at a picture pixel's edge, or the smooth copy of a picture brought down.
#[derive(PartialEq, Eq, Hash, Clone, Copy)]
enum Copied {
    Filtered,
    Sharp,
    Down,
}

impl Copied {
    fn of(copy: &WindowCopy) -> Self {
        match (copy.down, copy.filter) {
            (true, _) => Copied::Down,
            (false, filter::SHARP) => Copied::Sharp,
            _ => Copied::Filtered,
        }
    }
}

fn init_pipeline(
    mut commands: Commands,
    device: Res<RenderDevice>,
    fullscreen_shader: Res<FullscreenShader>,
    assets: Res<AssetServer>,
) {
    // Filterable, unlike Bevy's own copy's, since a view's picture is a format a sampler filters.
    let layout = BindGroupLayoutDescriptor::new(
        "bcs_scaled_layout",
        &BindGroupLayoutEntries::sequential(
            ShaderStages::FRAGMENT,
            (
                texture_2d(TextureSampleType::Float { filterable: true }),
                sampler(SamplerBindingType::Filtering),
            ),
        ),
    );
    let sampled = |filter: FilterMode| {
        device.create_sampler(&SamplerDescriptor {
            label: Some("bcs_scaled_sampler"),
            mag_filter: filter,
            min_filter: filter,
            ..Default::default()
        })
    };

    commands.insert_resource(ScalePipeline {
        layout,
        linear: sampled(FilterMode::Linear),
        nearest: sampled(FilterMode::Nearest),
        fullscreen_shader: fullscreen_shader.clone(),
        shader: load_embedded_asset!(assets.as_ref(), "scaled.wgsl"),
    });
}

impl SpecializedRenderPipeline for ScalePipeline {
    type Key = ScaleKey;

    fn specialize(&self, key: Self::Key) -> RenderPipelineDescriptor {
        let entry_point = match (key.copy, key.space) {
            (Copied::Filtered, Some(CompositingSpace::Srgb)) => "fragment_srgb",
            (Copied::Filtered, Some(CompositingSpace::Oklab)) => "fragment_oklab",
            (Copied::Filtered, Some(CompositingSpace::Linear) | None) => "fragment",
            (Copied::Sharp, Some(CompositingSpace::Srgb)) => "sharp_srgb",
            (Copied::Sharp, Some(CompositingSpace::Oklab)) => "sharp_oklab",
            (Copied::Sharp, Some(CompositingSpace::Linear) | None) => "sharp",
            (Copied::Down, Some(CompositingSpace::Srgb)) => "down_srgb",
            (Copied::Down, Some(CompositingSpace::Oklab)) => "down_oklab",
            (Copied::Down, Some(CompositingSpace::Linear) | None) => "down",
        };

        RenderPipelineDescriptor {
            label: Some("bcs_scaled_pipeline".into()),
            layout: vec![self.layout.clone()],
            vertex: self.fullscreen_shader.to_vertex_state(),
            fragment: Some(FragmentState {
                shader: self.shader.clone(),
                entry_point: Some(entry_point.into()),
                targets: vec![Some(ColorTargetState {
                    format: key.format,
                    blend: key.blend,
                    write_mask: ColorWrites::ALL,
                })],
                ..Default::default()
            }),
            ..Default::default()
        }
    }
}

/// The blending a camera's picture is put on its window with, as Bevy's copy decides it, over
/// what is there where an earlier camera drew on the same window and the camera names none.
fn blend_of(camera: &ExtractedCamera) -> Option<BlendState> {
    match camera.output_mode {
        CameraOutputMode::Write { blend_state: None, .. } if camera.sorted_camera_index_for_target > 0 => {
            Some(BlendState::ALPHA_BLENDING)
        }
        CameraOutputMode::Write { blend_state, .. } => blend_state,
        CameraOutputMode::Skip => None,
    }
}

/// Makes the copy's pipeline for each scaled view and waits for it, as Bevy does for its own copy,
/// so no frame goes without the picture.
fn prepare_scaled(
    views: Res<ScaledViews>,
    targets: Query<(&ViewTarget, &ExtractedCamera, Option<&ResolvedCompositingSpace>)>,
    mut cache: ResMut<PipelineCache>,
    pipeline: Option<Res<ScalePipeline>>,
    mut specialized: ResMut<SpecializedRenderPipelines<ScalePipeline>>,
    mut ids: ResMut<ScalePipelines>,
) {
    ids.0.clear();
    let Some(pipeline) = pipeline else { return };

    for (&view, copy) in &views.0 {
        let Ok((target, camera, space)) = targets.get(view) else {
            continue;
        };
        let Some(format) = target.out_texture_view_format() else {
            continue;
        };
        let key = ScaleKey {
            format,
            blend: blend_of(camera),
            space: ResolvedCompositingSpace::space(space),
            copy: Copied::of(copy),
        };
        let id = specialized.specialize(&cache, &pipeline, key);
        cache.block_on_render_pipeline(id);
        ids.0.insert(view, id);
    }
}

/// Draws a scaled camera's picture over the part of its window the whole target spans, clipped to
/// where the picture stands, with black round it on the window's first picture of the frame, and
/// leaves Bevy's own copy out of this view's frame.
fn copy_scaled(
    view: ViewQuery<(&ViewTarget, &mut ExtractedCamera)>,
    views: Res<ScaledViews>,
    ids: Res<ScalePipelines>,
    cache: Res<PipelineCache>,
    pipeline: Option<Res<ScalePipeline>>,
    mut groups: Local<HashMap<(TextureViewId, bool), BindGroup>>,
    mut ctx: RenderContext,
) {
    let entity = view.entity();
    let (Some(copy), Some(id), Some(pipeline)) = (views.0.get(&entity).copied(), ids.0.get(&entity), pipeline) else {
        return;
    };
    // Left to Bevy's copy until it compiles, which draws the picture over the window unclipped.
    let Some(render_pipeline) = cache.get_render_pipeline(*id) else {
        return;
    };
    let (target, mut camera) = view.into_inner();
    if matches!(camera.output_mode, CameraOutputMode::Skip) {
        return;
    }
    let Some(attachment) = target.out_texture_color_attachment(Some(LinearRgba::BLACK)) else {
        return;
    };

    // One for each of the two textures a view's picture alternates between, by sampler, and made
    // anew where the views' textures have all changed, as they do as a window is resized. The
    // sharp scaling reads its picture pixels by their place and takes the linear one unused, and a
    // picture brought down takes the linear one whatever the filter.
    let source = target.main_texture_view();
    let nearest = copy.filter == filter::NEAREST && !copy.down;
    if groups.len() > 16 {
        groups.clear();
    }
    let bind_group = groups
        .entry((source.id(), nearest))
        .or_insert_with(|| {
            ctx.render_device().create_bind_group(
                "bcs_scaled_bind_group",
                &cache.get_bind_group_layout(&pipeline.layout),
                &BindGroupEntries::sequential((source, if nearest { &pipeline.nearest } else { &pipeline.linear })),
            )
        })
        .clone();

    let diagnostics = ctx.diagnostic_recorder();
    let diagnostics = diagnostics.as_deref();
    let time_span = diagnostics.time_span(ctx.command_encoder(), "scaled_to_window");
    {
        let mut pass = ctx.command_encoder().begin_render_pass(&RenderPassDescriptor {
            label: Some("bcs_scaled_to_window"),
            color_attachments: &[Some(attachment)],
            depth_stencil_attachment: None,
            timestamp_writes: None,
            occlusion_query_set: None,
            multiview_mask: None,
        });
        pass.set_viewport(0.0, 0.0, copy.over.x as f32, copy.over.y as f32, 0.0, 1.0);
        pass.set_scissor_rect(copy.at.x, copy.at.y, copy.size.x.max(1), copy.size.y.max(1));
        pass.set_pipeline(render_pipeline);
        pass.set_bind_group(0, &bind_group, &[]);
        pass.draw(0..3, 0..1);
    }
    time_span.end(ctx.command_encoder());

    camera.output_mode = CameraOutputMode::Skip;
}

#[cfg(test)]
mod tests {
    /// The copy's shader is valid WGSL as wgpu takes it, with the entry point of each scaling and
    /// conversion its pipeline is specialized with.
    #[test]
    fn the_copy_shader_validates_with_each_conversion() {
        use naga::valid::{Capabilities, ValidationFlags, Validator};

        let text = include_str!("scaled.wgsl");
        let module = naga::front::wgsl::parse_str(text).unwrap_or_else(|error| panic!("{}", error.emit_to_string(text)));
        if let Err(error) = Validator::new(ValidationFlags::all(), Capabilities::default()).validate(&module) {
            panic!("{}", error.emit_to_string(text));
        }
        let points: Vec<_> = module.entry_points.iter().map(|point| point.name.as_str()).collect();
        assert_eq!(
            points,
            ["fragment", "fragment_srgb", "fragment_oklab", "sharp", "sharp_srgb", "sharp_oklab", "down", "down_srgb", "down_oklab"]
        );
    }
}
