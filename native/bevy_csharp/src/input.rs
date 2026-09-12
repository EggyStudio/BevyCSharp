//! The keyboard ABI shared with C#.
//!
//! Bevy identifies keys with the `KeyCode` enum, whose discriminant values are an
//! internal detail. Rather than leak them, this module pins an explicit, ordered key
//! table. Each key owns a bit index, and `BcsInput`'s bitsets are indexed by it.
//! `Bevy.Key` on the C# side declares the identical list in the identical order, so the
//! two stay in lockstep, if you add a key here, add it there at the same position.

use bevy::input::keyboard::KeyCode;

/// Declares the key table once and derives the bit mapping from it.
macro_rules! key_table {
    ($($name:ident),* $(,)?) => {
        /// Number of keys with a reserved bit in the input bitsets.
        pub const KEY_COUNT: usize = 0 $(+ { let _ = stringify!($name); 1 })*;

        /// Maps a Bevy `KeyCode` to its bit index, or `None` for keys outside the table.
        pub fn key_bit(key: KeyCode) -> Option<usize> {
            let mut index = 0usize;
            $(
                if key == KeyCode::$name { return Some(index); }
                index += 1;
            )*
            let _ = index;
            None
        }
    };
}

key_table![
    // 0..=25: letters
    KeyA, KeyB, KeyC, KeyD, KeyE, KeyF, KeyG, KeyH, KeyI, KeyJ, KeyK, KeyL, KeyM,
    KeyN, KeyO, KeyP, KeyQ, KeyR, KeyS, KeyT, KeyU, KeyV, KeyW, KeyX, KeyY, KeyZ,
    // 26..=35: top-row digits
    Digit0, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9,
    // 36..=47: function keys
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    // 48..=59: editing and whitespace
    Escape, Enter, Tab, Space, Backspace, Delete, Insert,
    Home, End, PageUp, PageDown, CapsLock,
    // 60..=63: arrows
    ArrowLeft, ArrowRight, ArrowUp, ArrowDown,
    // 64..=71: modifiers
    ShiftLeft, ShiftRight, ControlLeft, ControlRight,
    AltLeft, AltRight, SuperLeft, SuperRight,
    // 72..=82: punctuation
    Minus, Equal, BracketLeft, BracketRight, Backslash, Semicolon,
    Quote, Backquote, Comma, Period, Slash,
    // 83..=99: numpad
    Numpad0, Numpad1, Numpad2, Numpad3, Numpad4, Numpad5, Numpad6, Numpad7,
    Numpad8, Numpad9, NumpadAdd, NumpadSubtract, NumpadMultiply, NumpadDivide,
    NumpadDecimal, NumpadEnter, NumLock,
    // 100..=103: misc
    PrintScreen, ScrollLock, Pause, ContextMenu,
];

/// Number of `u64` words needed to hold [`KEY_COUNT`] bits.
pub const KEY_WORDS: usize = KEY_COUNT.div_ceil(64);

/// Sets the bit for `key` in a bitset, ignoring keys outside the table.
#[inline]
pub fn set_key(bits: &mut [u64; KEY_WORDS], key: KeyCode) {
    if let Some(bit) = key_bit(key) {
        bits[bit / 64] |= 1u64 << (bit % 64);
    }
}

/// Moves, presses or releases the pointer, as though a hand had.
///
/// What a test drives the scene with. The window's own messages are written, which is where a real
/// pointer's report begins, so everything downstream behaves exactly as it would: the camera reads
/// the button, picking raycasts the meshes, and a gizmo takes hold.
///
/// `action` is 0 to move, 1 to press and 2 to release; `button` is 0 for left, 1 for right and 2
/// for middle. The position is in logical pixels from the window's top left.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_input_pointer(x: f32, y: f32, action: i32, button: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (x, y, action, button);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::input::ButtonState;
            use bevy::input::mouse::MouseButtonInput;
            use bevy::prelude::*;
            use bevy::window::{CursorMoved, PrimaryWindow, Window};

            crate::state::with_world(|world| {
                let mut windows =
                    world.query_filtered::<Entity, bevy::prelude::With<PrimaryWindow>>();

                let Ok(window) = windows.single(world) else {
                    return crate::interop::status::INVALID_STATE;
                };

                // Moved first whatever the action is: a press somewhere the pointer has never been
                // is a press on whatever it was last over.
                let moved = CursorMoved {
                    window,
                    position: Vec2::new(x, y),
                    delta: None,
                };

                world.write_message(moved.clone());

                // And again as a window event, which is the one picking reads. Winit writes both
                // for every real pointer, so writing one is writing half a pointer.
                world.write_message(bevy::window::WindowEvent::CursorMoved(moved));

                // And the window is told where the pointer now is, which is what everything asking
                // for a cursor position reads. Not `set_cursor_position`, which moves the hand's
                // own pointer on the desktop and fails on a compositor that will not have it.
                if let Some(mut held) = world.get_mut::<Window>(window) {
                    let scale = held.resolution.scale_factor();
                    held.set_physical_cursor_position(Some(
                        (Vec2::new(x, y) * scale).as_dvec2(),
                    ));
                }

                let button = match button {
                    1 => MouseButton::Right,
                    2 => MouseButton::Middle,
                    _ => MouseButton::Left,
                };

                let state = match action {
                    1 => ButtonState::Pressed,
                    2 => ButtonState::Released,
                    _ => return crate::interop::status::OK,
                };

                let pressed = MouseButtonInput {
                    button,
                    state,
                    window,
                };

                world.write_message(pressed.clone());
                world.write_message(bevy::window::WindowEvent::MouseButtonInput(pressed));

                // The state the rest of the frame reads, which the message only reaches next
                // frame: a test that presses and releases in one call would otherwise report
                // nothing to anything asking whether a button is down.
                if let Some(mut buttons) = world.get_resource_mut::<ButtonInput<MouseButton>>() {
                    match state {
                        ButtonState::Pressed => buttons.press(button),
                        ButtonState::Released => buttons.release(button),
                    }
                }

                crate::interop::status::OK
            })
        }
    })
}
