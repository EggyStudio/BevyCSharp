//! Bevy's input messages, one for each change in the order it came, drained for C#.
//!
//! The managed side's `Input` is each frame's state, which says what is down and what changed but
//! not in what order, nor what a pad's axis read between frames. Bevy reports each change as a
//! message as well, a key going down, the mouse moving, a finger lifting, a pad's button reaching a
//! value, read through a cursor a C# system cannot hold. As the window's messages are, they are
//! drained here once a frame into one array, each tagged with its kind, and the managed side posts
//! each to its own message bus, so `ctx.Read<KeyboardInput>()` reads them as Bevy's examples read
//! theirs. The kinds come one after another in a fixed order, so order holds within a kind, and
//! Bevy's `GamepadEvent` holds a pad's connections, buttons and axes in the one order they came.

use crate::focus::KEY_TEXT_CAPACITY;
use crate::gamepad::NAME_CAPACITY;
use crate::interop::status;

/// One input message, the managed side's `NativeInputMessage` field for field. What each field
/// holds depends on `kind`, as the managed side's reading of each kind says.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsInputMessage {
    /// Which message, numbered as [`Kind`] numbers them.
    pub kind: i32,
    /// The key's place in the shared key table, the mouse button, the scroll unit, the touch's
    /// phase, the pad's button or axis, or one where a pad connected, by kind.
    pub code: i32,
    /// One for a press and two for a release, or a wheel's phase.
    pub state: i32,
    /// Which of the optional fields hold something, as the `FLAG_` constants say.
    pub flags: u32,
    /// The window, or the pad.
    pub entity: u64,
    /// A touch's finger, or a pad's vendor in the high half of the low thirty-two bits and its
    /// product in the low half.
    pub id: u64,
    pub x: f32,
    pub y: f32,
    pub z: f32,
    pub w: f32,
    pub u: f32,
    /// A key's logical key as `BcsFocusedKey` holds it.
    pub logical_kind: u8,
    pub logical_len: u8,
    pub text_len: u8,
    pub name_len: u8,
    pub logical: [u8; KEY_TEXT_CAPACITY],
    pub text: [u8; KEY_TEXT_CAPACITY],
    /// A pad's name as it connected.
    pub name: [u8; NAME_CAPACITY],
}

pub const FLAG_REPEAT: u32 = 1;
pub const FLAG_TEXT: u32 = 2;
pub const FLAG_DELTA: u32 = 4;
pub const FLAG_FORCE: u32 = 8;
pub const FLAG_VENDOR: u32 = 16;
pub const FLAG_PRODUCT: u32 = 32;
pub const FLAG_CALIBRATED: u32 = 64;
pub const FLAG_ALTITUDE: u32 = 128;

/// The kinds of message, in the order a drain reads them.
pub enum Kind {
    Keyboard = 0,
    MouseButton = 1,
    MouseMotion = 2,
    CursorMoved = 3,
    MouseWheel = 4,
    Pinch = 5,
    Rotation = 6,
    DoubleTap = 7,
    Touch = 8,
    PadConnection = 9,
    PadAxis = 10,
    PadButton = 11,
    PadButtonState = 12,
    OrderedConnection = 13,
    OrderedButton = 14,
    OrderedAxis = 15,
}

impl BcsInputMessage {
    fn new(kind: Kind) -> Self {
        Self {
            kind: kind as i32,
            code: -1,
            state: 0,
            flags: 0,
            entity: 0,
            id: 0,
            x: 0.0,
            y: 0.0,
            z: 0.0,
            w: 0.0,
            u: 0.0,
            logical_kind: 0,
            logical_len: 0,
            text_len: 0,
            name_len: 0,
            logical: [0; KEY_TEXT_CAPACITY],
            text: [0; KEY_TEXT_CAPACITY],
            name: [0; NAME_CAPACITY],
        }
    }
}

/// Copies text into a fixed buffer, cut at a character's end, and answers how many bytes it took.
fn copy<const N: usize>(into: &mut [u8; N], text: &str) -> u8 {
    let mut end = text.len().min(N).min(u8::MAX as usize);
    while !text.is_char_boundary(end) {
        end -= 1;
    }
    into[..end].copy_from_slice(&text.as_bytes()[..end]);
    end as u8
}

mod read {
    use super::*;

    use bevy::input::gamepad::{
        GamepadAxis, GamepadAxisChangedEvent, GamepadButtonChangedEvent, GamepadButtonStateChangedEvent, GamepadConnection,
        GamepadConnectionEvent,
    };
    use bevy::input::keyboard::{Key, KeyboardInput};
    use bevy::input::mouse::{MouseButton, MouseButtonInput, MouseScrollUnit};
    use bevy::input::touch::{ForceTouch, TouchPhase};
    use bevy::input::ButtonState;

    pub fn state(state: ButtonState) -> i32 {
        if state.is_pressed() { 1 } else { 2 }
    }

    pub fn phase(phase: TouchPhase) -> i32 {
        match phase {
            TouchPhase::Started => 0,
            TouchPhase::Moved => 1,
            TouchPhase::Ended => 2,
            TouchPhase::Canceled => 3,
        }
    }

    pub fn keyboard(m: &KeyboardInput) -> BcsInputMessage {
        let mut out = BcsInputMessage::new(Kind::Keyboard);
        out.code = crate::input::key_bit(m.key_code).map_or(-1, |bit| bit as i32);
        out.state = state(m.state);
        out.entity = m.window.to_bits();
        if m.repeat {
            out.flags |= FLAG_REPEAT;
        }
        let (kind, logical) = match &m.logical_key {
            Key::Character(character) => (1, character.to_string()),
            Key::Dead(character) => (2, character.map(String::from).unwrap_or_default()),
            Key::Unidentified(_) => (3, String::new()),
            named => (0, format!("{named:?}")),
        };
        out.logical_kind = kind;
        out.logical_len = copy(&mut out.logical, &logical);
        if let Some(text) = &m.text {
            out.flags |= FLAG_TEXT;
            out.text_len = copy(&mut out.text, text);
        }
        out
    }

    pub fn mouse_button(m: &MouseButtonInput) -> BcsInputMessage {
        let mut out = BcsInputMessage::new(Kind::MouseButton);
        out.code = match m.button {
            MouseButton::Left => 0,
            MouseButton::Right => 1,
            MouseButton::Middle => 2,
            MouseButton::Back => 3,
            MouseButton::Forward => 4,
            MouseButton::Other(_) => -1,
        };
        out.state = state(m.state);
        out.entity = m.window.to_bits();
        out
    }

    pub fn wheel(m: &bevy::input::mouse::MouseWheel) -> BcsInputMessage {
        let mut out = BcsInputMessage::new(Kind::MouseWheel);
        out.code = match m.unit {
            MouseScrollUnit::Line => 0,
            MouseScrollUnit::Pixel => 1,
        };
        out.state = phase(m.phase);
        (out.x, out.y, out.entity) = (m.x, m.y, m.window.to_bits());
        out
    }

    pub fn touch(m: &bevy::input::touch::TouchInput) -> BcsInputMessage {
        let mut out = BcsInputMessage::new(Kind::Touch);
        out.code = phase(m.phase);
        (out.x, out.y, out.entity, out.id) = (m.position.x, m.position.y, m.window.to_bits(), m.id);
        match m.force {
            Some(ForceTouch::Normalized(force)) => {
                out.flags |= FLAG_FORCE;
                out.z = force as f32;
            }
            Some(ForceTouch::Calibrated { force, max_possible_force, altitude_angle }) => {
                out.flags |= FLAG_FORCE | FLAG_CALIBRATED;
                (out.z, out.w) = (force as f32, max_possible_force as f32);
                if let Some(altitude) = altitude_angle {
                    out.flags |= FLAG_ALTITUDE;
                    out.u = altitude as f32;
                }
            }
            None => {}
        }
        out
    }

    pub fn connection(m: &GamepadConnectionEvent, kind: Kind) -> BcsInputMessage {
        let mut out = BcsInputMessage::new(kind);
        out.entity = m.gamepad.to_bits();
        out.code = 0;
        if let GamepadConnection::Connected { name, vendor_id, product_id } = &m.connection {
            out.code = 1;
            out.name_len = copy(&mut out.name, name);
            if let Some(vendor) = vendor_id {
                out.flags |= FLAG_VENDOR;
                out.id |= (*vendor as u64) << 16;
            }
            if let Some(product) = product_id {
                out.flags |= FLAG_PRODUCT;
                out.id |= *product as u64;
            }
        }
        out
    }

    /// The pad's axis in the managed side's table, the sticks alone, since Bevy reports a pad's
    /// triggers as buttons.
    pub fn axis(m: &GamepadAxisChangedEvent, kind: Kind) -> BcsInputMessage {
        let mut out = BcsInputMessage::new(kind);
        out.code = match m.axis {
            GamepadAxis::LeftStickX => 0,
            GamepadAxis::LeftStickY => 1,
            GamepadAxis::RightStickX => 2,
            GamepadAxis::RightStickY => 3,
            _ => -1,
        };
        (out.entity, out.x) = (m.entity.to_bits(), m.value);
        out
    }

    pub fn button(m: &GamepadButtonChangedEvent, kind: Kind) -> BcsInputMessage {
        let mut out = BcsInputMessage::new(kind);
        out.code = crate::gamepad::BUTTONS.iter().position(|b| *b == m.button).map_or(-1, |i| i as i32);
        (out.entity, out.state, out.x) = (m.entity.to_bits(), state(m.state), m.value);
        out
    }

    pub fn button_state(m: &GamepadButtonStateChangedEvent) -> BcsInputMessage {
        let mut out = BcsInputMessage::new(Kind::PadButtonState);
        out.code = crate::gamepad::BUTTONS.iter().position(|b| *b == m.button).map_or(-1, |i| i as i32);
        (out.entity, out.state) = (m.entity.to_bits(), state(m.state));
        out
    }
}

/// Where the drain keeps its place in each queue between frames, its own, so a reader in Bevy
/// loses nothing to it.
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct InputMessageCursors {
    keyboard: bevy::ecs::message::MessageCursor<bevy::input::keyboard::KeyboardInput>,
    mouse_button: bevy::ecs::message::MessageCursor<bevy::input::mouse::MouseButtonInput>,
    mouse_motion: bevy::ecs::message::MessageCursor<bevy::input::mouse::MouseMotion>,
    #[cfg(feature = "render")]
    cursor_moved: bevy::ecs::message::MessageCursor<bevy::window::CursorMoved>,
    wheel: bevy::ecs::message::MessageCursor<bevy::input::mouse::MouseWheel>,
    pinch: bevy::ecs::message::MessageCursor<bevy::input::gestures::PinchGesture>,
    rotation: bevy::ecs::message::MessageCursor<bevy::input::gestures::RotationGesture>,
    double_tap: bevy::ecs::message::MessageCursor<bevy::input::gestures::DoubleTapGesture>,
    touch: bevy::ecs::message::MessageCursor<bevy::input::touch::TouchInput>,
    connection: bevy::ecs::message::MessageCursor<bevy::input::gamepad::GamepadConnectionEvent>,
    axis: bevy::ecs::message::MessageCursor<bevy::input::gamepad::GamepadAxisChangedEvent>,
    button: bevy::ecs::message::MessageCursor<bevy::input::gamepad::GamepadButtonChangedEvent>,
    button_state: bevy::ecs::message::MessageCursor<bevy::input::gamepad::GamepadButtonStateChangedEvent>,
    ordered: bevy::ecs::message::MessageCursor<bevy::input::gamepad::GamepadEvent>,
}

/// Copies each input message that arrived since the last call into `out`, kind after kind, and
/// answers how many it wrote. A full buffer is not an error, and what did not fit stays queued for
/// the next call, a message dropped being a key the game never heard go up.
///
/// # Safety
/// `out` must be writable for `capacity` messages.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_input_messages(out: *mut BcsInputMessage, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        use bevy::ecs::message::Messages;
        use bevy::input::gamepad::GamepadEvent;

        if out.is_null() && capacity > 0 {
            return status::NULL_ARG;
        }
        let capacity = capacity.max(0) as usize;

        crate::state::with_world(|world| {
            world.get_resource_or_insert_with(InputMessageCursors::default);
            let mut written = 0usize;

            world.resource_scope(|world, mut cursors: bevy::ecs::world::Mut<InputMessageCursors>| {
                // Each queue read through its cursor, stopping at the buffer's end so what is
                // left stays where the next call finds it.
                macro_rules! drain {
                    ($cursor:ident, $ty:ty, $make:expr) => {
                        if let Some(messages) = world.get_resource::<Messages<$ty>>() {
                            let read: Vec<$ty> =
                                cursors.$cursor.read(messages).take(capacity.saturating_sub(written)).cloned().collect();
                            for message in read {
                                #[allow(clippy::redundant_closure_call)]
                                let made: Option<BcsInputMessage> = ($make)(&message);
                                if let Some(made) = made {
                                    // SAFETY: `written < capacity`, checked by the take above.
                                    unsafe { out.add(written).write(made) };
                                    written += 1;
                                }
                            }
                        }
                    };
                }

                drain!(keyboard, bevy::input::keyboard::KeyboardInput, |m| Some(read::keyboard(m)));
                drain!(mouse_button, bevy::input::mouse::MouseButtonInput, |m| Some(read::mouse_button(m)));
                drain!(mouse_motion, bevy::input::mouse::MouseMotion, |m: &bevy::input::mouse::MouseMotion| {
                    let mut out = BcsInputMessage::new(Kind::MouseMotion);
                    (out.x, out.y) = (m.delta.x, m.delta.y);
                    Some(out)
                });
                #[cfg(feature = "render")]
                drain!(cursor_moved, bevy::window::CursorMoved, |m: &bevy::window::CursorMoved| {
                    let mut out = BcsInputMessage::new(Kind::CursorMoved);
                    (out.entity, out.x, out.y) = (m.window.to_bits(), m.position.x, m.position.y);
                    if let Some(delta) = m.delta {
                        out.flags |= FLAG_DELTA;
                        (out.z, out.w) = (delta.x, delta.y);
                    }
                    Some(out)
                });
                drain!(wheel, bevy::input::mouse::MouseWheel, |m| Some(read::wheel(m)));
                drain!(pinch, bevy::input::gestures::PinchGesture, |m: &bevy::input::gestures::PinchGesture| {
                    let mut out = BcsInputMessage::new(Kind::Pinch);
                    out.x = m.0;
                    Some(out)
                });
                drain!(rotation, bevy::input::gestures::RotationGesture, |m: &bevy::input::gestures::RotationGesture| {
                    let mut out = BcsInputMessage::new(Kind::Rotation);
                    out.x = m.0;
                    Some(out)
                });
                drain!(double_tap, bevy::input::gestures::DoubleTapGesture, |_: &bevy::input::gestures::DoubleTapGesture| Some(
                    BcsInputMessage::new(Kind::DoubleTap)
                ));
                drain!(touch, bevy::input::touch::TouchInput, |m| Some(read::touch(m)));
                drain!(connection, bevy::input::gamepad::GamepadConnectionEvent, |m| Some(read::connection(m, Kind::PadConnection)));
                drain!(axis, bevy::input::gamepad::GamepadAxisChangedEvent, |m| Some(read::axis(m, Kind::PadAxis)));
                drain!(button, bevy::input::gamepad::GamepadButtonChangedEvent, |m| Some(read::button(m, Kind::PadButton)));
                drain!(button_state, bevy::input::gamepad::GamepadButtonStateChangedEvent, |m| Some(read::button_state(m)));
                drain!(ordered, GamepadEvent, |m: &GamepadEvent| Some(match m {
                    GamepadEvent::Connection(e) => read::connection(e, Kind::OrderedConnection),
                    GamepadEvent::Button(e) => read::button(e, Kind::OrderedButton),
                    GamepadEvent::Axis(e) => read::axis(e, Kind::OrderedAxis),
                }));
            });

            written as i32
        })
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_message_sits_where_the_managed_side_reads_it() {
        assert_eq!(core::mem::offset_of!(BcsInputMessage, entity), 16);
        assert_eq!(core::mem::offset_of!(BcsInputMessage, x), 32);
        assert_eq!(core::mem::offset_of!(BcsInputMessage, u), 48);
        assert_eq!(core::mem::offset_of!(BcsInputMessage, logical_kind), 52);
        assert_eq!(core::mem::offset_of!(BcsInputMessage, logical), 56);
        assert_eq!(core::mem::offset_of!(BcsInputMessage, text), 56 + KEY_TEXT_CAPACITY);
        assert_eq!(core::mem::offset_of!(BcsInputMessage, name), 56 + 2 * KEY_TEXT_CAPACITY);
        assert_eq!(core::mem::size_of::<BcsInputMessage>(), 160);
    }

    #[test]
    fn text_is_cut_where_a_character_ends() {
        let mut into = [0u8; 4];
        assert_eq!(copy(&mut into, "aé€"), 3);
        assert_eq!(&into[..3], "aé".as_bytes());
    }
}
