//! The stages a C# system is scheduled in, and the sets that order the C# systems among Bevy's
//! own, which `app` wires into Bevy's schedules.

use bevy::ecs::schedule::SystemSet;

/// The scheduling stage a C# system asked for. Mirrors `Bevy.Stage` on the managed side.
#[derive(Clone, Copy, PartialEq, Eq)]
#[repr(i32)]
pub enum Stage {
    /// Once, before the loop starts.
    Startup = 0,
    /// Top of each frame.
    First = 1,
    /// Before `Update`.
    PreUpdate = 2,
    /// Main gameplay stage.
    Update = 3,
    /// After `Update`; where queued commands are applied.
    PostUpdate = 4,
    /// Drawing and overlay work, ordered before `Last`.
    Render = 5,
    /// End of each frame.
    Last = 6,
    /// Once, after the loop exits.
    Cleanup = 7,
    /// Bevy's fixed timestep, run zero or more times a frame, each covering the same slice of time.
    FixedUpdate = 10,
    /// Internal: mirrors Bevy's time and input into C#, ahead of every user system.
    FrameSync = 8,
    /// Internal: applies the managed command buffer, after every user `PostUpdate` system.
    CommandFlush = 9,
}

impl Stage {
    /// Converts the discriminant C# sent, rejecting anything out of range.
    pub fn from_i32(value: i32) -> Option<Stage> {
        Some(match value {
            0 => Stage::Startup,
            1 => Stage::First,
            2 => Stage::PreUpdate,
            3 => Stage::Update,
            4 => Stage::PostUpdate,
            5 => Stage::Render,
            6 => Stage::Last,
            7 => Stage::Cleanup,
            8 => Stage::FrameSync,
            9 => Stage::CommandFlush,
            10 => Stage::FixedUpdate,
            _ => return None,
        })
    }
}

/// Orders the C# systems Bevy cannot order for itself.
///
/// Every C# system is an exclusive system, so Bevy serializes them but leaves the order
/// unspecified. These sets pin down the three places where order actually matters:
/// the frame-state sync must precede all user work, the command flush must follow all
/// user `PostUpdate` work, and `Stage.Render` must precede `Stage.Last`.
#[derive(SystemSet, Debug, Clone, PartialEq, Eq, Hash)]
pub(crate) enum BcsSet {
    Sync,
    First,
    PostUpdate,
    Flush,
    Render,
    Last,
    /// Where the engine decides whether this is the final frame.
    ExitCheck,
    Cleanup,
}

/// The variable naming a seed every schedule's order of systems is shuffled by.
pub const SHUFFLE_SEED_VARIABLE: &str = "BCS_SCHEDULE_SHUFFLE_SEED";

/// Shuffles the order every schedule of the app and its render world runs its systems in, within
/// what their constraints allow, where [`SHUFFLE_SEED_VARIABLE`] names a seed.
///
/// For finding a system that leans on an order nothing states, which another order breaks, as a
/// run of the suite with a seed does. Bevy's own `ScheduleBuildSettings::shuffle_seed`, which the
/// `debug` feature every profile carries offers. Set as the app starts to run, once the managed
/// side has added its systems and its states, and only the seed is changed of each schedule's
/// settings, so what else a schedule was built with stays.
pub(crate) fn shuffle_from_environment(app: &mut bevy::app::App) {
    let Some(seed) = std::env::var(SHUFFLE_SEED_VARIABLE)
        .ok()
        .and_then(|value| value.trim().parse::<u64>().ok())
    else {
        return;
    };

    let seeded = |world: &mut bevy::ecs::world::World| {
        if let Some(mut schedules) = world.get_resource_mut::<bevy::ecs::schedule::Schedules>() {
            for (_, schedule) in schedules.iter_mut() {
                let mut settings = schedule.get_build_settings();
                settings.shuffle_seed = Some(seed);
                schedule.set_build_settings(settings);
            }
        }
    };

    seeded(app.world_mut());

    #[cfg(feature = "render")]
    if let Some(render) = app.get_sub_app_mut(bevy::render::RenderApp) {
        seeded(render.world_mut());
    }

    bevy::log::info!("Every schedule runs its systems in an order shuffled by the seed {seed}.");
}
