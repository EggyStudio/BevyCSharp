//! Bevy's app states, reachable from C#.
//!
//! Not to be confused with [`crate::state`], which holds the world loan. These are Bevy `States`:
//! the menu/playing/paused axis a game scopes its systems to.
//!
//! A `States` type is a Rust type, and C# cannot define one. What it can do is choose a value, so
//! the bridge provides a fixed set of state types that each hold an `i32` and let the managed side
//! decide what the numbers mean. Each C# enum claims one of these slots on registration, which is
//! what keeps two unrelated state machines from treading on each other.
//!
//! The slot count is fixed because the types have to exist at compile time. Four is past what a
//! game normally needs: an app state, a pause state, and room to spare.

use bevy::app::App;
use bevy::ecs::world::World;
use bevy::state::app::AppExtStates;
use bevy::state::state::{NextState, OnEnter, OnExit, State, States};

use crate::interop::status;
use crate::state::{app_mut, loan_world, with_world, BcsApp, SystemReg};

/// Declares the state slots and the dispatch that reaches them by index.
///
/// Every slot is the same type with a different identity, which is the point: Bevy keys its state
/// resources, its transitions and its run conditions on the type, so two slots are two independent
/// state machines rather than two names for one.
macro_rules! define_slots {
    ($($ty:ident = $slot:literal),+ $(,)?) => {
        $(
            /// One state axis, whose values are given meaning by the managed side.
            #[derive(States, Default, Debug, Clone, Copy, PartialEq, Eq, Hash)]
            pub struct $ty(pub i32);
        )+

        /// How many slots exist, for the error the managed side reports when they run out.
        pub const SLOT_COUNT: i32 = 0 $(+ { let _ = $slot; 1 })+;

        fn insert(app: &mut App, slot: i32, initial: i32) -> i32 {
            match slot {
                $($slot => {
                    app.insert_state($ty(initial));
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        fn read(world: &World, slot: i32) -> Option<i32> {
            match slot {
                $($slot => world.get_resource::<State<$ty>>().map(|s| s.get().0),)+
                _ => None,
            }
        }

        /// Registers a system in the schedule Bevy runs when a slot enters or leaves a value.
        ///
        /// The two schedules are keyed by the state value as well as the type, so a system added
        /// here runs on that one transition and nothing else.
        fn add_edge(app: &mut App, slot: i32, value: i32, entering: bool, reg: SystemReg) -> i32 {
            let run = move |world: &mut World| loan_world(world, || reg.invoke());

            match slot {
                $($slot => {
                    if entering {
                        app.add_systems(OnEnter($ty(value)), run);
                    } else {
                        app.add_systems(OnExit($ty(value)), run);
                    }
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        fn queue(world: &mut World, slot: i32, value: i32) -> i32 {
            match slot {
                $($slot => match world.get_resource_mut::<NextState<$ty>>() {
                    Some(mut next) => {
                        next.set($ty(value));
                        status::OK
                    }
                    None => status::NOT_PRESENT,
                },)+
                _ => status::NULL_ARG,
            }
        }
    };
}

define_slots!(BcsState0 = 0, BcsState1 = 1, BcsState2 = 2, BcsState3 = 3);

/// Reports how many state slots this bridge provides.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_slots() -> i32 {
    SLOT_COUNT
}

/// Creates a state machine in `slot`, starting at `initial`.
///
/// Must happen before the app runs, because inserting a state adds the systems that apply its
/// transitions, and a schedule cannot be added to once the loop owns it.
///
/// # Safety
/// `handle` must be a live app.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_state_add(
    handle: *mut BcsApp,
    slot: i32,
    initial: i32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }

        insert(&mut app.app, slot, initial)
    })
}

/// Writes the current value of `slot` into `out`.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_state_get(slot: i32, out: *mut i32) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        with_world(|world| match read(world, slot) {
            Some(value) => {
                unsafe { out.write(value) };
                status::OK
            }
            None => status::NOT_PRESENT,
        })
    })
}

/// Queues a transition of `slot` to `value`.
///
/// Bevy applies it at the next transition point rather than immediately, which is what lets every
/// system in a frame agree on which state it is in.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_set(slot: i32, value: i32) -> i32 {
    crate::interop::guard(|| with_world(|world| queue(world, slot, value)))
}

/// Registers a C# system to run when `slot` enters or leaves `value`.
///
/// `edge` is `0` for entering and `1` for leaving. Unlike a stage, this runs once per transition
/// rather than once per frame, which is what makes it the place to build a level or tear one down.
///
/// # Safety
/// `handle` must be a live app; `func` must remain callable until the app is destroyed.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_state_add_system(
    handle: *mut BcsApp,
    slot: i32,
    value: i32,
    edge: i32,
    func: extern "C" fn(*mut core::ffi::c_void),
    user: *mut core::ffi::c_void,
) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }
        let entering = match edge {
            0 => true,
            1 => false,
            _ => return status::NULL_ARG,
        };

        add_edge(&mut app.app, slot, value, entering, SystemReg { func, user })
    })
}
