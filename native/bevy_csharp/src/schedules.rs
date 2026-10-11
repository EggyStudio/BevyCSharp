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
}
