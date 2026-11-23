//! What the system shows of a window, the mode it holds, the monitor it is on, whether its edges
//! resize it, whether it waits for the display, whether it has the platform's frame and whether its
//! picture is drawn at a size of its own, with the sets the window's settings make of them and a
//! check of a mode before it is asked for.
//!
//! Bevy's `Window` holds what a program asked for and keeps it where the system did otherwise, as
//! where Bevy finds no mode of the screen's for a fullscreen window and asks for a borderless one
//! in its place, so what the system made of it is asked of winit, whose windows are reachable
//! from the main thread alone, the one a system callback runs on.

#[cfg(feature = "render")]
use bevy::ecs::{entity::Entity, query::With};
#[cfg(feature = "render")]
use bevy::window::{Monitor, MonitorSelection, PresentMode, Window, WindowMode, WindowPosition};

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

/// Why the system would refuse a window a mode, by the numbers `bcs_window_set_mode` takes.
pub mod refusal {
    /// The system takes the mode.
    pub const NONE: i32 = 0;
    /// The platform gives no window a mode of the screen's, as Wayland gives none.
    pub const NO_SCREEN_MODES: i32 = 1;
    /// The window is on no screen the system names.
    pub const NO_SCREEN: i32 = 2;
    /// The screen lists no mode at its own size and rate for a fullscreen window to take.
    pub const NO_SCREEN_MODE: i32 = 3;
}

/// Writes the mode the system shows the window in, 0 windowed, 1 borderless over its screen and 2
/// fullscreen at a mode of the screen's, the monitor it is on, by the index
/// [`super::monitors::bcs_monitor_count`] counts up to, or -1 where the system names none, whether
/// its edges resize it, whether it waits for the display, whether it has the platform's frame, and
/// whether its picture is drawn at a size of its own while it covers its screen borderless
/// ([`bcs_window_set_scaling`]).
///
/// Wayland's winit names every window that covers its screen borderless, since Wayland has no mode
/// of a screen's to take, and answers as the compositor last configured the window, a frame or two
/// after it was asked. X11's answers with the mode last asked for. A monitor is matched to Bevy's
/// by its place and size, which both read from winit. Waiting for the display is the present mode
/// Bevy holds, since the surface takes it as it is made again. The frame is winit's, as the
/// platform shows it, and the scaling is the one the window holds whatever its mode.
///
/// # Safety
/// `mode`, `monitor`, `resizable`, `vsync`, `decorated` and `scaled` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_window_shown(
    window: u64,
    mode: *mut i32,
    monitor: *mut i32,
    resizable: *mut i32,
    vsync: *mut i32,
    decorated: *mut i32,
    scaled: *mut i32,
) -> i32 {
    crate::interop::guard(|| {
        if mode.is_null() || monitor.is_null() || resizable.is_null() || vsync.is_null() || decorated.is_null() || scaled.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = window;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use winit::window::Fullscreen;

            with_world(|world| {
                let Some(entity) = super::window_entity(world, window) else {
                    return status::NOT_PRESENT;
                };
                let monitors: Vec<_> = world
                    .query::<&Monitor>()
                    .iter(world)
                    .map(|m| (m.physical_position.x, m.physical_position.y, m.physical_width, m.physical_height))
                    .collect();
                let waits = matches!(
                    world.get::<Window>(entity).map(|w| w.present_mode),
                    Some(PresentMode::AutoVsync | PresentMode::Fifo | PresentMode::FifoRelaxed)
                );
                let drawn_scaled = world.get::<super::scaled::WindowScaling>(entity).is_some();

                let shown = bevy::winit::WINIT_WINDOWS.with_borrow(|windows| {
                    let shown = windows.get_window(entity)?;
                    let held = match shown.fullscreen() {
                        None => 0,
                        Some(Fullscreen::Borderless(_)) => 1,
                        Some(Fullscreen::Exclusive(_)) => 2,
                    };
                    let on = shown.current_monitor().and_then(|on| {
                        let (at, size) = (on.position(), on.size());
                        monitors.iter().position(|&m| m == (at.x, at.y, size.width, size.height))
                    });
                    Some((held, on.map_or(-1, |index| index as i32), shown.is_resizable(), shown.is_decorated()))
                });

                // A window winit has not made yet, in the frame it is spawned, shows nothing.
                let Some((held, on, edges, framed)) = shown else {
                    return status::NOT_PRESENT;
                };
                unsafe {
                    mode.write(held);
                    monitor.write(on);
                    resizable.write(i32::from(edges));
                    vsync.write(i32::from(waits));
                    decorated.write(i32::from(framed));
                    scaled.write(i32::from(drawn_scaled));
                }
                status::OK
            })
        }
    })
}

/// Writes why the system would refuse the window a mode, by the numbers `bcs_window_set_mode`
/// takes, as one of [`refusal`]'s, so a refused mode is said as it is asked for and the window
/// keeps the one it holds.
///
/// Only a fullscreen window is refused, since a borderless one covers whichever screen the system
/// gives it. Bevy asks for a borderless window in place of a fullscreen one where the screen lists
/// no mode at its own, which this finds as Bevy does, so the window is never left in a mode nobody
/// chose.
///
/// # Safety
/// `reason` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_window_mode_refusal(window: u64, mode: i32, reason: *mut i32) -> i32 {
    crate::interop::guard(|| {
        if reason.is_null() || !(0..=2).contains(&mode) {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = window;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::window::VideoModeSelection;
            use winit::raw_window_handle::{HasWindowHandle, RawWindowHandle};

            with_world(|world| {
                let Some(entity) = super::window_entity(world, window) else {
                    return status::NOT_PRESENT;
                };

                let found = bevy::winit::WINIT_WINDOWS.with_borrow(|windows| {
                    let shown = windows.get_window(entity)?;
                    if mode != 2 {
                        return Some(refusal::NONE);
                    }
                    let wayland = shown.window_handle().is_ok_and(|h| matches!(h.as_raw(), RawWindowHandle::Wayland(_)));
                    if wayland {
                        return Some(refusal::NO_SCREEN_MODES);
                    }
                    let Some(screen) = shown.current_monitor() else {
                        return Some(refusal::NO_SCREEN);
                    };
                    let listed = bevy::winit::get_selected_videomode(&screen, &VideoModeSelection::Current).is_some();
                    Some(if listed { refusal::NONE } else { refusal::NO_SCREEN_MODE })
                });

                let Some(found) = found else {
                    return status::NOT_PRESENT;
                };
                unsafe { reason.write(found) };
                status::OK
            })
        }
    })
}

/// Moves the window to a monitor, by the index [`super::monitors::bcs_monitor_count`] counts up
/// to, centered on it as a window and covering it in the mode it holds.
///
/// A fullscreen window takes the new monitor's own mode. Where the platform places windows itself,
/// as Wayland does, a window stays where it is, which [`bcs_window_shown`] then says.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_monitor(window: u64, index: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, index);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Ok(index) = usize::try_from(index) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let Some(entity) = super::window_entity(world, window) else {
                    return status::NOT_PRESENT;
                };
                let Some(monitor) = world.query_filtered::<Entity, With<Monitor>>().iter(world).nth(index) else {
                    return status::NO_ENTITY;
                };

                let mut settings = world.get_mut::<Window>(entity).expect("found as a window above");
                let on = MonitorSelection::Entity(monitor);
                settings.position = WindowPosition::Centered(on);
                settings.mode = match settings.mode {
                    WindowMode::Windowed => WindowMode::Windowed,
                    WindowMode::BorderlessFullscreen(_) => WindowMode::BorderlessFullscreen(on),
                    WindowMode::Fullscreen(_, video) => WindowMode::Fullscreen(on, video),
                };
                status::OK
            })
        }
    })
}

/// Makes the window's edges resize it, or not.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_resizable(window: u64, resizable: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, resizable);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            super::with_window(window, |window, _| {
                window.resizable = resizable != 0;
                status::OK
            })
        }
    })
}

/// Makes a frame wait for the display, or not, as `BcsConfig::vsync` does for the first window.
///
/// Bevy makes the window's surface again with the new present mode before the next frame is shown.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_vsync(window: u64, vsync: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, vsync);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            super::with_window(window, |window, _| {
                window.present_mode = if vsync != 0 { PresentMode::AutoVsync } else { PresentMode::AutoNoVsync };
                status::OK
            })
        }
    })
}

/// Gives the window the platform's frame, or takes it away, leaving its edges and its level as
/// they are, which the settings' borderless asks for with the window's size and place.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_decorations(window: u64, decorations: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, decorations);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            super::with_window(window, |window, _| {
                window.decorations = decorations != 0;
                status::OK
            })
        }
    })
}

/// Draws the window's picture at `width` by `height` in its logical pixels while it covers its
/// screen borderless, brought to the screen by `filter`, 0 filtered, 1 from the nearest picture
/// pixel and 2 enlarged by the largest whole number that fits with black round it, or at the
/// window's own size again where `width` or `height` is 0.
///
/// The window keeps the size in any mode and draws at it only while it covers its screen
/// borderless, at a size under the screen's (see [`super::scaled`]). The interface ImGui draws is
/// drawn after the picture at the screen's size, and Bevy's is drawn with it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_scaling(window: u64, width: u32, height: u32, filter: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, width, height, filter);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::scaled::{WindowScaling, filter::SHARP};

            if filter > SHARP {
                return status::NULL_ARG;
            }
            with_world(|world| {
                let Some(entity) = super::window_entity(world, window) else {
                    return status::NOT_PRESENT;
                };
                let mut found = world.entity_mut(entity);
                if width == 0 || height == 0 {
                    found.remove::<WindowScaling>();
                } else {
                    found.insert(WindowScaling { size: bevy::math::UVec2::new(width, height), filter });
                }
                status::OK
            })
        }
    })
}
