//! Windows a game opens beside the first, the cameras pointed at them, and how an offscreen run
//! draws them.
//!
//! A window is Bevy's `Window` component on an entity of its own, which a game spawns through
//! reflection as it adds any of Bevy's components, the way Bevy's `multiple_windows` spawns one.
//! What reflection cannot reach is a camera's `RenderTarget` naming that entity, because the entity
//! inside `WindowRef::Entity` is not a field it lists, so pointing a camera at a window is a call of
//! its own here, beside the one that points a camera at an image.
//!
//! An offscreen run opens no window at all, the first included, and draws into an image instead
//! ([`crate::offscreen`]). A window spawned in one draws into an image of its own in the same way,
//! at the size and the scale the window has, so a game with two windows runs the same with no
//! display and each window's picture can still be captured and looked at.

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

/// What each window a game spawned draws into in an offscreen run, by the window's entity, its
/// image and the scale factor the window had.
///
/// Inserted with the offscreen run's own image, so its absence says the windows are real ones.
/// The scale is kept with the image rather than read again from the window, because Bevy tells
/// one picture drawn into an image from another by the two together, and a capture of the window
/// has to name the same pair its cameras draw into or it is given a picture of its own that
/// nothing draws.
#[cfg(feature = "render")]
#[derive(bevy::prelude::Resource, Default)]
pub struct OffscreenWindows(
    pub std::collections::HashMap<bevy::ecs::entity::Entity, bevy::camera::ImageRenderTarget>,
);

/// Gives each window a game spawned in an offscreen run an image, and points the cameras aimed at
/// the window at its image instead.
///
/// The image is made the first frame the window is seen, at its physical size, which is the size
/// a camera aimed at the window would have drawn at, and with the window's scale factor, so an
/// interface is laid out for the window it was meant for. A window resized or scaled later keeps
/// the image and the scale it was given. A window despawned lets its image go.
#[cfg(feature = "render")]
pub(crate) fn draw_windows_offscreen(
    mut drawn: bevy::ecs::system::ResMut<OffscreenWindows>,
    mut images: bevy::ecs::system::ResMut<bevy::asset::Assets<bevy::image::Image>>,
    windows: bevy::ecs::system::Query<
        (bevy::ecs::entity::Entity, &bevy::window::Window),
        bevy::ecs::query::Without<bevy::window::PrimaryWindow>,
    >,
    mut cameras: bevy::ecs::system::Query<
        (&mut bevy::camera::RenderTarget, &mut bevy::camera::Projection),
        bevy::ecs::query::With<bevy::camera::Camera>,
    >,
) {
    use bevy::camera::{ImageRenderTarget, RenderTarget};
    use bevy::ecs::change_detection::DetectChangesMut;
    use bevy::window::WindowRef;

    drawn.0.retain(|entity, _| windows.contains(*entity));
    for (entity, window) in &windows {
        drawn.0.entry(entity).or_insert_with(|| ImageRenderTarget {
            handle: images.add(crate::render::images::target_image(
                window.physical_width().max(1),
                window.physical_height().max(1),
            )),
            scale_factor: window.scale_factor(),
        });
    }

    for (mut render_target, mut projection) in &mut cameras {
        // Read through the shared borrow, as the offscreen run's own pointing does, so a camera
        // already pointed at an image is not marked changed every frame by the asking.
        let current: &RenderTarget = &render_target;
        let RenderTarget::Window(WindowRef::Entity(window)) = current else {
            continue;
        };

        let Some(image) = drawn.0.get(window) else {
            continue;
        };

        *render_target = RenderTarget::Image(image.clone());

        // Bevy sizes a camera's target when its projection changes and not when its target does,
        // so a camera aimed at the window before this frame is sized again for the image.
        projection.set_changed();
    }
}

/// Points a camera at a window a game spawned, Bevy's `RenderTarget::Window(WindowRef::Entity)`.
///
/// The window is any entity carrying Bevy's `Window`, the first one's included. In an offscreen
/// run the camera draws into the image that stands for the window, from the next frame on.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_camera_window(camera: u64, window: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, window);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::RenderTarget;
            use bevy::ecs::entity::Entity;
            use bevy::window::WindowRef;

            let (camera, window) = (Entity::from_bits(camera), Entity::from_bits(window));

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, camera) {
                    return refusal;
                }

                if let Some(refusal) = refuse_unless_window(world, window) {
                    return refusal;
                }

                crate::render::layers::clear(world, camera);
                world.entity_mut(camera).insert(RenderTarget::Window(WindowRef::Entity(window)));
                status::OK
            })
        }
    })
}

/// Writes what a window a game spawned shows to a PNG file.
///
/// The window's own picture where it is a real one, and the image standing for it in an offscreen
/// run, which it has from the frame after it is spawned. As with any capture the file appears a
/// frame or two later, once the picture has come back off the GPU.
///
/// # Safety
/// `path` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_window_screenshot(path: *const core::ffi::c_char, window: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (path, window);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::RenderTarget;
            use bevy::render::view::screenshot::{save_to_disk, Screenshot};

            let Some(path) = (unsafe { crate::interop::cstr_to_string(path) }) else {
                return status::NULL_ARG;
            };
            let window = bevy::ecs::entity::Entity::from_bits(window);

            with_world(|world| {
                if let Some(refusal) = refuse_unless_window(world, window) {
                    return refusal;
                }

                let capture = match world.get_resource::<OffscreenWindows>() {
                    // Not given its image yet, which it is the first frame it is seen.
                    Some(drawn) => match drawn.0.get(&window) {
                        Some(image) => Screenshot(RenderTarget::Image(image.clone())),
                        None => return status::NOT_PRESENT,
                    },
                    None => Screenshot::window(window),
                };

                world.spawn(capture).observe(save_to_disk(path));
                status::OK
            })
        }
    })
}

/// Why `entity` is not a window, or `None` when it is one.
#[cfg(feature = "render")]
fn refuse_unless_window(world: &bevy::ecs::world::World, entity: bevy::ecs::entity::Entity) -> Option<i32> {
    match world.get_entity(entity) {
        Err(_) => Some(status::NO_ENTITY),
        Ok(found) if !found.contains::<bevy::window::Window>() => Some(status::NO_COMPONENT),
        Ok(_) => None,
    }
}
