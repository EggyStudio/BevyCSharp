//! The pointer over the window, held in place or hidden for a camera that reads the mouse's
//! movement, shown as one of the platform's shapes, or drawn from an image of the game's own.

#[cfg(feature = "render")]
use bevy::ecs::query::With;
#[cfg(feature = "render")]
use bevy::window::{CursorGrabMode, PrimaryWindow};

#[cfg(feature = "render")]
use super::with_window;

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

/// Sets whether the cursor is confined or hidden.
///
/// `grab` is `0` to leave the cursor free, `1` to confine it to the window, `2` to lock it in
/// place. A first-person camera needs locking, because it reads how far the mouse moved rather than
/// where it is, and a free cursor stops moving at the edge of the screen.
///
/// Platforms differ in which they support. Windows confines and macOS locks, and each emulates the
/// other. Asking for one and getting the other is normal, and is why the cursor should be hidden
/// while it is grabbed either way.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_cursor(grab: i32, visible: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (grab, visible);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let requested = match grab {
                0 => CursorGrabMode::None,
                1 => CursorGrabMode::Confined,
                2 => CursorGrabMode::Locked,
                _ => return status::NULL_ARG,
            };

            with_window(|_, cursor| {
                cursor.grab_mode = requested;
                cursor.visible = visible != 0;
                status::OK
            })
        }
    })
}

/// Sets the shape of the pointer while it is over the window.
///
/// `0` the platform's arrow, `1` a text caret, `2` a hand for something to press, `3` four arrows
/// for moving, `4` a no-entry sign, `5` left and right, `6` up and down, `7` the diagonal from the
/// bottom left to the top right, `8` the other diagonal, `9` an open hand, `10` a closed one, `11`
/// the platform's sign to wait, and `12` its sign of work going on that still takes clicks. The
/// shapes an interface asks for as the pointer crosses what it draws, which makes a field
/// read as one to type in and an edge as one to drag, and the two a game shows while it is busy.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_cursor_shape(shape: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = shape;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::{CursorIcon, SystemCursorIcon};

            let icon = match shape {
                0 => SystemCursorIcon::Default,
                1 => SystemCursorIcon::Text,
                2 => SystemCursorIcon::Pointer,
                3 => SystemCursorIcon::Move,
                4 => SystemCursorIcon::NotAllowed,
                5 => SystemCursorIcon::EwResize,
                6 => SystemCursorIcon::NsResize,
                7 => SystemCursorIcon::NeswResize,
                8 => SystemCursorIcon::NwseResize,
                9 => SystemCursorIcon::Grab,
                10 => SystemCursorIcon::Grabbing,
                11 => SystemCursorIcon::Wait,
                12 => SystemCursorIcon::Progress,
                _ => return status::NULL_ARG,
            };

            with_world(|world| {
                let mut windows = world.query_filtered::<bevy::ecs::entity::Entity, With<PrimaryWindow>>();

                let Ok(window) = windows.single(world) else {
                    return status::NOT_PRESENT;
                };

                let wanted = CursorIcon::System(icon);

                // Only when it differs, since an interface asks every frame and an insert marks the
                // component changed, which winit answers by setting the platform's cursor again.
                if world.get::<CursorIcon>(window) != Some(&wanted) {
                    world.entity_mut(window).insert(wanted);
                }

                status::OK
            })
        }
    })
}

/// Sets the pointer over the window to an image, its hotspot, the pixel that points, `x` across and
/// `y` down from its top left corner.
///
/// Bevy's `CustomCursor::Image`, which needs eight bits a channel of color and alpha or thirty-two
/// bit floats, as a PNG gives, and draws nothing until the image has loaded. The platform shows it
/// at the size it is, and a hotspot outside it is refused by the platform as the image arrives.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_cursor_image(image: i32, x: u32, y: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (image, x, y);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::{CursorIcon, CustomCursor, CustomCursorImage};

            with_world(|world| {
                let handle = match crate::render::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };

                let mut windows = world.query_filtered::<bevy::ecs::entity::Entity, With<PrimaryWindow>>();
                let Ok(window) = windows.single(world) else {
                    return status::NOT_PRESENT;
                };

                let hotspot = (x.min(u16::MAX as u32) as u16, y.min(u16::MAX as u32) as u16);
                let wanted = CursorIcon::Custom(CustomCursor::Image(CustomCursorImage {
                    handle,
                    hotspot,
                    ..Default::default()
                }));

                // Only when it differs, as for a shape, since an insert has winit set it again.
                if world.get::<CursorIcon>(window) != Some(&wanted) {
                    world.entity_mut(window).insert(wanted);
                }

                status::OK
            })
        }
    })
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use bevy::asset::Handle;
    use bevy::image::Image;
    use bevy::prelude::World;
    use bevy::window::{CursorIcon, CustomCursor, PrimaryWindow, Window};

    use crate::interop::status;
    use crate::state::loan_world;

    #[test]
    fn a_cursor_image_is_put_on_the_window_with_its_hotspot() {
        let mut world = World::new();
        let image: Handle<Image> = Handle::default();
        let key = crate::assets::key_for(&mut world, image.clone().untyped());

        // No window to put it on yet.
        let answer = loan_world(&mut world, || super::bcs_window_set_cursor_image(key, 128, 64));
        assert_eq!(answer, status::NOT_PRESENT);

        let window = world.spawn((Window::default(), PrimaryWindow)).id();
        let answer = loan_world(&mut world, || super::bcs_window_set_cursor_image(key, 128, 64));
        assert_eq!(answer, status::OK);

        match world.get::<CursorIcon>(window) {
            Some(CursorIcon::Custom(CustomCursor::Image(cursor))) => {
                assert_eq!(cursor.handle, image);
                assert_eq!(cursor.hotspot, (128, 64));
            }
            other => panic!("the window's pointer is {other:?}"),
        }
    }
}
