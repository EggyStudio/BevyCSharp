//! The input focus from C#: a key observed as it reaches the focused entity, the focus given and
//! moved along the tab order, and keys handed to the focused entity in a run with no window.
//!
//! Bevy hands each key to the entity with the input focus as a `FocusedInput<KeyboardInput>`,
//! which goes on up the entity's parents and then to the window, and its text fields, buttons and
//! menus take their keys from it. A C# game observes it as any entity event, so the first C#
//! observer asks for an observer here, which copies the key into one shape and queues a call into
//! C# with the whole world on loan, as [`crate::pointer`] reports what a pointer does. It calls C#
//! at the first step alone, the focused entity, since C# takes an event up the parents itself.

use core::ffi::c_void;

use crate::interop::status;

/// How long a key's logical name or its text may be, in bytes of UTF-8.
pub const KEY_TEXT_CAPACITY: usize = 28;

/// A key as it reached the focused entity, the managed side's `NativeFocusedKey` field for field.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsFocusedKey {
    /// The entity with the focus, which the key reached first.
    pub entity: u64,
    /// The physical key's place in the shared key table, or `-1` for one outside it.
    pub key: i32,
    /// One for a press and two for a release.
    pub state: i32,
    /// One where the key is held and the platform repeats it.
    pub repeat: u32,
    /// Zero for a named logical key, one for a character, two for a dead key and three for one the
    /// platform could not identify.
    pub logical_kind: u8,
    pub logical_len: u8,
    pub text_len: u8,
    /// One where the key typed something, which `text` then holds.
    pub has_text: u8,
    pub logical: [u8; KEY_TEXT_CAPACITY],
    pub text: [u8; KEY_TEXT_CAPACITY],
}

/// What C# is called with, the key, valid only for the call, and the handle C# finds its handlers
/// through.
pub type FocusedKeyCallback = unsafe extern "C" fn(key: *const BcsFocusedKey, user: *mut c_void);

#[cfg(feature = "render")]
mod observers {
    use super::*;

    use bevy::ecs::observer::{Observer, On};
    use bevy::ecs::world::{DeferredWorld, World};
    use bevy::input::keyboard::{Key, KeyboardInput};
    use bevy::input_focus::FocusedInput;
    use bevy::prelude::Entity;

    use crate::state::loan_world;

    /// The callback and the handle C# finds its handlers through, carried into the observer.
    #[derive(Clone, Copy)]
    pub struct Target {
        pub callback: FocusedKeyCallback,
        pub user: *mut c_void,
    }

    // SAFETY: the pointers are only dereferenced by calling back into the .NET runtime, which is
    // thread-safe itself, and Bevy needs the bound to keep them in an observer.
    unsafe impl Send for Target {}
    unsafe impl Sync for Target {}

    impl Target {
        /// Calls C#, through the struct as a whole so a closure carries the struct and its
        /// bounds rather than the raw pointer inside it.
        fn call(&self, key: &BcsFocusedKey) {
            unsafe { (self.callback)(key, self.user) };
        }
    }

    /// Copies as much of `text` as fits, answering how much that was.
    fn copy(into: &mut [u8; KEY_TEXT_CAPACITY], text: &str) -> u8 {
        let len = text.len().min(KEY_TEXT_CAPACITY);
        into[..len].copy_from_slice(&text.as_bytes()[..len]);
        len as u8
    }

    /// Spawns the observer of keys reaching the focused entity, and returns its entity.
    pub fn spawn(world: &mut World, target: Target) -> Entity {
        let observer = Observer::new(move |event: On<FocusedInput<KeyboardInput>>, mut world: DeferredWorld| {
            if event.focused_entity != event.original_event_target() {
                return;
            }

            let input = &event.input;
            let mut report = BcsFocusedKey {
                entity: event.focused_entity.to_bits(),
                key: crate::input::key_bit(input.key_code).map_or(-1, |bit| bit as i32),
                state: if input.state.is_pressed() { 1 } else { 2 },
                repeat: input.repeat as u32,
                logical_kind: 0,
                logical_len: 0,
                text_len: 0,
                has_text: 0,
                logical: [0; KEY_TEXT_CAPACITY],
                text: [0; KEY_TEXT_CAPACITY],
            };

            let (kind, logical) = match &input.logical_key {
                Key::Character(character) => (1, character.to_string()),
                Key::Dead(character) => (2, character.map(String::from).unwrap_or_default()),
                Key::Unidentified(_) => (3, String::new()),
                named => (0, format!("{named:?}")),
            };
            report.logical_kind = kind;
            report.logical_len = copy(&mut report.logical, &logical);
            if let Some(text) = &input.text {
                report.has_text = 1;
                report.text_len = copy(&mut report.text, text);
            }

            world.commands().queue(move |world: &mut World| {
                loan_world(world, || target.call(&report));
            });
        });

        world.spawn(observer).id()
    }

    /// Hands each key to the entity with the input focus, in a run with no window.
    ///
    /// Bevy hands keys out only where there is a primary window, which it names in the event as
    /// where the key goes after the focused entity's parents, so an offscreen run's text fields
    /// took no keys and nothing observed one. This hands them out as Bevy would, naming no window.
    /// The event's window is private to Bevy, so it is made through Bevy's reflection, which builds
    /// any reflected struct from its fields.
    pub fn dispatch_offscreen_keys(
        mut keys: bevy::prelude::MessageReader<KeyboardInput>,
        focus: Option<bevy::prelude::Res<bevy::input_focus::InputFocus>>,
        windows: bevy::prelude::Query<(), bevy::prelude::With<bevy::window::PrimaryWindow>>,
        offscreen: Option<bevy::prelude::Res<crate::offscreen::OffscreenTarget>>,
        mut commands: bevy::prelude::Commands,
    ) {
        use bevy::reflect::FromReflect;
        use bevy::reflect::structs::DynamicStruct;

        if !windows.is_empty() || offscreen.is_none() {
            keys.clear();
            return;
        }
        let Some(focused) = focus.and_then(|focus| focus.get()) else {
            keys.clear();
            return;
        };

        for key in keys.read() {
            let mut fields = DynamicStruct::default();
            fields.insert("focused_entity", focused);
            fields.insert("input", key.clone());
            fields.insert("window", Entity::PLACEHOLDER);
            if let Some(event) = FocusedInput::<KeyboardInput>::from_reflect(&fields) {
                commands.trigger(event);
            }
        }
    }
}

#[cfg(feature = "render")]
pub use observers::dispatch_offscreen_keys;

/// Asks Bevy to report each key that reaches the focused entity to C#, through an observer it
/// spawns, and writes the observer's entity to `out`.
///
/// On the app before it runs, or on the world from inside a system where `app` is null. Returns
/// [`status::UNSUPPORTED`] in a build without the renderer, which has no input focus.
///
/// # Safety
/// `out` must be writable, and `app` a live app or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_observe_focused_keys(
    app: *mut crate::state::BcsApp,
    callback: Option<FocusedKeyCallback>,
    user: *mut c_void,
    out: *mut u64,
) -> i32 {
    crate::interop::guard(|| {
        let Some(callback) = callback else {
            return status::NULL_ARG;
        };
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = (app, callback, user);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let target = observers::Target { callback, user };
            let watch = |world: &mut bevy::ecs::world::World| -> i32 {
                let entity = observers::spawn(world, target);
                unsafe { out.write(entity.to_bits()) };
                status::OK
            };

            if app.is_null() {
                return crate::state::with_world(watch);
            }
            match unsafe { crate::state::app_mut(app) } {
                Some(app) => watch(app.app.world_mut()),
                None => status::NULL_ARG,
            }
        }
    })
}

/// Gives `entity` the input focus, as Bevy's `InputFocus::set` does, `cause` zero for focus
/// navigated to and one for focus pressed into.
///
/// Through Bevy's own call rather than the field, since it also records the change, from which
/// Bevy tells the entity that lost the focus and the one that gained it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ui_focus(entity: u64, cause: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, cause);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::input_focus::{FocusCause, InputFocus};

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                if world.get_entity(entity).is_err() {
                    return status::NO_ENTITY;
                }
                let cause = if cause == 1 { FocusCause::Pressed } else { FocusCause::Navigated };
                world.get_resource_or_insert_with(InputFocus::default).set(entity, cause);
                status::OK
            })
        }
    })
}

/// Writes the entity the focus would move to along the tab order, Bevy's
/// `TabNavigation::navigate`, `action` zero for the next, one for the previous, two for the first
/// and three for the last, without moving it.
///
/// Returns one where it wrote one, and zero where there is nowhere to move it, no tab group or
/// nothing in one.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_navigate(action: i32, out: *mut u64) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = action;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::system::SystemState;
            use bevy::input_focus::InputFocus;
            use bevy::input_focus::tab_navigation::{NavAction, TabNavigation};

            let action = match action {
                1 => NavAction::Previous,
                2 => NavAction::First,
                3 => NavAction::Last,
                _ => NavAction::Next,
            };

            crate::state::with_world(|world| {
                world.get_resource_or_insert_with(InputFocus::default);
                let mut state = SystemState::<(TabNavigation, bevy::prelude::Res<InputFocus>)>::new(world);
                let Ok((navigation, focus)) = state.get(world) else {
                    return 0;
                };
                match navigation.navigate(&focus, action) {
                    Ok(next) => {
                        unsafe { out.write(next.to_bits()) };
                        1
                    }
                    Err(_) => 0,
                }
            })
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_focused_key_sits_where_the_managed_side_reads_it() {
        assert_eq!(core::mem::offset_of!(BcsFocusedKey, entity), 0);
        assert_eq!(core::mem::offset_of!(BcsFocusedKey, key), 8);
        assert_eq!(core::mem::offset_of!(BcsFocusedKey, state), 12);
        assert_eq!(core::mem::offset_of!(BcsFocusedKey, repeat), 16);
        assert_eq!(core::mem::offset_of!(BcsFocusedKey, logical_kind), 20);
        assert_eq!(core::mem::offset_of!(BcsFocusedKey, logical_len), 21);
        assert_eq!(core::mem::offset_of!(BcsFocusedKey, text_len), 22);
        assert_eq!(core::mem::offset_of!(BcsFocusedKey, has_text), 23);
        assert_eq!(core::mem::offset_of!(BcsFocusedKey, logical), 24);
        assert_eq!(core::mem::offset_of!(BcsFocusedKey, text), 52);
        assert_eq!(core::mem::size_of::<BcsFocusedKey>(), 80);
    }
}
