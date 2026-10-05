//! C# systems registered in a stage, and ordered against each other.

use core::ffi::c_void;

use bevy::app::{First, FixedUpdate, Last, PostUpdate, PreUpdate, Startup, Update};
use bevy::ecs::schedule::{IntoScheduleConfigs, SystemSet};
use bevy::ecs::world::World;

use crate::app::{BcsSet, Stage};
use crate::interop::status;
use crate::state::{app_mut, loan_world, BcsApp, SystemReg};

/// One C# system, by the number `bcs_app_add_system` gave it, so another can be ordered against it.
///
/// Bevy orders systems by the sets they are in, and a C# system is a closure with no name Bevy
/// knows, so each is put in a set of its own when it is added and ordered through that.
#[derive(SystemSet, Debug, Clone, PartialEq, Eq, Hash)]
struct BcsSystem(u32);

/// Registers a C# system in `stage`, and returns the number it was given, for ordering it.
///
/// # Safety
/// `handle` must be a live app; `func` must remain callable until the app is destroyed.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_add_system(
    handle: *mut BcsApp,
    stage: i32,
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
        let Some(stage) = Stage::from_i32(stage) else {
            return status::NULL_ARG;
        };

        let reg = SystemReg { func, user };
        let id = app.next_system;
        app.next_system += 1;
        let run = (move |world: &mut World| loan_world(world, || reg.invoke())).in_set(BcsSystem(id));

        match stage {
            Stage::Startup => {
                app.app.add_systems(Startup, run);
            }
            Stage::First => {
                app.app.add_systems(First, run.in_set(BcsSet::First));
            }
            Stage::PreUpdate => {
                app.app.add_systems(PreUpdate, run);
            }
            Stage::Update => {
                app.app.add_systems(Update, run);
            }
            // Deliberately unordered against the once-a-frame stages, because it runs a variable
            // number of times between them.
            Stage::FixedUpdate => {
                app.app.add_systems(FixedUpdate, run);
            }
            Stage::PostUpdate => {
                app.app.add_systems(PostUpdate, run.in_set(BcsSet::PostUpdate));
            }
            Stage::Render => {
                app.app.add_systems(Last, run.in_set(BcsSet::Render));
            }
            Stage::Last => {
                app.app.add_systems(Last, run.in_set(BcsSet::Last));
            }
            Stage::FrameSync => {
                app.app.add_systems(First, run.in_set(BcsSet::Sync));
            }
            Stage::CommandFlush => {
                app.app.add_systems(PostUpdate, run.in_set(BcsSet::Flush));
            }
            Stage::Cleanup => {
                // Bevy has no post-loop schedule, so these are held in a shared list that the
                // in-loop cleanup system drains when an exit is requested.
                match app.cleanup.lock() {
                    Ok(mut guard) => guard.push(reg),
                    Err(poisoned) => poisoned.into_inner().push(reg),
                }
            }
        }

        id as i32
    })
}

/// Orders two C# systems of one stage, `first` before `then`, by the numbers
/// [`bcs_app_add_system`] gave them.
///
/// Through their sets, so the order holds in Bevy's own schedule, as `.before` on a Rust system
/// does. Startup, the frame stages and the fixed step each have a schedule the order is put in.
/// The engine's two internal stages and `Cleanup`, which runs its callbacks from a list in the
/// order they were added, are refused, since a game has no systems of its own in the first two and
/// nothing to order in the third.
///
/// # Safety
/// `handle` must be a live app that has not started running.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_order_systems(handle: *mut BcsApp, stage: i32, first: i32, then: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }
        let (Ok(first), Ok(then)) = (u32::try_from(first), u32::try_from(then)) else {
            return status::NULL_ARG;
        };
        if first >= app.next_system || then >= app.next_system || first == then {
            return status::NULL_ARG;
        }

        let order = BcsSystem(first).before(BcsSystem(then));
        match Stage::from_i32(stage) {
            Some(Stage::Startup) => app.app.configure_sets(Startup, order),
            Some(Stage::First) => app.app.configure_sets(First, order),
            Some(Stage::PreUpdate) => app.app.configure_sets(PreUpdate, order),
            Some(Stage::Update) => app.app.configure_sets(Update, order),
            Some(Stage::FixedUpdate) => app.app.configure_sets(FixedUpdate, order),
            Some(Stage::PostUpdate) => app.app.configure_sets(PostUpdate, order),
            Some(Stage::Render | Stage::Last) => app.app.configure_sets(Last, order),
            _ => return status::NULL_ARG,
        };

        status::OK
    })
}
