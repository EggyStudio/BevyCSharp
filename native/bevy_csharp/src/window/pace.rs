//! A cap on the frames drawn a second, beside the display's vertical sync, which the settings'
//! `render.fps_limit` asks for.
//!
//! The main world's frame waits at its end until a frame's time has gone by since the one before
//! was due, so frames come at the cap's pace and one that ran late is followed at once and not by a
//! burst catching up. The wait sleeps for all but its last two milliseconds and spins through
//! them, since a sleep on most systems wakes a millisecond or more late, which at a cap of 144
//! would be a sixth of a frame. With the rendering pipelined, the render world draws the frame
//! before while the main world waits, so the wait holds both to the cap.

#[cfg(feature = "render")]
use std::sync::atomic::{AtomicU32, Ordering};

use crate::interop::status;

/// The cap in frames a second, 0 for none.
#[cfg(feature = "render")]
static LIMIT: AtomicU32 = AtomicU32::new(0);

/// Sets the cap in frames a second, 0 for none.
#[cfg(feature = "render")]
pub fn set(fps: u32) {
    LIMIT.store(fps, Ordering::Relaxed);
}

/// Adds the wait at the end of each frame of a run that shows a window.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    app.add_systems(bevy::app::Last, wait);
}

#[cfg(feature = "render")]
fn wait(mut due: bevy::ecs::system::Local<Option<std::time::Instant>>) {
    use std::time::{Duration, Instant};

    let fps = LIMIT.load(Ordering::Relaxed);
    if fps == 0 {
        *due = None;
        return;
    }
    let period = Duration::from_secs_f64(1.0 / f64::from(fps));
    let now = Instant::now();
    // On time or late by less than a frame keeps the pace, and the first frame or one late by a
    // frame or more starts it again from now.
    let at = match *due {
        Some(at) if now < at + period => at,
        _ => now,
    };
    let spin = Duration::from_millis(2);
    if at > now + spin {
        std::thread::sleep(at - now - spin);
    }
    while Instant::now() < at {
        std::hint::spin_loop();
    }
    *due = Some(at + period);
}

/// Caps the frames drawn a second at `fps`, or takes the cap away at 0, in a run that shows a
/// window, which a headless run's own pace (`headless_fps`) stands in for.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_frame_limit(fps: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = fps;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Ok(fps) = u32::try_from(fps) else {
                return status::NULL_ARG;
            };
            set(fps);
            status::OK
        }
    })
}
