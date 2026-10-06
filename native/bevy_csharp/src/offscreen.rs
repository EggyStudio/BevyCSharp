//! The image an offscreen run draws into, the cameras pointed at it, and the camera its
//! interface is drawn on.

#[cfg(feature = "render")]
use bevy::app::{App, First};
#[cfg(feature = "render")]
use bevy::prelude::Resource;

/// The image a windowless run draws into.
///
/// Present only in an offscreen run, so its absence says a capture should read the window instead.
/// [`crate::render::scene::bcs_render_screenshot`] asks it that question.
#[cfg(feature = "render")]
#[derive(Resource)]
pub struct OffscreenTarget {
    /// The texture every camera is pointed at, and the one a capture is read back from.
    pub image: bevy::asset::Handle<bevy::image::Image>,
}

/// Creates the image an offscreen run draws into, and points the cameras at it.
///
/// Two steps rather than one, because the image is an asset and a camera is spawned by whatever
/// managed code asked for one, at whatever point in the run it asked. Creating the target once at
/// startup and re-pointing cameras every frame covers a camera the app spawns later, and one the
/// interface spawns for itself, without either having to know how this run is drawing.
///
/// The image is made before startup, in PreStartup, because it is also the run's answer to how
/// large the window is, and a game's own startup system lays out what it spawns by that answer.
/// A resource inserted by a startup system's commands lands only once the schedule applies them,
/// so made in Startup it would be missing for a startup system asking the size.
///
/// Public for `native/stress`, which runs Bevy's own stress tests drawn as an offscreen run here
/// is drawn, so a test is measured beside its C# version with nothing between them but the bridge.
#[cfg(feature = "render")]
pub fn install_offscreen_target(app: &mut App, width: u32, height: u32) {
    use bevy::asset::Assets;
    use bevy::camera::{Camera, RenderTarget};
    use bevy::ecs::change_detection::DetectChangesMut;
    use bevy::ecs::query::With;
    use bevy::ecs::schedule::IntoScheduleConfigs;
    use bevy::ecs::system::{Commands, Query, Res, ResMut};
    use bevy::image::Image;
    use bevy::window::WindowRef;

    app.add_systems(
        bevy::app::PreStartup,
        move |mut commands: Commands, mut images: ResMut<Assets<Image>>| {
            // The same image a camera is given one of when a portal or a minimap asks for one.
            // What makes this one the run's is that every camera is pointed at it below.
            let image = crate::render::assets::target_image(width, height);

            commands.insert_resource(OffscreenTarget {
                image: images.add(image),
            });
        },
    );

    // Every camera still pointed at the window pointed at the image instead.
    fn point_cameras_at_image(
        target: Option<Res<OffscreenTarget>>,
        mut cameras: Query<(&mut RenderTarget, &mut bevy::camera::Projection), With<Camera>>,
    ) {
        let Some(target) = target else {
            return;
        };

        for (mut render_target, mut projection) in &mut cameras {
            // Read through the shared borrow, so a camera that is already pointed at the image is
            // not marked changed by the asking, every frame, for the life of the run.
            let current: &RenderTarget = &render_target;
            if !matches!(current, RenderTarget::Window(WindowRef::Primary)) {
                continue;
            }

            *render_target = RenderTarget::Image(target.image.clone().into());

            // Bevy works out a camera's target size when the camera is added or its projection
            // changes, and not when its target does. A camera spawned at startup is pointed here
            // before that first look, but one spawned while the run goes on was added pointing at a
            // window that is not there, found nothing to size, and would draw nothing for the rest
            // of the run. Marking the projection changed has the target looked at again.
            projection.set_changed();
        }
    }

    app.add_systems(
        First,
        // Chosen once the cameras are pointed at the image, which is how it tells the run's cameras
        // from the rest, so a camera spawned at startup draws the interface from the first frame.
        // Looking first, it would find that camera still pointed at the window, and the interface
        // would be laid out that frame against no camera, at no size, where a node with a margin
        // inside one that stretches comes out smaller than nothing and Bevy's border radius asserts
        // on it.
        (point_cameras_at_image, choose_offscreen_ui_camera).chain(),
    );
}

/// Marks the camera an offscreen run's interface is drawn on, which this bridge chose rather than
/// a game.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component)]
struct OffscreenUiCamera;

/// Gives an offscreen run's interface the camera a window's would have had.
///
/// Bevy draws a node that names no camera on the highest camera drawing to the primary window,
/// and an offscreen run has none, since every camera is pointed at an image instead, so a game's
/// menu and its count of coins would be laid out nowhere and a test of the game would never see
/// them. Marking the highest of the cameras drawing to the run's image as the default answers as a
/// window would, and moves the mark when a higher one is spawned or the marked one goes. One the
/// game marked itself is left alone, since Bevy allows one mark and the game's choice is the
/// game's to make.
#[cfg(feature = "render")]
fn choose_offscreen_ui_camera(
    mut commands: bevy::ecs::system::Commands,
    target: Option<bevy::ecs::system::Res<OffscreenTarget>>,
    cameras: bevy::ecs::system::Query<(
        bevy::ecs::entity::Entity,
        &bevy::camera::Camera,
        &bevy::camera::RenderTarget,
        Option<&OffscreenUiCamera>,
        bevy::ecs::query::Has<bevy::ui::IsDefaultUiCamera>,
    )>,
) {
    use bevy::camera::RenderTarget;

    let Some(target) = target else {
        return;
    };

    // A mark without this bridge's beside it is the game's own.
    if cameras.iter().any(|(_, _, _, ours, marked)| marked && ours.is_none()) {
        return;
    }

    let best = cameras
        .iter()
        .filter(|(_, camera, render_target, _, _)| {
            camera.is_active
                && matches!(render_target, RenderTarget::Image(image) if image.handle == target.image)
        })
        .max_by_key(|(entity, camera, _, _, _)| (camera.order, *entity))
        .map(|(entity, ..)| entity);

    for (entity, _, _, ours, _) in &cameras {
        if ours.is_some() && Some(entity) != best {
            commands
                .entity(entity)
                .remove::<(OffscreenUiCamera, bevy::ui::IsDefaultUiCamera)>();
        }
    }

    if let Some(entity) = best
        && cameras.get(entity).is_ok_and(|(_, _, _, ours, _)| ours.is_none())
    {
        commands
            .entity(entity)
            .insert((OffscreenUiCamera, bevy::ui::IsDefaultUiCamera));
    }
}
