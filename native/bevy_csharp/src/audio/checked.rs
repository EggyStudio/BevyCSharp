//! Sounds checked to be sounds before Bevy plays them.
//!
//! Bevy's loader reads a sound file's bytes and nothing more, and looks at them only when the sound
//! first plays, where its decoder unwraps whatever reading the file gave. So an empty file, random
//! bytes or a file named `.wav` that holds something else loaded as a sound like any other, and
//! playing it panicked in Bevy's audio system and ended the game. Each sound is checked here as it
//! arrives, and again before a player plays one not yet checked, by building Bevy's decoder where
//! its panic is caught. One that gives none is taken out of the sounds Bevy holds, so a player of
//! it waits for a sound that never comes, its load answers that it failed, and the failure is
//! reported with the file's path as the asset server reports any failed load.
//!
//! A second loader for the same extensions would refuse such a file as it loads, but Bevy warns at
//! every app's start that two loaders claim one extension, and its warning says those loads will
//! not work, which they would.

use std::collections::{HashMap, HashSet};
use std::panic::AssertUnwindSafe;

use bevy::asset::{AssetEvent, AssetId, AssetServer, Assets, UntypedAssetId};
use bevy::audio::{AudioPlayer, AudioSink, AudioSource, Decodable, SpatialAudioSink};
use bevy::ecs::message::MessageReader;
use bevy::ecs::query::Without;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Query, Res, ResMut};

/// What the check has found.
#[derive(Resource, Default)]
pub struct CheckedSounds {
    /// Each sound checked since it arrived, and whether it plays.
    checked: HashMap<AssetId<AudioSource>, bool>,

    /// The sounds refused, whose loads answer that they failed until a sound that plays arrives
    /// under the same id, as a file put right and loaded again does.
    refused: HashSet<UntypedAssetId>,

    /// The sounds refused since the failures were last read, by path and reason.
    pub reported: Vec<(String, String)>,
}

impl CheckedSounds {
    /// Whether a sound was refused, which its load answers as failed.
    pub fn refuses(&self, id: UntypedAssetId) -> bool {
        self.refused.contains(&id)
    }
}

/// Whether Bevy's decoder reads a sound, or why it does not.
///
/// Bevy's own `Decodable`, which unwraps what building the decoder gave and so panics on a file it
/// cannot read, run where the panic is caught, so Bevy plays whatever passes here. Rodio's
/// builder answers with an error instead, and is a package Bevy's audio depends on that the bridge
/// does not reference, which N 2.8 of NORM.md leaves to the owner.
fn decodes(source: &AudioSource) -> Result<(), String> {
    // Quietly, so the panic is neither printed as a fault nor kept as the last one said.
    let built = crate::crash::quietly(|| std::panic::catch_unwind(AssertUnwindSafe(|| drop(source.decoder()))));

    built.map_err(|payload| {
        let said = payload
            .downcast_ref::<String>()
            .map(String::as_str)
            .or_else(|| payload.downcast_ref::<&str>().copied())
            .unwrap_or("its decoder failed");
        said.rsplit_once("value: ").map_or(said, |(_, reason)| reason).to_string()
    })
}

/// Checks the sounds arrived since the last frame and those a player waits to play.
fn check(
    mut events: MessageReader<AssetEvent<AudioSource>>,
    waiting: Query<&AudioPlayer, (Without<AudioSink>, Without<SpatialAudioSink>)>,
    mut sounds: ResMut<Assets<AudioSource>>,
    server: Res<AssetServer>,
    mut found: ResMut<CheckedSounds>,
) {
    let mut arrived = Vec::new();
    for event in events.read() {
        match event {
            AssetEvent::Added { id } | AssetEvent::Modified { id } => {
                found.checked.remove(id);
                arrived.push(*id);
            }
            AssetEvent::Unused { id } => {
                found.checked.remove(id);
            }
            _ => {}
        }
    }

    for id in arrived.into_iter().chain(waiting.iter().map(|player| player.0.id())) {
        if found.checked.contains_key(&id) {
            continue;
        }
        let Some(source) = sounds.get(id) else {
            continue;
        };

        let answer = decodes(source);
        found.checked.insert(id, answer.is_ok());
        let Err(reason) = answer else {
            found.refused.remove(&id.untyped());
            continue;
        };

        let path = server.get_path(id).map_or_else(|| format!("{id:?}"), |path| path.to_string());
        bevy::log::error!("{path} is not a sound this build plays: {reason}");
        sounds.remove(id);
        found.refused.insert(id.untyped());
        found.reported.push((path, reason));
    }
}

/// Adds the check, after Bevy's audio plugin has added the sounds, ahead of Bevy's playing them,
/// which comes after transforms are propagated.
pub fn install(app: &mut bevy::app::App) {
    app.init_resource::<CheckedSounds>();
    app.add_systems(
        bevy::app::PostUpdate,
        check.before(bevy::transform::TransformSystems::Propagate),
    );
}
