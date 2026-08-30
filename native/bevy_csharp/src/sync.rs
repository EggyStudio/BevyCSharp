//! Mirrors Bevy's per-frame `Time` and input resources into a flat struct for C#.
//!
//! Bevy stays the single source of truth. C# calls [`bcs_frame_state`] once at the top
//! of each frame (from its `First`-stage system) and refreshes its `Time` and `Input`
//! objects from the snapshot, so behavior scripts read plain managed properties in the
//! hot path instead of crossing the FFI boundary per query.

use bevy::input::mouse::{AccumulatedMouseMotion, AccumulatedMouseScroll, MouseButton};
use bevy::input::ButtonInput;
use bevy::input::keyboard::KeyCode;

use crate::input::set_key;
use crate::interop::{status, BcsFrameState};
use crate::state::with_world;

/// Maps a Bevy mouse button to the bit C# expects, matching `Bevy.MouseButton`.
fn mouse_bit(button: MouseButton) -> Option<u32> {
    Some(match button {
        MouseButton::Left => 0,
        MouseButton::Right => 1,
        MouseButton::Middle => 2,
        MouseButton::Back => 3,
        MouseButton::Forward => 4,
        MouseButton::Other(n) => {
            if n < 27 {
                5 + n as u32
            } else {
                return None;
            }
        }
    })
}

/// Fills `out` with this frame's timing and input state.
///
/// # Safety
/// `out` must point to a writable [`BcsFrameState`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_frame_state(out: *mut BcsFrameState) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let mut state = BcsFrameState::default();

            if let Some(time) = world.get_resource::<bevy::time::Time>() {
                state.time.elapsed_seconds = time.elapsed_secs_f64();
                state.time.delta_seconds = time.delta_secs_f64();
            }
            if let Some(real) = world.get_resource::<bevy::time::Time<bevy::time::Real>>() {
                state.time.raw_delta_seconds = real.delta_secs_f64();
            } else {
                state.time.raw_delta_seconds = state.time.delta_seconds;
            }
            if let Some(frames) = world.get_resource::<bevy::diagnostic::FrameCount>() {
                state.time.frame_count = frames.0 as u64;
            }

            if let Some(keys) = world.get_resource::<ButtonInput<KeyCode>>() {
                for key in keys.get_pressed() {
                    set_key(&mut state.input.keys_down, *key);
                }
                for key in keys.get_just_pressed() {
                    set_key(&mut state.input.keys_pressed, *key);
                }
                for key in keys.get_just_released() {
                    set_key(&mut state.input.keys_released, *key);
                }
            }

            if let Some(buttons) = world.get_resource::<ButtonInput<MouseButton>>() {
                for button in buttons.get_pressed() {
                    if let Some(bit) = mouse_bit(*button) {
                        state.input.mouse_down |= 1 << bit;
                    }
                }
                for button in buttons.get_just_pressed() {
                    if let Some(bit) = mouse_bit(*button) {
                        state.input.mouse_pressed |= 1 << bit;
                    }
                }
                for button in buttons.get_just_released() {
                    if let Some(bit) = mouse_bit(*button) {
                        state.input.mouse_released |= 1 << bit;
                    }
                }
            }

            if let Some(motion) = world.get_resource::<AccumulatedMouseMotion>() {
                state.input.mouse_delta_x = motion.delta.x;
                state.input.mouse_delta_y = motion.delta.y;
            }
            if let Some(scroll) = world.get_resource::<AccumulatedMouseScroll>() {
                state.input.wheel_x = scroll.delta.x;
                state.input.wheel_y = scroll.delta.y;
            }

            // Cursor position needs a window, so it stays zero in headless builds.
            #[cfg(feature = "render")]
            {
                use bevy::window::{PrimaryWindow, Window};
                let mut windows = world.query_filtered::<&Window, bevy::prelude::With<PrimaryWindow>>();
                if let Ok(window) = windows.single(world)
                    && let Some(position) = window.cursor_position()
                {
                    state.input.mouse_x = position.x;
                    state.input.mouse_y = position.y;
                }
            }

            // SAFETY: checked non-null above; C# owns a correctly sized buffer.
            unsafe { out.write(state) };
            status::OK
        })
    })
}
