//! Bevy's own messages, drained for C#.
//!
//! Bevy reports what happens to a window as buffered messages, read through a cursor. A C# system
//! cannot hold one, so the bridge keeps the cursors and hands over whatever arrived since it last
//! looked. The managed side calls this once a frame and posts the result to its own message bus,
//! so a reader cannot tell an engine message from one another system sent.
//!
//! Only messages whose payload is plain data are here. Anything carrying a path or a string needs
//! a way to hand text back that this does not have.

use crate::interop::BcsWindowEvent;
#[cfg(feature = "render")]
use crate::interop::status;

/// Where the readers keep their place in each message queue between frames.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct WindowEventCursors {
    /// Resizes.
    pub resized: bevy::ecs::message::MessageCursor<bevy::window::WindowResized>,
    /// Focus gained and lost.
    pub focused: bevy::ecs::message::MessageCursor<bevy::window::WindowFocused>,
    /// Requests to close, which the app may ignore.
    pub close: bevy::ecs::message::MessageCursor<bevy::window::WindowCloseRequested>,
    /// Display scale changes.
    pub scale: bevy::ecs::message::MessageCursor<bevy::window::WindowScaleFactorChanged>,
    /// The pointer arriving.
    pub entered: bevy::ecs::message::MessageCursor<bevy::window::CursorEntered>,
    /// The pointer leaving.
    pub left: bevy::ecs::message::MessageCursor<bevy::window::CursorLeft>,
}

/// Copies whatever the window has reported since the last call into `out`.
///
/// Returns how many were written, or a negative status. A full buffer is not an error: the rest
/// are left in the queue and come back on the next call, because dropping a resize silently would
/// leave the caller's idea of the window wrong until the next one.
///
/// # Safety
/// `out` must be writable for `capacity` events.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_window_events(out: *mut BcsWindowEvent, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (out, capacity);
            0
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::message::Messages;
            use bevy::window::{
                CursorEntered, CursorLeft, WindowCloseRequested, WindowFocused, WindowResized,
                WindowScaleFactorChanged,
            };

            if out.is_null() && capacity > 0 {
                return status::NULL_ARG;
            }
            let capacity = capacity.max(0) as usize;

            crate::state::with_world(|world| {
                if !world.contains_resource::<WindowEventCursors>() {
                    return 0;
                }

                let mut written = 0usize;

                world.resource_scope(
                    |world, mut cursors: bevy::ecs::world::Mut<WindowEventCursors>| {
                        // Each arm reads one queue and stops at the buffer's end, leaving the
                        // cursor where it is so nothing is lost.
                        macro_rules! drain {
                            ($cursor:ident, $ty:ty, $kind:expr, $body:expr) => {
                                if let Some(messages) = world.get_resource::<Messages<$ty>>() {
                                    let read: Vec<_> = cursors
                                        .$cursor
                                        .read(messages)
                                        .take(capacity.saturating_sub(written))
                                        .cloned()
                                        .collect();

                                    for message in read {
                                        #[allow(clippy::redundant_closure_call)]
                                        let (a, b) = ($body)(&message);
                                        // SAFETY: `written < capacity`, checked by the take above.
                                        unsafe {
                                            out.add(written).write(BcsWindowEvent {
                                                kind: $kind,
                                                a,
                                                b,
                                            })
                                        };
                                        written += 1;
                                    }
                                }
                            };
                        }

                        drain!(resized, WindowResized, 0, |m: &WindowResized| (
                            m.width, m.height
                        ));
                        drain!(focused, WindowFocused, 1, |m: &WindowFocused| (
                            if m.focused { 1.0 } else { 0.0 },
                            0.0
                        ));
                        drain!(close, WindowCloseRequested, 2, |_: &WindowCloseRequested| (
                            0.0, 0.0
                        ));
                        drain!(
                            scale,
                            WindowScaleFactorChanged,
                            3,
                            |m: &WindowScaleFactorChanged| (m.scale_factor as f32, 0.0)
                        );
                        drain!(entered, CursorEntered, 4, |_: &CursorEntered| (0.0, 0.0));
                        drain!(left, CursorLeft, 5, |_: &CursorLeft| (0.0, 0.0));
                    },
                );

                written as i32
            })
        }
    })
}
