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

        /// The other way round, a bit index back to the key that owns it.
        ///
        /// What a synthetic keypress needs. The table is the shared truth about which key is
        /// which, so reading it backwards is the only way to send one without the two sides
        /// keeping separate lists.
        pub fn key_from(index: usize) -> Option<KeyCode> {
            let mut at = 0usize;
            $(
                if index == at { return Some(KeyCode::$name); }
                at += 1;
            )*
            let _ = at;
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
/// pointer's report begins, so everything downstream behaves exactly as it would. The camera reads
/// the button, picking raycasts the meshes, and a gizmo takes hold. An offscreen run, which has no
/// window, has the pointer put on the image it draws into, for picking (see [`offscreen_pointer`]).
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
                    return offscreen_pointer(world, Vec2::new(x, y), action, button);
                };

                // Moved first whatever the action is, because a press somewhere the pointer has
                // never been is a press on whatever it was last over.
                let moved = CursorMoved {
                    window,
                    position: Vec2::new(x, y),
                    delta: None,
                };

                world.write_message(moved.clone());

                // And again as a window event, which is the one picking reads. Winit writes both
                // for every real pointer, so writing one is writing half a pointer.
                write_window_event(world, bevy::window::WindowEvent::CursorMoved(moved));

                // And the window is told where the pointer now is, which everything asking for a
                // cursor position reads. Not `set_cursor_position`, which moves the hand's own
                // pointer on the desktop and fails on a compositor that will not have it.
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

                // Only as messages, which Bevy's input system turns into the button's state at the
                // start of the next frame, as it does a real click. Pressing the state directly as
                // well counted a click twice, once now and once when the message arrived, and a
                // press written after the frame's input was read was cleared before anything saw
                // it. A press and a release in one call still reads as both, on the same frame.
                world.write_message(pressed.clone());
                write_window_event(world, bevy::window::WindowEvent::MouseButtonInput(pressed));

                crate::interop::status::OK
            })
        }
    })
}

/// Turns the wheel as though a hand had. `y` is away from the hand, which is up a list, and `x` is
/// to the right, in lines when `unit` is 0 and in pixels when it is 1.
///
/// Written as Bevy's `MouseWheel`, which is where a real wheel's report begins, so the frame's
/// accumulated scroll counts it, a game reading the wheel sees it, and an interface fed from that
/// sees it. A window's wheel goes to picking as a window event as well, as winit writes both, and
/// picking turns it into a scroll of the mouse's pointer where that last was. An offscreen run has
/// no window for that to name, so the scroll is put straight onto the image it draws into, where the
/// pretend pointer was last put (see [`offscreen_pointer`]). A headless run has no picking, and the
/// message alone is all a wheel there is.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_input_wheel(x: f32, y: f32, unit: i32) -> i32 {
    crate::interop::guard(|| {
        use bevy::input::mouse::{MouseScrollUnit, MouseWheel};
        use bevy::input::touch::TouchPhase;
        use bevy::prelude::Entity;

        let unit = if unit == 1 { MouseScrollUnit::Pixel } else { MouseScrollUnit::Line };

        crate::state::with_world(|world| {
            #[cfg(feature = "render")]
            {
                use bevy::window::PrimaryWindow;

                let mut windows = world.query_filtered::<Entity, bevy::prelude::With<PrimaryWindow>>();
                if let Ok(window) = windows.single(world) {
                    let wheel = MouseWheel { unit, x, y, window, phase: TouchPhase::Moved };
                    world.write_message(wheel);
                    write_window_event(world, bevy::window::WindowEvent::MouseWheel(wheel));
                    return crate::interop::status::OK;
                }

                offscreen_wheel(world, x, y, unit);
            }

            // Naming no window, which the accumulated scroll does not ask about.
            world.write_message(MouseWheel { unit, x, y, window: Entity::PLACEHOLDER, phase: TouchPhase::Moved });
            crate::interop::status::OK
        })
    })
}

/// Scrolls the mouse's pointer on the image an offscreen run draws into, where it was last put,
/// through Bevy's `PointerInput`, which picking sends to everything the pointer is over as a
/// `Pointer<Scroll>`. Nothing for a run with no such image.
#[cfg(feature = "render")]
fn offscreen_wheel(world: &mut bevy::prelude::World, x: f32, y: f32, unit: bevy::input::mouse::MouseScrollUnit) {
    use bevy::camera::NormalizedRenderTarget;
    use bevy::input::touch::TouchPhase;
    use bevy::picking::pointer::{Location, PointerAction, PointerId, PointerInput};

    let Some(target) = world.get_resource::<crate::offscreen::OffscreenTarget>() else {
        return;
    };
    let location = Location {
        target: NormalizedRenderTarget::Image(target.image.clone().into()),
        position: world.get_resource::<OffscreenPointer>().map_or(bevy::prelude::Vec2::ZERO, |at| at.0),
    };
    let action = PointerAction::Scroll { x, y, unit, phase: TouchPhase::Moved };
    world.write_message(PointerInput::new(PointerId::Mouse, location, action));
}

/// Where the pretend pointer was last put in an offscreen run, for how far the next move goes and
/// for the cursor's position the frame's input reports.
#[cfg(feature = "render")]
#[derive(bevy::prelude::Resource, Default)]
pub(crate) struct OffscreenPointer(pub bevy::prelude::Vec2);

/// Moves, presses or releases the mouse's pointer over the image an offscreen run draws into.
///
/// An offscreen run has no window for the cursor's messages to name, and its cameras draw into an
/// image, so the pointer is put on that image instead, through Bevy's own `PointerInput`, which is
/// what picking reads every pointer from. What is drawn there is then pointed at as it would be in
/// a window, the interface's nodes, sprites and meshes alike, so a test or a script drives an
/// offscreen run as it would a windowed one. The button is written as Bevy's `MouseButtonInput` as
/// well, naming no window, which Bevy's input turns into the button's state and picking does not
/// read, so a game reading the mouse sees it too. Returns
/// [`crate::interop::status::INVALID_STATE`] for a run with neither a window nor such an image.
#[cfg(feature = "render")]
fn offscreen_pointer(world: &mut bevy::prelude::World, at: bevy::prelude::Vec2, action: i32, button: i32) -> i32 {
    use bevy::camera::NormalizedRenderTarget;
    use bevy::picking::pointer::{Location, PointerAction, PointerButton, PointerId, PointerInput};

    let Some(target) = world.get_resource::<crate::offscreen::OffscreenTarget>() else {
        return crate::interop::status::INVALID_STATE;
    };
    let location = Location {
        target: NormalizedRenderTarget::Image(target.image.clone().into()),
        position: at,
    };

    let was = world.get_resource_or_insert_with(OffscreenPointer::default).0;
    world.resource_mut::<OffscreenPointer>().0 = at;

    // Moved first whatever the action is, as a window's pointer is, so a press lands where it is.
    world.write_message(PointerInput::new(PointerId::Mouse, location.clone(), PointerAction::Move { delta: at - was }));

    let button = match button {
        1 => PointerButton::Secondary,
        2 => PointerButton::Middle,
        _ => PointerButton::Primary,
    };
    let (pressed, state) = match action {
        1 => (PointerAction::Press(button), bevy::input::ButtonState::Pressed),
        2 => (PointerAction::Release(button), bevy::input::ButtonState::Released),
        _ => return crate::interop::status::OK,
    };
    world.write_message(PointerInput::new(PointerId::Mouse, location, pressed));
    world.write_message(bevy::input::mouse::MouseButtonInput {
        button: match button {
            PointerButton::Secondary => bevy::input::mouse::MouseButton::Right,
            PointerButton::Middle => bevy::input::mouse::MouseButton::Middle,
            PointerButton::Primary => bevy::input::mouse::MouseButton::Left,
        },
        state,
        window: bevy::prelude::Entity::PLACEHOLDER,
    });

    crate::interop::status::OK
}

/// Says something as the platform's input method would: `0` composing `text` with the caret over
/// `start` to `end` (bytes of the UTF-8, or `-1` to hide it), `1` committing `text`, `2` turned on,
/// `3` turned off.
///
/// For a test or a tool driving a text field that takes composed input, which no key press can
/// produce. The message names the primary window where there is one, and no window otherwise, since
/// nothing reading it here asks which.
///
/// # Safety
/// `text` must point to `len` readable bytes, or be null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_input_ime(kind: i32, text: *const u8, len: u32, start: i32, end: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, text, len, start, end);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::prelude::*;
            use bevy::window::{Ime, PrimaryWindow};

            let value = if text.is_null() || len == 0 {
                String::new()
            } else {
                let bytes = unsafe { core::slice::from_raw_parts(text, len as usize) };
                match core::str::from_utf8(bytes) {
                    Ok(text) => text.to_string(),
                    Err(_) => return crate::interop::status::NULL_ARG,
                }
            };

            crate::state::with_world(|world| {
                let mut windows = world.query_filtered::<Entity, With<PrimaryWindow>>();
                let window = windows.single(world).unwrap_or(Entity::PLACEHOLDER);

                let message = match kind {
                    0 => Ime::Preedit {
                        window,
                        value,
                        cursor: (start >= 0 && end >= 0).then_some((start as usize, end as usize)),
                    },
                    1 => Ime::Commit { window, value },
                    2 => Ime::Enabled { window },
                    3 => Ime::Disabled { window },
                    _ => return crate::interop::status::NULL_ARG,
                };

                world.write_message(message);
                crate::interop::status::OK
            })
        }
    })
}

/// Presses or releases a key, as though a hand had, with whatever text it produced.
///
/// The other half of [`bcs_input_pointer`]. A test that can move a pointer but not press a key
/// cannot reach a text field at all, and the path from the window to a field is exactly where the
/// interesting failures are.
///
/// `key` is the bit index the key table gives the key, `action` is 1 to press and 2 to release.
/// `text` holds what the keypress typed, as UTF-8, or null for a key that types nothing; it is only
/// carried on a press, as a window does.
///
/// # Safety
/// `text` must point to `len` readable bytes, or be null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_input_key(key: i32, action: i32, text: *const u8, len: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (key, action, text, len);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::input::ButtonState;
            use bevy::input::keyboard::{Key, KeyboardInput};
            use bevy::prelude::*;
            use bevy::window::PrimaryWindow;

            let Ok(index) = usize::try_from(key) else {
                return crate::interop::status::NULL_ARG;
            };

            let Some(code) = key_from(index) else {
                return crate::interop::status::NULL_ARG;
            };

            let state = match action {
                1 => ButtonState::Pressed,
                2 => ButtonState::Released,
                _ => return crate::interop::status::NULL_ARG,
            };

            let typed = if text.is_null() || len == 0 || state != ButtonState::Pressed {
                None
            } else {
                let bytes = unsafe { core::slice::from_raw_parts(text, len as usize) };

                // Whatever `KeyboardInput` holds its text in, which is one of two types
                // depending on how the engine was built. Converted rather than named.
                match core::str::from_utf8(bytes) {
                    Ok(text) => Some(text.into()),
                    Err(_) => return crate::interop::status::NULL_ARG,
                }
            };

            crate::state::with_world(|world| {
                // An offscreen run has no window, and a key still means something there, since a
                // game's systems read the keyboard and not the window it came from. The
                // placeholder stands in for the window, as it does for the IME above, so a script
                // can play a game on a machine with no display.
                let mut windows = world.query_filtered::<Entity, With<PrimaryWindow>>();
                let window = windows.single(world).unwrap_or(Entity::PLACEHOLDER);

                let logical = match typed.clone() {
                    Some(text) => Key::Character(text),
                    None => Key::Unidentified(bevy::input::keyboard::NativeKey::Unidentified),
                };

                let press = KeyboardInput {
                    key_code: code,
                    logical_key: logical,
                    state,
                    text: typed,
                    repeat: false,
                    window,
                };

                // Only as a message, as a real key arrives, for the reason the pointer gives above.
                world.write_message(press.clone());

                // And as a window event, for the same reason the pointer writes both, which is that
                // winit writes each of them for every real key, so writing one is writing half a
                // keyboard.
                write_window_event(world, bevy::window::WindowEvent::KeyboardInput(press));

                crate::interop::status::OK
            })
        }
    })
}

/// Writes a window event where the app keeps them, which an app with no window plugin, a headless
/// one, does not, and where Bevy says so as an error for every event written.
#[cfg(feature = "render")]
fn write_window_event(world: &mut bevy::ecs::world::World, event: bevy::window::WindowEvent) {
    if world.contains_resource::<bevy::ecs::message::Messages<bevy::window::WindowEvent>>() {
        world.write_message(event);
    }
}
