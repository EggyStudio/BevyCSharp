//! The state slots and the dispatch that reaches them by index, which the entry points in the
//! parent module call. Apart from them for the length of the declaration alone.

use super::*;

/// Declares the state slots and the dispatch that reaches them by index.
///
/// Every slot is the same type with a different identity, because Bevy keys its state resources,
/// its transitions and its run conditions on the type, so two slots are two independent state
/// machines rather than two names for one.
macro_rules! define_slots {
    (
        states { $($ty:ident = $slot:literal as $axis:ident),+ $(,)? }
        subs { $($sub:ident of $parent:ident at $subslot:literal),+ $(,)? }
        computed { $($derived:ident from $source:ident at $cslot:literal),+ $(,)? }
        joints { $($joint:ident at $jslot:literal),+ $(,)? }
        joint_subs { $($jsub:ident at $jsslot:literal),+ $(,)? }
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
                    // A rule of the managed side's, where one was given in place of a table, for
                    // what a table cannot state, such as a range of a source's values.
                    if COMPUTED_BY_RULE[$cslot].load(Ordering::Relaxed) {
                        return by_rule($cslot, sources.0).map($derived);
                    }

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

        $(
            /// One joint state, worked out from any of the axes at once by the managed side's
            /// rule, where a computed state reads one.
            ///
            /// Its source is every axis, each optional, since Bevy builds a computed state's
            /// systems from the types it names and a joint's sources are chosen at runtime. Bevy
            /// works it out again whenever any axis changes, and the rule reads only the ones it
            /// was given, so an axis it ignores changes nothing, and the same value worked out
            /// again runs no transition.
            #[derive(Default, Debug, Clone, Copy, PartialEq, Eq, Hash)]
            pub struct $joint(pub i32);

            impl bevy::state::state::ComputedStates for $joint {
                type SourceStates = EveryAxis;

                fn compute(sources: EveryAxis) -> Option<Self> {
                    by_joint_rule($jslot, &axis_values(sources)).map($joint)
                }
            }
        )+

        /// Every axis, each optional, the source a joint state is worked out from.
        pub type EveryAxis = ($(Option<$ty>,)+);

        /// What each axis holds, in slot order, or `None` for one that holds no state.
        ///
        /// Each axis is unpacked under the name the list gives it, since a tuple's fields cannot
        /// be counted off in a macro.
        fn axis_values(sources: EveryAxis) -> [Option<i32>; SLOT_COUNT as usize] {
            let ($($axis,)+) = sources;
            [$($axis.map(|state| state.0)),+]
        }

        $(
            /// One sub-state over several axes, which exists only while each of them holds the
            /// value [`bcs_joint_substate_add`] gave it, and is set like any sub-state otherwise.
            ///
            /// Fed every axis as a joint state is, for the same reason. Bevy asks again whenever
            /// any axis changes, and keeps the value this holds while it should still exist, so a
            /// change to an axis it does not name leaves it as it was.
            #[derive(States, Default, Debug, Clone, Copy, PartialEq, Eq, Hash)]
            pub struct $jsub(pub i32);

            impl bevy::state::state::SubStates for $jsub {
                type SourceStates = EveryAxis;

                fn should_exist(sources: EveryAxis) -> Option<Self> {
                    let values = axis_values(sources);

                    for (axis, value) in values.iter().enumerate() {
                        let wanted = JOINT_SUB_WANT[$jsslot][axis].load(Ordering::Relaxed);
                        if wanted != i32::MIN && *value != Some(wanted) {
                            return None;
                        }
                    }

                    Some($jsub(JOINT_SUB_INITIAL[$jsslot].load(Ordering::Relaxed)))
                }
            }
        )+

        /// How many sub-states over several axes exist.
        pub const JOINT_SUB_COUNT: i32 = 0 $(+ { let _ = $jsslot; 1 })+;

        /// The value each sub-state over several axes needs of each axis, or `i32::MIN` for an
        /// axis it does not name. Process-wide, as a sub-state's parent value is.
        static JOINT_SUB_WANT: [[AtomicI32; SLOT_COUNT as usize]; JOINT_SUB_COUNT as usize] =
            [const { [const { AtomicI32::new(i32::MIN) }; SLOT_COUNT as usize] }; JOINT_SUB_COUNT as usize];

        /// What each sub-state over several axes starts at when it comes into existence.
        static JOINT_SUB_INITIAL: [AtomicI32; JOINT_SUB_COUNT as usize] =
            [const { AtomicI32::new(0) }; JOINT_SUB_COUNT as usize];

        /// Whether an axis holds a state in this app.
        fn axis_added(app: &App, axis: usize) -> bool {
            match axis {
                $($slot => app.world().contains_resource::<State<$ty>>(),)+
                _ => false,
            }
        }

        /// Adds the sub-state over several axes in `slot`, existing while each axis `wants` names
        /// (by a value other than `i32::MIN`) holds that value.
        pub(super) fn insert_joint_sub(app: &mut App, slot: i32, wants: &[i32], initial: i32) -> i32 {
            if wants.len() != SLOT_COUNT as usize {
                return status::NULL_ARG;
            }

            // Every axis it names has to be there first, as a sub-state's one parent does.
            for (axis, wanted) in wants.iter().enumerate() {
                if *wanted != i32::MIN && !axis_added(app, axis) {
                    return status::INVALID_STATE;
                }
            }

            match slot {
                $($jsslot => {
                    for (axis, wanted) in wants.iter().enumerate() {
                        JOINT_SUB_WANT[$jsslot][axis].store(*wanted, Ordering::Relaxed);
                    }

                    JOINT_SUB_INITIAL[$jsslot].store(initial, Ordering::Relaxed);
                    every_axis_message(app);
                    app.add_sub_state::<$jsub>();
                    crate::ecs::despawn_all::scope_state::<$jsub>(app);
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        /// How many joint states exist, each able to read any of the axes.
        pub const JOINT_COUNT: i32 = 0 $(+ { let _ = $jslot; 1 })+;

        /// Registers the transition messages of every axis, which a joint reads whether or not the
        /// app has added that axis, and which Bevy registers only as an axis is added.
        fn every_axis_message(app: &mut App) {
            $(app.add_message::<bevy::state::state::StateTransitionEvent<$ty>>();)+
        }

        /// Adds the joint state in `slot`, worked out by the managed side's joint rule.
        pub(super) fn insert_joint(app: &mut App, slot: i32) -> i32 {
            match slot {
                $($jslot => {
                    every_axis_message(app);
                    app.add_computed_state::<$joint>();
                    crate::ecs::despawn_all::scope_state::<$joint>(app);
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

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

        /// Which computed states are worked out by the managed side's rule rather than a table.
        static COMPUTED_BY_RULE: [core::sync::atomic::AtomicBool; COMPUTED_COUNT as usize] =
            [const { core::sync::atomic::AtomicBool::new(false) }; COMPUTED_COUNT as usize];

        /// Adds the computed state in `slot`, working its value out from the table given, or from
        /// the managed side's rule where `ruled` is set and the table is empty.
        pub(super) fn insert_computed(app: &mut App, slot: i32, from: &[i32], to: &[i32], ruled: bool) -> i32 {
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
                    COMPUTED_BY_RULE[$cslot].store(ruled, Ordering::Relaxed);
                    app.add_computed_state::<$derived>();
                    crate::ecs::despawn_all::scope_state::<$derived>(app);
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
        pub(super) fn insert_sub(app: &mut App, slot: i32, parent: i32, initial: i32) -> i32 {
            match slot {
                $($subslot => {
                    // The parent has to be there first. Bevy computes a sub-state from its source
                    // whenever that source changes, and a sub-state added under an axis that holds
                    // no state would never come into existence, which is a silence rather than an
                    // answer.
                    if !app.world().contains_resource::<State<$parent>>() {
                        return status::INVALID_STATE;
                    }

                    SUB_PARENT[$subslot].store(parent, Ordering::Relaxed);
                    SUB_INITIAL[$subslot].store(initial, Ordering::Relaxed);
                    app.add_sub_state::<$sub>();
                    crate::ecs::despawn_all::scope_state::<$sub>(app);
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        pub(super) fn insert(app: &mut App, slot: i32, initial: i32) -> i32 {
            match slot {
                $($slot => {
                    app.insert_state($ty(initial));
                    crate::ecs::despawn_all::scope_state::<$ty>(app);
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        pub(super) fn read(world: &World, slot: i32) -> Option<i32> {
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
                $(_ if slot == SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + $jslot =>
                    world.get_resource::<State<$joint>>().map(|s| s.get().0),)+
                $(_ if slot == SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + JOINT_COUNT + $jsslot =>
                    world.get_resource::<State<$jsub>>().map(|s| s.get().0),)+
                _ => None,
            }
        }

        /// Registers a system in the schedule Bevy runs when a slot enters or leaves a value.
        ///
        /// The two schedules are keyed by the state value as well as the type, so a system added
        /// here runs on that one transition and nothing else.
        pub(super) fn add_edge(app: &mut App, slot: i32, value: i32, entering: bool, reg: SystemReg) -> i32 {
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
                $(_ if slot == SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + $jslot => {
                    if entering {
                        app.add_systems(OnEnter($joint(value)), run);
                    } else {
                        app.add_systems(OnExit($joint(value)), run);
                    }
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + JOINT_COUNT + $jsslot => {
                    if entering {
                        app.add_systems(OnEnter($jsub(value)), run);
                    } else {
                        app.add_systems(OnExit($jsub(value)), run);
                    }
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        /// Adds a system to the schedule Bevy runs when a slot moves from one value to another,
        /// after the first's exit and before the second's entry.
        pub(super) fn add_transition(app: &mut App, slot: i32, from: i32, to: i32, reg: SystemReg) -> i32 {
            let run = move |world: &mut World| loan_world(world, || reg.invoke());

            match slot {
                $($slot => {
                    app.add_systems(OnTransition { exited: $ty(from), entered: $ty(to) }, run);
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + $subslot => {
                    app.add_systems(OnTransition { exited: $sub(from), entered: $sub(to) }, run);
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + SUB_COUNT + $cslot => {
                    app.add_systems(OnTransition { exited: $derived(from), entered: $derived(to) }, run);
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + $jslot => {
                    app.add_systems(OnTransition { exited: $joint(from), entered: $joint(to) }, run);
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + JOINT_COUNT + $jsslot => {
                    app.add_systems(OnTransition { exited: $jsub(from), entered: $jsub(to) }, run);
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        /// Marks an entity to be despawned when a slot leaves a value, or as it enters the value
        /// where `on_enter` is set, Bevy's `DespawnOnEnter`.
        ///
        /// The component is generic over the state type, so the slot decides which one is
        /// inserted. `insert_state` registers the systems that act on it, so a slot the managed
        /// side added is already watched.
        /// Copies the transitions every slot has made since the last call into `out`, slot by
        /// slot in the numbering the managed side addresses them by, and answers how many.
        pub(super) fn drain_transitions(world: &mut World, out: *mut BcsStateTransition, capacity: usize) -> usize {
            let mut written = 0;
            $(written = drain_one::<$ty>(world, $slot, |s| s.0, out, written, capacity);)+
            $(written = drain_one::<$sub>(world, SLOT_COUNT + $subslot, |s| s.0, out, written, capacity);)+
            $(written = drain_one::<$derived>(world, SLOT_COUNT + SUB_COUNT + $cslot, |s| s.0, out, written, capacity);)+
            $(written = drain_one::<$joint>(world, SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + $jslot, |s| s.0, out, written, capacity);)+
            $(written = drain_one::<$jsub>(world, SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + JOINT_COUNT + $jsslot, |s| s.0, out, written, capacity);)+
            written
        }

        pub(super) fn scope(world: &mut World, entity: bevy::ecs::entity::Entity, slot: i32, value: i32, on_enter: bool)
            -> i32
        {
            let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                return status::NO_ENTITY;
            };

            match slot {
                $($slot => {
                    if on_enter {
                        entity_mut.insert(DespawnOnEnter($ty(value)));
                    } else {
                        entity_mut.insert(DespawnOnExit($ty(value)));
                    }
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + $subslot => {
                    if on_enter {
                        entity_mut.insert(DespawnOnEnter($sub(value)));
                    } else {
                        entity_mut.insert(DespawnOnExit($sub(value)));
                    }
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + SUB_COUNT + $cslot => {
                    if on_enter {
                        entity_mut.insert(DespawnOnEnter($derived(value)));
                    } else {
                        entity_mut.insert(DespawnOnExit($derived(value)));
                    }
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + $jslot => {
                    if on_enter {
                        entity_mut.insert(DespawnOnEnter($joint(value)));
                    } else {
                        entity_mut.insert(DespawnOnExit($joint(value)));
                    }
                    status::OK
                })+
                $(_ if slot == SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + JOINT_COUNT + $jsslot => {
                    if on_enter {
                        entity_mut.insert(DespawnOnEnter($jsub(value)));
                    } else {
                        entity_mut.insert(DespawnOnExit($jsub(value)));
                    }
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        pub(super) fn queue(world: &mut World, slot: i32, value: i32) -> i32 {
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
                $(_ if slot == SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + $jslot => {
                    let _ = value;
                    status::INVALID_STATE
                })+

                // Set as any sub-state is, while it exists.
                $(_ if slot == SLOT_COUNT + SUB_COUNT + COMPUTED_COUNT + JOINT_COUNT + $jsslot =>
                    match world.get_resource_mut::<NextState<$jsub>>() {
                        Some(mut next) => {
                            next.set($jsub(value));
                            status::OK
                        }
                        None => status::NOT_PRESENT,
                    },)+
                _ => status::NULL_ARG,
            }
        }
    };
}

define_slots!(
    states {
        BcsState0 = 0 as axis0,
        BcsState1 = 1 as axis1,
        BcsState2 = 2 as axis2,
        BcsState3 = 3 as axis3,
        BcsState4 = 4 as axis4,
        BcsState5 = 5 as axis5,
        BcsState6 = 6 as axis6,
        BcsState7 = 7 as axis7,
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
        BcsComputed0_2 from BcsState0 at 2,
        BcsComputed1_0 from BcsState1 at 3,
        BcsComputed1_1 from BcsState1 at 4,
        BcsComputed1_2 from BcsState1 at 5,
        BcsComputed2_0 from BcsState2 at 6,
        BcsComputed2_1 from BcsState2 at 7,
        BcsComputed2_2 from BcsState2 at 8,
        BcsComputed3_0 from BcsState3 at 9,
        BcsComputed3_1 from BcsState3 at 10,
        BcsComputed3_2 from BcsState3 at 11,
        BcsComputed4_0 from BcsState4 at 12,
        BcsComputed4_1 from BcsState4 at 13,
        BcsComputed4_2 from BcsState4 at 14,
        BcsComputed5_0 from BcsState5 at 15,
        BcsComputed5_1 from BcsState5 at 16,
        BcsComputed5_2 from BcsState5 at 17,
        BcsComputed6_0 from BcsState6 at 18,
        BcsComputed6_1 from BcsState6 at 19,
        BcsComputed6_2 from BcsState6 at 20,
        BcsComputed7_0 from BcsState7 at 21,
        BcsComputed7_1 from BcsState7 at 22,
        BcsComputed7_2 from BcsState7 at 23,
    }
    joints {
        BcsJoint0 at 0,
        BcsJoint1 at 1,
        BcsJoint2 at 2,
        BcsJoint3 at 3,
        BcsJoint4 at 4,
        BcsJoint5 at 5,
        BcsJoint6 at 6,
        BcsJoint7 at 7,
    }
    joint_subs {
        BcsJointSub0 at 0,
        BcsJointSub1 at 1,
        BcsJointSub2 at 2,
        BcsJointSub3 at 3,
        BcsJointSub4 at 4,
        BcsJointSub5 at 5,
        BcsJointSub6 at 6,
        BcsJointSub7 at 7,
    }
);

/// Where the transition drain keeps its place in one slot's queue of Bevy's `StateTransitionEvent`,
/// a resource of its own for each state type, so a reader in Bevy loses nothing to it.
#[derive(bevy::ecs::resource::Resource)]
pub(super) struct TransitionCursor<S: States>(bevy::ecs::message::MessageCursor<bevy::state::state::StateTransitionEvent<S>>);

impl<S: States> Default for TransitionCursor<S> {
    fn default() -> Self {
        Self(Default::default())
    }
}

/// Copies one slot's transitions since the last call into `out` from `written` on, stopping at the
/// buffer's end so what is left stays queued, and answers where the next slot's begin. A slot no
/// app added has no queue and writes nothing.
fn drain_one<S: States>(
    world: &mut World,
    slot: i32,
    value: fn(&S) -> i32,
    out: *mut BcsStateTransition,
    mut written: usize,
    capacity: usize,
) -> usize {
    use bevy::ecs::message::Messages;
    use bevy::state::state::StateTransitionEvent;

    if written >= capacity || !world.contains_resource::<Messages<StateTransitionEvent<S>>>() {
        return written;
    }
    world.get_resource_or_insert_with(TransitionCursor::<S>::default);
    world.resource_scope(|world, mut cursor: bevy::ecs::world::Mut<TransitionCursor<S>>| {
        let messages = world.resource::<Messages<StateTransitionEvent<S>>>();
        for transition in cursor.0.read(messages).take(capacity - written) {
            let mut made = BcsStateTransition { slot, flags: 0, exited: 0, entered: 0 };
            if let Some(exited) = &transition.exited {
                made.flags |= 1;
                made.exited = value(exited);
            }
            if let Some(entered) = &transition.entered {
                made.flags |= 2;
                made.entered = value(entered);
            }
            // SAFETY: `written < capacity`, checked by the take above.
            unsafe { out.add(written).write(made) };
            written += 1;
        }
    });
    written
}
