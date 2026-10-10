//! Bevy's diagnostics store, which Bevy's own plugins measure frames, entities and render passes
//! into, and which a game registers its own measures in and reads them all back from.
//!
//! A diagnostic is a named history of numbers, `fps` or `game/enemies`, with a value smoothed over
//! time and an average over its history, and Bevy's log prints every one once a second where an
//! app adds the log. A measurement goes straight into the store here rather than through Bevy's
//! `Diagnostics` parameter, which queues one until its system ends, since the managed side's call
//! is already inside a system with the world on loan.

use crate::interop::status;
use crate::state::with_world;

/// Bevy's log of every diagnostic once a second (`LogDiagnosticsPlugin`).
pub const LOG: u32 = 1;
/// Bevy's frame time, frames a second and frame count (`FrameTimeDiagnosticsPlugin`).
pub const FRAME_TIME: u32 = 2;
/// Bevy's count of the entities in the world (`EntityCountDiagnosticsPlugin`).
pub const ENTITY_COUNT: u32 = 4;
/// Bevy's render passes' times, CPU and GPU, copied from the render world
/// (`RenderDiagnosticsPlugin`).
pub const RENDER: u32 = 8;

/// Adds the diagnostics plugins an app asks for, one bit each, after Bevy's own plugins, since
/// they read Bevy's clock and the render one reads the render world.
///
/// The store comes with Bevy's default plugins and is added here where the app has the minimal
/// ones. Render diagnostics are skipped where the app measures render timings already
/// (`Config.GpuTimings`), which adds the same plugin, and where it draws nothing.
pub fn install(app: &mut bevy::app::App, plugins: u32) {
    use bevy::diagnostic::{
        DiagnosticsPlugin, EntityCountDiagnosticsPlugin, FrameTimeDiagnosticsPlugin, LogDiagnosticsPlugin,
    };

    if plugins == 0 {
        return;
    }

    if !app.is_plugin_added::<DiagnosticsPlugin>() {
        app.add_plugins(DiagnosticsPlugin);
    }

    if plugins & FRAME_TIME != 0 {
        app.add_plugins(FrameTimeDiagnosticsPlugin::default());
    }

    if plugins & ENTITY_COUNT != 0 {
        app.add_plugins(EntityCountDiagnosticsPlugin::default());
    }

    #[cfg(feature = "render")]
    if plugins & RENDER != 0
        && app.get_sub_app(bevy::render::RenderApp).is_some()
        && !app.is_plugin_added::<bevy::render::diagnostic::RenderDiagnosticsPlugin>()
    {
        app.add_plugins(bevy::render::diagnostic::RenderDiagnosticsPlugin);
    }

    if plugins & LOG != 0 {
        app.add_plugins(LogDiagnosticsPlugin::default());
    }
}

/// What a diagnostic holds now, as [`bcs_diagnostic_read`] hands it out. A value it has none of
/// yet is NaN.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsDiagnostic {
    /// The last measurement.
    pub value: f64,
    /// The measurements smoothed over time, which Bevy's log prints first.
    pub smoothed: f64,
    /// The mean of the measurements kept.
    pub average: f64,
    /// How many measurements are kept.
    pub history: u32,
    /// Non-zero where it is measured and logged.
    pub enabled: u32,
}

/// A path as Bevy takes one, or nothing for one it would refuse: empty, beginning or ending with a
/// slash, or with two slashes together, which Bevy checks only in a debug build.
///
/// # Safety
/// `path` must be null or a NUL-terminated string.
unsafe fn path_of(path: *const core::ffi::c_char) -> Option<bevy::diagnostic::DiagnosticPath> {
    let path = unsafe { crate::interop::cstr_to_string(path) }?;
    if path.is_empty() || path.starts_with('/') || path.ends_with('/') || path.contains("//") {
        return None;
    }

    Some(bevy::diagnostic::DiagnosticPath::new(path))
}

/// Registers a diagnostic in the store, which a measurement needs before it is kept. Only valid
/// inside a system.
///
/// `history` is how many measurements are kept for the average, or `0` for Bevy's own number.
/// Registering a path again starts it over. Reports [`status::INVALID_STATE`] for a path Bevy would
/// refuse.
///
/// # Safety
/// `path` and `suffix` must be NUL-terminated strings, `suffix` or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_diagnostic_register(
    path: *const core::ffi::c_char,
    suffix: *const core::ffi::c_char,
    history: u32,
) -> i32 {
    crate::interop::guard(|| {
        use bevy::diagnostic::{Diagnostic, DiagnosticsStore};

        if path.is_null() {
            return status::NULL_ARG;
        }
        let Some(path) = (unsafe { path_of(path) }) else {
            return status::INVALID_STATE;
        };
        let suffix = unsafe { crate::interop::cstr_to_string(suffix) }.unwrap_or_default();

        with_world(|world| {
            let mut diagnostic = Diagnostic::new(path).with_suffix(suffix);
            if history > 0 {
                diagnostic = diagnostic.with_max_history_length(history as usize);
            }

            world.init_resource::<DiagnosticsStore>();
            world.resource_mut::<DiagnosticsStore>().add(diagnostic);
            status::OK
        })
    })
}

/// Adds a measurement to a diagnostic, taken now. Only valid inside a system.
///
/// Reports [`status::NOT_PRESENT`] for a path nobody registered and [`status::INVALID_STATE`] for
/// one turned off, neither of which keeps it, as Bevy's own measuring does not.
///
/// # Safety
/// `path` must be a NUL-terminated string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_diagnostic_measure(path: *const core::ffi::c_char, value: f64) -> i32 {
    crate::interop::guard(|| {
        use bevy::diagnostic::{DiagnosticMeasurement, DiagnosticsStore};

        if path.is_null() {
            return status::NULL_ARG;
        }
        let Some(path) = (unsafe { path_of(path) }) else {
            return status::NOT_PRESENT;
        };

        with_world(|world| {
            let Some(mut store) = world.get_resource_mut::<DiagnosticsStore>() else {
                return status::NOT_PRESENT;
            };
            let Some(diagnostic) = store.get_mut(&path) else {
                return status::NOT_PRESENT;
            };
            if !diagnostic.is_enabled {
                return status::INVALID_STATE;
            }

            diagnostic.add_measurement(DiagnosticMeasurement { time: bevy::platform::time::Instant::now(), value });
            status::OK
        })
    })
}

/// Reads what a diagnostic holds now. Only valid inside a system.
///
/// # Safety
/// `path` must be a NUL-terminated string and `out` writable for one [`BcsDiagnostic`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_diagnostic_read(path: *const core::ffi::c_char, out: *mut BcsDiagnostic) -> i32 {
    crate::interop::guard(|| {
        use bevy::diagnostic::DiagnosticsStore;

        if path.is_null() || out.is_null() {
            return status::NULL_ARG;
        }
        let Some(path) = (unsafe { path_of(path) }) else {
            return status::NOT_PRESENT;
        };

        with_world(|world| {
            let Some(diagnostic) = world.get_resource::<DiagnosticsStore>().and_then(|store| store.get(&path)) else {
                return status::NOT_PRESENT;
            };

            let read = BcsDiagnostic {
                value: diagnostic.value().unwrap_or(f64::NAN),
                smoothed: diagnostic.smoothed().unwrap_or(f64::NAN),
                average: diagnostic.average().unwrap_or(f64::NAN),
                history: diagnostic.get_max_history_length() as u32,
                enabled: u32::from(diagnostic.is_enabled),
            };
            unsafe { out.write(read) };
            status::OK
        })
    })
}

/// Turns a diagnostic's measuring and logging on or off, its history kept. Only valid inside a
/// system.
///
/// # Safety
/// `path` must be a NUL-terminated string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_diagnostic_set_enabled(path: *const core::ffi::c_char, on: i32) -> i32 {
    crate::interop::guard(|| {
        use bevy::diagnostic::DiagnosticsStore;

        if path.is_null() {
            return status::NULL_ARG;
        }
        let Some(path) = (unsafe { path_of(path) }) else {
            return status::NOT_PRESENT;
        };

        with_world(|world| {
            let Some(mut store) = world.get_resource_mut::<DiagnosticsStore>() else {
                return status::NOT_PRESENT;
            };
            let Some(diagnostic) = store.get_mut(&path) else {
                return status::NOT_PRESENT;
            };

            diagnostic.is_enabled = on != 0;
            status::OK
        })
    })
}

/// Copies every diagnostic in the store out, by the text convention, a line each in path order:
/// its path, its suffix, `1` or `0` for whether it is on, how many measurements it keeps, and its
/// last, smoothed and average values, apart by tabs, a value it has none of yet empty. Only valid
/// inside a system.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_diagnostics_list(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        use bevy::diagnostic::DiagnosticsStore;
        use std::fmt::Write;

        with_world(|world| {
            let Some(store) = world.get_resource::<DiagnosticsStore>() else {
                return unsafe { crate::interop::write_text("", out, capacity) };
            };

            let mut diagnostics: Vec<_> = store.iter().collect();
            diagnostics.sort_by(|a, b| a.path().as_str().cmp(b.path().as_str()));

            let number = |value: Option<f64>| value.map(|value| value.to_string()).unwrap_or_default();
            let mut text = String::new();
            for diagnostic in diagnostics {
                let _ = writeln!(
                    text,
                    "{}\t{}\t{}\t{}\t{}\t{}\t{}",
                    diagnostic.path(),
                    diagnostic.suffix,
                    u32::from(diagnostic.is_enabled),
                    diagnostic.get_max_history_length(),
                    number(diagnostic.value()),
                    number(diagnostic.smoothed()),
                    number(diagnostic.average()),
                );
            }

            unsafe { crate::interop::write_text(&text, out, capacity) }
        })
    })
}

/// Says which diagnostics Bevy's log prints, as paths a line each, or every one where `paths` is
/// null. An empty list prints none. Only valid inside a system.
///
/// Reports [`status::INVALID_STATE`] in an app that logs no diagnostics.
///
/// # Safety
/// `paths` must be null or a NUL-terminated string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_diagnostics_set_log_filter(paths: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        use bevy::diagnostic::{DiagnosticPath, LogDiagnosticsState};

        let paths = unsafe { crate::interop::cstr_to_string(paths) };

        with_world(|world| {
            let Some(mut state) = world.get_resource_mut::<LogDiagnosticsState>() else {
                return status::INVALID_STATE;
            };

            match paths {
                None => state.disable_filtering(),
                Some(paths) => {
                    state.enable_filtering();
                    state.extend_filter(
                        paths.lines().filter(|line| !line.is_empty()).map(|line| DiagnosticPath::new(line.to_owned())),
                    );
                }
            }

            status::OK
        })
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::app::App;

    use crate::state::loan_world;

    #[test]
    fn a_diagnostic_is_registered_measured_turned_off_and_listed() {
        let mut app = App::new();

        loan_world(app.world_mut(), || {
            let path = c"game/enemies";
            assert_eq!(status::OK, unsafe { bcs_diagnostic_register(path.as_ptr(), c" enemies".as_ptr(), 0) });
            assert_eq!(status::OK, unsafe { bcs_diagnostic_measure(path.as_ptr(), 3.0) });
            assert_eq!(status::OK, unsafe { bcs_diagnostic_measure(path.as_ptr(), 5.0) });

            let mut read = BcsDiagnostic { value: 0.0, smoothed: 0.0, average: 0.0, history: 0, enabled: 0 };
            assert_eq!(status::OK, unsafe { bcs_diagnostic_read(path.as_ptr(), &mut read) });
            assert_eq!((5.0, 4.0, 1), (read.value, read.average, read.enabled));

            // Off, a measurement is refused and the history kept.
            assert_eq!(status::OK, unsafe { bcs_diagnostic_set_enabled(path.as_ptr(), 0) });
            assert_eq!(status::INVALID_STATE, unsafe { bcs_diagnostic_measure(path.as_ptr(), 9.0) });
            assert_eq!(status::OK, unsafe { bcs_diagnostic_read(path.as_ptr(), &mut read) });
            assert_eq!((5.0, 0), (read.value, read.enabled));

            let mut buffer = [0u8; 256];
            let written = unsafe { bcs_diagnostics_list(buffer.as_mut_ptr(), buffer.len() as i32) };
            let text = core::str::from_utf8(&buffer[..written as usize]).unwrap();
            assert!(text.starts_with("game/enemies\t enemies\t0\t"), "listed as {text:?}");

            // A path nobody registered, and one Bevy would refuse.
            assert_eq!(status::NOT_PRESENT, unsafe { bcs_diagnostic_measure(c"game/none".as_ptr(), 1.0) });
            assert_eq!(status::INVALID_STATE, unsafe { bcs_diagnostic_register(c"game//enemies".as_ptr(), core::ptr::null(), 0) });

            // An app that logs no diagnostics has no filter to set.
            assert_eq!(status::INVALID_STATE, unsafe { bcs_diagnostics_set_log_filter(core::ptr::null()) });
        });
    }

    /// Where the managed mirror (`NativeDiagnosticLayoutTests`) expects the fields.
    #[test]
    fn a_reading_is_laid_out_where_the_mirror_expects() {
        use core::mem::{offset_of, size_of};

        assert_eq!(offset_of!(BcsDiagnostic, average), 16);
        assert_eq!(offset_of!(BcsDiagnostic, history), 24);
        assert_eq!(offset_of!(BcsDiagnostic, enabled), 28);
        assert_eq!(size_of::<BcsDiagnostic>(), 32);
    }
}
