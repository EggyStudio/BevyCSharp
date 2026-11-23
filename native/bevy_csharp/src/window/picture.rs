//! The picture a window's cameras draw, at a size and a shape of its own, and how it stands in the
//! window, which the settings' render resolution, aspect, fit and filter ask for in every mode of
//! the window's.
//!
//! **How the cameras draw at the picture's size.** Each camera on such a window is told its target
//! is a size of the picture's with a scale factor of its own, so every texture of its view, its
//! passes and Bevy's interface on it are made at the picture's size. Where the picture fits the
//! window with its shape kept, the target is the window at the picture's density, its logical size
//! the window's, and the camera is given the part the picture stands in as its viewport, so a
//! pointer at a place on the window lands on what is drawn there through Bevy's own sums. Where the
//! picture is stretched over the window, the target is the picture itself with a logical size no
//! smaller than the window's on either side, and the rays Bevy casts from a pointer are cast again
//! through the place the stretch puts it at, since a target has one scale factor for both sides and
//! a stretch has two. A game's own viewport is scaled into the picture and given back when the
//! picture is the window's again.
//!
//! **The interface.** ImGui draws on a camera of its own that is not scaled, after the picture is
//! brought to the window. Bevy's interface goes to the highest camera on the window, which is
//! ImGui's where it runs and otherwise a camera of this module's, spawned while some picture
//! differs from its window, so the interface keeps the window's own size whatever the picture's. A
//! node a game marks with [`OnPicture`] stands over the part of the window the picture stands in.
//!
//! The render world's copy of the picture to the window is [`super::scaled`]'s.

#![cfg(feature = "render")]

use bevy::app::{App, PostUpdate, PreUpdate};
use bevy::camera::visibility::{RenderLayers, update_frusta};
use bevy::camera::{
    Camera, Camera2d, CameraMainTextureUsages, CameraOutputMode, CameraUpdateSystems, ClearColorConfig,
    NormalizedRenderTarget, Projection, RenderTarget, RenderTargetInfo, Viewport,
};
use bevy::core_pipeline::tonemapping::Tonemapping;
use bevy::ecs::prelude::*;
use bevy::math::{Rect, UVec2, Vec2};
use bevy::picking::PickingSystems;
use bevy::picking::backend::ray::{RayId, RayMap};
use bevy::picking::pointer::{PointerId, PointerLocation};
use bevy::render::camera::camera_system;
use bevy::render::render_resource::{BlendState, TextureUsages};
use bevy::render::view::Msaa;
use bevy::transform::components::GlobalTransform;
use bevy::ui::{Node, PositionType, UiScale, UiSystems, Val};
use bevy::window::{PrimaryWindow, Window};

use super::scaled::WindowCopy;
use crate::imgui::render::InterfaceView;

/// How a window's picture is brought to it, by the numbers `bcs_window_set_picture` takes.
pub mod filter {
    /// Filtered, each window pixel a blend of the picture pixels round it, soft.
    pub const LINEAR: u32 = 0;
    /// Each window pixel from the picture pixel under it, sharp, and even only at a whole ratio.
    pub const NEAREST: u32 = 1;
    /// Each window pixel inside a picture pixel from it whole and one across an edge a blend of
    /// the two by how much of each it spans, so every picture pixel is a block of one width with
    /// one blended pixel at most on its edge, which holds still as the picture moves at any ratio
    /// and equals [`NEAREST`] at a whole one.
    pub const SHARP: u32 = 2;
}

/// How a picture of a shape unlike its window's stands in it, by the numbers
/// `bcs_window_set_picture` takes.
pub mod fit {
    /// The picture's shape kept, as large as the window holds, in its middle with black round it.
    pub const FIT: u32 = 0;
    /// The picture over the whole window, its shape given up.
    pub const STRETCH: u32 = 1;
}

/// The most pixels a picture spans along a side, the largest texture every device takes.
const LARGEST: u32 = 8192;

/// On a window, the picture its cameras draw, its size or its share of the window's, its shape,
/// how it stands in the window and how it is brought to it.
#[derive(Component, Clone, Copy, PartialEq, Debug)]
pub struct WindowPicture {
    /// The picture's size in pixels, or zero where it follows the window by `share`.
    pub size: UVec2,
    /// The picture's share of the window's size along each side, where `size` is zero.
    pub share: f32,
    /// The picture's shape as a width and a height, or zero for the shape of its size.
    pub aspect: Vec2,
    /// One of [`fit`]'s.
    pub fit: u32,
    /// One of [`filter`]'s.
    pub filter: u32,
}

impl WindowPicture {
    /// The picture the numbers [`super::shown::bcs_window_set_picture`] takes ask for, none where
    /// it is the window's own size and shape, or an error where a number lies outside its range.
    pub fn from_numbers(
        width: u32,
        height: u32,
        share: f32,
        aspect: (f32, f32),
        fit: u32,
        filter: u32,
    ) -> Result<Option<Self>, ()> {
        let sized = width != 0 && height != 0;
        let numbers = [share, aspect.0, aspect.1];
        if filter > filter::SHARP || fit > fit::STRETCH || numbers.iter().any(|n| !n.is_finite() || *n < 0.0) {
            return Err(());
        }
        if !sized && share <= 0.0 {
            return Err(());
        }
        let aspect = if aspect.0 > 0.0 && aspect.1 > 0.0 { Vec2::new(aspect.0, aspect.1) } else { Vec2::ZERO };
        if !sized && share == 1.0 && aspect == Vec2::ZERO {
            return Ok(None);
        }
        Ok(Some(WindowPicture {
            size: if sized { UVec2::new(width, height) } else { UVec2::ZERO },
            share: if sized { 1.0 } else { share },
            aspect,
            fit,
            filter,
        }))
    }
}

/// On a camera drawing a window's picture, the viewport its game gave it, the one set in its
/// place, the frame's copy of its picture, and for a stretched picture how far the target's
/// logical size stands from the window's along each side.
#[derive(Component)]
pub struct ScaledCamera {
    own: Option<Viewport>,
    set: Option<Viewport>,
    pub(crate) copy: WindowCopy,
    stretch: Option<Vec2>,
}

impl ScaledCamera {
    /// A place on the window in its logical pixels as a place on the camera's target, the same
    /// place where the picture fits the window and moved by the stretch where it is stretched.
    pub fn to_target(&self, at: Vec2) -> Vec2 {
        self.stretch.map_or(at, |stretch| at * stretch)
    }

    /// A place on the camera's target as a place on the window, the other way from
    /// [`ScaledCamera::to_target`].
    pub fn from_target(&self, at: Vec2) -> Vec2 {
        self.stretch.map_or(at, |stretch| at / stretch)
    }
}

/// On an interface node, standing over the part of the first window the picture stands in, its
/// place and size written in each frame they differ from it.
#[derive(Component, Clone, Copy, Default)]
pub struct OnPicture;

/// Marks the camera this module spawns for Bevy's interface, which is not scaled.
#[derive(Component)]
struct PictureInterface;

/// The uses of the picture of a camera drawing an interface over a window, which no game camera's
/// has, so the camera is given a picture of its own at the window's size.
///
/// Bevy gives the cameras on one target one picture where their uses, format and multisampling
/// match, sized by the first of them, which holds while every camera on a window draws at the
/// window's size. A scaled camera's picture is smaller or larger, and an interface camera given it
/// would draw a picture unlike its depth and its window. A use to copy into the picture, which
/// nothing asks of these, makes the key its own, and the picture costs one texture the window's
/// size.
pub(crate) fn interface_usages() -> CameraMainTextureUsages {
    CameraMainTextureUsages::default().with(TextureUsages::COPY_DST)
}

/// How a camera on a window draws its picture, the target it is told of, the place and size of
/// the picture in it, the window's size, the stretch and the copy.
#[derive(Debug, PartialEq)]
pub(crate) struct Plan {
    target: UVec2,
    scale_factor: f32,
    at: UVec2,
    pub(crate) picture: UVec2,
    screen: UVec2,
    stretch: Option<Vec2>,
    pub(crate) copy: WindowCopy,
}

/// Adds what makes a window's cameras draw its picture and what keeps its interface at the
/// window's size.
pub fn install(app: &mut App) {
    // After Bevy's camera system has sized each camera from its window, so the size given here
    // stands, and before the frusta and Bevy's interface read it. The interface's camera follows,
    // and the nodes over the picture are placed before the interface is laid out.
    app.add_systems(
        PostUpdate,
        (
            (scale_cameras, interface_camera)
                .chain()
                .in_set(CameraUpdateSystems)
                .after(camera_system)
                .before(update_frusta),
            place_on_picture.before(UiSystems::Layout),
        ),
    );
    app.add_systems(PreUpdate, stretched_rays.after(RayMap::repopulate).in_set(PickingSystems::ProcessInput));
}

/// The picture's size in pixels on a window of `screen` pixels, its own size or its share of the
/// window's, a pixel at the least and twice the window's at the most along each side, cut to its
/// shape where it has one, the largest of that shape inside it.
fn picture_size(screen: UVec2, picture: &WindowPicture) -> UVec2 {
    let drawn = if picture.size != UVec2::ZERO { picture.size.as_vec2() } else { screen.as_vec2() * picture.share };
    let drawn = drawn.round().max(Vec2::ONE).min((screen * 2).as_vec2()).min(Vec2::splat(LARGEST as f32));
    let shaped = if picture.aspect.x > 0.0 && picture.aspect.y > 0.0 {
        let shape = picture.aspect.x / picture.aspect.y;
        if drawn.x / drawn.y > shape { Vec2::new(drawn.y * shape, drawn.y) } else { Vec2::new(drawn.x, drawn.x / shape) }
    } else {
        drawn
    };
    shaped.round().max(Vec2::ONE).as_uvec2()
}

/// How a camera on `window` draws its picture, or none where the picture is the window's own.
///
/// A fitted picture is enlarged or brought down by the one ratio that keeps its shape and fills
/// the window along one side, and stands in the window's middle. A stretched one is drawn over the
/// whole window, and one whose shape is the window's is the same either way, so it takes the fit's
/// plan and Bevy's own rays. A picture brought down along either side is copied smoothly whatever
/// the filter, which only says how a picture is enlarged.
pub(crate) fn plan(window: &Window, picture: &WindowPicture) -> Option<Plan> {
    let screen = window.physical_size();
    if screen.x == 0 || screen.y == 0 {
        return None;
    }
    let scale_factor = window.resolution.scale_factor();
    let size = picture_size(screen, picture);
    if size == screen {
        return None;
    }
    let filter = picture.filter.min(filter::SHARP);

    let ratio = (screen.as_vec2() / size.as_vec2()).min_element();
    let target = (screen.as_vec2() / ratio).round().as_uvec2().max(size);
    let at = (target - size) / 2;
    let enlarged = screen.as_vec2() / target.as_vec2();
    let copy = WindowCopy {
        over: screen,
        at: (at.as_vec2() * enlarged).round().as_uvec2(),
        size: (size.as_vec2() * enlarged).round().as_uvec2().min(screen),
        filter,
        down: ratio < 1.0,
    };
    let fills = copy.at == UVec2::ZERO && copy.size == screen;
    if picture.fit != fit::STRETCH || fills {
        return Some(Plan { target, scale_factor: scale_factor / ratio, at, picture: size, screen, stretch: None, copy });
    }

    // The scale factor that leaves the target's logical size the window's along the side it is
    // enlarged most and larger along the other, so every place on the window lies inside it for
    // the checks Bevy's picking makes before it takes a ray.
    let over = screen.as_vec2() / size.as_vec2();
    let stretched = scale_factor / over.max_element();
    let window_logical = screen.as_vec2() / scale_factor;
    Some(Plan {
        target: size,
        scale_factor: stretched,
        at: UVec2::ZERO,
        picture: size,
        screen,
        stretch: Some(size.as_vec2() / stretched / window_logical),
        copy: WindowCopy { over: screen, at: UVec2::ZERO, size: screen, filter, down: over.min_element() < 1.0 },
    })
}

/// The part of `window` its picture stands in, in its logical pixels from its corner, the whole
/// window where the picture is its own or stretched over it.
pub fn area(window: &Window, picture: Option<&WindowPicture>) -> Rect {
    let scale = window.resolution.scale_factor();
    match picture.and_then(|picture| plan(window, picture)) {
        Some(plan) => Rect::from_corners(
            plan.copy.at.as_vec2() / scale,
            (plan.copy.at + plan.copy.size).as_vec2() / scale,
        ),
        None => Rect::from_corners(Vec2::ZERO, Vec2::new(window.width(), window.height())),
    }
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

/// Tells each camera on a window whose picture differs from it the picture's target, and gives a
/// camera told so whose window's picture is its own again its own viewport and its window's size
/// back.
///
/// Only what differs from what the camera holds is written, so a camera stays unchanged from frame
/// to frame once it is scaled, until the window or the game changes something.
#[allow(clippy::type_complexity)]
fn scale_cameras(
    mut commands: Commands,
    primary: Query<Entity, With<PrimaryWindow>>,
    windows: Query<(&Window, Option<&WindowPicture>)>,
    mut cameras: Query<
        (Entity, &mut Camera, &RenderTarget, &mut Projection, Option<&mut ScaledCamera>),
        (Without<InterfaceView>, Without<PictureInterface>),
    >,
) {
    let primary = primary.iter().next();

    for (entity, mut camera, target, mut projection, scaled) in &mut cameras {
        let window = match target.normalize(primary) {
            Some(NormalizedRenderTarget::Window(window)) => windows.get(window.entity()).ok(),
            _ => None,
        };
        let planned = window.and_then(|(window, picture)| plan(window, picture?));

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
                let wanted = ScaledCamera { own, set, copy: plan.copy, stretch: plan.stretch };
                match scaled {
                    Some(mut scaled) => {
                        if !same(&scaled.set, &wanted.set)
                            || scaled.copy != wanted.copy
                            || !same(&scaled.own, &wanted.own)
                            || scaled.stretch != wanted.stretch
                        {
                            *scaled = wanted;
                        }
                    }
                    None => {
                        commands.entity(entity).insert(wanted);
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

/// Spawns the camera Bevy's interface is drawn on while some camera draws a picture unlike its
/// window and no ImGui camera is there to take it, and despawns it once neither holds.
///
/// Bevy draws the interface on the window's highest camera where no camera is marked as its own,
/// and this one is last, so the interface is drawn over the copied picture at the window's size. It
/// starts as nothing, is put on the window by blending, and stands on a layer of its own, since
/// every 2D camera is one the gizmos are queued into, which would draw the world's gizmos again in
/// screen space. A run whose pictures are all their windows' own spawns none, so its entities are
/// the ones it had.
fn interface_camera(
    mut commands: Commands,
    scaled: Query<(), With<ScaledCamera>>,
    imgui: Query<(), With<InterfaceView>>,
    ours: Query<Entity, With<PictureInterface>>,
) {
    let wanted = !scaled.is_empty() && imgui.is_empty();
    match (wanted, ours.iter().next()) {
        (true, None) => {
            commands.spawn((
                Camera2d,
                Camera {
                    order: isize::MAX,
                    clear_color: ClearColorConfig::Custom(bevy::color::Color::NONE),
                    output_mode: CameraOutputMode::Write {
                        blend_state: Some(BlendState::ALPHA_BLENDING),
                        clear_color: ClearColorConfig::None,
                    },
                    ..Default::default()
                },
                Tonemapping::None,
                Msaa::Off,
                interface_usages(),
                RenderLayers::layer(31),
                PictureInterface,
                Name::new("Picture interface camera"),
            ));
        }
        (false, Some(camera)) => {
            commands.entity(camera).despawn();
        }
        _ => {}
    }
}

/// Places each node marked [`OnPicture`] over the part of the first window the picture stands in,
/// in the interface's own pixels, which its scale divides the window's into.
fn place_on_picture(
    windows: Query<(&Window, Option<&WindowPicture>), With<PrimaryWindow>>,
    scale: Option<Res<UiScale>>,
    mut nodes: Query<&mut Node, With<OnPicture>>,
) {
    let Ok((window, picture)) = windows.single() else {
        return;
    };
    let rect = area(window, picture);
    let scale = scale.map_or(1.0, |scale| scale.0).max(f32::EPSILON);
    let (at, size) = (rect.min / scale, rect.size() / scale);
    let wanted = (PositionType::Absolute, Val::Px(at.x), Val::Px(at.y), Val::Px(size.x), Val::Px(size.y));
    for mut node in &mut nodes {
        if (node.position_type, node.left, node.top, node.width, node.height) != wanted {
            (node.position_type, node.left, node.top, node.width, node.height) = wanted;
        }
    }
}

/// Casts again each ray Bevy cast from a pointer on a window through a camera whose picture is
/// stretched over it, through the place on the target the stretch puts the pointer at, or takes
/// it away where that place lies outside the camera's viewport.
///
/// Bevy casts from the pointer's place on the window as the camera's logical target holds it,
/// which a stretched picture's target holds at another scale along one side. The meshes and
/// sprites under a pointer are found along these rays.
fn stretched_rays(
    rays: Option<ResMut<RayMap>>,
    primary: Query<Entity, With<PrimaryWindow>>,
    cameras: Query<(Entity, &Camera, &RenderTarget, &GlobalTransform, &ScaledCamera)>,
    pointers: Query<(&PointerId, &PointerLocation)>,
) {
    let Some(mut rays) = rays else {
        return;
    };
    let primary = primary.iter().next();

    for (entity, camera, target, transform, scaled) in &cameras {
        if scaled.stretch.is_none() || !camera.is_active {
            continue;
        }
        let Some(target) = target.normalize(primary) else {
            continue;
        };
        for (&pointer, location) in &pointers {
            let Some(location) = location.location() else {
                continue;
            };
            if location.target != target {
                continue;
            }
            let id = RayId::new(entity, pointer);
            let at = scaled.to_target(location.position);
            let inside = camera.logical_viewport_rect().map_or(Some(at), |rect| rect.contains(at).then(|| at - rect.min));
            match inside.and_then(|at| camera.viewport_to_world(transform, at).ok()) {
                Some(ray) => {
                    rays.map.insert(id, ray);
                }
                None => {
                    rays.map.remove(&id);
                }
            }
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn window(width: u32, height: u32) -> Window {
        Window {
            resolution: bevy::window::WindowResolution::new(width, height).with_scale_factor_override(1.0),
            ..Default::default()
        }
    }

    fn sized(width: u32, height: u32, filter: u32) -> WindowPicture {
        WindowPicture { size: UVec2::new(width, height), share: 1.0, aspect: Vec2::ZERO, fit: fit::FIT, filter }
    }

    #[test]
    fn a_picture_of_the_windows_shape_is_drawn_over_the_whole_window() {
        let plan = plan(&window(1920, 1080), &sized(1280, 720, filter::LINEAR)).unwrap();
        assert_eq!(plan.target, UVec2::new(1280, 720));
        assert_eq!(plan.at, UVec2::ZERO);
        let whole = WindowCopy { over: UVec2::new(1920, 1080), at: UVec2::ZERO, size: UVec2::new(1920, 1080), filter: filter::LINEAR, down: false };
        assert_eq!(plan.copy, whole);
        assert!((plan.scale_factor - 1.0 / 1.5).abs() < 1e-6);
        assert!(viewport(&plan, None).is_none());
        assert!(plan.stretch.is_none());
    }

    #[test]
    fn the_sharp_scaling_fills_the_window_as_the_other_two_do() {
        let plan = plan(&window(1920, 1080), &sized(800, 450, filter::SHARP)).unwrap();
        assert_eq!(plan.target, UVec2::new(800, 450));
        assert_eq!(plan.copy.size, UVec2::new(1920, 1080));
        assert_eq!(plan.copy.filter, filter::SHARP);
        assert!(viewport(&plan, None).is_none());
    }

    #[test]
    fn a_fitted_shape_unlike_the_windows_keeps_its_own_with_black_at_the_sides() {
        let plan = plan(&window(1920, 1080), &sized(1000, 1000, filter::NEAREST)).unwrap();
        assert_eq!(plan.target, UVec2::new(1778, 1000));
        assert_eq!(plan.at, UVec2::new(389, 0));
        assert_eq!(plan.copy.size.y, 1080);
        assert_eq!(plan.copy.filter, filter::NEAREST);
        assert!(plan.stretch.is_none());
    }

    #[test]
    fn an_aspect_cuts_the_largest_picture_of_its_shape_from_the_size() {
        let four_by_three = WindowPicture { aspect: Vec2::new(4.0, 3.0), ..sized(1920, 1080, filter::LINEAR) };
        let plan = plan(&window(1920, 1080), &four_by_three).unwrap();
        assert_eq!(plan.picture, UVec2::new(1440, 1080));
        assert_eq!((plan.copy.at, plan.copy.size), (UVec2::new(240, 0), UVec2::new(1440, 1080)));
        let rect = area(&window(1920, 1080), Some(&four_by_three));
        assert_eq!((rect.min, rect.max), (Vec2::new(240.0, 0.0), Vec2::new(1680.0, 1080.0)));
    }

    #[test]
    fn a_stretched_picture_covers_the_window_and_moves_the_pointer_along_its_narrower_side() {
        let stretched = WindowPicture { aspect: Vec2::new(4.0, 3.0), fit: fit::STRETCH, ..sized(1920, 1080, filter::LINEAR) };
        let plan = plan(&window(1920, 1080), &stretched).unwrap();
        assert_eq!(plan.target, UVec2::new(1440, 1080));
        assert_eq!((plan.copy.at, plan.copy.size), (UVec2::ZERO, UVec2::new(1920, 1080)));
        assert!(viewport(&plan, None).is_none());
        // The target's logical size is 1920 by 1440, so the window's right edge is the target's
        // and its bottom edge three quarters down the target.
        let stretch = plan.stretch.unwrap();
        assert!((stretch - Vec2::new(1.0, 4.0 / 3.0)).abs().max_element() < 1e-5);
        assert!((plan.scale_factor - 0.75).abs() < 1e-6);
        let rect = area(&window(1920, 1080), Some(&stretched));
        assert_eq!((rect.min, rect.max), (Vec2::ZERO, Vec2::new(1920.0, 1080.0)));
    }

    #[test]
    fn a_stretch_of_the_windows_own_shape_is_the_fit() {
        let stretched = WindowPicture { fit: fit::STRETCH, ..sized(960, 540, filter::LINEAR) };
        let plan = plan(&window(1920, 1080), &stretched).unwrap();
        assert!(plan.stretch.is_none());
        assert_eq!(plan.target, UVec2::new(960, 540));
    }

    #[test]
    fn a_share_follows_the_window_and_a_picture_above_it_is_brought_down_to_twice_at_most() {
        let share = |share: f32| WindowPicture { size: UVec2::ZERO, share, ..sized(0, 0, filter::SHARP) };
        assert_eq!(plan(&window(1920, 1080), &share(0.5)).unwrap().target, UVec2::new(960, 540));
        assert_eq!(plan(&window(1280, 720), &share(0.5)).unwrap().target, UVec2::new(640, 360));
        let above = plan(&window(1920, 1080), &share(1.5)).unwrap();
        assert_eq!(above.target, UVec2::new(2880, 1620));
        assert!(above.copy.down);
        assert!((above.scale_factor - 1.5).abs() < 1e-6);
        assert_eq!(plan(&window(1920, 1080), &sized(9000, 9000, filter::LINEAR)).unwrap().picture, UVec2::new(3840, 2160));
    }

    #[test]
    fn a_picture_of_the_windows_own_size_is_not_scaled_in_any_mode() {
        assert!(plan(&window(1920, 1080), &sized(1920, 1080, filter::LINEAR)).is_none());
        let share = WindowPicture { size: UVec2::ZERO, share: 1.0, ..sized(0, 0, filter::LINEAR) };
        assert!(plan(&window(1280, 720), &share).is_none());
        let mut windowed = window(1280, 720);
        windowed.mode = bevy::window::WindowMode::Windowed;
        assert!(plan(&windowed, &sized(640, 360, filter::LINEAR)).is_some());
    }

    #[test]
    fn a_games_own_viewport_is_scaled_into_where_the_picture_stands() {
        let plan = plan(&window(1920, 1080), &sized(1000, 1000, filter::SHARP)).unwrap();
        let own = Viewport { physical_position: UVec2::new(960, 0), physical_size: UVec2::new(960, 1080), depth: 0.0..1.0 };
        let set = viewport(&plan, Some(&own)).unwrap();
        assert_eq!((set.physical_position, set.physical_size), (UVec2::new(889, 0), UVec2::new(500, 1000)));
    }
}
