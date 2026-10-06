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
/// that reads them, and so nothing is seen twice. Control characters are left out, because
/// Backspace and Enter arrive here as text on some platforms, and a field that inserted them as
/// characters would be wrong on all of them. Read those as keys instead.
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

/// Pauses or resumes the game's clock, and sets how fast it runs against the wall's.
///
/// Bevy's `Time<Virtual>`, which every system's delta comes from and the fixed timestep spends,
/// so a paused game stops moving and stepping in `FixedUpdate` both, while the window, the
/// interface and anything reading `Time<Real>` go on. `speed` is how many seconds of game time
/// pass for each of the wall's, and a negative one leaves the speed as it is.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_time_set_virtual(paused: i32, speed: f32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let Some(mut time) = world.get_resource_mut::<bevy::time::Time<bevy::time::Virtual>>() else {
                return status::NOT_PRESENT;
            };

            if paused != 0 {
                time.pause();
            } else {
                time.unpause();
            }

            if speed >= 0.0 {
                time.set_relative_speed(speed);
            }

            status::OK
        })
    })
}

/// The strategy that advances the clock by `seconds` a frame, or `None` for a number that is no
/// length of time, which leaves the machine's clock.
///
/// Bevy's `TimeUpdateStrategy::ManualDuration`. Each frame the real clock moves on by that much
/// whatever time the frame took, and the game's clock and the fixed steps spend it as they would
/// spend a frame of that length, so a run is the same frame for frame on every machine. The first
/// frame still reads no time gone, as Bevy's first frame always does.
pub(crate) fn frame_strategy(seconds: f64) -> Option<bevy::time::TimeUpdateStrategy> {
    (seconds.is_finite() && seconds > 0.0)
        .then(|| bevy::time::TimeUpdateStrategy::ManualDuration(std::time::Duration::from_secs_f64(seconds)))
}

/// Sets how many seconds each frame advances the clock by, or the machine's clock again for `0`.
///
/// From the next frame, through `frame_strategy`, so anything that is no length of time is the
/// machine's clock too. The managed side refuses those before they get here.
///
/// A clock set to a length a frame runs ahead of the machine's or behind it, by however much the
/// frames were shorter or longer than the length. Bevy reads the machine's again as the time since
/// the last frame it counted, so a clock that ran ahead would read no time at all until the
/// machine caught up, which for a run of short frames is minutes. So the real clock is begun
/// again from now when the set one is let go, keeping the time it has counted.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_time_set_frame_seconds(seconds: f64) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            if let Some(strategy) = frame_strategy(seconds) {
                world.insert_resource(strategy);
                return status::OK;
            }

            let was_set = matches!(
                world.get_resource::<bevy::time::TimeUpdateStrategy>(),
                Some(bevy::time::TimeUpdateStrategy::ManualDuration(_))
            );
            world.insert_resource(bevy::time::TimeUpdateStrategy::Automatic);

            if was_set && let Some(mut real) = world.get_resource_mut::<bevy::time::Time<bevy::time::Real>>() {
                let mut again = bevy::time::Time::<bevy::time::Real>::new(real.startup());
                again.update_with_instant(bevy::platform::time::Instant::now());
                again.advance_to(real.elapsed());
                *real = again;
            }

            status::OK
        })
    })
}

/// Writes how many seconds each frame advances the clock by, or `0` where it reads the machine's.
///
/// A strategy this bridge did not set, an instant or a count of fixed steps, reads as the
/// machine's, since neither is a length a frame.
///
/// # Safety
/// `seconds` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_time_frame_seconds(seconds: *mut f64) -> i32 {
    crate::interop::guard(|| {
        if seconds.is_null() {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let set = match world.get_resource::<bevy::time::TimeUpdateStrategy>() {
                Some(bevy::time::TimeUpdateStrategy::ManualDuration(duration)) => duration.as_secs_f64(),
                _ => 0.0,
            };

            unsafe { seconds.write(set) };
            status::OK
        })
    })
}

/// Writes how far the fixed clock has run past its last step, as a share of a step, from zero up
/// to less than one.
///
/// Bevy runs as many fixed steps in a frame as the time gone has room for, and what is left over
/// waits for the next frame. Something moved in fixed steps and drawn every frame is drawn that
/// far between its last two places, so it moves smoothly when the frame rate and the step rate
/// differ. Read when asked, since it is right only once this frame's fixed steps have run,
/// after the top of the frame the rest of time is taken at.
///
/// # Safety
/// `fraction` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_time_fixed_overstep(fraction: *mut f32) -> i32 {
    crate::interop::guard(|| {
        if fraction.is_null() {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let Some(time) = world.get_resource::<bevy::time::Time<bevy::time::Fixed>>() else {
                return status::NOT_PRESENT;
            };

            unsafe { fraction.write(time.overstep_fraction()) };
            status::OK
        })
    })
}

/// Writes whether the game's clock is paused and how fast it runs, as `bcs_time_set_virtual` sets them.
///
/// # Safety
/// `paused` and `speed` must each be writable or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_time_virtual(paused: *mut i32, speed: *mut f32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let Some(time) = world.get_resource::<bevy::time::Time<bevy::time::Virtual>>() else {
                return status::NOT_PRESENT;
            };

            if !paused.is_null() {
                unsafe { paused.write(time.is_paused() as i32) };
            }
            if !speed.is_null() {
                unsafe { speed.write(time.relative_speed()) };
            }

            status::OK
        })
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::time::TimeUpdateStrategy;
    use std::time::Duration;

    #[test]
    fn a_length_a_frame_sets_the_clock_and_anything_else_leaves_the_machines() {
        assert!(matches!(
            frame_strategy(0.25),
            Some(TimeUpdateStrategy::ManualDuration(duration)) if duration == Duration::from_millis(250)
        ));

        for nothing in [0.0, -0.5, f64::NAN, f64::INFINITY] {
            assert!(frame_strategy(nothing).is_none(), "{nothing} set the clock");
        }
    }
}
