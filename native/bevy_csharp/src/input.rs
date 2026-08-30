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
