//! App ownership and the "world loan" that lets C# reach into Bevy mid-system.
//!
//! While a registered C# system is executing, its `&mut World` is parked in a
//! thread-local raw pointer. Every `bcs_ecs_*` entry point reconstitutes a `&mut World`
//! from it for the duration of that one call and drops it again before returning, so no
//! two mutable borrows are ever live at once.
//!
//! The pointer is deliberately *thread-local*: behaviour methods that the generator runs
//! on worker threads see a null world and get [`status::NO_WORLD`] back, which is what
//! pushes users toward the thread-safe command buffer instead of racing on the world.

use core::cell::Cell;
use core::ptr;
use std::sync::{Arc, Mutex};

use bevy::app::App;
use bevy::ecs::world::World;

use crate::interop::status;

thread_local! {
    /// The world currently loaned to a C# callback on this thread, or null.
    static CURRENT_WORLD: Cell<*mut World> = const { Cell::new(ptr::null_mut()) };
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
    f(unsafe { &mut *ptr })
}

/// Like [`with_world`] but for entry points whose failure value is not an `i32` status.
pub fn with_world_opt<T, F: FnOnce(&mut World) -> T>(f: F) -> Option<T> {
    let ptr = CURRENT_WORLD.with(|c| c.get());
    if ptr.is_null() {
        return None;
    }
    // SAFETY: see `with_world`.
    Some(f(unsafe { &mut *ptr }))
}

/// A C# system entry point: a function pointer plus the opaque state C# wants back.
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
        (self.func)(self.user);
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
    /// The Bevy app under construction, or driving the loop.
    pub app: App,
    /// Callbacks to run on the way out, shared with the cleanup system in the schedule.
    pub cleanup: CleanupList,
    /// Set once `bcs_app_run` has been entered, to reject a second run.
    pub running: bool,
}

impl BcsApp {
    /// Wraps a freshly configured Bevy app and the cleanup list wired into its schedule.
    pub fn new(app: App, cleanup: CleanupList) -> Self {
        Self {
            app,
            cleanup,
            running: false,
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
