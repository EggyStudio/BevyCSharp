//! Calling back into C# when one of its components is added to, inserted on, discarded from,
//! removed from or despawned with an entity, through Bevy's observers.
//!
//! A remove hook ([`crate::lifecycle`]) runs with the world borrowed and serves the library's own
//! bookkeeping. A game's code does more when a component comes or goes, reading other
//! components, spawning or recoloring a sprite, so it is given Bevy's observers, which run after
//! the change. Bevy names an observed component by its type, and a C# component is known to Bevy only
//! by its id, so each observer here is an untyped one watching one component id
//! ([`Observer::with_component`]).
//!
//! The observer itself only copies the component's bytes, while they are still there, and queues
//! a command. The command runs with the whole world and calls C# inside it, as a C# system is
//! called, so the managed handler can reach anything, and the bytes let a handler of a removal see
//! the value that went, as Bevy's own `On<Remove<T>>` can. Bevy applies the queue as the change
//! that triggered the observer finishes, so a change made from C# has been observed by the time
//! the call that made it returns.

use core::ffi::c_void;

use bevy::ecs::component::ComponentId;
use bevy::ecs::lifecycle::{AddEvent, DespawnEvent, DiscardEvent, InsertEvent, RemoveEvent};
use bevy::ecs::observer::{Observer, On};
use bevy::ecs::world::{DeferredWorld, World};
use bevy::prelude::Entity;

use crate::interop::status;
use crate::state::{loan_world, with_world};

/// What C# is called with: which of the five kinds of change, the component's id, the entity's
/// bits, and the component's bytes as they were when the observer ran, valid only for the call.
pub type LifecycleCallback =
    unsafe extern "C" fn(kind: i32, component: i32, entity: u64, data: *const u8, len: usize, user: *mut c_void);

/// The callback and the handle C# finds its handlers through, carried into the observer.
#[derive(Clone, Copy)]
struct Target {
    callback: LifecycleCallback,
    user: *mut c_void,
}

// SAFETY: the pointers are only dereferenced by calling back into the .NET runtime, which is
// thread-safe itself, and Bevy needs the bound to keep them in an observer.
unsafe impl Send for Target {}
unsafe impl Sync for Target {}

impl Target {
    /// Calls C#, through the struct as a whole so a closure carries the struct and its bounds
    /// rather than the raw pointer inside it.
    fn call(&self, kind: i32, component: i32, entity: Entity, bytes: &[u8]) {
        unsafe { (self.callback)(kind, component, entity.to_bits(), bytes.as_ptr(), bytes.len(), self.user) };
    }
}

/// The component's bytes on `entity`, copied, or nothing where it is no longer there.
fn bytes_of(world: &DeferredWorld, entity: Entity, id: ComponentId) -> Vec<u8> {
    let Some(size) = world.components().get_info(id).map(|info| info.layout().size()) else {
        return Vec::new();
    };
    let Ok(entity) = world.get_entity(entity) else {
        return Vec::new();
    };
    match entity.get_by_id(id) {
        Ok(ptr) => unsafe { core::slice::from_raw_parts(ptr.as_ptr(), size) }.to_vec(),
        Err(_) => Vec::new(),
    }
}

/// Queues the call into C#, with the bytes it is to see, for when the world is whole again.
fn queue(world: &mut DeferredWorld, target: Target, kind: i32, id: ComponentId, entity: Entity) {
    let bytes = bytes_of(world, entity, id);
    let component = id.index() as i32;
    world.commands().queue(move |world: &mut World| {
        loan_world(world, || target.call(kind, component, entity, &bytes));
    });
}

/// Spawns an observer of one kind of change to one component, and returns its entity.
fn spawn(world: &mut World, kind: i32, id: ComponentId, target: Target) -> Option<Entity> {
    let observer = match kind {
        0 => Observer::new(move |event: On<AddEvent>, mut world: DeferredWorld| queue(&mut world, target, kind, id, event.entity)),
        1 => Observer::new(move |event: On<InsertEvent>, mut world: DeferredWorld| queue(&mut world, target, kind, id, event.entity)),
        2 => Observer::new(move |event: On<DiscardEvent>, mut world: DeferredWorld| queue(&mut world, target, kind, id, event.entity)),
        3 => Observer::new(move |event: On<RemoveEvent>, mut world: DeferredWorld| queue(&mut world, target, kind, id, event.entity)),
        4 => Observer::new(move |event: On<DespawnEvent>, mut world: DeferredWorld| queue(&mut world, target, kind, id, event.entity)),
        _ => return None,
    };
    Some(world.spawn(observer.with_component(id)).id())
}

/// Watches `component` for one kind of change, `0` added, `1` inserted, `2` discarded, `3`
/// removed or `4` despawned with its entity, and calls `callback` after each, with the world whole.
///
/// The observer's entity is written to `out`, and despawning it stops the watch. Through the app
/// handle before the run and through the loaned world (with `app` null) during it, as component
/// hooks are.
///
/// # Safety
/// `app` must be null or a live app, `out` writable, and `callback` callable for the app's life.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_observe_component(
    app: *mut crate::state::BcsApp,
    kind: i32,
    component: i32,
    callback: Option<LifecycleCallback>,
    user: *mut c_void,
    out: *mut u64,
) -> i32 {
    crate::interop::guard(|| {
        let Some(callback) = callback else {
            return status::NULL_ARG;
        };
        if out.is_null() || component < 0 {
            return status::NULL_ARG;
        }
        let id = ComponentId::new(component as usize);
        let target = Target { callback, user };

        let watch = |world: &mut World| -> i32 {
            if world.components().get_info(id).is_none() {
                return status::NO_COMPONENT;
            }
            match spawn(world, kind, id, target) {
                Some(entity) => {
                    unsafe { out.write(entity.to_bits()) };
                    status::OK
                }
                None => status::NULL_ARG,
            }
        };

        if app.is_null() {
            return with_world(watch);
        }
        match unsafe { crate::state::app_mut(app) } {
            Some(app) => watch(app.app.world_mut()),
            None => status::NULL_ARG,
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use core::sync::atomic::{AtomicU32, Ordering};

    use bevy::app::App;
    use bevy::ecs::component::{ComponentCloneBehavior, ComponentDescriptor, StorageType};
    use std::alloc::Layout;

    static SEEN: AtomicU32 = AtomicU32::new(0);
    static VALUE: AtomicU32 = AtomicU32::new(0);

    unsafe extern "C" fn record(kind: i32, _component: i32, _entity: u64, data: *const u8, len: usize, _user: *mut c_void) {
        SEEN.fetch_add(1 << (kind * 4), Ordering::SeqCst);
        if kind == 3 && len == 4 {
            VALUE.store(unsafe { *(data as *const u32) }, Ordering::SeqCst);
        }
    }

    #[test]
    fn a_removal_is_seen_with_the_value_that_went() {
        let mut app = App::new();
        let world = app.world_mut();
        let descriptor = unsafe {
            ComponentDescriptor::new_with_layout(
                "Probe",
                StorageType::Table,
                Layout::new::<u32>(),
                None,
                true,
                false,
                ComponentCloneBehavior::Default,
                None,
            )
        };
        let id = world.register_component_with_descriptor(descriptor);
        let target = Target { callback: record, user: core::ptr::null_mut() };
        for kind in [0, 3] {
            spawn(world, kind, id, target).expect("an observer");
        }
        world.flush();

        let mut value = 41u32;
        let entity = world.spawn_empty().id();
        unsafe {
            let ptr = bevy::ptr::OwningPtr::new(core::ptr::NonNull::from(&mut value).cast());
            world.entity_mut(entity).insert_by_id(id, ptr);
        }
        world.flush();
        world.entity_mut(entity).remove_by_id(id);
        world.flush();

        assert_eq!(1, SEEN.load(Ordering::SeqCst) & 0xF, "the addition was not seen once");
        assert_eq!(1, (SEEN.load(Ordering::SeqCst) >> 12) & 0xF, "the removal was not seen once");
        assert_eq!(41, VALUE.load(Ordering::SeqCst));
    }
}
