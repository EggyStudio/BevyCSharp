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
            // The timestep, not the delta. `Time<Fixed>`'s delta reports the step that last ran
            // and is zero until one has, which would hand a fixed system nothing to integrate
            // with on the first frame. The timestep is the constant those steps are made of, so
            // it is correct from the start and does not go stale between them.
            if let Some(fixed) = world.get_resource::<bevy::time::Time<bevy::time::Fixed>>() {
                state.time.fixed_delta_seconds = fixed.timestep().as_secs_f64();
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

            collect_text(world, &mut state.input);
            collect_touches(world, &mut state.input);

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

/// Copies this frame's typed text into `input`.
///
/// Read through a cursor rather than by draining, so the messages stay available to anything else
/// that reads them, and so nothing is seen twice. Control characters are left out: Backspace and
/// Enter arrive here as text on some platforms, and a field that inserted them as characters
/// would be wrong on all of them. Read those as keys instead.
fn collect_text(world: &mut bevy::ecs::world::World, input: &mut crate::interop::BcsInput) {
    use bevy::ecs::message::Messages;
    use bevy::input::keyboard::KeyboardInput;
    use bevy::input::ButtonState;

    if !world.contains_resource::<TextCursor>() {
        return;
    }

    world.resource_scope(|world, mut cursor: bevy::ecs::world::Mut<TextCursor>| {
        let Some(messages) = world.get_resource::<Messages<KeyboardInput>>() else {
            return;
        };

        let mut written = 0usize;
        for message in cursor.0.read(messages) {
            if message.state != ButtonState::Pressed {
                continue;
            }
            let Some(text) = message.text.as_ref() else {
                continue;
            };

            for character in text.chars().filter(|c| !c.is_control()) {
                let len = character.len_utf8();
                if written + len > crate::interop::TEXT_CAPACITY {
                    break;
                }
                character.encode_utf8(&mut input.text[written..]);
                written += len;
            }
        }

        input.text_len = written as u32;
    });
}

/// Copies the touches in progress into `input`.
fn collect_touches(world: &bevy::ecs::world::World, input: &mut crate::interop::BcsInput) {
    use bevy::input::touch::Touches;

    let Some(touches) = world.get_resource::<Touches>() else {
        return;
    };

    let mut count = 0usize;
    let mut push = |touch: &bevy::input::touch::Touch, phase: i32| {
        if count >= crate::interop::TOUCH_CAPACITY {
            return;
        }
        input.touches[count] = crate::interop::BcsTouch {
            id: touch.id(),
            x: touch.position().x,
            y: touch.position().y,
            phase,
            _pad: 0,
        };
        count += 1;
    };

    for touch in touches.iter_just_pressed() {
        push(touch, 1);
    }
    for touch in touches.iter() {
        if touches.just_pressed(touch.id()) {
            continue;
        }
        push(touch, 0);
    }
    // Released touches are gone from `iter`, so they are reported once, on the frame they end.
    for touch in touches.iter_just_released() {
        push(touch, 2);
    }

    input.touch_count = count as u32;
}

/// Where the text reader's place in the keyboard message queue is kept between frames.
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct TextCursor(pub bevy::ecs::message::MessageCursor<bevy::input::keyboard::KeyboardInput>);
