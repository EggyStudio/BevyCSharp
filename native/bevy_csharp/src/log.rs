//! Bevy's log, installed once for the process, and the errors it says kept for the managed side.
//!
//! Bevy's `LogPlugin` sets the process's one logger, and a second app's plugin finds it set and
//! says so at the error level, so every app after the first in a process began with an error a
//! test or a game had no part in. The first app's plugin is the process's logger from then on, with
//! a layer of the bridge's own in it, and every later app leaves the plugin out.
//!
//! The layer keeps each line Bevy logs at the error level, its target and its message, so the
//! managed side hears an error a game never sees on a console, as a test that has to fail for one
//! does. The managed side takes them with [`bcs_log_take_error`] as a run ends and as the app is
//! disposed, so they are laid to the app that logged them. The most recent [`KEPT`] are kept, which a
//! run that logs more than that between two takes loses the oldest of.
//!
//! While the managed side asks for them ([`bcs_log_collect`]), the layer keeps every line it is
//! shown as well, at each level Bevy's filter lets through, for the managed side to take each frame
//! with [`bcs_log_take_line`] into the console's log and the run's log file, since Bevy prints its
//! lines to the process's output itself and C#'s console never sees them. The most recent
//! [`COLLECTED`] are kept between two takes.

use std::collections::VecDeque;
use std::fmt::Write as _;
use std::sync::Mutex;
use std::sync::atomic::{AtomicBool, Ordering};

use bevy::log::tracing::field::{Field, Visit};
use bevy::log::tracing::{Event, Level, Subscriber};
use bevy::log::tracing_subscriber::layer::{Context, Layer};

/// How many error lines are kept between two takes.
pub const KEPT: usize = 256;

/// How many lines of every level are kept between two takes, while they are collected.
pub const COLLECTED: usize = 4096;

static ERRORS: Mutex<VecDeque<String>> = Mutex::new(VecDeque::new());
static INSTALLED: AtomicBool = AtomicBool::new(false);

/// Every line shown to the layer since the last take, with its level, `0` trace up to `4` error.
static LINES: Mutex<VecDeque<(i32, String)>> = Mutex::new(VecDeque::new());
static COLLECTING: AtomicBool = AtomicBool::new(false);

/// Whether this app is the first of the process, whose log plugin becomes the process's logger.
/// Answers yes once and no from then on.
pub fn first() -> bool {
    !INSTALLED.swap(true, Ordering::SeqCst)
}

/// Bevy's log plugin with the layer that keeps errors, for the first app of the process.
///
/// Bevy's own filter, less the error the audio decoders' format probe logs when a file is none of
/// the formats it knows. The bridge reports that file itself, with its path, as it refuses it
/// (`audio/checked.rs`), where the probe's line names no file.
pub fn plugin() -> bevy::log::LogPlugin {
    bevy::log::LogPlugin {
        filter: format!("{},symphonia_core::probe=off", bevy::log::DEFAULT_FILTER),
        custom_layer: |_| Some(Box::new(Kept)),
        ..Default::default()
    }
}

/// The layer that keeps each error line.
struct Kept;

impl<S: Subscriber> Layer<S> for Kept {
    fn on_event(&self, event: &Event<'_>, _: Context<'_, S>) {
        let level = *event.metadata().level();
        if COLLECTING.load(Ordering::Relaxed) {
            collect(level, event);
        }

        if level != Level::ERROR {
            return;
        }

        let mut line = format!("{}: ", event.metadata().target());
        event.record(&mut Message(&mut line));

        // A logger that panics or blocks would take down whatever logged, so a lock poisoned by
        // another thread's panic drops the line rather than adding a second panic.
        if let Ok(mut errors) = ERRORS.lock() {
            if errors.len() == KEPT {
                errors.pop_front();
            }
            errors.push_back(line);
        }
    }
}

/// Keeps a line of any level for the managed side, a line C# wrote without its target, which the
/// managed side's log says by where it is.
fn collect(level: Level, event: &Event<'_>) {
    let target = event.metadata().target();
    let mut line = if target == "csharp" { String::new() } else { format!("{target}: ") };
    event.record(&mut Message(&mut line));

    let level = match level {
        Level::ERROR => 4,
        Level::WARN => 3,
        Level::INFO => 2,
        Level::DEBUG => 1,
        Level::TRACE => 0,
    };

    if let Ok(mut lines) = LINES.lock() {
        if lines.len() == COLLECTED {
            lines.pop_front();
        }
        lines.push_back((level, line));
    }
}

/// Writes an event's message, then each other field as `name=value`.
struct Message<'a>(&'a mut String);

impl Visit for Message<'_> {
    fn record_str(&mut self, field: &Field, value: &str) {
        if field.name() == "message" {
            self.0.push_str(value);
        } else {
            let _ = write!(self.0, " {}={value}", field.name());
        }
    }

    fn record_debug(&mut self, field: &Field, value: &dyn std::fmt::Debug) {
        if field.name() == "message" {
            let _ = write!(self.0, "{value:?}");
        } else {
            let _ = write!(self.0, " {}={value:?}", field.name());
        }
    }
}

/// Takes the oldest error line kept, writing it as UTF-8 into `out`.
///
/// Answers the line's length in bytes, or `0` when none is kept. A buffer too small for the line
/// leaves it kept and answers the length it needs, so the caller asks again with room for it.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_log_take_error(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        let Ok(mut errors) = ERRORS.lock() else {
            return 0;
        };
        let Some(line) = errors.front() else {
            return 0;
        };

        let needed = unsafe { crate::interop::write_text(line, out, capacity) };
        if needed <= capacity && !out.is_null() {
            errors.pop_front();
        }

        needed
    })
}

/// Starts keeping every line the layer is shown for [`bcs_log_take_line`] when `on` is not zero,
/// and stops and forgets what was kept when it is.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_log_collect(on: i32) -> i32 {
    crate::interop::guard(|| {
        COLLECTING.store(on != 0, Ordering::Relaxed);
        if on == 0 {
            if let Ok(mut lines) = LINES.lock() {
                lines.clear();
            }
        }

        crate::interop::status::OK
    })
}

/// Takes the oldest line kept by [`bcs_log_collect`], writing it as UTF-8 into `out` and its level
/// into `level`, `0` trace up to `4` error.
///
/// Answers the line's length in bytes, or `0` when none is kept. A buffer too small for the line
/// leaves it kept and answers the length it needs, as [`bcs_log_take_error`] does.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero, and `level` must
/// be writable or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_log_take_line(out: *mut u8, capacity: i32, level: *mut i32) -> i32 {
    crate::interop::guard(|| {
        let Ok(mut lines) = LINES.lock() else {
            return 0;
        };
        let Some((said, line)) = lines.front() else {
            return 0;
        };

        if !level.is_null() {
            unsafe { *level = *said };
        }

        let needed = unsafe { crate::interop::write_text(line, out, capacity) };
        if needed <= capacity && !out.is_null() {
            lines.pop_front();
        }

        needed
    })
}

/// Writes a line from C# into Bevy's log at `level`, `0` trace, `1` debug, `2` info, `3` warn and
/// `4` error, under the target `csharp`.
///
/// Bevy's log is a `tracing` subscriber whose filter is set when the app is made, at the info level
/// by default and by `RUST_LOG` where it is set, so a line from C# is shown or left out as Bevy's
/// own are, and `RUST_LOG=csharp=debug` shows the debug lines of C# alone. The target is one name
/// for every line, since a target is fixed where a line is written in Rust and these are written in
/// one place. A line at the error level is kept for the managed side as Bevy's own are.
///
/// # Safety
/// `message` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_log_write(level: i32, message: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        let Some(message) = (unsafe { crate::interop::cstr_to_string(message) }) else {
            return crate::interop::status::NULL_ARG;
        };

        match level {
            0 => bevy::log::trace!(target: "csharp", "{message}"),
            1 => bevy::log::debug!(target: "csharp", "{message}"),
            2 => bevy::log::info!(target: "csharp", "{message}"),
            3 => bevy::log::warn!(target: "csharp", "{message}"),
            4 => bevy::log::error!(target: "csharp", "{message}"),
            _ => return crate::interop::status::INVALID_STATE,
        }

        crate::interop::status::OK
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::log::tracing_subscriber::prelude::*;

    /// One test holds the lock on what is kept for its whole length, so two that log at once do
    /// not take each other's lines.
    static ALONE: Mutex<()> = Mutex::new(());

    fn take() -> Option<String> {
        let mut buffer = vec![0u8; 512];
        let length = unsafe { bcs_log_take_error(buffer.as_mut_ptr(), buffer.len() as i32) };
        (length > 0).then(|| String::from_utf8_lossy(&buffer[..length as usize]).into_owned())
    }

    #[test]
    fn an_error_is_kept_with_its_target_and_fields_and_a_warning_is_not() {
        let _alone = ALONE.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
        while take().is_some() {}

        let subscriber = bevy::log::tracing_subscriber::registry().with(Kept);
        bevy::log::tracing::subscriber::with_default(subscriber, || {
            bevy::log::warn!("only a warning");
            bevy::log::error!(target: "bcs_test", asset = "ship.glb", "it broke");
        });

        assert_eq!(take().as_deref(), Some("bcs_test: it broke asset=ship.glb"));
        assert_eq!(take(), None);
    }

    #[test]
    fn a_line_too_long_for_the_buffer_is_kept_until_there_is_room() {
        let _alone = ALONE.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
        while take().is_some() {}

        let subscriber = bevy::log::tracing_subscriber::registry().with(Kept);
        bevy::log::tracing::subscriber::with_default(subscriber, || {
            bevy::log::error!(target: "bcs_test", "a line longer than four bytes");
        });

        let mut small = [0u8; 4];
        let needed = unsafe { bcs_log_take_error(small.as_mut_ptr(), small.len() as i32) };
        assert_eq!(needed as usize, "bcs_test: a line longer than four bytes".len());
        assert_eq!(take().as_deref(), Some("bcs_test: a line longer than four bytes"));
    }

    #[test]
    fn a_line_from_csharp_at_the_error_level_is_kept_under_its_target() {
        let _alone = ALONE.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
        while take().is_some() {}

        let subscriber = bevy::log::tracing_subscriber::registry().with(Kept);
        bevy::log::tracing::subscriber::with_default(subscriber, || {
            let line = std::ffi::CString::new("something failed").unwrap();
            assert_eq!(crate::interop::status::OK, unsafe { bcs_log_write(3, line.as_ptr()) });
            assert_eq!(crate::interop::status::OK, unsafe { bcs_log_write(4, line.as_ptr()) });
            assert_eq!(crate::interop::status::INVALID_STATE, unsafe { bcs_log_write(5, line.as_ptr()) });
        });

        assert_eq!(take().as_deref(), Some("csharp: something failed"));
        assert_eq!(take(), None);
    }

    #[test]
    fn every_line_is_collected_while_asked_for_with_its_level() {
        let _alone = ALONE.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
        assert_eq!(crate::interop::status::OK, bcs_log_collect(1));

        let subscriber = bevy::log::tracing_subscriber::registry().with(Kept);
        bevy::log::tracing::subscriber::with_default(subscriber, || {
            bevy::log::info!(target: "bcs_test", "an ordinary line");
            let line = std::ffi::CString::new("from C#").unwrap();
            assert_eq!(crate::interop::status::OK, unsafe { bcs_log_write(3, line.as_ptr()) });
        });

        let mut taken = Vec::new();
        loop {
            let mut buffer = vec![0u8; 512];
            let mut level = -1;
            let length = unsafe { bcs_log_take_line(buffer.as_mut_ptr(), buffer.len() as i32, &mut level) };
            if length == 0 {
                break;
            }
            taken.push((level, String::from_utf8_lossy(&buffer[..length as usize]).into_owned()));
        }

        assert_eq!(crate::interop::status::OK, bcs_log_collect(0));
        while take().is_some() {}

        assert!(taken.contains(&(2, "bcs_test: an ordinary line".to_string())), "{taken:?}");
        assert!(taken.contains(&(3, "from C#".to_string())), "{taken:?}");
    }

    #[test]
    fn the_first_app_alone_installs_the_logger() {
        // Whichever test asks first, the answer is yes at most once.
        let answers = [first(), first(), first()];
        assert!(answers.iter().filter(|answer| **answer).count() <= 1);
        assert!(!answers[1] && !answers[2]);
    }
}
