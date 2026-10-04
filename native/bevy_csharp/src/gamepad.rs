//! Gamepads, read by C# a frame at a time, rumbled, and pretended for a script.
//!
//! Bevy keeps a gamepad as an entity with a `Gamepad` component, which `bevy_gilrs` spawns and
//! feeds where a profile carries it. What C# reads is a copy of each one's buttons and axes, in an
//! order this module fixes and `Bevy.GamepadButton` declares again, so Bevy's own enum can grow
//! without the managed side reading the wrong button.
//!
//! A pad can also be pretended. One connected here is an entity of its own that the same messages
//! a real pad sends are written for, so Bevy's processing, its dead zones and the `Gamepad`
//! component it keeps, runs for it exactly as for a real one. That is how a script presses a
//! button on a machine with no pad attached, and how the test suite reads a pad with no gilrs at
//! all, since the headless profile carries Bevy's gamepad input and only the render profiles carry
//! the backend that finds real ones.

use bevy::ecs::entity::Entity;
use bevy::ecs::world::World;
use bevy::input::gamepad::{
    Gamepad, GamepadAxis, GamepadButton, GamepadConnection, GamepadConnectionEvent,
    GamepadRumbleIntensity, GamepadRumbleRequest, RawGamepadAxisChangedEvent,
    RawGamepadButtonChangedEvent, RawGamepadEvent,
};

use crate::interop::status;
use crate::state::with_world;

/// The buttons in the order a button's bit is numbered, Bevy's standard ones.
pub const BUTTONS: [GamepadButton; 19] = GamepadButton::all();

/// How many bytes of a pad's name one snapshot carries.
pub const NAME_CAPACITY: usize = 48;

/// One connected pad as C# reads it.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsGamepad {
    /// The pad's entity, which stays the same while it is connected.
    pub entity: u64,
    /// Bit `n` set while button `n` of [`BUTTONS`] is held.
    pub down: u32,
    /// Bit `n` set on the frame button `n` went down.
    pub pressed: u32,
    /// Bit `n` set on the frame button `n` came up.
    pub released: u32,
    /// The left stick's X and Y, the right stick's X and Y, then the left and right triggers.
    pub axes: [f32; 6],
    /// The USB vendor id, or zero where the platform did not say.
    pub vendor: u16,
    /// The USB product id, or zero where the platform did not say.
    pub product: u16,
    /// Bytes of `name` in use.
    pub name_len: u32,
    /// The pad's name as the platform gives it, as UTF-8, cut at a character boundary.
    pub name: [u8; NAME_CAPACITY],
}

/// Copies up to `capacity` connected pads into `out` and answers how many are connected, which
/// may be more than were copied.
///
/// # Safety
/// `out` must be writable for `capacity` entries, or null with a capacity of zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_gamepads(out: *mut BcsGamepad, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let mut pads = world.query::<(Entity, &Gamepad, Option<&bevy::ecs::name::Name>)>();
            let mut count = 0i32;

            for (entity, pad, name) in pads.iter(world) {
                if count < capacity && !out.is_null() {
                    let mut snapshot = BcsGamepad {
                        entity: entity.to_bits(),
                        down: 0,
                        pressed: 0,
                        released: 0,
                        axes: [
                            pad.get(GamepadAxis::LeftStickX).unwrap_or(0.0),
                            pad.get(GamepadAxis::LeftStickY).unwrap_or(0.0),
                            pad.get(GamepadAxis::RightStickX).unwrap_or(0.0),
                            pad.get(GamepadAxis::RightStickY).unwrap_or(0.0),
                            pad.get(GamepadButton::LeftTrigger2).unwrap_or(0.0),
                            pad.get(GamepadButton::RightTrigger2).unwrap_or(0.0),
                        ],
                        vendor: pad.vendor_id().unwrap_or(0),
                        product: pad.product_id().unwrap_or(0),
                        name_len: 0,
                        name: [0; NAME_CAPACITY],
                    };

                    for (bit, button) in BUTTONS.iter().enumerate() {
                        if pad.pressed(*button) {
                            snapshot.down |= 1 << bit;
                        }
                        if pad.just_pressed(*button) {
                            snapshot.pressed |= 1 << bit;
                        }
                        if pad.just_released(*button) {
                            snapshot.released |= 1 << bit;
                        }
                    }

                    if let Some(name) = name {
                        let text = name.as_str();
                        let mut end = text.len().min(NAME_CAPACITY);
                        while !text.is_char_boundary(end) {
                            end -= 1;
                        }
                        snapshot.name[..end].copy_from_slice(&text.as_bytes()[..end]);
                        snapshot.name_len = end as u32;
                    }

                    unsafe { out.add(count as usize).write(snapshot) };
                }

                count += 1;
            }

            count
        })
    })
}

/// Rumbles a pad, its strong and weak motors each from zero to one, for `seconds`, or stops what
/// it is doing where `seconds` is zero or less.
///
/// Bevy takes the request in every profile, and only gilrs carries it to a pad, so on the headless
/// profile, or for a pretended pad, it is taken and nothing rumbles, as for a pad with no motors.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_gamepad_rumble(entity: u64, strong: f32, weak: f32, seconds: f32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let gamepad = crate::ecs::entity_from(entity);
            if world.get::<Gamepad>(gamepad).is_none() {
                return status::NO_ENTITY;
            }

            let request = if seconds <= 0.0 {
                GamepadRumbleRequest::Stop { gamepad }
            } else {
                GamepadRumbleRequest::Add {
                    duration: core::time::Duration::from_secs_f32(seconds),
                    intensity: GamepadRumbleIntensity {
                        strong_motor: strong.clamp(0.0, 1.0),
                        weak_motor: weak.clamp(0.0, 1.0),
                    },
                    gamepad,
                }
            };

            world.write_message(request);
            status::OK
        })
    })
}

/// Connects a pretended pad under `name` and answers its entity, or zero where the world cannot
/// take one. It is a pad from the next frame, when Bevy has processed its connection.
///
/// # Safety
/// `name` must point to `len` readable bytes of UTF-8, or be null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_gamepad_connect(name: *const u8, len: u32) -> u64 {
    crate::interop::guard_with(0u64, || {
        let name = if name.is_null() || len == 0 {
            "Console pad".to_string()
        } else {
            let bytes = unsafe { core::slice::from_raw_parts(name, len as usize) };
            String::from_utf8_lossy(bytes).into_owned()
        };

        crate::state::with_world_opt(|world| {
            let gamepad = world.spawn_empty().id();
            connection(world, gamepad, GamepadConnection::Connected { name, vendor_id: None, product_id: None });
            gamepad.to_bits()
        })
        .unwrap_or(0)
    })
}

/// Disconnects a pretended pad, which Bevy takes its `Gamepad` from as it would a real one's.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_gamepad_disconnect(entity: u64) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let gamepad = crate::ecs::entity_from(entity);
            if world.get_entity(gamepad).is_err() {
                return status::NO_ENTITY;
            }

            connection(world, gamepad, GamepadConnection::Disconnected);
            status::OK
        })
    })
}

/// Sets one of a pad's buttons to `value`, from zero to one, where a button pressed past Bevy's
/// threshold, three quarters by default, counts as down.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_gamepad_button(entity: u64, button: i32, value: f32) -> i32 {
    crate::interop::guard(|| {
        let Some(button) = usize::try_from(button).ok().and_then(|index| BUTTONS.get(index)) else {
            return status::NULL_ARG;
        };

        with_world(|world| {
            let gamepad = crate::ecs::entity_from(entity);
            world.write_message(RawGamepadEvent::Button(RawGamepadButtonChangedEvent::new(gamepad, *button, value.clamp(0.0, 1.0))));
            status::OK
        })
    })
}

/// Sets one of a pad's axes to `value`, the four stick axes from minus one to one and the two
/// triggers, which Bevy keeps as buttons, from zero to one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_gamepad_axis(entity: u64, axis: i32, value: f32) -> i32 {
    crate::interop::guard(|| {
        let event = |gamepad: Entity| match axis {
            0 => Some(RawGamepadEvent::Axis(RawGamepadAxisChangedEvent::new(gamepad, GamepadAxis::LeftStickX, value.clamp(-1.0, 1.0)))),
            1 => Some(RawGamepadEvent::Axis(RawGamepadAxisChangedEvent::new(gamepad, GamepadAxis::LeftStickY, value.clamp(-1.0, 1.0)))),
            2 => Some(RawGamepadEvent::Axis(RawGamepadAxisChangedEvent::new(gamepad, GamepadAxis::RightStickX, value.clamp(-1.0, 1.0)))),
            3 => Some(RawGamepadEvent::Axis(RawGamepadAxisChangedEvent::new(gamepad, GamepadAxis::RightStickY, value.clamp(-1.0, 1.0)))),
            4 => Some(RawGamepadEvent::Button(RawGamepadButtonChangedEvent::new(gamepad, GamepadButton::LeftTrigger2, value.clamp(0.0, 1.0)))),
            5 => Some(RawGamepadEvent::Button(RawGamepadButtonChangedEvent::new(gamepad, GamepadButton::RightTrigger2, value.clamp(0.0, 1.0)))),
            _ => None,
        };

        with_world(|world| {
            let Some(message) = event(crate::ecs::entity_from(entity)) else {
                return status::NULL_ARG;
            };

            world.write_message(message);
            status::OK
        })
    })
}

/// Writes a connection as gilrs does, once raw for Bevy's processing and once as the event its
/// connection system reads, which adds or takes away the `Gamepad`.
fn connection(world: &mut World, gamepad: Entity, connection: GamepadConnection) {
    let event = GamepadConnectionEvent::new(gamepad, connection);
    world.write_message(RawGamepadEvent::Connection(event.clone()));
    world.write_message(event);
}
