//! The window, after it has opened.
//!
//! `BcsConfig` decides how the window is created; everything here changes it while the app runs.
//! Cursor grab is the one that blocks something outright rather than merely being convenient: a
//! first-person camera cannot work without it.
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

/// Writes the window's current size, in logical pixels.
///
/// The size the window ended up at, which is not always the size that was asked for: a window
/// manager may refuse, and a fullscreen window takes the monitor's.
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
            with_window(|window, _| {
                if !width.is_null() {
                    unsafe { width.write(window.resolution.width() as u32) };
                }
                if !height.is_null() {
                    unsafe { height.write(window.resolution.height() as u32) };
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
/// asking for a different resolution, which is what avoids a mode switch the compositor has to
/// undo on every alt-tab.
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
/// place. Locking is what a first-person camera needs, because it reads how far the mouse moved
/// rather than where it is, and a free cursor stops moving at the edge of the screen.
///
/// Platforms differ in which they support: Windows confines and macOS locks, and each emulates
/// the other. Asking for one and getting the other is normal, and is why the cursor should be
/// hidden while it is grabbed either way.
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
