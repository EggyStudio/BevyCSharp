//! Schedules of a game's own, run when a system asks, as Bevy's custom_transitions runs its
//! `OnReenter` and `OnReexit` from inside a state's transition.
//!
//! Bevy keys a schedule by a label, a value of a type the game declares, and C# cannot declare a
//! Rust type, so here a schedule is keyed by a name the game chooses, which can carry a value as
//! `OnReenter(InGame)` does.

use core::ffi::{c_char, c_void};

use bevy::ecs::schedule::ScheduleLabel;

use crate::interop::status;
use crate::state::{app_mut, with_world, BcsApp, SystemReg};

/// A schedule of a game's own, by the name C# gave it.
#[derive(ScheduleLabel, Debug, Clone, PartialEq, Eq, Hash)]
struct Named(String);

/// Registers a C# system called `name` in the game's schedule called `schedule`, made by the first
/// system put in it.
///
/// # Safety
/// `handle` must be a live app; `schedule` must be a NUL-terminated UTF-8 string and `name` one or
/// null; `func` must remain callable until the app is destroyed.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_add_schedule_system(
    handle: *mut BcsApp,
    schedule: *const c_char,
    name: *const c_char,
    func: extern "C" fn(*mut c_void),
    user: *mut c_void,
) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }
        let Some(schedule) = (unsafe { crate::interop::cstr_to_string(schedule) }) else {
            return status::NULL_ARG;
        };

        let name = unsafe { crate::interop::cstr_to_string(name) };
        let system = crate::systems::csharp_system(name, SystemReg { func, user });
        app.app.add_systems(Named(schedule), system);
        status::OK
    })
}

/// Places the game's schedule called `schedule` in Bevy's main order, run once a frame right after
/// the stage `after` names, or once at startup where `after` is a startup stage, as Bevy's
/// custom_schedule places its own with `MainScheduleOrder`. A non-zero `single_threaded` runs its
/// systems one after another on one thread, as Bevy's `SingleThreadedExecutor` does.
///
/// Its systems are put in before or after, the placing keeping them. A stage that is no schedule of
/// its own, as the fixed step or the bridge's sets within a schedule, is refused with
/// [`status::NULL_ARG`], and a schedule placed already with [`status::INVALID_STATE`].
///
/// # Safety
/// `handle` must be a live app and `schedule` a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_place_schedule(handle: *mut BcsApp, schedule: *const c_char, after: i32, single_threaded: u32) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }
        let Some(schedule) = (unsafe { crate::interop::cstr_to_string(schedule) }) else {
            return status::NULL_ARG;
        };

        place(&mut app.app, schedule, after, single_threaded != 0)
    })
}

/// [`bcs_app_place_schedule`] on a Bevy app.
fn place(app: &mut bevy::app::App, schedule: String, after: i32, single_threaded: bool) -> i32 {
    use bevy::app::{First, Last, MainScheduleOrder, PostUpdate, PreStartup, PreUpdate, Startup, Update};
    use crate::stages::Stage;

    let Some(after) = Stage::from_i32(after) else {
        return status::NULL_ARG;
    };

    let named = Named(schedule);
    let label = named.clone().intern();
    let mut order = app.world_mut().resource_mut::<MainScheduleOrder>();
    if order.labels.contains(&label) || order.startup_labels.contains(&label) {
        return status::INVALID_STATE;
    }

    // Each by its own type, which the order tells its labels apart by.
    match after {
        Stage::PreStartup => order.insert_startup_after(PreStartup, named),
        Stage::Startup => order.insert_startup_after(Startup, named),
        Stage::First => order.insert_after(First, named),
        Stage::PreUpdate => order.insert_after(PreUpdate, named),
        Stage::Update => order.insert_after(Update, named),
        Stage::PostUpdate => order.insert_after(PostUpdate, named),
        Stage::Last => order.insert_after(Last, named),
        _ => return status::NULL_ARG,
    }

    app.edit_schedule(label, |placed| {
        if single_threaded {
            placed.set_executor(bevy::ecs::schedule::SingleThreadedExecutor::new());
        }
    });
    status::OK
}

/// Runs the game's schedule called `schedule` to its end, as Bevy's `World::try_run_schedule`
/// does, and answers 1, or 0 where no system was put in it or it is running already, as one that
/// would run itself is.
///
/// # Safety
/// `schedule` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_schedule_run(schedule: *const c_char) -> i32 {
    crate::interop::guard(|| {
        let Some(schedule) = (unsafe { crate::interop::cstr_to_string(schedule) }) else {
            return status::NULL_ARG;
        };
        with_world(|world| world.try_run_schedule(Named(schedule)).is_ok() as i32)
    })
}

#[cfg(test)]
mod tests {
    use bevy::ecs::prelude::*;

    use crate::state::loan_world;

    use super::Named;

    #[derive(Resource, Default)]
    struct Ran(u32);

    #[test]
    fn a_schedule_of_its_own_runs_when_asked_and_one_unknown_is_refused() {
        let mut world = World::new();
        world.init_resource::<Ran>();
        world.add_schedule(Schedule::new(Named("Tests.Own".into())));
        world.schedule_scope(Named("Tests.Own".into()), |_, schedule| {
            schedule.add_systems(|mut ran: ResMut<Ran>| ran.0 += 1);
        });

        let own = c"Tests.Own";
        let unknown = c"Tests.Unknown";
        assert_eq!(loan_world(&mut world, || unsafe { super::bcs_schedule_run(own.as_ptr()) }), 1);
        assert_eq!(loan_world(&mut world, || unsafe { super::bcs_schedule_run(unknown.as_ptr()) }), 0);
        assert_eq!(world.resource::<Ran>().0, 1);
    }

    /// A schedule placed after a stage takes its place in Bevy's main order, one placed after a
    /// startup stage in the startup order, a second placing is refused, and so is a stage that is
    /// no schedule of its own.
    #[test]
    fn a_schedule_takes_its_place_in_the_main_order() {
        use bevy::app::{MainScheduleOrder, PreStartup, Startup, Update};
        use bevy::ecs::schedule::ScheduleLabel;

        use crate::interop::status;
        use crate::stages::Stage;

        let mut app = bevy::app::App::new();
        assert_eq!(super::place(&mut app, "Tests.Frame".into(), Stage::Update as i32, true), status::OK);
        assert_eq!(super::place(&mut app, "Tests.Start".into(), Stage::PreStartup as i32, false), status::OK);
        assert_eq!(super::place(&mut app, "Tests.Frame".into(), Stage::Last as i32, false), status::INVALID_STATE);
        assert_eq!(super::place(&mut app, "Tests.Fixed".into(), Stage::FixedUpdate as i32, false), status::NULL_ARG);

        let order = app.world().resource::<MainScheduleOrder>();
        let at = |labels: &[bevy::ecs::schedule::InternedScheduleLabel], label: bevy::ecs::schedule::InternedScheduleLabel| {
            labels.iter().position(|held| *held == label).unwrap()
        };
        assert_eq!(at(&order.labels, Named("Tests.Frame".into()).intern()), at(&order.labels, Update.intern()) + 1);
        assert_eq!(at(&order.startup_labels, Named("Tests.Start".into()).intern()), at(&order.startup_labels, PreStartup.intern()) + 1);
        assert!(at(&order.startup_labels, Startup.intern()) > at(&order.startup_labels, Named("Tests.Start".into()).intern()));
    }
}
