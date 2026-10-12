//! The window, after it has opened.
//!
//! `BcsConfig` decides how the window is created; everything here changes it while the app runs.
//! The pointer over it is [`cursor`]'s.
//!
//! Every entry point takes the window it addresses, as the bits of the entity carrying Bevy's
//! `Window`, or [`PRIMARY`] for the primary window, which C#'s calls naming no window pass. A
//! headless run has none, and says so rather than silently doing nothing.

#[cfg(feature = "render")]
use bevy::ecs::query::With;
#[cfg(feature = "render")]
use bevy::window::{
    CursorOptions, MonitorSelection, PrimaryWindow, Window, WindowMode,
};

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

pub mod cursor;
pub mod monitors;
pub mod updates;
#[cfg(feature = "render")]
mod alpha;

#[cfg(feature = "render")]
pub use alpha::{install, pick_alpha};

/// The bits C# passes for the primary window, which no entity has, since an entity's index never
/// fills a whole `u32`. Zero would not do, because it is C#'s `Entity.None` and Bevy's first
/// entity alike.
pub const PRIMARY: u64 = u64::MAX;

/// The entity of the window C# named, the primary one for [`PRIMARY`], or nothing where the entity
/// carries no `Window`.
#[cfg(feature = "render")]
pub(crate) fn window_entity(world: &mut bevy::ecs::world::World, window: u64) -> Option<bevy::ecs::entity::Entity> {
    if window == PRIMARY {
        let mut query = world.query_filtered::<bevy::ecs::entity::Entity, With<PrimaryWindow>>();
        return query.single(world).ok();
    }

    let entity = crate::ecs::entity_from(window);
    world.get_entity(entity).ok()?.contains::<Window>().then_some(entity)
}

/// Runs `f` against the window C# named, or reports why it could not.
#[cfg(feature = "render")]
fn with_window<F>(window: u64, f: F) -> i32
where
    F: FnOnce(&mut Window, &mut CursorOptions) -> i32,
{
    with_world(|world| {
        let Some(entity) = window_entity(world, window) else {
            return status::NOT_PRESENT;
        };
        let mut query = world.query::<(&mut Window, &mut CursorOptions)>();
        match query.get_mut(world, entity) {
            Ok((mut window, mut cursor)) => f(&mut window, &mut cursor),
            Err(_) => status::NOT_PRESENT,
        }
    })
}

/// Sets the window's title.
///
/// # Safety
/// `title` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_window_set_title(window: u64, title: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, title);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(title) = (unsafe { crate::interop::cstr_to_string(title) }) else {
                return status::NULL_ARG;
            };

            with_window(window, |window, _| {
                window.title = title.clone();
                status::OK
            })
        }
    })
}

/// Resizes the window, in logical pixels.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_size(window: u64, width: u32, height: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, width, height);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if width == 0 || height == 0 {
                return status::NULL_ARG;
            }

            with_window(window, |window, _| {
                window.resolution.set(width as f32, height as f32);
                status::OK
            })
        }
    })
}

/// How many physical pixels a logical one is.
///
/// What the desktop's scaling is set to. Everything the bridge reports about a window is in logical
/// pixels, so this turns one into the units the framebuffer is actually divided into.
///
/// # Safety
/// `scale` must point to a writable float.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_window_scale(window: u64, scale: *mut f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, scale);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if scale.is_null() {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| {
                let Some(found) = window_entity(world, window).and_then(|entity| world.get::<Window>(entity)) else {
                    // An offscreen run draws into an image, and an image has pixels rather than a
                    // desktop's opinion about how big a pixel is. Bevy says the same by giving
                    // `ImageRenderTarget` a scale factor of 1.0. A window C# named is not that
                    // image, so one that is missing is missing.
                    if window == PRIMARY && world.contains_resource::<crate::app::OffscreenTarget>() {
                        unsafe { scale.write(1.0) };
                        return status::OK;
                    }

                    return if window == PRIMARY { status::INVALID_STATE } else { status::NOT_PRESENT };
                };

                unsafe { scale.write(found.resolution.scale_factor()) };
                status::OK
            })
        }
    })
}

/// Writes the primary window's entity, so the components Bevy keeps on it, such as its
/// `CursorOptions` and its `Window`, can be read and written by reflection.
///
/// The entry points here cover what a game commonly changes, and a field none of them reaches,
/// such as whether the pointer passes through the window, is still on the entity. An offscreen
/// run draws into an image and has no window entity, and says so.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_window_entity(out: *mut u64) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let mut query = world.query_filtered::<bevy::ecs::entity::Entity, With<PrimaryWindow>>();
                match query.single(world) {
                    Ok(entity) => {
                        unsafe { out.write(entity.to_bits()) };
                        status::OK
                    }
                    Err(_) => status::NOT_PRESENT,
                }
            })
        }
    })
}

/// Writes the window's current size, in logical pixels.
///
/// The size the window ended up at, which is not always the size that was asked for, because a
/// window manager may refuse, and a fullscreen window takes the monitor's.
///
/// # Safety
/// `width` and `height` must be writable, or null to skip that output.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_window_size(window: u64, width: *mut u32, height: *mut u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, width, height);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            // The size of whatever is being drawn into. A window has one, and so does the image an
            // offscreen run draws into instead, and everything that lays out an interface needs an
            // answer rather than needing to know which kind of run this is. The rest of this
            // module is about a window and has nothing to say without one.
            let answered = with_window(window, |window, _| {
                if !width.is_null() {
                    unsafe { width.write(window.resolution.width() as u32) };
                }
                if !height.is_null() {
                    unsafe { height.write(window.resolution.height() as u32) };
                }
                status::OK
            });

            if answered != status::NOT_PRESENT || window != PRIMARY {
                return answered;
            }

            with_world(|world| {
                use bevy::asset::Assets;
                use bevy::image::Image;

                let Some(target) = world.get_resource::<crate::app::OffscreenTarget>() else {
                    return status::NOT_PRESENT;
                };

                let handle = target.image.clone();
                let Some(images) = world.get_resource::<Assets<Image>>() else {
                    return status::NOT_PRESENT;
                };

                let Some(image) = images.get(&handle) else {
                    return status::NOT_PRESENT;
                };

                let size = image.texture_descriptor.size;

                if !width.is_null() {
                    unsafe { width.write(size.width) };
                }
                if !height.is_null() {
                    unsafe { height.write(size.height) };
                }

                status::OK
            })
        }
    })
}

/// Sets how the window fills the screen.
///
/// `0` windowed, `1` borderless fullscreen, `2` exclusive fullscreen. Both fullscreen modes take
/// the monitor the window is on. Exclusive takes that monitor's current video mode rather than
/// asking for a different resolution, which avoids a mode switch the compositor has to undo on
/// every alt-tab.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_mode(window: u64, mode: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, mode);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let requested = match mode {
                0 => WindowMode::Windowed,
                1 => WindowMode::BorderlessFullscreen(MonitorSelection::Current),
                2 => WindowMode::Fullscreen(
                    MonitorSelection::Current,
                    bevy::window::VideoModeSelection::Current,
                ),
                _ => return status::NULL_ARG,
            };

            with_window(window, |window, _| {
                window.mode = requested;
                status::OK
            })
        }
    })
}

/// Moves the window, in physical pixels from the desktop's top-left corner.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_position(window: u64, x: i32, y: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, x, y);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::WindowPosition;

            with_window(window, |window, _| {
                window.position = WindowPosition::At(bevy::math::IVec2::new(x, y));
                status::OK
            })
        }
    })
}

/// Sets whether the window has a title bar and border, whether it can be resized by dragging, and
/// whether it stays above other windows.
///
/// The three travel together because each is one flag, and a call that set only one would have to
/// read the other two back first to leave them alone.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_style(window: u64, decorations: i32, resizable: i32, always_on_top: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, decorations, resizable, always_on_top);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::WindowLevel;

            with_window(window, |window, _| {
                window.decorations = decorations != 0;
                window.resizable = resizable != 0;
                window.window_level = if always_on_top != 0 {
                    WindowLevel::AlwaysOnTop
                } else {
                    WindowLevel::Normal
                };
                status::OK
            })
        }
    })
}

/// Minimizes the window to the taskbar or dock.
///
/// A window without the platform's title bar has no button of its own for this, so an app that
/// draws its own title bar calls this from one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_minimize(window: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = window;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_window(window, |window, _| {
                window.set_minimized(true);
                status::OK
            })
        }
    })
}

/// Maximizes the window to fill the screen less the taskbar, or with `0` puts it back to the size
/// it had before.
///
/// Apart from borderless fullscreen, which covers the taskbar too and is a mode rather than a size.
/// The platform does the work and remembers the size to go back to, so nothing here keeps one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_maximized(window: u64, maximized: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, maximized);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_window(window, |window, _| {
                window.set_maximized(maximized != 0);
                status::OK
            })
        }
    })
}

/// Writes where the window is, how large, and whether it is maximized.
///
/// What a game keeps to reopen where it was closed. The position is Bevy's, which it updates as
/// the platform reports a move, so on Wayland, which never reports one, `has_position` stays zero.
/// Bevy does not keep whether a window is maximized, so that is asked of winit, which is reachable
/// only from the main thread, where a system callback runs.
///
/// # Safety
/// `place` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_window_place(window: u64, place: *mut crate::interop::BcsWindowPlace) -> i32 {
    crate::interop::guard(|| {
        if place.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = window;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::WindowPosition;

            with_world(|world| {
                // Read without `mut`, so asking every frame does not mark the window changed and
                // send it back through Bevy's window sync for nothing.
                let Some(entity) = window_entity(world, window) else {
                    return status::NOT_PRESENT;
                };
                let Some(window) = world.get::<Window>(entity) else {
                    return status::NOT_PRESENT;
                };

                let (has_position, x, y) = match window.position {
                    WindowPosition::At(at) => (1, at.x, at.y),
                    _ => (0, 0, 0),
                };

                let maximized = bevy::winit::WINIT_WINDOWS.with_borrow(|windows| {
                    windows.get_window(entity).is_some_and(|window| window.is_maximized())
                });

                unsafe {
                    place.write(crate::interop::BcsWindowPlace {
                        has_position,
                        x,
                        y,
                        width: window.resolution.width() as u32,
                        height: window.resolution.height() as u32,
                        maximized: maximized as i32,
                    })
                };

                status::OK
            })
        }
    })
}

/// Hands the window to the platform to be moved by the pointer, from a button that is held now
/// until it is let go.
///
/// How a window without a title bar is dragged. The platform moves it rather than the app, since
/// only the platform knows where the window may go, and on Wayland an app is not told where its
/// window is at all. Called on the press, while the button is down, or the platform ignores it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_start_drag_move(window: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = window;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let started = with_window(window, |window, _| {
                window.start_drag_move();
                status::OK
            });

            if started == status::OK {
                release_after_drag(window);
            }

            started
        }
    })
}

/// Hands the window to the platform to be resized by the pointer from one edge or corner, from a
/// button that is held now until it is let go.
///
/// `edge` counts clockwise from the top: `0` north, `1` north-east, `2` east, `3` south-east,
/// `4` south, `5` south-west, `6` west, `7` north-west. A window without the platform's border
/// has no edge the platform will resize it by, so an app that draws its own frame calls this from
/// the edges of it. Called on the press, like a drag move.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_start_drag_resize(window: u64, edge: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, edge);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::math::CompassOctant;

            let direction = match edge {
                0 => CompassOctant::North,
                1 => CompassOctant::NorthEast,
                2 => CompassOctant::East,
                3 => CompassOctant::SouthEast,
                4 => CompassOctant::South,
                5 => CompassOctant::SouthWest,
                6 => CompassOctant::West,
                7 => CompassOctant::NorthWest,
                _ => return status::NULL_ARG,
            };

            let started = with_window(window, |window, _| {
                window.start_drag_resize(direction);
                status::OK
            });

            if started == status::OK {
                release_after_drag(window);
            }

            started
        }
    })
}

/// Lets the left button go, as far as the app is concerned, once the platform has taken a drag.
///
/// The platform moves or resizes the window with the button that started it, and on most of them
/// the button's release goes to the platform and never reaches the app. Without this the app
/// believes the button is still held, so the next press on the window is not a press at all,
/// since a button already down cannot go down again, and every second click on a title bar does
/// nothing. Whatever was being hovered when the drag began also stays active, which keeps its
/// pointer shape after the drag is over. A release the platform does deliver later lands on a
/// button already up and changes nothing.
#[cfg(feature = "render")]
fn release_after_drag(window: u64) {
    use bevy::input::ButtonState;
    use bevy::input::mouse::{MouseButton, MouseButtonInput};
    use bevy::prelude::ButtonInput;

    with_world(|world| {
        let Some(window) = window_entity(world, window) else {
            return status::NOT_PRESENT;
        };

        let released = MouseButtonInput {
            button: MouseButton::Left,
            state: ButtonState::Released,
            window,
        };

        world.write_message(released.clone());
        world.write_message(bevy::window::WindowEvent::MouseButtonInput(released));

        if let Some(mut buttons) = world.get_resource_mut::<ButtonInput<MouseButton>>() {
            buttons.release(MouseButton::Left);
        }

        status::OK
    });
}

/// Turns the platform's input method on or off for the window, and says where the text being
/// composed is, in logical pixels from the window's top left.
///
/// An input method (IME) is how Japanese, Chinese or Korean is typed. Keys build up a candidate
/// that is shown and chosen from before it becomes text. It is off unless asked for, because with
/// it on the keys a game reads as movement are taken by the input method instead. The position
/// places the platform's candidate list beside the field being typed into rather than in a corner.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_ime(window: u64, enabled: i32, x: f32, y: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, enabled, x, y);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_window(window, |window, _| {
                window.ime_enabled = enabled != 0;
                window.ime_position = bevy::math::Vec2::new(x, y);
                status::OK
            })
        }
    })
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use bevy::prelude::World;
    use bevy::window::{PrimaryWindow, Window};

    use crate::interop::status;
    use crate::state::loan_world;

    /// A window past the first is addressed by its entity and the first by [`super::PRIMARY`],
    /// each changing only itself, and an entity with no window is refused rather than taken for
    /// the first.
    #[test]
    fn each_window_is_addressed_by_its_entity() {
        let mut world = World::new();
        let first = world.spawn((Window::default(), PrimaryWindow)).id();
        let second = world.spawn(Window::default()).id();
        let plain = world.spawn_empty().id();

        let title = |world: &mut World, window: u64, text: &core::ffi::CStr| {
            loan_world(world, || unsafe { super::bcs_window_set_title(window, text.as_ptr()) })
        };

        assert_eq!(title(&mut world, second.to_bits(), c"Second"), status::OK);
        assert_eq!(title(&mut world, super::PRIMARY, c"First"), status::OK);
        assert_eq!(title(&mut world, plain.to_bits(), c"Plain"), status::NOT_PRESENT);

        assert_eq!(world.get::<Window>(first).map(|window| window.title.as_str()), Some("First"));
        assert_eq!(world.get::<Window>(second).map(|window| window.title.as_str()), Some("Second"));

        assert_eq!(loan_world(&mut world, || super::bcs_window_set_size(second.to_bits(), 300, 200)), status::OK);
        let (mut width, mut height) = (0u32, 0u32);
        assert_eq!(loan_world(&mut world, || unsafe { super::bcs_window_size(second.to_bits(), &mut width, &mut height) }), status::OK);
        assert_eq!((width, height), (300, 200));
        assert_eq!(world.get::<Window>(first).map(|window| window.resolution.width() as u32), Some(Window::default().resolution.width() as u32));
    }
}
