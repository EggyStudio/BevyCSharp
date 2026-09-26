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
use bevy::state::state::{NextState, OnEnter, OnExit, State, States};
use bevy::state::state_scoped::DespawnOnExit;

use crate::interop::status;
use crate::state::{app_mut, loan_world, with_world, BcsApp, SystemReg};

/// Declares the state slots and the dispatch that reaches them by index.
///
/// Every slot is the same type with a different identity, which is the point: Bevy keys its state
/// resources, its transitions and its run conditions on the type, so two slots are two independent
/// state machines rather than two names for one.
macro_rules! define_slots {
    (
        states { $($ty:ident = $slot:literal),+ $(,)? }
        subs { $($sub:ident of $parent:ident at $subslot:literal),+ $(,)? }
        computed { $($derived:ident from $source:ident at $cslot:literal),+ $(,)? }
    ) => {
        $(
            /// One state axis, whose values are given meaning by the managed side.
            #[derive(States, Default, Debug, Clone, Copy, PartialEq, Eq, Hash)]
            pub struct $ty(pub i32);
        )+

        $(
            /// One sub-state of the axis it names, which exists only while that axis holds the
            /// value [`bcs_substate_add`] was given.
            #[derive(States, Default, Debug, Clone, Copy, PartialEq, Eq, Hash)]
            pub struct $sub(pub i32);

            impl bevy::state::state::SubStates for $sub {
                type SourceStates = $parent;

                fn should_exist(sources: $parent) -> Option<Self> {
                    // Read from a static rather than from the world, because Bevy asks this
                    // through a plain function with the parent's value and nothing else. The
                    // managed side writes both numbers before the app runs.
                    (sources.0 == SUB_PARENT[$subslot].load(Ordering::Relaxed))
                        .then(|| $sub(SUB_INITIAL[$subslot].load(Ordering::Relaxed)))
                }
            }
        )+

        $(
            /// One computed state, whose value is worked out from the axis it names rather than
            /// set. What a caller states is the table, and [`bcs_computed_add`] fills it in.
            ///
            /// No `States` derive, unlike the two above, because `ComputedStates` carries a
            /// blanket implementation of it and deriving one as well is two of the same.
            #[derive(Default, Debug, Clone, Copy, PartialEq, Eq, Hash)]
            pub struct $derived(pub i32);

            impl bevy::state::state::ComputedStates for $derived {
                type SourceStates = $source;

                fn compute(sources: $source) -> Option<Self> {
                    // A linear scan, because the table is a handful of entries and a caller
                    // writing one long enough for that to matter is describing a lookup rather
                    // than a state.
                    let used = COMPUTED_LEN[$cslot].load(Ordering::Relaxed).max(0) as usize;

                    for pair in 0..used.min(COMPUTED_PAIRS) {
                        if COMPUTED_FROM[$cslot][pair].load(Ordering::Relaxed) == sources.0 {
                            return Some($derived(
                                COMPUTED_TO[$cslot][pair].load(Ordering::Relaxed),
                            ));
                        }
                    }

                    // A parent value the table says nothing about means the state does not exist,
                    // which is the whole difference between a computed state and a derived value.
                    None
                }
            }
        )+

        /// How many slots exist, for the error the managed side reports when they run out.
        pub const SLOT_COUNT: i32 = 0 $(+ { let _ = $slot; 1 })+;

        /// How many computed states exist.
        pub const COMPUTED_COUNT: i32 = 0 $(+ { let _ = $cslot; 1 })+;

        /// How many computed states one axis can carry.
        pub const COMPUTED_PER_SLOT: i32 = COMPUTED_COUNT / SLOT_COUNT;

        /// How many parent values one computed state can name.
        pub const COMPUTED_PAIRS: usize = 16;

        /// Which parent value each entry of each computed state's table matches.
        static COMPUTED_FROM: [[AtomicI32; COMPUTED_PAIRS]; COMPUTED_COUNT as usize] =
            [const { [const { AtomicI32::new(i32::MIN) }; COMPUTED_PAIRS] };
                COMPUTED_COUNT as usize];

        /// What each entry computes to.
        static COMPUTED_TO: [[AtomicI32; COMPUTED_PAIRS]; COMPUTED_COUNT as usize] =
            [const { [const { AtomicI32::new(0) }; COMPUTED_PAIRS] }; COMPUTED_COUNT as usize];

        /// How many entries of each table are in use.
        static COMPUTED_LEN: [AtomicI32; COMPUTED_COUNT as usize] =
            [const { AtomicI32::new(0) }; COMPUTED_COUNT as usize];

        /// Adds the computed state in `slot`, working its value out from the table given.
        fn insert_computed(app: &mut App, slot: i32, from: &[i32], to: &[i32]) -> i32 {
            if from.len() != to.len() || from.len() > COMPUTED_PAIRS {
                return status::NULL_ARG;
            }

            match slot {
                $($cslot => {
                    // The source has to be there first, for the same reason a sub-state's parent
                    // does. Bevy computes the value whenever the source changes, and a source
                    // that holds no state never changes.
                    if !app.world().contains_resource::<State<$source>>() {
                        return status::INVALID_STATE;
                    }

                    for (pair, (&value, &result)) in from.iter().zip(to).enumerate() {
                        COMPUTED_FROM[$cslot][pair].store(value, Ordering::Relaxed);
                        COMPUTED_TO[$cslot][pair].store(result, Ordering::Relaxed);
                    }

                    COMPUTED_LEN[$cslot].store(from.len() as i32, Ordering::Relaxed);
                    app.add_computed_state::<$derived>();
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        /// How many sub-states exist in total, across every axis.
        pub const SUB_COUNT: i32 = 0 $(+ { let _ = $subslot; 1 })+;

        /// How many sub-states one axis can carry.
        ///
        /// Every axis carries the same number, so the managed side can work out a sub-state's slot
        /// from its parent's and how many that parent has already handed out.
        pub const SUBS_PER_SLOT: i32 = SUB_COUNT / SLOT_COUNT;

        /// Which parent value each sub-state exists under.
        ///
        /// Process-wide rather than per app, because `should_exist` is a static function. A second
        /// app in the same process overwrites what the first wrote, which suits a test that builds
        /// one app after another and is the only shape this trait allows.
        static SUB_PARENT: [AtomicI32; SUB_COUNT as usize] =
            [const { AtomicI32::new(i32::MIN) }; SUB_COUNT as usize];

        /// What each sub-state starts at when it comes into existence.
        static SUB_INITIAL: [AtomicI32; SUB_COUNT as usize] =
            [const { AtomicI32::new(0) }; SUB_COUNT as usize];

        /// Adds the sub-state in `slot`, existing while its own axis holds `parent`.
        fn insert_sub(app: &mut App, slot: i32, parent: i32, initial: i32) -> i32 {
            match slot {
                $($subslot => {
                    // The parent has to be there first. Bevy computes a sub-state from its
                    // source whenever that source changes, and a sub-state added under an axis
                    // that holds no state would simply never come into existence, which is a
                    // silence rather than an answer.
                    if !app.world().contains_resource::<State<$parent>>() {
                        return status::INVALID_STATE;
                    }

                    SUB_PARENT[$subslot].store(parent, Ordering::Relaxed);
                    SUB_INITIAL[$subslot].store(initial, Ordering::Relaxed);
                    app.add_sub_state::<$sub>();
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

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

                // A sub-state has no resource at all while its parent is elsewhere, so this
                // answers nothing and the caller is told the state does not exist, which is the
                // honest answer rather than a default value.
                $(_ if slot == SLOT_COUNT + $subslot =>
                    world.get_resource::<State<$sub>>().map(|s| s.get().0),)+

                // A computed state has no resource either while its table says nothing about the
                // value the source holds, which is the same silence a sub-state answers with.
                $(_ if slot == SLOT_COUNT + SUB_COUNT + $cslot =>
                    world.get_resource::<State<$derived>>().map(|s| s.get().0),)+
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
                $(_ if slot == SLOT_COUNT + $subslot => {
                    if entering {
                        app.add_systems(OnEnter($sub(value)), run);
                    } else {
                        app.add_systems(OnExit($sub(value)), run);
                    }
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + SUB_COUNT + $cslot => {
                    if entering {
                        app.add_systems(OnEnter($derived(value)), run);
                    } else {
                        app.add_systems(OnExit($derived(value)), run);
                    }
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        /// Marks an entity to be despawned when a slot leaves a value.
        ///
        /// The component is generic over the state type, so the slot decides which one is
        /// inserted. `insert_state` registers the systems that act on it, so a slot the managed
        /// side added is already watched.
        fn scope(world: &mut World, entity: bevy::ecs::entity::Entity, slot: i32, value: i32)
            -> i32
        {
            let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                return status::NO_ENTITY;
            };

            match slot {
                $($slot => {
                    entity_mut.insert(DespawnOnExit($ty(value)));
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + $subslot => {
                    entity_mut.insert(DespawnOnExit($sub(value)));
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + SUB_COUNT + $cslot => {
                    entity_mut.insert(DespawnOnExit($derived(value)));
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
                $(_ if slot == SLOT_COUNT + $subslot =>
                    match world.get_resource_mut::<NextState<$sub>>() {
                        Some(mut next) => {
                            next.set($sub(value));
                            status::OK
                        }

                        // The parent is elsewhere, so there is nothing to transition. Queuing it
                        // anyway would be a change that happens when the parent comes back, which
                        // is not what the caller asked for.
                        None => status::NOT_PRESENT,
                    },)+

                // A computed state is worked out rather than set, so there is nothing to queue
                // and asking says so rather than doing nothing.
                $(_ if slot == SLOT_COUNT + SUB_COUNT + $cslot => {
                    let _ = value;
                    status::INVALID_STATE
                })+
                _ => status::NULL_ARG,
            }
        }
    };
}

define_slots!(
    states {
        BcsState0 = 0,
        BcsState1 = 1,
        BcsState2 = 2,
        BcsState3 = 3,
        BcsState4 = 4,
        BcsState5 = 5,
        BcsState6 = 6,
        BcsState7 = 7,
    }
    subs {
        BcsSub0_0 of BcsState0 at 0,
        BcsSub0_1 of BcsState0 at 1,
        BcsSub1_0 of BcsState1 at 2,
        BcsSub1_1 of BcsState1 at 3,
        BcsSub2_0 of BcsState2 at 4,
        BcsSub2_1 of BcsState2 at 5,
        BcsSub3_0 of BcsState3 at 6,
        BcsSub3_1 of BcsState3 at 7,
        BcsSub4_0 of BcsState4 at 8,
        BcsSub4_1 of BcsState4 at 9,
        BcsSub5_0 of BcsState5 at 10,
        BcsSub5_1 of BcsState5 at 11,
        BcsSub6_0 of BcsState6 at 12,
        BcsSub6_1 of BcsState6 at 13,
        BcsSub7_0 of BcsState7 at 14,
        BcsSub7_1 of BcsState7 at 15,
    }
    computed {
        BcsComputed0_0 from BcsState0 at 0,
        BcsComputed0_1 from BcsState0 at 1,
        BcsComputed1_0 from BcsState1 at 2,
        BcsComputed1_1 from BcsState1 at 3,
        BcsComputed2_0 from BcsState2 at 4,
        BcsComputed2_1 from BcsState2 at 5,
        BcsComputed3_0 from BcsState3 at 6,
        BcsComputed3_1 from BcsState3 at 7,
        BcsComputed4_0 from BcsState4 at 8,
        BcsComputed4_1 from BcsState4 at 9,
        BcsComputed5_0 from BcsState5 at 10,
        BcsComputed5_1 from BcsState5 at 11,
        BcsComputed6_0 from BcsState6 at 12,
        BcsComputed6_1 from BcsState6 at 13,
        BcsComputed7_0 from BcsState7 at 14,
        BcsComputed7_1 from BcsState7 at 15,
    }
);

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

        insert_computed(&mut app.app, slot, from, to)
    })
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
