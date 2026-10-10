//! Despawns done one entity at a time ahead of Bevy's own, which lose every index they despawn.
//!
//! Bevy 0.20.0's `World::despawn_all_where`, and `despawn_all` through it, despawns each matching
//! entity without freeing its index and then frees those whose old id still reads as despawned.
//! Despawning moves each index on to a new generation, so the old id reads as invalid instead and
//! none is freed. Every index it despawns is lost for the life of the world, which hands out new
//! ones in their place, and the record Bevy keeps for each index grows with them. Three of Bevy's
//! systems despawn through it, the one for what is scoped to a state with `DespawnOnExit` or
//! `DespawnOnEnter`, the one for a sound played to a device once it has ended, and the one for a
//! screenshot once it has been taken. Without what follows, a game that leaves a state over and
//! over climbs to many more indices than it ever holds, Swarm reaching 16,384 in ten minutes of
//! its soak with 500 alive.
//!
//! What follows despawns the same entities first, one at a time, which frees each, so Bevy's own
//! finds nothing left to lose. A test below holds Bevy's call to the fault, and fails once a
//! release frees what it despawns, which is when this module can go.

use bevy::app::App;
use bevy::ecs::entity::Entity;
use bevy::ecs::entity_disabling::Disabled;
use bevy::ecs::message::MessageReader;
use bevy::ecs::query::Allow;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query};
use bevy::state::state::{StateTransition, StateTransitionEvent, StateTransitionSystems, States};
use bevy::state::state_scoped::{
    despawn_entities_on_enter_state, despawn_entities_on_exit_state, DespawnOnEnter, DespawnOnExit,
};

/// Despawns what is scoped to the state `S` as it is left or entered, ahead of Bevy's systems for
/// the same and in the same sets of the transition schedule, so an entity goes at the moment it
/// did before. Called wherever the bridge adds a state, since Bevy adds its own there.
pub(crate) fn scope_state<S: States>(app: &mut App) {
    app.add_systems(
        StateTransition,
        despawn_on_exit::<S>
            .in_set(StateTransitionSystems::ExitSchedules)
            .before(despawn_entities_on_exit_state::<S>),
    )
    .add_systems(
        StateTransition,
        despawn_on_enter::<S>
            .in_set(StateTransitionSystems::EnterSchedules)
            .before(despawn_entities_on_enter_state::<S>),
    );
}

/// The state a transition left and the one it entered, as Bevy's systems read them. The last
/// transition alone, since a state makes one a frame at most, and none for a value set again
/// unless the transition says that counts.
fn transition<S: States>(
    transitions: &mut MessageReader<StateTransitionEvent<S>>,
) -> Option<(Option<S>, Option<S>)> {
    let transition = transitions.read().last()?;
    if !transition.allow_same_state_transitions && transition.entered == transition.exited {
        return None;
    }

    Some((transition.exited.clone(), transition.entered.clone()))
}

/// Despawns what `DespawnOnExit` ties to the state a transition left. A disabled entity goes too,
/// as it does in Bevy's, because being switched off is not leaving the state it belongs to.
fn despawn_on_exit<S: States>(
    mut commands: Commands,
    mut transitions: MessageReader<StateTransitionEvent<S>>,
    scoped: Query<(Entity, &DespawnOnExit<S>), Allow<Disabled>>,
) {
    let Some((Some(exited), _)) = transition(&mut transitions) else {
        return;
    };

    for (entity, scope) in &scoped {
        if scope.0 == exited {
            commands.entity(entity).try_despawn();
        }
    }
}

/// Despawns what `DespawnOnEnter` ties to the state a transition entered.
fn despawn_on_enter<S: States>(
    mut commands: Commands,
    mut transitions: MessageReader<StateTransitionEvent<S>>,
    scoped: Query<(Entity, &DespawnOnEnter<S>), Allow<Disabled>>,
) {
    let Some((_, Some(entered))) = transition(&mut transitions) else {
        return;
    };

    for (entity, scope) in &scoped {
        if scope.0 == entered {
            commands.entity(entity).try_despawn();
        }
    }
}

/// Adds the despawns of sounds and screenshots. Called once all of an app's plugins are in, since
/// whether sounds reach a device is known only then.
#[cfg(feature = "render")]
pub(crate) fn install(app: &mut App) {
    use bevy::app::{Last, PostUpdate};

    app.add_systems(Last, despawn_captured);

    // The bridge's own plugin plays to no device and despawns a sound that has ended itself, one
    // at a time, so this is for Bevy's alone.
    if !app.world().contains_resource::<crate::audio::silent::SilentOutput>() {
        app.add_observer(play_once);
        app.add_systems(
            PostUpdate,
            despawn_ended.after(bevy::transform::TransformSystems::Propagate),
        );
    }
}

/// A sound played to a device that was told to despawn at its end, and which Bevy's plugin is told
/// to play once instead, leaving the despawn to [`despawn_ended`].
///
/// Bevy's marker for the same is private to its crate, as is the set its systems run in, so
/// nothing here can be ordered ahead of them. A system that looked earlier in the frame would
/// still lose a sound that ended after its look and before theirs, since the device plays on a
/// thread of its own.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component)]
pub(crate) struct DespawnedAtEnd;

/// Turns a sound told to despawn at its end into one played once and marked, as its settings
/// arrive and before Bevy's plugin starts it, whichever way it was spawned. Its settings then read
/// `Once` where they were given as `Despawn`.
#[cfg(feature = "render")]
fn play_once(
    inserted: bevy::ecs::observer::On<bevy::ecs::lifecycle::Insert<bevy::audio::PlaybackSettings>>,
    mut commands: Commands,
    mut sounds: Query<&mut bevy::audio::PlaybackSettings>,
) {
    use bevy::audio::PlaybackMode;

    let Ok(mut settings) = sounds.get_mut(inserted.entity) else {
        return;
    };
    if !matches!(settings.mode, PlaybackMode::Despawn) {
        return;
    }

    settings.mode = PlaybackMode::Once;
    commands.entity(inserted.entity).insert(DespawnedAtEnd);
}

/// Despawns a marked sound whose device has played all of it.
#[cfg(feature = "render")]
fn despawn_ended(
    mut commands: Commands,
    sounds: Query<
        (Entity, Option<&bevy::audio::AudioSink>, Option<&bevy::audio::SpatialAudioSink>),
        bevy::ecs::query::With<DespawnedAtEnd>,
    >,
) {
    use bevy::audio::AudioSinkPlayback;

    for (entity, sink, spatial) in &sounds {
        let ended = sink.is_some_and(AudioSinkPlayback::empty)
            || spatial.is_some_and(AudioSinkPlayback::empty);
        if ended {
            commands.entity(entity).try_despawn();
        }
    }
}

/// Despawns a screenshot that has been taken, at the end of the frame its picture arrived in.
/// Bevy's despawns it at the start of the next, and the observers it was taken for have run by
/// then, in the commands of the frame's update.
#[cfg(feature = "render")]
fn despawn_captured(
    mut commands: Commands,
    captured: Query<Entity, bevy::ecs::query::With<bevy::render::view::screenshot::Captured>>,
) {
    for entity in &captured {
        commands.entity(entity).try_despawn();
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::ecs::component::Component;
    use bevy::ecs::query::With;
    use bevy::ecs::world::World;
    use bevy::state::app::{AppExtStates, StatesPlugin};
    use bevy::state::state::NextState;

    #[derive(Component)]
    struct Churned;

    #[derive(States, Default, Debug, Clone, Copy, PartialEq, Eq, Hash)]
    enum Screen {
        #[default]
        Menu,
        Playing,
    }

    #[test]
    fn bevy_despawn_all_still_loses_what_it_despawns() {
        let mut world = World::new();
        for _ in 0..4 {
            for _ in 0..100 {
                world.spawn(Churned);
            }
            world.despawn_all::<With<Churned>>();
        }

        // Four rounds of a hundred, none of whose indices came back for the next.
        assert!(
            world.entities().len() >= 400,
            "Bevy's despawn_all now frees what it despawns ({} indices for four rounds of a \
             hundred), so the despawns in ecs::despawn_all are no longer needed",
            world.entities().len()
        );
    }

    #[test]
    fn leaving_a_state_frees_the_indices_of_what_it_despawned() {
        let mut app = App::new();
        app.add_plugins(StatesPlugin).init_state::<Screen>();
        scope_state::<Screen>(&mut app);

        // Fifty that go as play is left, then fifty that go as the menu is entered, the two on the
        // same transition.
        for round in 0..200 {
            let world = app.world_mut();
            let next = if round % 2 == 0 {
                for _ in 0..50 {
                    world.spawn((Churned, DespawnOnExit(Screen::Playing)));
                }
                Screen::Playing
            } else {
                for _ in 0..50 {
                    world.spawn((Churned, DespawnOnEnter(Screen::Menu)));
                }
                Screen::Menu
            };
            world.resource_mut::<NextState<Screen>>().set(next);
            app.update();
        }

        let world = app.world_mut();
        let left = world.query::<&Churned>().iter(world).count();
        assert_eq!(left, 0, "the transitions left {left} behind");

        // Ten thousand despawned, a hundred alive at most, and the indices of the first rounds
        // given again in every one after. Bevy keeps a record for 256 at the least.
        let indices = world.entities().len();
        assert!(indices <= 256, "{indices} indices for a hundred alive at most");
    }

    #[cfg(feature = "render")]
    #[test]
    fn a_taken_screenshot_frees_its_index() {
        use bevy::render::view::screenshot::Captured;

        let mut app = App::new();
        app.add_systems(bevy::app::Last, despawn_captured);
        for _ in 0..200 {
            for _ in 0..10 {
                app.world_mut().spawn(Captured);
            }
            app.update();
        }

        // Two thousand despawned, which lost would take the world past 2,048 indices. Bevy keeps a
        // record for 256 at the least.
        let indices = app.world().entities().len();
        assert!(indices <= 256, "{indices} indices for ten alive at most");
    }

    #[cfg(feature = "render")]
    #[test]
    fn a_sound_told_to_despawn_is_played_once_and_marked() {
        use bevy::audio::{PlaybackMode, PlaybackSettings};

        let mut app = App::new();
        app.add_observer(play_once);
        let sound = app.world_mut().spawn(PlaybackSettings::DESPAWN).id();
        let looped = app.world_mut().spawn(PlaybackSettings::LOOP).id();
        app.world_mut().flush();

        let world = app.world();
        assert!(matches!(world.get::<PlaybackSettings>(sound).unwrap().mode, PlaybackMode::Once));
        assert!(world.get::<DespawnedAtEnd>(sound).is_some());
        assert!(matches!(world.get::<PlaybackSettings>(looped).unwrap().mode, PlaybackMode::Loop));
        assert!(world.get::<DespawnedAtEnd>(looped).is_none());
    }
}
