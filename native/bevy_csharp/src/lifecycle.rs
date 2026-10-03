//! Calling back into C# when one of its components leaves an entity.
//!
//! A C# component is plain bytes to Bevy, registered with no drop glue, because the managed side
//! owns what the bytes mean. Most components need nothing done when they go. One holding a handle
//! into a store on the managed side (a list or a map too large or too dynamic to live in the
//! component's own bytes) does, or every despawn leaks the list it held. Bevy's `on_remove` hook
//! runs whenever a component leaves an entity, by removal or despawn, so it is the place to free
//! the store's slot.
//!
//! A hook is a plain function pointer, with no closure to carry which managed callback it calls,
//! so one function serves every component and looks the callback up by component id.
//!
//! `on_remove` alone, not `on_replace`. Inserting over a component replaces its value, and the
//! common read, modify, write of a component writes back the same handle it read, so freeing on a
//! replace would free the list the new value still holds.
//!
//! The same handles need the opposite when an entity is cloned. Bevy clones only components that
//! implement `Clone` or `Reflect`, which a C# component's bytes do not, so every C# component is
//! registered with [`cloned`] as its clone behavior. That copies the bytes, and first lets C#
//! replace each handle in the copy with a handle to a copy of what it named, so the clone and the
//! original do not share a list the first of them to go would free.

use std::collections::HashMap;
use std::sync::Mutex;

use bevy::ecs::component::ComponentId;
use bevy::ecs::entity::{ComponentCloneCtx, SourceComponent};
use bevy::ecs::lifecycle::HookContext;
use bevy::ecs::world::{DeferredWorld, World};

use crate::interop::status;
use crate::state::with_world;

/// What C# is called with when a component it registered a hook for leaves an entity: the
/// entity's bits, the component id, and a pointer to the component's bytes, valid only for the
/// call.
pub type RemoveCallback = unsafe extern "C" fn(entity: u64, component: i32, data: *const u8);

/// The callback for each component id that has one.
///
/// Global rather than per world, because a hook has no world-specific state to find it through.
/// Ids are reused by the next app in the same process, and registering for the new app replaces
/// the old entry, which only the old world's hooks could have reached.
static CALLBACKS: Mutex<Option<HashMap<usize, RemoveCallback>>> = Mutex::new(None);

/// What C# is called with when a component is cloned, the clone's bytes, to rewrite in place before
/// they are written to the new entity.
pub type CloneCallback = unsafe extern "C" fn(component: i32, data: *mut u8);

/// The clone callback for each component id that has one, kept as [`CALLBACKS`] is.
static CLONERS: Mutex<Option<HashMap<usize, CloneCallback>>> = Mutex::new(None);

/// How every C# component is cloned, its bytes copied after C# has rewritten the handles in them.
///
/// The bytes are copied into a buffer of the component's own layout first, because the callback
/// rewrites them and the source has to stay as it was.
pub(crate) fn cloned(source: &SourceComponent, context: &mut ComponentCloneCtx) {
    let layout = context.component_info().layout();
    let id = context.component_id().index();

    // A component with no fields has no bytes, and allocating none is not allowed, so a dangling
    // pointer of the right alignment stands in for the empty buffer.
    let buffer = if layout.size() == 0 {
        layout.align() as *mut u8
    } else {
        // SAFETY: the layout has a size, checked above.
        unsafe { std::alloc::alloc(layout) }
    };
    if buffer.is_null() {
        return;
    }

    // SAFETY: the source is a value of this component, of `layout.size()` bytes, and the buffer
    // has that size and the layout's alignment.
    unsafe { core::ptr::copy_nonoverlapping(source.ptr().as_ptr(), buffer, layout.size()) };

    let callback = CLONERS.lock().ok().and_then(|table| table.as_ref()?.get(&id).copied());
    if let Some(callback) = callback {
        // SAFETY: the buffer holds a copy of the component, which the callback rewrites in place.
        unsafe { callback(id as i32, buffer) };
    }

    // SAFETY: the buffer holds a value of this component's type. A C# component is plain bytes
    // with no drop glue, so the copy owns nothing the source still owns, the handles having been
    // replaced by the callback.
    unsafe {
        let pointer = bevy::ptr::Ptr::new(core::ptr::NonNull::new_unchecked(buffer));
        context.write_target_component_ptr(pointer);
    }

    if layout.size() != 0 {
        // SAFETY: allocated above with this layout, and copied out of by the write.
        unsafe { std::alloc::dealloc(buffer, layout) };
    }
}

/// The hook Bevy calls for every component with a C# remove callback.
fn removed(world: DeferredWorld, context: HookContext) {
    let callback = CALLBACKS
        .lock()
        .ok()
        .and_then(|table| table.as_ref()?.get(&context.component_id.index()).copied());
    let Some(callback) = callback else {
        return;
    };

    let Ok(entity) = world.get_entity(context.entity) else {
        return;
    };
    let Ok(data) = entity.get_by_id(context.component_id) else {
        return;
    };

    // SAFETY: the pointer is the component's bytes, which Bevy keeps alive for the hook's duration,
    // and the callback reads them and nothing else, since the world is borrowed by Bevy here.
    unsafe {
        callback(
            context.entity.to_bits(),
            context.component_id.index() as i32,
            data.as_ptr() as *const u8,
        )
    };
}

/// Attaches the remove hook to one component in one world.
fn attach(world: &mut World, component: i32, callback: RemoveCallback) -> i32 {
    if component < 0 {
        return status::NO_COMPONENT;
    }
    let id = ComponentId::new(component as usize);
    if world.components().get_info(id).is_none() {
        return status::NO_COMPONENT;
    }

    // Bevy refuses, by panicking, to change the hooks of a component that is already on an
    // entity, since entities added before the hook would leave without it.
    if world.archetypes().iter().any(|archetype| archetype.contains(id)) {
        return status::INVALID_STATE;
    }

    let Some(hooks) = world.register_component_hooks_by_id(id) else {
        return status::NO_COMPONENT;
    };
    // Refused when a hook is already attached, as a second attach for the same component finds
    // it. The hook is the same function either way, and the callback is replaced below, which is
    // what a second attach means.
    let _ = hooks.try_on_remove(removed);

    match CALLBACKS.lock() {
        Ok(mut table) => {
            table.get_or_insert_with(HashMap::new).insert(id.index(), callback);
            status::OK
        }
        Err(_) => status::INVALID_STATE,
    }
}

/// Calls `callback` with the copy of `component` an entity clone is about to receive, so the copy
/// can be given handles of its own.
///
/// The clone behavior itself is set when the component is registered, so this only records the
/// callback, and can be called at any time.
///
/// # Safety
/// `callback` must stay callable for the app's life.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_component_on_clone(
    component: i32,
    callback: Option<CloneCallback>,
) -> i32 {
    crate::interop::guard(|| {
        let Some(callback) = callback else {
            return status::NULL_ARG;
        };
        if component < 0 {
            return status::NO_COMPONENT;
        }

        match CLONERS.lock() {
            Ok(mut table) => {
                table.get_or_insert_with(HashMap::new).insert(component as usize, callback);
                status::OK
            }
            Err(_) => status::INVALID_STATE,
        }
    })
}

/// Calls `callback` whenever `component` leaves an entity, by removal or despawn.
///
/// Through the app handle before the run, and through the loaned world (with `app` null) during
/// it, the same split component registration has. It has to be called before any entity carries
/// the component, and answers `INVALID_STATE` after.
///
/// # Safety
/// `app` must be null or a live app, and `callback` must stay callable for the app's life.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_component_on_remove(
    app: *mut crate::state::BcsApp,
    component: i32,
    callback: Option<RemoveCallback>,
) -> i32 {
    crate::interop::guard(|| {
        let Some(callback) = callback else {
            return status::NULL_ARG;
        };

        if app.is_null() {
            return with_world(|world| attach(world, component, callback));
        }

        match unsafe { crate::state::app_mut(app) } {
            Some(app) => attach(app.app.world_mut(), component, callback),
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

    static SEEN: AtomicU32 = AtomicU32::new(0);

    unsafe extern "C" fn count(_entity: u64, _component: i32, data: *const u8) {
        let value = unsafe { *(data as *const u32) };
        SEEN.fetch_add(value, Ordering::SeqCst);
    }

    fn registered(app: &mut App) -> ComponentId {
        let layout = core::alloc::Layout::new::<u32>();
        let descriptor = unsafe {
            ComponentDescriptor::new_with_layout(
                "Counted",
                StorageType::Table,
                layout,
                None,
                true,
                ComponentCloneBehavior::Custom(cloned),
                None,
            )
        };
        app.world_mut().register_component_with_descriptor(descriptor)
    }

    #[test]
    fn the_callback_sees_the_bytes_of_a_component_as_it_leaves() {
        let mut app = App::new();
        let id = registered(&mut app);
        assert_eq!(status::OK, attach(app.world_mut(), id.index() as i32, count));

        let mut value = 7u32;
        let entity = app.world_mut().spawn_empty().id();
        unsafe {
            let ptr = bevy::ptr::OwningPtr::new(core::ptr::NonNull::from(&mut value).cast());
            app.world_mut().entity_mut(entity).insert_by_id(id, ptr);
        }

        let before = SEEN.load(Ordering::SeqCst);
        app.world_mut().despawn(entity);
        assert_eq!(before + 7, SEEN.load(Ordering::SeqCst));
    }

    unsafe extern "C" fn doubled(_component: i32, data: *mut u8) {
        unsafe { *(data as *mut u32) *= 2 };
    }

    #[test]
    fn a_clone_is_given_the_bytes_the_callback_rewrote_and_the_source_keeps_its_own() {
        // A component of bytes is cloned at all only through its clone behavior, since Bevy clones
        // nothing that implements neither Clone nor Reflect.
        let mut app = App::new();
        let id = registered(&mut app);
        let code = unsafe { bcs_component_on_clone(id.index() as i32, Some(doubled)) };
        assert_eq!(status::OK, code);

        let mut value = 21u32;
        let source = app.world_mut().spawn_empty().id();
        unsafe {
            let ptr = bevy::ptr::OwningPtr::new(core::ptr::NonNull::from(&mut value).cast());
            app.world_mut().entity_mut(source).insert_by_id(id, ptr);
        }

        let copy = app.world_mut().entity_mut(source).clone_and_spawn();
        let read = |entity: bevy::ecs::entity::Entity| unsafe {
            *app.world().entity(entity).get_by_id(id).unwrap().as_ptr().cast::<u32>()
        };

        assert_eq!(42, read(copy));
        assert_eq!(21, read(source));
    }

    #[test]
    fn a_hook_is_refused_once_an_entity_carries_the_component() {
        // Bevy panics on this rather than answering, so it is asked first.
        let mut app = App::new();
        let id = registered(&mut app);

        let mut value = 1u32;
        let entity = app.world_mut().spawn_empty().id();
        unsafe {
            let ptr = bevy::ptr::OwningPtr::new(core::ptr::NonNull::from(&mut value).cast());
            app.world_mut().entity_mut(entity).insert_by_id(id, ptr);
        }

        assert_eq!(status::INVALID_STATE, attach(app.world_mut(), id.index() as i32, count));
    }
}
