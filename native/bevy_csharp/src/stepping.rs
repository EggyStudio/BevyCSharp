//! Stepping through the systems of a schedule one at a time, as Bevy's `Stepping` resource does,
//! which its breakout example's panel and its system_stepping step.
//!
//! Bevy asks for a system by its type or by its node in a built schedule. Every C# system is the
//! same closure type here, so C# names one by the name it was added under, and the name becomes
//! nodes once the schedule holds them. A schedule is taken out of `Schedules` while it runs, and a
//! C# system is always running in one, so what C# asks of a schedule is answered from here, after
//! the frame, when none of them is running, [`refresh`] runs in Bevy's `Main` once every schedule
//! of the frame has, as Bevy's own helper reads them from a schedule of its own.

use bevy::app::{First, FixedUpdate, Last, Main, PostUpdate, PreUpdate, Update};
use bevy::ecs::intern::Interned;
use bevy::ecs::prelude::*;
use bevy::ecs::schedule::{NodeId, Schedule, ScheduleLabel, Schedules, Stepping, SystemKey};

use crate::interop::status;
use crate::stages::Stage;
use crate::state::with_world;

/// What [`bcs_stepping_act`] asks of the stepping. Mirrors `Bevy.Stepping`'s calls.
const DISABLE: i32 = 0;
const ENABLE: i32 = 1;
const STEP: i32 = 2;
const CONTINUE: i32 = 3;

/// What [`bcs_stepping_set_behavior`] gives a system. Mirrors `Bevy.Stepping`'s calls.
const CLEAR: i32 = 0;
const ALWAYS_RUN: i32 = 1;
const NEVER_RUN: i32 = 2;
const BREAKPOINT: i32 = 3;

/// What the bridge keeps beside Bevy's `Stepping` for C#, which only reaches a schedule by name.
#[derive(Resource, Default)]
struct Stepped {
    /// The schedules C# has asked to step.
    stages: Vec<Interned<dyn ScheduleLabel>>,

    /// Behaviors asked for by a system's name, given to Bevy by node once the frame has built the
    /// schedule, since only a built schedule has nodes to give.
    pending: Vec<(Interned<dyn ScheduleLabel>, String, i32)>,

    /// The systems of the stepped schedules, in the order a stepping frame walks them, as they were
    /// at the end of the last frame.
    systems: Vec<(Interned<dyn ScheduleLabel>, SystemKey, String)>,

    /// How many systems each schedule held when `systems` was read, so it is read again only when a
    /// schedule has changed.
    counts: Vec<(Interned<dyn ScheduleLabel>, usize)>,
}

/// Reads the stepped schedules after each frame, once something has asked for stepping.
pub(crate) fn install(app: &mut bevy::app::App) {
    app.add_systems(
        Main,
        refresh
            .after(Main::run_main)
            .run_if(resource_exists::<Stepped>)
            .run_if(resource_exists::<Stepping>),
    );
}

/// The schedules a stage can step, in the order Bevy's `Main` runs them, which is the order a
/// stepping frame walks them in, `FixedUpdate` running between `PreUpdate` and `Update`.
///
/// Bevy learns that order as each stepped schedule runs with stepping on, and the bridge knows it
/// from the start, so a list of the stepped systems is there the frame after a stage is added and
/// a game can leave some of them to run before it first turns stepping on.
fn steppable() -> [(Interned<dyn ScheduleLabel>, Stage); 6] {
    [
        (First.intern(), Stage::First),
        (PreUpdate.intern(), Stage::PreUpdate),
        (FixedUpdate.intern(), Stage::FixedUpdate),
        (Update.intern(), Stage::Update),
        (PostUpdate.intern(), Stage::PostUpdate),
        (Last.intern(), Stage::Last),
    ]
}

/// The schedule a stage's systems are in. `Render` shares `Last`, and the stages that run once or
/// are the engine's own have nothing to step.
fn label_of(stage: i32) -> Option<Interned<dyn ScheduleLabel>> {
    Some(match Stage::from_i32(stage)? {
        Stage::First => First.intern(),
        Stage::PreUpdate => PreUpdate.intern(),
        Stage::Update => Update.intern(),
        Stage::FixedUpdate => FixedUpdate.intern(),
        Stage::PostUpdate => PostUpdate.intern(),
        Stage::Render | Stage::Last => Last.intern(),
        _ => return None,
    })
}

/// The stage C# knows a schedule by, the reverse of [`label_of`].
fn stage_of(label: Interned<dyn ScheduleLabel>) -> Stage {
    steppable()
        .into_iter()
        .find_map(|(known, stage)| (known == label).then_some(stage))
        .unwrap_or(Stage::Update)
}

/// Bevy's `Stepping` and the bridge's record beside it, made the first time C# asks for either.
fn stepping(world: &mut World) -> Mut<'_, Stepping> {
    world.init_resource::<Stepped>();
    world.get_resource_or_init::<Stepping>()
}

/// Gives Bevy the behaviors asked for by name, and reads the stepped schedules' systems again where
/// one has changed.
fn refresh(world: &mut World) {
    world.resource_scope(|world, mut stepped: Mut<Stepped>| {
        world.resource_scope(|world, mut stepping: Mut<Stepping>| {
            let Some(schedules) = world.get_resource::<Schedules>() else {
                return;
            };

            // Each schedule's names read once however many behaviors name its systems, since a
            // game leaving every system but its own to run names each of the rest.
            let mut named: Vec<(Interned<dyn ScheduleLabel>, Vec<(SystemKey, String)>)> = Vec::new();
            for (label, name, behavior) in core::mem::take(&mut stepped.pending) {
                if !named.iter().any(|(known, _)| *known == label) {
                    // A schedule that has not run yet has no nodes, so the behavior waits for it.
                    let Some(Ok(systems)) = schedules.get(label).map(Schedule::systems) else {
                        stepped.pending.push((label, name, behavior));
                        continue;
                    };
                    let names = systems.map(|(key, system)| (key, system.name().to_string())).collect();
                    named.push((label, names));
                }
                let Some((_, systems)) = named.iter().find(|(known, _)| *known == label) else {
                    continue;
                };

                let mut found = false;
                for (key, _) in systems.iter().filter(|(_, system)| *system == name) {
                    found = true;
                    let node = NodeId::System(*key);
                    match behavior {
                        ALWAYS_RUN => stepping.always_run_node(label, node),
                        NEVER_RUN => stepping.never_run_node(label, node),
                        BREAKPOINT => stepping.set_breakpoint_node(label, node),
                        _ => stepping.clear_node(label, node),
                    };
                }
                if !found {
                    bevy::log::warn!("There is no system called {name} in {label:?} for stepping to change.");
                }
            }

            let stages: Vec<_> = steppable()
                .into_iter()
                .map(|(label, _)| label)
                .filter(|label| stepped.stages.contains(label))
                .collect();
            let counts: Vec<_> = stages
                .iter()
                .map(|label| (*label, schedules.get(*label).map_or(0, Schedule::systems_len)))
                .collect();
            if counts == stepped.counts {
                return;
            }

            stepped.systems = stages
                .iter()
                .flat_map(|label| {
                    let systems = schedules.get(*label).and_then(|schedule| schedule.systems().ok());
                    systems
                        .into_iter()
                        .flatten()
                        .map(|(key, system)| (*label, key, system.name().to_string()))
                })
                .collect();
            stepped.counts = counts;
        });
    });
}

/// Steps the systems of a stage, as Bevy's `Stepping::add_schedule` does, from the next frame.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_stepping_add(stage: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(label) = label_of(stage) else {
            return status::NULL_ARG;
        };
        with_world(|world| {
            stepping(world).add_schedule(label);
            let mut stepped = world.resource_mut::<Stepped>();
            if !stepped.stages.contains(&label) {
                stepped.stages.push(label);
            }
            status::OK
        })
    })
}

/// Stops stepping the systems of a stage, and forgets what was set for each, as Bevy's
/// `Stepping::remove_schedule` does.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_stepping_remove(stage: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(label) = label_of(stage) else {
            return status::NULL_ARG;
        };
        with_world(|world| {
            stepping(world).remove_schedule(label);
            world.resource_mut::<Stepped>().stages.retain(|stage| *stage != label);
            status::OK
        })
    })
}

/// Turns stepping on or off, or runs the next system or the rest of the stepping frame, each from
/// the next frame, as Bevy's `enable`, `disable`, `step_frame` and `continue_frame` do.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_stepping_act(action: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let mut stepping = stepping(world);
            match action {
                DISABLE => stepping.disable(),
                ENABLE => stepping.enable(),
                STEP => stepping.step_frame(),
                CONTINUE => stepping.continue_frame(),
                _ => return status::NULL_ARG,
            };
            status::OK
        })
    })
}

/// Answers 1 while stepping is on and 0 while it is off.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_stepping_enabled() -> i32 {
    crate::interop::guard(|| {
        with_world(|world| world.get_resource::<Stepping>().is_some_and(Stepping::is_enabled) as i32)
    })
}

/// Has every system of a stage called `name` always run, never run, stop a continued frame before
/// it, or step as the rest do, from the frame after the schedule has been built.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_stepping_set_behavior(stage: i32, name: *const core::ffi::c_char, behavior: i32) -> i32 {
    crate::interop::guard(|| {
        let (Some(label), Some(name)) = (label_of(stage), unsafe { crate::interop::cstr_to_string(name) }) else {
            return status::NULL_ARG;
        };
        if !(CLEAR..=BREAKPOINT).contains(&behavior) {
            return status::NULL_ARG;
        }
        with_world(|world| {
            stepping(world);
            world.resource_mut::<Stepped>().pending.push((label, name, behavior));
            status::OK
        })
    })
}

/// Writes the stepped systems to `out`, a line each holding the stage's number and the system's
/// name apart by a tab, in the order a stepping frame walks them, and answers the length.
///
/// Answers [`status::NOT_PRESENT`] until a frame has ended since a stage was added, and while none
/// is.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_stepping_systems(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let Some(stepped) = world.get_resource::<Stepped>().filter(|stepped| !stepped.systems.is_empty()) else {
                return status::NOT_PRESENT;
            };
            let lines: Vec<String> = stepped
                .systems
                .iter()
                .map(|(label, _, name)| format!("{}\t{name}", stage_of(*label) as i32))
                .collect();
            unsafe { crate::interop::write_text(&lines.join("\n"), out, capacity) }
        })
    })
}

/// Answers the place in [`bcs_stepping_systems`]' list of the system stepping runs next, or
/// [`status::NOT_PRESENT`] while there is none, as when stepping is off or the frame's last system
/// has run.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_stepping_cursor() -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let (Some(stepping), Some(stepped)) = (world.get_resource::<Stepping>(), world.get_resource::<Stepped>())
            else {
                return status::NOT_PRESENT;
            };
            let Some((label, NodeId::System(key))) = stepping.cursor() else {
                return status::NOT_PRESENT;
            };
            stepped
                .systems
                .iter()
                .position(|(known, system, _)| *known == label && *system == key)
                .map_or(status::NOT_PRESENT, |index| index as i32)
        })
    })
}

/// Writes Bevy's own account of the stepping to `out`, as its helper prints it, and answers the
/// length, or [`status::NOT_PRESENT`] where nothing has asked for stepping.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_stepping_describe(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| match world.get_resource::<Stepping>() {
            Some(stepping) => unsafe { crate::interop::write_text(&format!("{stepping:#?}"), out, capacity) },
            None => status::NOT_PRESENT,
        })
    })
}

#[cfg(test)]
mod tests {
    use bevy::app::{App, PreUpdate, Update};
    use bevy::ecs::prelude::*;
    use bevy::ecs::system::IntoSystem;

    use crate::interop::status;
    use crate::state::loan_world;

    #[derive(Resource, Default)]
    struct Ran(Vec<&'static str>);

    fn ran(name: &'static str) -> impl FnMut(ResMut<Ran>) + Send + Sync + 'static {
        move |mut ran: ResMut<Ran>| ran.0.push(name)
    }

    fn take(app: &mut App) -> Vec<&'static str> {
        core::mem::take(&mut app.world_mut().resource_mut::<Ran>().0)
    }

    fn list(app: &mut App) -> Option<String> {
        loan_world(app.world_mut(), || {
            let length = unsafe { super::bcs_stepping_systems(core::ptr::null_mut(), 0) };
            if length < 0 {
                return None;
            }
            let mut buffer = vec![0u8; length as usize];
            unsafe { super::bcs_stepping_systems(buffer.as_mut_ptr(), length) };
            Some(String::from_utf8(buffer).unwrap())
        })
    }

    /// Bevy's system_stepping in small, a schedule stepped one named system at a time, its list and
    /// cursor read as a panel reads them, and a system named to always run.
    #[test]
    fn named_systems_step_one_at_a_time_and_are_listed_in_order() {
        let mut app = App::new();
        super::install(&mut app);
        app.init_resource::<Ran>();
        app.add_systems(PreUpdate, IntoSystem::into_system(ran("pre")).with_name("pre"));
        app.add_systems(
            Update,
            (
                IntoSystem::into_system(ran("one")).with_name("one"),
                IntoSystem::into_system(ran("two")).with_name("two"),
                IntoSystem::into_system(ran("three")).with_name("three"),
            )
                .chain(),
        );

        let act = |app: &mut App, action| loan_world(app.world_mut(), || super::bcs_stepping_act(action));
        let answer = loan_world(app.world_mut(), || super::bcs_stepping_add(crate::stages::Stage::Update as i32));
        assert_eq!(answer, status::OK);
        assert_eq!(list(&mut app), None);

        // Off, so everything runs, and the list is known once the frame has.
        app.update();
        assert_eq!(take(&mut app), ["pre", "one", "two", "three"]);
        assert_eq!(list(&mut app).as_deref(), Some("3\tone\n3\ttwo\n3\tthree"));
        assert_eq!(loan_world(app.world_mut(), || super::bcs_stepping_cursor()), status::NOT_PRESENT);

        // Waiting, so only the schedule that is not stepped runs.
        assert_eq!(act(&mut app, super::ENABLE), status::OK);
        app.update();
        assert_eq!(take(&mut app), ["pre"]);
        assert_eq!(loan_world(app.world_mut(), || super::bcs_stepping_cursor()), 0);
        assert_eq!(loan_world(app.world_mut(), || super::bcs_stepping_enabled()), 1);

        act(&mut app, super::STEP);
        app.update();
        assert_eq!(take(&mut app), ["pre", "one"]);
        assert_eq!(loan_world(app.world_mut(), || super::bcs_stepping_cursor()), 1);

        // Two always runs from the frame after the one it was named in, and a continued frame runs
        // the rest.
        let name = c"two";
        let answer = loan_world(app.world_mut(), || unsafe {
            super::bcs_stepping_set_behavior(crate::stages::Stage::Update as i32, name.as_ptr(), super::ALWAYS_RUN)
        });
        assert_eq!(answer, status::OK);
        app.update();
        assert_eq!(take(&mut app), ["pre"]);
        app.update();
        assert_eq!(take(&mut app), ["pre", "two"]);
        act(&mut app, super::CONTINUE);
        app.update();
        assert_eq!(take(&mut app), ["pre", "two", "three"]);

        act(&mut app, super::DISABLE);
        app.update();
        assert_eq!(take(&mut app), ["pre", "one", "two", "three"]);
        assert_eq!(loan_world(app.world_mut(), || super::bcs_stepping_cursor()), status::NOT_PRESENT);
        assert_eq!(loan_world(app.world_mut(), || super::bcs_stepping_enabled()), 0);
    }

    #[test]
    fn a_stage_with_nothing_to_step_is_refused() {
        let mut world = World::new();
        let answer = loan_world(&mut world, || super::bcs_stepping_add(crate::stages::Stage::Startup as i32));
        assert_eq!(answer, status::NULL_ARG);
        assert_eq!(loan_world(&mut world, || super::bcs_stepping_enabled()), 0);
    }
}
