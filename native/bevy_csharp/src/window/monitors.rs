//! The monitors the platform knows about, their video modes, and exclusive fullscreen at one of
//! them.

#[cfg(feature = "render")]
use bevy::window::{MonitorSelection, WindowMode};

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

#[cfg(feature = "render")]
use super::with_window;

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
/// [`bcs_window_set_mode`](super::bcs_window_set_mode) takes the mode the monitor is already in, which avoids a switch the
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
