//! App ownership and the "world loan" that lets C# reach into Bevy mid-system.
//!
//! While a registered C# system is executing, its `&mut World` is parked in a
//! thread-local raw pointer. Every `bcs_ecs_*` entry point reconstitutes a `&mut World`
//! from it for the duration of that one call and drops it again before returning, so no
//! two mutable borrows are ever live at once.
//!
//! The pointer is deliberately *thread-local*: behavior methods that the generator runs on worker
//! threads see a null world and get [`status::NO_WORLD`] back, which pushes users toward the
//! thread-safe command buffer instead of racing on the world.

use core::cell::{Cell, UnsafeCell};
use core::ptr;
use std::sync::{Arc, Mutex};

use bevy::app::App;
use bevy::ecs::world::World;

use crate::interop::status;

thread_local! {
    /// The world currently loaned to a C# callback on this thread, or null.
    static CURRENT_WORLD: Cell<*mut World> = const { Cell::new(ptr::null_mut()) };

    /// The row a behavior's method holds on this thread while it runs ([`BcsRowGuard`]).
    static ROW_GUARD: UnsafeCell<BcsRowGuard> = const {
        UnsafeCell::new(BcsRowGuard { entity: 0, address: 0, component: -1, running: 0, moved: 0 })
    };
}

/// The row of a table a behavior's method holds by reference while it runs, which the runner
/// names here before each method so a call the method makes that moves the row is noticed.
///
/// A call that inserts a table component on the running entity, or takes one off, moves the entity
/// to another table and the old table's last row into its place, and one that adds an entity to
/// the table can grow its columns into new memory. The method's reference then points at another
/// entity's row or at memory the table let go of. Every entry point reaches the world through
/// [`with_world`] or [`with_world_opt`], so each looks, as it returns, for the running entity's
/// component where the runner handed it out, and sets `moved` where it is elsewhere or gone, and
/// the runner writes the method's copy back to wherever the entity has gone. Only the thread a
/// world is loaned to calls in, so the guard is a thread's own, and a method on a worker thread,
/// which can reach no world, cannot move a row.
#[repr(C)]
pub struct BcsRowGuard {
    /// The running entity's bits, read while `running` is set.
    pub entity: u64,
    /// The address of its component as the runner handed it to the method.
    pub address: usize,
    /// The component's id, as `bcs_ecs_chunks` takes it.
    pub component: i32,
    /// Whether a walk is under way, set by the runner as it walks a run of a table and cleared
    /// when a move stops it or the walk ends.
    pub running: u32,
    /// Set here where a call left the component somewhere other than `address`, or gone, and
    /// cleared by the runner as it starts walking a run.
    pub moved: u32,
}

/// Sets the row guard's `moved` where the call that has run left the running method's component
/// somewhere other than where the runner handed it out.
///
/// A thread-local read and a branch where no method runs, and one entity's lookup where one does.
fn check_row(world: &World) {
    ROW_GUARD.with(|guard| {
        let guard = guard.get();
        // SAFETY: the guard is this thread's own, and the runner writes it only between calls, on
        // this thread, so nothing writes it while this reads.
        let guard = unsafe { &mut *guard };
        if guard.running == 0 {
            return;
        }

        let here = crate::ecs::component_from(guard.component).and_then(|component| {
            let entity = world.get_entity(crate::ecs::entity_from(guard.entity)).ok()?;
            entity.get_by_id(component).ok().map(|value| value.as_ptr() as usize)
        });
        if here != Some(guard.address) {
            guard.moved = 1;
        }
    });
}

/// Writes the address of this thread's row guard ([`BcsRowGuard`]), which the behavior runner
/// writes before each method it runs on the thread and reads after.
///
/// # Safety
/// `guard` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_row_guard(guard: *mut *mut BcsRowGuard) -> i32 {
    crate::interop::guard(|| {
        if guard.is_null() {
            return status::NULL_ARG;
        }

        let address = ROW_GUARD.with(|row| row.get());
        unsafe { guard.write(address) };
        status::OK
    })
}

/// Parks `world` for the duration of `f`, restoring the previous loan afterwards.
///
/// Nesting is supported (a C# system may trigger another registered system), and the
/// previous pointer is restored even if `f` unwinds, because the guard restores on drop.
pub fn loan_world<R>(world: &mut World, f: impl FnOnce() -> R) -> R {
    struct Restore(*mut World);
    impl Drop for Restore {
        fn drop(&mut self) {
            CURRENT_WORLD.with(|c| c.set(self.0));
        }
    }

    let previous = CURRENT_WORLD.with(|c| c.replace(world as *mut World));
    let _restore = Restore(previous);
    f()
}

/// Runs `f` with no world loaned, and the loan as it was after.
///
/// For a callback into C# from inside Bevy, a component hook's, where Bevy holds the world and the
/// loan higher in the stack, a C# system's, points at the same world. An entry point C# called from
/// there would make a second `&mut World` beside Bevy's, so it answers [`status::NO_WORLD`] instead.
pub fn without_world<R>(f: impl FnOnce() -> R) -> R {
    struct Restore(*mut World);
    impl Drop for Restore {
        fn drop(&mut self) {
            CURRENT_WORLD.with(|c| c.set(self.0));
        }
    }

    let previous = CURRENT_WORLD.with(|c| c.replace(ptr::null_mut()));
    let _restore = Restore(previous);
    f()
}

/// Runs `f` against the loaned world, or returns [`status::NO_WORLD`] if there is none.
///
/// This is the single choke point through which the ECS entry points touch Bevy.
pub fn with_world<F: FnOnce(&mut World) -> i32>(f: F) -> i32 {
    let ptr = CURRENT_WORLD.with(|c| c.get());
    if ptr.is_null() {
        return status::NO_WORLD;
    }
    // SAFETY: the pointer was parked by `loan_world` higher in this thread's call stack
    // and stays valid for that frame; no other `&mut World` is live for this thread while
    // this call runs, because `bcs_*` entry points never re-enter each other.
    let answer = f(unsafe { &mut *ptr });
    // SAFETY: as above, and the borrow `f` had has ended.
    check_row(unsafe { &*ptr });
    answer
}

/// Like [`with_world`] but for entry points whose failure value is not an `i32` status.
pub fn with_world_opt<T, F: FnOnce(&mut World) -> T>(f: F) -> Option<T> {
    let ptr = CURRENT_WORLD.with(|c| c.get());
    if ptr.is_null() {
        return None;
    }
    // SAFETY: see `with_world`.
    let answer = f(unsafe { &mut *ptr });
    // SAFETY: see `with_world`.
    check_row(unsafe { &*ptr });
    Some(answer)
}

/// A C# system entry point, a function pointer plus the opaque state C# gets back.
#[derive(Clone, Copy)]
pub struct SystemReg {
    /// The `extern "C"` callback into managed code.
    pub func: extern "C" fn(*mut core::ffi::c_void),
    /// Opaque handle C# uses to find the managed delegate again.
    pub user: *mut core::ffi::c_void,
}

// SAFETY: the pointers are only ever dereferenced by calling back into the .NET runtime,
// which is itself thread-safe. Bevy needs the bound to store them in a schedule.
unsafe impl Send for SystemReg {}
unsafe impl Sync for SystemReg {}

impl SystemReg {
    /// Invokes the managed callback.
    #[inline]
    pub fn invoke(&self) {
        crate::profile::managed(|| (self.func)(self.user));
    }
}

/// The list of `Cleanup`-stage callbacks, shared between the handle and the Bevy schedule.
///
/// It has to be shared rather than owned, because `App::run` moves the app out of the handle
/// (it replaces `self` with an empty `App`). Anything the cleanup system needs must therefore
/// travel with the schedule, not with the handle.
pub type CleanupList = Arc<Mutex<Vec<SystemReg>>>;

/// The opaque handle C# holds for the lifetime of the engine.
pub struct BcsApp {
    /// The Bevy app under construction, or running the loop.
    pub app: App,
    /// Callbacks to run on the way out, shared with the cleanup system in the schedule.
    pub cleanup: CleanupList,
    /// Set once `bcs_app_run` has been entered, to reject a second run.
    pub running: bool,
    /// The number the next C# system is given, which names its set for ordering.
    pub next_system: u32,
}

impl BcsApp {
    /// Wraps a freshly configured Bevy app and the cleanup list wired into its schedule.
    pub fn new(app: App, cleanup: CleanupList) -> Self {
        Self {
            app,
            cleanup,
            running: false,
            next_system: 0,
        }
    }
}

/// Dereferences a `BcsApp*` handed back from C#.
///
/// # Safety
/// `handle` must be null or a pointer previously returned by `bcs_app_create` and not yet
/// passed to `bcs_app_destroy`.
pub unsafe fn app_mut<'a>(handle: *mut BcsApp) -> Option<&'a mut BcsApp> {
    if handle.is_null() {
        None
    } else {
        Some(unsafe { &mut *handle })
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::ecs::component::Component;
    use bevy::ecs::entity::Entity;

    #[derive(Component)]
    struct Held(u32);

    #[derive(Component)]
    struct Mark;

    /// Makes `call` through [`with_world`] with the guard naming `entity`'s `Held` where it stands,
    /// as the runner names it before a method, and answers whether the call left it moved.
    fn moved_by(world: &mut World, entity: Entity, call: impl FnOnce(&mut World)) -> bool {
        let component = world.register_component::<Held>();
        let address = world.get::<Held>(entity).map_or(0, |held| held as *const Held as usize);
        let guard = ROW_GUARD.with(|guard| guard.get());
        unsafe {
            *guard = BcsRowGuard {
                entity: entity.to_bits(),
                address,
                component: component.index() as i32,
                running: 1,
                moved: 0,
            };
        }

        loan_world(world, || {
            with_world(|world| {
                call(world);
                status::OK
            })
        });

        let moved = unsafe { (*guard).moved != 0 };
        unsafe { (*guard).running = 0 };
        moved
    }

    #[test]
    fn a_call_that_moves_the_running_entitys_component_is_noticed_and_one_that_writes_it_is_not() {
        let mut world = World::new();
        let entity = world.spawn(Held(1)).id();
        world.spawn(Held(2));

        assert!(!moved_by(&mut world, entity, |world| world.get_mut::<Held>(entity).unwrap().0 = 3));
        assert!(moved_by(&mut world, entity, |world| {
            world.entity_mut(entity).insert(Mark);
        }));
        assert!(moved_by(&mut world, entity, |world| {
            world.despawn(entity);
        }));
    }

    #[test]
    fn a_table_grown_under_the_running_entity_is_noticed() {
        let mut world = World::new();
        let entity = world.spawn(Held(1)).id();

        assert!(moved_by(&mut world, entity, |world| {
            for i in 0..4096 {
                world.spawn(Held(i));
            }
        }));
    }
}
