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
