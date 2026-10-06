//! Bevy's app states, reachable from C#.
//!
//! Not to be confused with [`crate::state`], which holds the world loan. These are Bevy `States`:
//! the menu/playing/paused axis a game scopes its systems to.
//!
//! A `States` type is a Rust type, and C# cannot define one. What it can do is choose a value, so
//! the bridge provides a fixed set of state types that each hold an `i32` and let the managed side
//! decide what the numbers mean. Each C# enum claims one of these slots on registration, which
//! keeps two unrelated state machines from treading on each other.
//!
//! The slot count is fixed because the types have to exist at compile time, and each one costs
//! compile time rather than runtime: `insert_state` is generic, so a slot brings its own copy of
//! the state resources, the transition schedules and the systems that despawn what a state owns,
//! whether or not a game ever adds it. Eight covers an app state, a pause, a menu page, a phase
//! and room to spare, at about four seconds of build time each. Raising it is this list and a
//! rebuild; nothing else, because the managed side asks how many there are.

use core::sync::atomic::{AtomicI32, Ordering};

use bevy::app::App;
use bevy::ecs::world::World;
use bevy::state::app::AppExtStates;
use bevy::state::state::{NextState, OnEnter, OnExit, OnTransition, State, States};
use bevy::state::state_scoped::DespawnOnExit;

use crate::interop::status;
use crate::state::{app_mut, loan_world, with_world, BcsApp, SystemReg};

mod slots;
use slots::*;


/// Reports how many state slots this bridge provides.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_slots() -> i32 {
    SLOT_COUNT
}

/// Creates the computed state in `slot`, working its value out from a table.
///
/// A computed state is not set. It is worked out from another state whenever that one changes,
/// which suits "the interface is up on these three screens", where a plain state would leave two
/// facts to keep in step and a sub-state would only answer whether it exists.
///
/// `from` and `to` are the two halves of the table, of `count` entries each: a value of the source
/// axis and what the computed state is while the source holds it. A source value the table says
/// nothing about means the computed state does not exist at all, so [`bcs_state_get`] on it
/// answers [`status::NOT_PRESENT`] rather than a value, and [`bcs_state_set`] on it is always
/// refused because there is nothing to set.
///
/// `slot` counts computed states rather than axes. Those of axis `n` are the block starting at
/// `n * `[`bcs_state_computed_per_slot`]`()`, and everywhere else the same state is addressed as
/// that number plus [`bcs_state_slots`]`()` plus the total number of sub-states.
///
/// Must happen before the app runs, for the same reason a state must.
///
/// # Safety
/// `handle` must be a live app, and `from` and `to` must each point at `count` readable integers.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_computed_add(
    handle: *mut BcsApp,
    slot: i32,
    from: *const i32,
    to: *const i32,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }

        if count <= 0 || from.is_null() || to.is_null() {
            return status::NULL_ARG;
        }

        let count = count as usize;
        let from = unsafe { core::slice::from_raw_parts(from, count) };
        let to = unsafe { core::slice::from_raw_parts(to, count) };

        insert_computed(&mut app.app, slot, from, to, false)
    })
}

/// Reports how many joint states exist, each worked out from any of the axes at once.
///
/// They are addressed past every computed state, as [`bcs_state_slots`]`()` plus the total number
/// of sub-states plus the total number of computed states plus the joint's own number.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_joint_count() -> i32 {
    JOINT_COUNT
}

/// Reports how many sub-states over several axes exist.
///
/// They are addressed past every joint state, as the joint states' first number plus
/// [`bcs_state_joint_count`]`()` plus the sub-state's own number.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_joint_sub_count() -> i32 {
    JOINT_SUB_COUNT
}

/// Creates the sub-state over several axes in `slot`, counted among such sub-states alone, which
/// exists while each axis holds the value `wants` gives it and starts at `initial` each time it
/// comes into existence.
///
/// `wants` is one value an axis, in slot order, `count` of them, with `i32::MIN` for an axis the
/// sub-state does not name. A pause that means something only while playing online is a sub-state
/// of both the screen and the mode. Every axis named has to have been added, and this must happen
/// before the app runs, as for any sub-state.
///
/// # Safety
/// `handle` must be a live app, and `wants` must point at `count` readable integers.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_joint_substate_add(
    handle: *mut BcsApp,
    slot: i32,
    wants: *const i32,
    count: i32,
    initial: i32,
) -> i32 {
    crate::interop::guard(|| {
        if wants.is_null() || count < 0 {
            return status::NULL_ARG;
        }

        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }

        // SAFETY: the caller promises `count` readable integers at `wants`.
        let wants = unsafe { core::slice::from_raw_parts(wants, count as usize) };
        insert_joint_sub(&mut app.app, slot, wants, initial)
    })
}

/// Reports how many computed states exist in total, which is where joint slots start counting.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_computed_count() -> i32 {
    COMPUTED_COUNT
}

/// Reports how many computed states one state slot can carry.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_computed_per_slot() -> i32 {
    COMPUTED_PER_SLOT
}

/// Reports how many sub-states exist in total, which is where computed slots start counting.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_sub_count() -> i32 {
    SUB_COUNT
}

/// Reports how many sub-states one state slot can carry.
///
/// The managed side needs it to work a sub-state's slot out from its parent's, since the two are
/// laid out as one block of sub slots per axis.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_subs_per_slot() -> i32 {
    SUBS_PER_SLOT
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

/// Creates the sub-state in `slot`, existing only while its own axis holds `parent`.
///
/// A sub-state is a state whose existence is decided by another one, as a pause screen inside a run
/// is. Leaving the run should take the pause with it rather than leave a menu state that means
/// nothing. While the parent holds any other value there is no state at all, and [`bcs_state_get`]
/// on it reports [`status::NOT_PRESENT`] rather than a value.
///
/// `slot` here counts sub-states rather than axes. The sub-states of axis `n` are the block
/// starting at `n * `[`bcs_state_subs_per_slot`]`()`, and everywhere else the same sub-state is
/// addressed as that number plus [`bcs_state_slots`]`()`, so reading it, queueing a transition,
/// hanging a system off one of its edges and scoping an entity to it are the calls that already
/// exist rather than four more.
///
/// A fixed number of sub-states per axis, because Bevy names the parent as an associated type and
/// the types have to exist at compile time. More of them is more entries in the slot list and a
/// rebuild, and nothing else.
///
/// Must happen before the app runs, for the same reason a state must.
///
/// # Safety
/// `handle` must be a live app.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_substate_add(
    handle: *mut BcsApp,
    slot: i32,
    parent: i32,
    initial: i32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }

        insert_sub(&mut app.app, slot, parent, initial)
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
/// Bevy applies it at the next transition point rather than immediately, so every system in a frame
/// agrees on which state it is in.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_set(slot: i32, value: i32) -> i32 {
    crate::interop::guard(|| with_world(|world| queue(world, slot, value)))
}

/// Registers a C# system to run when `slot` enters or leaves `value`.
///
/// `edge` is `0` for entering and `1` for leaving. Unlike a stage, this runs once per transition
/// rather than once per frame, which makes it the place to build a level or tear one down.
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

/// Registers a C# system to run when `slot` moves from `from` to `to`, and on no other move.
///
/// Bevy runs it between the two values' exit and entry, so it sees what leaving `from` took away
/// and nothing entering `to` has built yet. For what differs by where a state came from, such as
/// a level kept as it was when play resumes from a pause but built again from the menu.
///
/// # Safety
/// `handle` must be a live app; `func` must remain callable until the app is destroyed.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_state_add_transition(
    handle: *mut BcsApp,
    slot: i32,
    from: i32,
    to: i32,
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

        add_transition(&mut app.app, slot, from, to, SystemReg { func, user })
    })
}

/// Marks an entity to be despawned when `slot` leaves `value`.
///
/// What removes a level without a teardown system listing everything it spawned. The entities a
/// screen produced say which screen they belong to, and leaving it takes them with it. Bevy acts
/// on this at the transition rather than in `OnExit`, so it covers every way out of the value.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_state_despawn_on_exit(entity: u64, slot: i32, value: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| scope(world, crate::ecs::entity_from(entity), slot, value))
    })
}

/// What the managed side's rule for computed states is called through, with the computed state's
/// number, the source's value, and where to write the result, answering non-zero when the state
/// exists for that value.
type ComputeRule = unsafe extern "C" fn(slot: i32, source: i32, result: *mut i32) -> i32;

/// The managed side's rule, set once, or zero for none.
static RULE: core::sync::atomic::AtomicUsize = core::sync::atomic::AtomicUsize::new(0);

/// Asks the managed side's rule what a computed state is while its source holds a value.
///
/// Bevy asks through a plain function with the source's value and nothing else, from whichever
/// thread runs the transition, so the rule is a function of that value alone and keeps no world.
fn by_rule(slot: usize, source: i32) -> Option<i32> {
    let rule = RULE.load(Ordering::Relaxed);
    if rule == 0 {
        return None;
    }

    let rule: ComputeRule = unsafe { core::mem::transmute::<usize, ComputeRule>(rule) };
    let mut result = 0;
    (unsafe { rule(slot as i32, source, &mut result) } != 0).then_some(result)
}

/// What the managed side's rule for joint states is called through, with the joint's number, the
/// value of every axis in slot order, a bit for each axis that holds a state, and where to write
/// the result, answering non-zero when the joint exists for those values.
type JointRule =
    unsafe extern "C" fn(slot: i32, values: *const i32, present: u32, result: *mut i32) -> i32;

/// The managed side's joint rule, set once, or zero for none.
static JOINT_RULE: core::sync::atomic::AtomicUsize = core::sync::atomic::AtomicUsize::new(0);

/// Asks the managed side's joint rule what a joint state is while the axes hold `values`, an axis
/// with no state being `None`.
fn by_joint_rule(slot: i32, values: &[Option<i32>]) -> Option<i32> {
    let rule = JOINT_RULE.load(Ordering::Relaxed);
    if rule == 0 {
        return None;
    }

    let mut numbers = [0i32; 32];
    let mut present = 0u32;

    for (axis, value) in values.iter().enumerate().take(numbers.len()) {
        if let Some(value) = value {
            numbers[axis] = *value;
            present |= 1 << axis;
        }
    }

    let rule: JointRule = unsafe { core::mem::transmute::<usize, JointRule>(rule) };
    let mut result = 0;
    (unsafe { rule(slot, numbers.as_ptr(), present, &mut result) } != 0).then_some(result)
}

/// Sets the function joint states are worked out by.
///
/// One function for every joint, told which by its number, as [`bcs_computed_rule`] is for
/// computed states.
///
/// # Safety
/// `rule` must be a function of the [`JointRule`] shape that stays callable while any app runs,
/// and never unwinds.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_joint_rule(rule: Option<JointRule>) -> i32 {
    crate::interop::guard(|| {
        JOINT_RULE.store(rule.map_or(0, |rule| rule as usize), Ordering::Relaxed);
        status::OK
    })
}

/// Creates the joint state in `slot`, counted among joints alone, worked out by the rule
/// [`bcs_joint_rule`] set from whichever axes it reads.
///
/// Must happen before the app runs, for the same reason a state must.
///
/// # Safety
/// `handle` must be a live app.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_joint_add(handle: *mut BcsApp, slot: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }

        insert_joint(&mut app.app, slot)
    })
}

/// Sets the function computed states added with [`bcs_computed_add_rule`] are worked out by.
///
/// One function for every such state, told which by its number, so the managed side keeps a
/// rule per state and the bridge keeps one pointer. Set before any of them is computed.
///
/// # Safety
/// `rule` must be a function of the [`ComputeRule`] shape that stays callable while any app runs,
/// and never unwinds.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_computed_rule(rule: Option<ComputeRule>) -> i32 {
    crate::interop::guard(|| {
        RULE.store(rule.map_or(0, |rule| rule as usize), Ordering::Relaxed);
        status::OK
    })
}

/// Creates the computed state in `slot`, working its value out from the rule
/// [`bcs_computed_rule`] set rather than from a table.
///
/// For what a table cannot state, such as "every level past the tenth", which would be a table
/// as long as the levels. The rule answers whether the state exists and what it is, given the
/// source's value. Otherwise the same as [`bcs_computed_add`].
///
/// # Safety
/// `handle` must be a live app.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_computed_add_rule(handle: *mut BcsApp, slot: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }

        insert_computed(&mut app.app, slot, &[], &[], true)
    })
}
