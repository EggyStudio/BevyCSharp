//! The window, after it has opened.
//!
//! `BcsConfig` decides how the window is created; everything here changes it while the app runs.
//! Cursor grab is the one that blocks something outright rather than merely being convenient,
//! because a first-person camera cannot work without it.
//!
//! Every entry point addresses the primary window. A headless run has none, and says so rather
//! than silently doing nothing.

#[cfg(feature = "render")]
use bevy::ecs::query::With;
#[cfg(feature = "render")]
use bevy::window::{
    CursorGrabMode, CursorOptions, MonitorSelection, PrimaryWindow, Window, WindowMode,
};

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

/// Runs `f` against the primary window, or reports why it could not.
#[cfg(feature = "render")]
fn with_window<F>(f: F) -> i32
where
    F: FnOnce(&mut Window, &mut CursorOptions) -> i32,
{
    with_world(|world| {
        let mut query = world.query_filtered::<(&mut Window, &mut CursorOptions), With<PrimaryWindow>>();
        match query.single_mut(world) {
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
pub unsafe extern "C" fn bcs_window_set_title(title: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = title;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(title) = (unsafe { crate::interop::cstr_to_string(title) }) else {
                return status::NULL_ARG;
            };

            with_window(|window, _| {
                window.title = title.clone();
                status::OK
            })
        }
    })
}

/// Resizes the window, in logical pixels.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_size(width: u32, height: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (width, height);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if width == 0 || height == 0 {
                return status::NULL_ARG;
            }

            with_window(|window, _| {
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
pub unsafe extern "C" fn bcs_window_scale(scale: *mut f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = scale;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if scale.is_null() {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| {
                use bevy::window::{PrimaryWindow, Window};

                let mut windows =
                    world.query_filtered::<&Window, bevy::prelude::With<PrimaryWindow>>();

                let Ok(window) = windows.single(world) else {
                    // An offscreen run draws into an image, and an image has pixels rather than a
                    // desktop's opinion about how big a pixel is. Bevy says the same by giving
                    // `ImageRenderTarget` a scale factor of 1.0.
                    if world.contains_resource::<crate::app::OffscreenTarget>() {
                        unsafe { scale.write(1.0) };
                        return status::OK;
                    }

                    return status::INVALID_STATE;
                };

                unsafe { scale.write(window.resolution.scale_factor()) };
                status::OK
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
pub unsafe extern "C" fn bcs_window_size(width: *mut u32, height: *mut u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (width, height);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            // The size of whatever is being drawn into. A window has one, and so does the image an
            // offscreen run draws into instead, and everything that lays out an interface needs an
            // answer rather than needing to know which kind of run this is. The rest of this
            // module is about a window and has nothing to say without one.
            let answered = with_window(|window, _| {
                if !width.is_null() {
                    unsafe { width.write(window.resolution.width() as u32) };
                }
                if !height.is_null() {
                    unsafe { height.write(window.resolution.height() as u32) };
                }
                status::OK
            });

            if answered != status::NOT_PRESENT {
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
pub extern "C" fn bcs_window_set_mode(mode: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = mode;
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

            with_window(|window, _| {
                window.mode = requested;
                status::OK
            })
        }
    })
}

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

/// Moves the window, in physical pixels from the desktop's top-left corner.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_position(x: i32, y: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (x, y);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::WindowPosition;

            with_window(|window, _| {
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
pub extern "C" fn bcs_window_set_style(decorations: i32, resizable: i32, always_on_top: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (decorations, resizable, always_on_top);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::WindowLevel;

            with_window(|window, _| {
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

/// Sets the shape of the pointer while it is over the window.
///
/// `0` the platform's arrow, `1` a text caret, `2` a hand for something to press, `3` four arrows
/// for moving, `4` a no-entry sign, `5` left and right, `6` up and down, `7` the diagonal from the
/// bottom left to the top right, `8` the other diagonal, `9` an open hand, `10` a closed one. The
/// shapes an interface asks for as the pointer crosses what it draws, which makes a field
/// read as one to type in and an edge as one to drag.
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

/// Minimizes the window to the taskbar or dock.
///
/// A window without the platform's title bar has no button of its own for this, so an app that
/// draws its own title bar calls this from one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_minimize() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_window(|window, _| {
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
pub extern "C" fn bcs_window_set_maximized(maximized: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = maximized;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_window(|window, _| {
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
pub unsafe extern "C" fn bcs_window_place(place: *mut crate::interop::BcsWindowPlace) -> i32 {
    crate::interop::guard(|| {
        if place.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::WindowPosition;

            with_world(|world| {
                // Read without `mut`, so asking every frame does not mark the window changed and
                // send it back through Bevy's window sync for nothing.
                let mut query = world.query_filtered::<(bevy::ecs::entity::Entity, &Window), With<PrimaryWindow>>();
                let Ok((entity, window)) = query.single(world) else {
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
pub extern "C" fn bcs_window_start_drag_move() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let started = with_window(|window, _| {
                window.start_drag_move();
                status::OK
            });

            if started == status::OK {
                release_after_drag();
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
pub extern "C" fn bcs_window_start_drag_resize(edge: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = edge;
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

            let started = with_window(|window, _| {
                window.start_drag_resize(direction);
                status::OK
            });

            if started == status::OK {
                release_after_drag();
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
fn release_after_drag() {
    use bevy::input::ButtonState;
    use bevy::input::mouse::{MouseButton, MouseButtonInput};
    use bevy::prelude::ButtonInput;

    with_world(|world| {
        let mut windows = world.query_filtered::<bevy::ecs::entity::Entity, With<PrimaryWindow>>();

        let Ok(window) = windows.single(world) else {
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
pub extern "C" fn bcs_window_set_ime(enabled: i32, x: f32, y: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (enabled, x, y);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_window(|window, _| {
                window.ime_enabled = enabled != 0;
                window.ime_position = bevy::math::Vec2::new(x, y);
                status::OK
            })
        }
    })
}

/// Reports how many monitors the platform knows about.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_monitor_count() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::Monitor;

            with_world(|world| {
                let mut monitors = world.query::<&Monitor>();
                monitors.iter(world).count() as i32
            })
        }
    })
}

/// Describes one monitor, by the index [`bcs_monitor_count`] counts up to.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_monitor_info(index: i32, out: *mut crate::interop::BcsMonitor) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (index, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::Monitor;

            if out.is_null() {
                return status::NULL_ARG;
            }
            let Ok(index) = usize::try_from(index) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let mut monitors = world.query::<&Monitor>();
                let Some(monitor) = monitors.iter(world).nth(index) else {
                    return status::NO_ENTITY;
                };

                let info = crate::interop::BcsMonitor {
                    width: monitor.physical_width,
                    height: monitor.physical_height,
                    x: monitor.physical_position.x,
                    y: monitor.physical_position.y,
                    refresh_millihertz: monitor.refresh_rate_millihertz.unwrap_or(0),
                    scale_factor: monitor.scale_factor as f32,
                };

                unsafe { out.write(info) };
                status::OK
            })
        }
    })
}

/// Writes a monitor's name into `out`, and returns its length in bytes.
///
/// Follows the text convention, where the return value is the length whether or not it fitted, so a
/// caller that guessed too small can ask again. A monitor the platform does not name reports a
/// length of zero rather than failing, because an unnamed monitor is ordinary.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_monitor_name(index: i32, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (index, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::Monitor;

            let Ok(index) = usize::try_from(index) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let mut monitors = world.query::<&Monitor>();
                let Some(monitor) = monitors.iter(world).nth(index) else {
                    return status::NO_ENTITY;
                };

                match monitor.name.as_deref() {
                    Some(name) => unsafe { crate::interop::write_text(name, out, capacity) },
                    None => 0,
                }
            })
        }
    })
}

/// Reports how many video modes a monitor offers.
///
/// A video mode is a resolution, a color depth and a refresh rate together, which an exclusive
/// fullscreen window takes over the screen with. Returns [`status::NO_ENTITY`] where there is no
/// monitor at that index.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_monitor_mode_count(index: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = index;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::Monitor;

            let Ok(index) = usize::try_from(index) else {
                return status::NULL_ARG;
            };

            crate::state::with_world(|world| {
                let mut monitors = world.query::<&Monitor>();

                match monitors.iter(world).nth(index) {
                    Some(monitor) => monitor.video_modes.len() as i32,
                    None => status::NO_ENTITY,
                }
            })
        }
    })
}

/// Describes one video mode of one monitor.
///
/// `mode` counts up to what [`bcs_monitor_mode_count`] reported. The width and height are physical
/// pixels, and the refresh rate is in millihertz the way a monitor's own is.
///
/// # Safety
/// `out` must be writable for one [`crate::interop::BcsVideoMode`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_monitor_mode(
    index: i32,
    mode: i32,
    out: *mut crate::interop::BcsVideoMode,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (index, mode, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::Monitor;

            if out.is_null() {
                return status::NULL_ARG;
            }

            let (Ok(index), Ok(mode)) = (usize::try_from(index), usize::try_from(mode)) else {
                return status::NULL_ARG;
            };

            crate::state::with_world(|world| {
                let mut monitors = world.query::<&Monitor>();

                let Some(monitor) = monitors.iter(world).nth(index) else {
                    return status::NO_ENTITY;
                };

                let Some(found) = monitor.video_modes.get(mode) else {
                    return status::NOT_PRESENT;
                };

                unsafe {
                    out.write(crate::interop::BcsVideoMode {
                        width: found.physical_size.x,
                        height: found.physical_size.y,
                        bit_depth: found.bit_depth as u32,
                        refresh_millihertz: found.refresh_rate_millihertz,
                    });
                }

                status::OK
            })
        }
    })
}

/// Takes the screen over in exclusive fullscreen at one of a monitor's own video modes.
///
/// [`bcs_window_set_mode`] takes the mode the monitor is already in, which avoids a switch the
/// compositor has to undo on every alt-tab. This is the other case, where a game needs to run at a
/// resolution the desktop is not in, and it asks for one of the modes [`bcs_monitor_mode`]
/// described rather than for arbitrary numbers, because a monitor can only be driven at the modes
/// it offers.
///
/// Returns [`status::NO_ENTITY`] where there is no such monitor and [`status::NOT_PRESENT`] where
/// it has no such mode.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_video_mode(index: i32, mode: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (index, mode);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::{Monitor, VideoModeSelection};

            let (Ok(index), Ok(mode)) = (usize::try_from(index), usize::try_from(mode)) else {
                return status::NULL_ARG;
            };

            // Read before the window is borrowed, because both live in the same world and the
            // monitor list is a query rather than a resource.
            let chosen = crate::state::with_world_opt(|world| {
                let mut monitors = world.query::<&Monitor>();

                monitors
                    .iter(world)
                    .nth(index)
                    .map(|monitor| monitor.video_modes.get(mode).copied())
            });

            let chosen = match chosen {
                None => return status::NO_WORLD,
                Some(None) => return status::NO_ENTITY,
                Some(Some(None)) => return status::NOT_PRESENT,
                Some(Some(Some(chosen))) => chosen,
            };

            with_window(|window, _| {
                window.mode = WindowMode::Fullscreen(
                    MonitorSelection::Index(index),
                    VideoModeSelection::Specific(chosen),
                );

                status::OK
            })
        }
    })
}

/// What each window that asked to be see-through was given, by its entity, once decided.
#[cfg(feature = "render")]
static ALPHA: std::sync::Mutex<Option<std::collections::HashMap<bevy::ecs::entity::Entity, bevy::window::CompositeAlphaMode>>> =
    std::sync::Mutex::new(None);

/// Adds what checks a see-through window can be one before its surface is made.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    use bevy::ecs::schedule::IntoScheduleConfigs;

    if let Ok(mut alpha) = ALPHA.lock() {
        *alpha = None;
    }

    let Some(render_app) = app.get_sub_app_mut(bevy::render::RenderApp) else {
        return;
    };

    render_app.add_systems(
        bevy::render::Render,
        settle_alpha.before(bevy::render::view::window::create_surfaces),
    );
}

/// The mode a see-through window is composited in, out of those its surface offers: premultiplied,
/// since a camera and the interface write that, then straight, then opaque.
///
/// Opaque last and always, because Bevy configures a surface with whatever mode the window asked
/// for without asking the surface first, and a mode the surface lacks is a validation error that
/// ends the app. A window that cannot be see-through is drawn opaque, where what would have been
/// clear is black.
#[cfg(feature = "render")]
pub fn pick_alpha(offered: &[wgpu::CompositeAlphaMode]) -> bevy::window::CompositeAlphaMode {
    use bevy::window::CompositeAlphaMode;

    if offered.contains(&wgpu::CompositeAlphaMode::PreMultiplied) {
        CompositeAlphaMode::PreMultiplied
    } else if offered.contains(&wgpu::CompositeAlphaMode::PostMultiplied) {
        CompositeAlphaMode::PostMultiplied
    } else {
        CompositeAlphaMode::Opaque
    }
}

/// Settles the compositing mode of every window that asked to be see-through, before its surface
/// is made.
///
/// Asked of a surface made for the question and dropped straight after, since the one Bevy keeps
/// is made and configured in the same step, and a surface's capabilities are only known once it
/// exists. A surface with no swapchain on it is allowed alongside the real one on every backend.
/// Written into the extracted window every frame, since extraction copies the main world's
/// request over it each time, though only the frame the surface is made reads it.
#[cfg(feature = "render")]
fn settle_alpha(
    mut windows: bevy::ecs::system::ResMut<bevy::render::view::ExtractedWindows>,
    instance: bevy::ecs::system::Res<bevy::render::renderer::RenderInstance>,
    adapter: bevy::ecs::system::Res<bevy::render::renderer::RenderAdapter>,
) {
    use bevy::window::CompositeAlphaMode;

    let Ok(mut settled) = ALPHA.lock() else { return };
    let settled = settled.get_or_insert_with(Default::default);

    for window in windows.windows.values_mut() {
        if !matches!(
            window.alpha_mode,
            CompositeAlphaMode::PreMultiplied | CompositeAlphaMode::PostMultiplied
        ) {
            continue;
        }

        let mode = *settled.entry(window.entity).or_insert_with(|| {
            let target = wgpu::SurfaceTargetUnsafe::RawHandle {
                raw_display_handle: Some(window.handle.get_display_handle()),
                raw_window_handle: window.handle.get_window_handle(),
            };

            // SAFETY: the handles come from a window that is open, as the ones Bevy makes its own
            // surface from do, and the surface is dropped before this returns.
            let offered = match unsafe { instance.create_surface_unsafe(target) } {
                Ok(surface) => surface.get_capabilities(&adapter).alpha_modes,
                Err(_) => Vec::new(),
            };

            let mode = pick_alpha(&offered);

            if mode == CompositeAlphaMode::Opaque {
                bevy::log::warn!(
                    "The window asked to be see-through, and its surface offers only {offered:?}, so it is drawn opaque and what would have been clear is black."
                );
            } else {
                bevy::log::info!("The window is see-through, composited as {mode:?}.");
            }

            mode
        });

        window.alpha_mode = mode;
    }
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use bevy::window::CompositeAlphaMode;

    #[test]
    fn a_see_through_window_takes_premultiplied_then_straight_then_opaque() {
        use wgpu::CompositeAlphaMode as Offered;

        assert_eq!(
            super::pick_alpha(&[Offered::Opaque, Offered::PostMultiplied, Offered::PreMultiplied]),
            CompositeAlphaMode::PreMultiplied
        );
        assert_eq!(
            super::pick_alpha(&[Offered::Opaque, Offered::PostMultiplied]),
            CompositeAlphaMode::PostMultiplied
        );
        assert_eq!(super::pick_alpha(&[Offered::Opaque, Offered::Inherit]), CompositeAlphaMode::Opaque);
        assert_eq!(super::pick_alpha(&[]), CompositeAlphaMode::Opaque);
    }
}
