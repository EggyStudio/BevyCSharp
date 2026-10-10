//! Calling back into C# when one of Bevy's widgets reports what was done with it, through
//! observers of the widgets' own events.
//!
//! Bevy's widgets do not change themselves. A button triggers `Activate` when it is pressed, a
//! slider, a checkbox, a radio group and a tab list trigger `ValueChange` with the value the player
//! asked for, and a menu triggers `MenuEvent` to open or close, and whoever listens decides what
//! follows. A
//! C# game observes them as any entity event, so the first C# observer of a kind asks for an
//! observer here, which copies the event into one shape every kind fits and queues a call into C#,
//! made with the whole world on loan once the trigger finishes, as [`crate::pointer`] reports what
//! a pointer does.
//!
//! A menu's event goes on up the entity's parents, from the item to the popup and the button that
//! owns it. The observer here watches every entity, so it runs at each step, and calls C# at the
//! first alone, since C# takes an event it is handed up the parents itself, where its observers can
//! stop it. The others happen to the widget alone.

use core::ffi::c_void;

use crate::interop::status;

/// What a widget reported, every kind in one shape, a field a kind has no use for being zero.
///
/// The managed side's `NativeWidgetEvent`, field for field.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsWidgetEvent {
    /// The widget it happened to, the button activated or the slider changed.
    pub entity: u64,
    /// The value of a change that names an entity, the radio button a group's choice moved to or
    /// the tab a tab list's did.
    pub other: u64,
    /// Which of the six kinds, in the order of [`spawn`]'s match.
    pub kind: i32,
    /// The value of a change that is a number, a slider's.
    pub value: f32,
    /// The value of a change that is yes or no, a checkbox's, as one or zero, and for a tab list's
    /// whether it names a tab at all.
    pub flag: u32,
    /// One where the change is the last of its interaction, the button let go or the drag ended,
    /// and zero while it goes on.
    pub is_final: u32,
    /// What a menu is asked to do, zero to open, one to toggle, two to close every menu and three
    /// to give the focus back to its owner.
    pub action: i32,
    /// For a menu opened, which of its items takes the focus, zero the next, one the previous,
    /// two the first and three the last.
    pub navigation: i32,
}

/// What C# is called with, the event, valid only for the call, and the handle C# finds its
/// handlers through.
pub type WidgetCallback = unsafe extern "C" fn(event: *const BcsWidgetEvent, user: *mut c_void);

#[cfg(feature = "render")]
mod observers {
    use super::*;

    use bevy::ecs::observer::{Observer, On};
    use bevy::ecs::world::{DeferredWorld, World};
    use bevy::input_focus::tab_navigation::NavAction;
    use bevy::prelude::Entity;
    use bevy::ui_widgets::{Activate, MenuAction, MenuEvent, ValueChange};

    use crate::state::loan_world;

    /// The callback and the handle C# finds its handlers through, carried into the observer.
    #[derive(Clone, Copy)]
    pub struct Target {
        pub callback: WidgetCallback,
        pub user: *mut c_void,
    }

    // SAFETY: the pointers are only dereferenced by calling back into the .NET runtime, which is
    // thread-safe itself, and Bevy needs the bound to keep them in an observer.
    unsafe impl Send for Target {}
    unsafe impl Sync for Target {}

    impl Target {
        /// Calls C#, through the struct as a whole so a closure carries the struct and its
        /// bounds rather than the raw pointer inside it.
        fn call(&self, event: &BcsWidgetEvent) {
            unsafe { (self.callback)(event, self.user) };
        }
    }

    /// Queues the call into C#, for when the trigger has finished and the world is whole.
    fn forward(world: &mut DeferredWorld, target: Target, report: BcsWidgetEvent) {
        world.commands().queue(move |world: &mut World| {
            loan_world(world, || target.call(&report));
        });
    }

    fn navigation(action: NavAction) -> i32 {
        match action {
            NavAction::Next => 0,
            NavAction::Previous => 1,
            NavAction::First => 2,
            NavAction::Last => 3,
        }
    }

    /// An observer of one kind of thing a widget reports, by the numbers the managed side's
    /// `WidgetEvents` gives them.
    pub fn spawn(world: &mut World, kind: i32, target: Target) -> Option<Entity> {
        let observer = match kind {
            0 => Observer::new(move |event: On<Activate>, mut world: DeferredWorld| {
                let report = BcsWidgetEvent { entity: event.entity.to_bits(), kind, ..Default::default() };
                forward(&mut world, target, report);
            }),
            1 => Observer::new(move |event: On<ValueChange<f32>>, mut world: DeferredWorld| {
                let report = BcsWidgetEvent {
                    entity: event.source.to_bits(),
                    kind,
                    value: event.value,
                    is_final: event.is_final as u32,
                    ..Default::default()
                };
                forward(&mut world, target, report);
            }),
            2 => Observer::new(move |event: On<ValueChange<bool>>, mut world: DeferredWorld| {
                let report = BcsWidgetEvent {
                    entity: event.source.to_bits(),
                    kind,
                    flag: event.value as u32,
                    is_final: event.is_final as u32,
                    ..Default::default()
                };
                forward(&mut world, target, report);
            }),
            3 => Observer::new(move |event: On<ValueChange<Entity>>, mut world: DeferredWorld| {
                let report = BcsWidgetEvent {
                    entity: event.source.to_bits(),
                    kind,
                    other: event.value.to_bits(),
                    is_final: event.is_final as u32,
                    ..Default::default()
                };
                forward(&mut world, target, report);
            }),
            4 => Observer::new(move |event: On<MenuEvent>, mut world: DeferredWorld| {
                if event.source != event.original_event_target() {
                    return;
                }
                let (action, navigation) = match event.action {
                    MenuAction::Open(to) => (0, navigation(to)),
                    MenuAction::Toggle => (1, 0),
                    MenuAction::CloseAll => (2, 0),
                    MenuAction::FocusRoot => (3, 0),
                };
                let report = BcsWidgetEvent { entity: event.source.to_bits(), kind, action, navigation, ..Default::default() };
                forward(&mut world, target, report);
            }),
            5 => Observer::new(move |event: On<ValueChange<Option<Entity>>>, mut world: DeferredWorld| {
                let report = BcsWidgetEvent {
                    entity: event.source.to_bits(),
                    kind,
                    other: event.value.map_or(0, Entity::to_bits),
                    flag: event.value.is_some() as u32,
                    is_final: event.is_final as u32,
                    ..Default::default()
                };
                forward(&mut world, target, report);
            }),
            _ => return None,
        };

        Some(world.spawn(observer).id())
    }
}

/// Asks Bevy to report one kind of thing a widget reports to C#, through an observer it spawns,
/// and writes the observer's entity to `out`.
///
/// On the app before it runs, or on the world from inside a system where `app` is null. Returns
/// [`status::UNSUPPORTED`] in a build without the renderer, which has no widgets, and
/// [`status::NULL_ARG`] for a kind it does not know.
///
/// # Safety
/// `out` must be writable, and `app` a live app or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_observe_widget(
    app: *mut crate::state::BcsApp,
    kind: i32,
    callback: Option<WidgetCallback>,
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
            let _ = (app, kind, callback, user);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let target = observers::Target { callback, user };
            let watch = |world: &mut bevy::ecs::world::World| -> i32 {
                match observers::spawn(world, kind, target) {
                    Some(entity) => {
                        unsafe { out.write(entity.to_bits()) };
                        status::OK
                    }
                    None => status::NULL_ARG,
                }
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

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn every_field_sits_where_the_managed_side_reads_it() {
        assert_eq!(core::mem::offset_of!(BcsWidgetEvent, entity), 0);
        assert_eq!(core::mem::offset_of!(BcsWidgetEvent, other), 8);
        assert_eq!(core::mem::offset_of!(BcsWidgetEvent, kind), 16);
        assert_eq!(core::mem::offset_of!(BcsWidgetEvent, value), 20);
        assert_eq!(core::mem::offset_of!(BcsWidgetEvent, flag), 24);
        assert_eq!(core::mem::offset_of!(BcsWidgetEvent, is_final), 28);
        assert_eq!(core::mem::offset_of!(BcsWidgetEvent, action), 32);
        assert_eq!(core::mem::offset_of!(BcsWidgetEvent, navigation), 36);
        assert_eq!(core::mem::size_of::<BcsWidgetEvent>(), 40);
    }
}
