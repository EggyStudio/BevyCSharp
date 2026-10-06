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

use std::collections::VecDeque;
use std::fmt::Write as _;
use std::sync::Mutex;
use std::sync::atomic::{AtomicBool, Ordering};

use bevy::log::tracing::field::{Field, Visit};
use bevy::log::tracing::{Event, Level, Subscriber};
use bevy::log::tracing_subscriber::layer::{Context, Layer};

/// How many error lines are kept between two takes.
pub const KEPT: usize = 256;

static ERRORS: Mutex<VecDeque<String>> = Mutex::new(VecDeque::new());
static INSTALLED: AtomicBool = AtomicBool::new(false);

/// Whether this app is the first of the process, whose log plugin becomes the process's logger.
/// Answers yes once and no from then on.
pub fn first() -> bool {
    !INSTALLED.swap(true, Ordering::SeqCst)
}

/// Bevy's log plugin with the layer that keeps errors, for the first app of the process.
pub fn plugin() -> bevy::log::LogPlugin {
    bevy::log::LogPlugin {
        custom_layer: |_| Some(Box::new(Kept)),
        ..Default::default()
    }
}

/// The layer that keeps each error line.
struct Kept;

impl<S: Subscriber> Layer<S> for Kept {
    fn on_event(&self, event: &Event<'_>, _: Context<'_, S>) {
        if *event.metadata().level() != Level::ERROR {
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
    fn the_first_app_alone_installs_the_logger() {
        // Whichever test asks first, the answer is yes at most once.
        let answers = [first(), first(), first()];
        assert!(answers.iter().filter(|answer| **answer).count() <= 1);
        assert!(!answers[1] && !answers[2]);
    }
}
