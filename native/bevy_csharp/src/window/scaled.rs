//! A window covering its screen whose picture is drawn at a size of its own and brought to the
//! screen in one copy at the frame's end, softer and cheaper at a size under the screen's, which
//! the settings' borderless fullscreen asks for.
//!
//! **How the cameras draw at the picture's size.** Each camera on such a window is told its target
//! is the picture's size in pixels with the window's logical size, its scale factor divided by the
//! ratio the picture is enlarged by, so every texture of its view, its passes and Bevy's interface
//! on it are made at the picture's size, and a pointer at a place on the window lands on what is
//! drawn there, since the camera's logical rectangle is the one it is shown over. Where the picture
//! stands in the middle with black round it, as a whole ratio leaves it or a shape unlike the
//! screen's does, the target is the screen divided by the ratio and the camera is given the middle
//! of it as its viewport, which stands in the window's middle once the target is drawn over the
//! window. A game's own viewport is scaled into that middle and given back when the scaling ends.
//!
//! **The copy.** Bevy's own copy to the window clips to the camera's viewport in the target's
//! pixels, which here are not the window's, and samples the nearest pixel, so it is left out for a
//! scaled camera's frame, and this copy draws the whole target over the window with the filter the
//! settings chose, clipped to where the picture stands. The interface ImGui draws is on a camera
//! of its own that is not scaled, so it is drawn after the copy at the screen's size, and Bevy's
//! interface, drawn by the scene's camera, is scaled with the picture.

#![cfg(feature = "render")]

use bevy::app::{App, PostUpdate};
use bevy::asset::{AssetServer, Handle, embedded_asset, load_embedded_asset};
use bevy::camera::visibility::update_frusta;
use bevy::camera::{
    Camera, CameraOutputMode, CameraUpdateSystems, CompositingSpace, NormalizedRenderTarget,
    Projection, RenderTarget, RenderTargetInfo, Viewport,
};
use bevy::color::LinearRgba;
use bevy::core_pipeline::upscaling::upscaling;
use bevy::core_pipeline::{Core2d, Core2dSystems, Core3d, Core3dSystems, FullscreenShader};
use bevy::ecs::prelude::*;
use bevy::math::UVec2;
use bevy::platform::collections::HashMap;
use bevy::render::camera::{ExtractedCamera, camera_system};
use bevy::render::diagnostic::RecordDiagnostics;
use bevy::render::render_resource::binding_types::{sampler, texture_2d};
use bevy::render::render_resource::{
    BindGroup, BindGroupEntries, BindGroupLayoutDescriptor, BindGroupLayoutEntries, BlendState,
    CachedRenderPipelineId, ColorTargetState, ColorWrites, FilterMode, FragmentState, PipelineCache,
    RenderPassDescriptor, RenderPipelineDescriptor, Sampler, SamplerBindingType, SamplerDescriptor,
    ShaderStages, SpecializedRenderPipeline, SpecializedRenderPipelines, TextureFormat, TextureSampleType,
    TextureViewId,
};
use bevy::render::renderer::{RenderContext, RenderDevice, ViewQuery};
use bevy::render::sync_world::RenderEntity;
use bevy::render::view::{ResolvedCompositingSpace, ViewTarget};
use bevy::render::{Extract, ExtractSchedule, Render, RenderApp, RenderStartup, RenderSystems};
use bevy::shader::Shader;
use bevy::window::{PrimaryWindow, Window, WindowMode};

use crate::imgui::render::InterfaceView;

/// How a scaled window's picture is brought to its screen, by the numbers
/// `bcs_window_set_scaling` takes.
pub mod filter {
    /// Filtered, each screen pixel a blend of the picture pixels round it, soft.
    pub const LINEAR: u32 = 0;
    /// Each screen pixel from the picture pixel under it, sharp, and even only at a whole ratio.
    pub const NEAREST: u32 = 1;
    /// Enlarged by the largest whole number that fits, each picture pixel a square block of screen
    /// pixels, in the screen's middle with black round it.
    pub const INTEGER: u32 = 2;
}

/// On a window, the size its picture is drawn at while it covers its screen borderless, in the
/// window's logical pixels as its own size is, and how the picture is brought to the screen.
#[derive(Component, Clone, Copy, PartialEq, Eq, Debug)]
pub struct WindowScaling {
    /// The picture's size, which the window's scale factor turns into the pixels it is drawn at.
    pub size: UVec2,
    /// One of [`filter`]'s.
    pub filter: u32,
}

/// On a camera drawing a scaled window's picture, the viewport its game gave it, the one set in
/// its place, and what the frame's end copies of its picture.
#[derive(Component)]
struct ScaledCamera {
    own: Option<Viewport>,
    set: Option<Viewport>,
    copy: WindowCopy,
}

/// What the frame's end copies of a scaled camera's picture, the part of the window from its
/// corner the whole target is drawn over, the part of that the picture stands in, and whether each
/// window pixel takes the nearest picture pixel.
#[derive(Clone, Copy, PartialEq, Eq, Debug)]
struct WindowCopy {
    over: UVec2,
    at: UVec2,
    size: UVec2,
    nearest: bool,
}

/// How a camera on a scaled window draws, the target it is told of, the place and size of the
/// picture in it, the window's size, and the copy.
#[derive(Debug, PartialEq)]
struct Plan {
    target: UVec2,
    scale_factor: f32,
    at: UVec2,
    picture: UVec2,
    screen: UVec2,
    copy: WindowCopy,
}

/// Adds what draws a scaled window's picture at its own size and brings it to the screen.
pub fn install(app: &mut App) {
    embedded_asset!(app, "scaled.wgsl");

    // After Bevy's camera system has sized each camera from its window, so the size given here
    // stands, and before the frusta and Bevy's interface read it.
    app.add_systems(
        PostUpdate,
        scale_cameras.in_set(CameraUpdateSystems).after(camera_system).before(update_frusta),
    );

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
        // In the place of Bevy's copy to the window, after everything the camera does.
        .add_systems(Core3d, copy_scaled.after(Core3dSystems::PostProcess).before(upscaling))
        .add_systems(Core2d, copy_scaled.after(Core2dSystems::PostProcess).before(upscaling));
}

/// How a camera on `window` draws where the window covers its screen borderless and is scaled,
/// or none where it draws at the window's own size.
///
/// The picture is the scaling's size in the window's pixels, enlarged by the ratio of the screen
/// to it that keeps its shape, the whole number below that ratio for [`filter::INTEGER`]. A picture
/// as large as the screen or larger is drawn at the screen's own size, since it would be shrunk.
fn plan(window: &Window, scaling: &WindowScaling) -> Option<Plan> {
    if !matches!(window.mode, WindowMode::BorderlessFullscreen(_)) {
        return None;
    }
    let screen = window.physical_size();
    let scale_factor = window.resolution.scale_factor();
    let picture = (scaling.size.as_vec2() * scale_factor).round().as_uvec2().max(UVec2::ONE);
    if screen.x == 0 || screen.y == 0 {
        return None;
    }
    let ratio = (screen.as_vec2() / picture.as_vec2()).min_element();
    if ratio <= 1.0 {
        return None;
    }

    let integer = scaling.filter == filter::INTEGER;
    let by = if integer { ratio.floor() } else { ratio };
    let target = if integer { (screen.as_vec2() / by).floor() } else { (screen.as_vec2() / by).round() };
    let target = target.as_uvec2().max(picture);
    let at = (target - picture) / 2;

    // A whole ratio drawn over the window's corner at exactly that ratio, so every picture pixel is
    // the same block, and any other over the whole window.
    let over = if integer { (target.as_vec2() * by).as_uvec2().min(screen) } else { screen };
    let stretch = over.as_vec2() / target.as_vec2();
    let copy = WindowCopy {
        over,
        at: (at.as_vec2() * stretch).round().as_uvec2(),
        size: (picture.as_vec2() * stretch).round().as_uvec2().min(over),
        nearest: scaling.filter != filter::LINEAR,
    };

    Some(Plan { target, scale_factor: scale_factor / by, at, picture, screen, copy })
}

/// The viewport a scaled camera is given, the game's own scaled into where the picture stands, or
/// where the picture stands where that is not the whole target.
fn viewport(plan: &Plan, own: Option<&Viewport>) -> Option<Viewport> {
    match own {
        None if plan.at == UVec2::ZERO && plan.target == plan.picture => None,
        None => Some(Viewport { physical_position: plan.at, physical_size: plan.picture, depth: 0.0..1.0 }),
        Some(own) => {
            let ratio = plan.picture.as_vec2() / plan.screen.as_vec2();
            Some(Viewport {
                physical_position: plan.at + (own.physical_position.as_vec2() * ratio).round().as_uvec2(),
                physical_size: (own.physical_size.as_vec2() * ratio).round().as_uvec2().max(UVec2::ONE),
                depth: own.depth.clone(),
            })
        }
    }
}

fn same(a: &Option<Viewport>, b: &Option<Viewport>) -> bool {
    match (a, b) {
        (None, None) => true,
        (Some(a), Some(b)) => {
            a.physical_position == b.physical_position && a.physical_size == b.physical_size && a.depth == b.depth
        }
        _ => false,
    }
}

fn same_target(a: &Option<RenderTargetInfo>, size: UVec2, scale_factor: f32) -> bool {
    a.as_ref().is_some_and(|a| a.physical_size == size && a.scale_factor == scale_factor)
}

/// The camera's projection made again for its logical viewport, as Bevy's camera system makes it.
///
/// Written past change detection where the camera system wrote the projection this frame, which
/// has marked it for the frusta, so the camera system does not make it again from the window in
/// the next frame and this again after it, in every frame.
fn reproject(camera: &mut Camera, projection: &mut Mut<Projection>) {
    let Some(size) = camera.logical_viewport_size().filter(|size| size.x != 0.0 && size.y != 0.0) else {
        return;
    };
    let marked = projection.is_changed();
    let projection: &mut Projection = if marked { projection.bypass_change_detection() } else { &mut **projection };
    projection.update(size.x, size.y);
    camera.computed.clip_from_view = match &camera.sub_camera_view {
        Some(sub) => projection.get_clip_from_view_for_sub(sub),
        None => projection.get_clip_from_view(),
    };
}

/// Tells each camera on a scaled window the picture's size as its target, and gives a camera told
/// so whose window is unscaled its own viewport and its window's size back.
///
/// Only what differs from what the camera holds is written, so a camera stays unchanged from frame
/// to frame once it is scaled, until the window or the game changes something.
fn scale_cameras(
    mut commands: Commands,
    primary: Query<Entity, With<PrimaryWindow>>,
    windows: Query<(&Window, Option<&WindowScaling>)>,
    mut cameras: Query<
        (Entity, &mut Camera, &RenderTarget, &mut Projection, Option<&mut ScaledCamera>),
        Without<InterfaceView>,
    >,
) {
    let primary = primary.iter().next();

    for (entity, mut camera, target, mut projection, scaled) in &mut cameras {
        let window = match target.normalize(primary) {
            Some(NormalizedRenderTarget::Window(window)) => windows.get(window.entity()).ok(),
            _ => None,
        };
        let planned = window.and_then(|(window, scaling)| plan(window, scaling?));

        // The game's own viewport, the one set here standing for it until the game sets another.
        let own = match &scaled {
            Some(scaled) if same(&camera.viewport, &scaled.set) => scaled.own.clone(),
            _ => camera.viewport.clone(),
        };

        match (planned, scaled) {
            (Some(plan), scaled) => {
                let set = viewport(&plan, own.as_ref());
                if !same_target(&camera.computed.target_info, plan.target, plan.scale_factor) || !same(&camera.viewport, &set) {
                    camera.viewport = set.clone();
                    camera.computed.target_info = Some(RenderTargetInfo {
                        physical_size: plan.target,
                        scale_factor: plan.scale_factor,
                    });
                    reproject(&mut camera, &mut projection);
                }
                match scaled {
                    Some(mut scaled) => {
                        if !same(&scaled.set, &set) || scaled.copy != plan.copy || !same(&scaled.own, &own) {
                            *scaled = ScaledCamera { own, set, copy: plan.copy };
                        }
                    }
                    None => {
                        commands.entity(entity).insert(ScaledCamera { own, set, copy: plan.copy });
                    }
                }
            }
            (None, Some(_)) => {
                camera.viewport = own;
                match window {
                    Some((window, _)) => {
                        camera.computed.target_info = Some(RenderTargetInfo {
                            physical_size: window.physical_size(),
                            scale_factor: window.resolution.scale_factor(),
                        });
                        reproject(&mut camera, &mut projection);
                    }
                    // A camera moved to another target has it sized by the camera system, which a
                    // changed projection asks for.
                    None => projection.set_changed(),
                }
                commands.entity(entity).remove::<ScaledCamera>();
            }
            (None, None) => {}
        }
    }
}

/// The copies the render world makes this frame, by the view of each scaled camera.
#[derive(Resource, Default)]
struct ScaledViews(HashMap<Entity, WindowCopy>);

/// The copy's pipeline for each scaled view, for its window's format and its blending.
#[derive(Resource, Default)]
struct ScalePipelines(HashMap<Entity, CachedRenderPipelineId>);

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
        let entry_point = match key.space {
            Some(CompositingSpace::Srgb) => "fragment_srgb",
            Some(CompositingSpace::Oklab) => "fragment_oklab",
            Some(CompositingSpace::Linear) | None => "fragment",
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

    for &view in views.0.keys() {
        let Ok((target, camera, space)) = targets.get(view) else {
            continue;
        };
        let Some(format) = target.out_texture_view_format() else {
            continue;
        };
        let key = ScaleKey { format, blend: blend_of(camera), space: ResolvedCompositingSpace::space(space) };
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

    // One for each of the two textures a view's picture alternates between, by filter, and made
    // anew where the views' textures have all changed, as they do as a window is resized.
    let source = target.main_texture_view();
    if groups.len() > 16 {
        groups.clear();
    }
    let bind_group = groups
        .entry((source.id(), copy.nearest))
        .or_insert_with(|| {
            ctx.render_device().create_bind_group(
                "bcs_scaled_bind_group",
                &cache.get_bind_group_layout(&pipeline.layout),
                &BindGroupEntries::sequential((source, if copy.nearest { &pipeline.nearest } else { &pipeline.linear })),
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
    use super::*;

    fn window(width: u32, height: u32) -> Window {
        Window {
            mode: WindowMode::BorderlessFullscreen(bevy::window::MonitorSelection::Primary),
            resolution: bevy::window::WindowResolution::new(width, height).with_scale_factor_override(1.0),
            ..Default::default()
        }
    }

    fn scaling(width: u32, height: u32, filter: u32) -> WindowScaling {
        WindowScaling { size: UVec2::new(width, height), filter }
    }

    #[test]
    fn a_picture_of_the_screens_shape_is_drawn_over_the_whole_window() {
        let plan = plan(&window(1920, 1080), &scaling(1280, 720, filter::LINEAR)).unwrap();
        assert_eq!(plan.target, UVec2::new(1280, 720));
        assert_eq!(plan.at, UVec2::ZERO);
        assert_eq!(plan.copy, WindowCopy { over: UVec2::new(1920, 1080), at: UVec2::ZERO, size: UVec2::new(1920, 1080), nearest: false });
        assert!((plan.scale_factor - 1.0 / 1.5).abs() < 1e-6);
        assert!(viewport(&plan, None).is_none());
    }

    #[test]
    fn a_whole_ratio_stands_in_the_middle_with_black_round_it() {
        let plan = plan(&window(1920, 1080), &scaling(800, 450, filter::INTEGER)).unwrap();
        // Twice fits, three times does not.
        assert_eq!(plan.target, UVec2::new(960, 540));
        assert_eq!(plan.at, UVec2::new(80, 45));
        assert_eq!(plan.copy, WindowCopy { over: UVec2::new(1920, 1080), at: UVec2::new(160, 90), size: UVec2::new(1600, 900), nearest: true });
        let set = viewport(&plan, None).unwrap();
        assert_eq!((set.physical_position, set.physical_size), (UVec2::new(80, 45), UVec2::new(800, 450)));
    }

    #[test]
    fn a_shape_unlike_the_screens_keeps_its_own_with_black_at_the_sides() {
        let plan = plan(&window(1920, 1080), &scaling(1000, 1000, filter::NEAREST)).unwrap();
        assert_eq!(plan.target, UVec2::new(1778, 1000));
        assert_eq!(plan.at, UVec2::new(389, 0));
        assert_eq!(plan.copy.size.y, 1080);
        assert!(plan.copy.nearest);
    }

    #[test]
    fn a_picture_as_large_as_the_screen_or_a_window_that_does_not_cover_it_is_not_scaled() {
        assert!(plan(&window(1920, 1080), &scaling(1920, 1080, filter::LINEAR)).is_none());
        assert!(plan(&window(1920, 1080), &scaling(2560, 1000, filter::INTEGER)).is_none());
        let mut windowed = window(1920, 1080);
        windowed.mode = WindowMode::Windowed;
        assert!(plan(&windowed, &scaling(1280, 720, filter::LINEAR)).is_none());
    }

    #[test]
    fn a_games_own_viewport_is_scaled_into_where_the_picture_stands() {
        let plan = plan(&window(1920, 1080), &scaling(800, 450, filter::INTEGER)).unwrap();
        let own = Viewport { physical_position: UVec2::new(960, 0), physical_size: UVec2::new(960, 1080), depth: 0.0..1.0 };
        let set = viewport(&plan, Some(&own)).unwrap();
        assert_eq!((set.physical_position, set.physical_size), (UVec2::new(480, 45), UVec2::new(400, 450)));
    }

    /// The copy's shader is valid WGSL as wgpu takes it, with the entry point of each conversion
    /// its pipeline is specialized with.
    #[test]
    fn the_copy_shader_validates_with_each_conversion() {
        use naga::valid::{Capabilities, ValidationFlags, Validator};

        let text = include_str!("scaled.wgsl");
        let module = naga::front::wgsl::parse_str(text).unwrap_or_else(|error| panic!("{}", error.emit_to_string(text)));
        if let Err(error) = Validator::new(ValidationFlags::all(), Capabilities::default()).validate(&module) {
            panic!("{}", error.emit_to_string(text));
        }
        let points: Vec<_> = module.entry_points.iter().map(|point| point.name.as_str()).collect();
        assert_eq!(points, ["fragment", "fragment_srgb", "fragment_oklab"]);
    }
}
