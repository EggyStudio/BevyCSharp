//! Sound, reachable from C#.
//!
//! A sound that is playing is an entity carrying an `AudioPlayer` and the settings it was started
//! with, so it is despawned like anything else and can be parented, tagged or queried. Bevy adds
//! an `AudioSink` to it once playback begins, which is what volume and pausing go through.
//!
//! Everything here needs a render build, because that is the profile Bevy's audio is compiled
//! into: it is the one that takes a system library.

use crate::interop::{status, BcsAudioConfig};
#[cfg(feature = "render")]
use crate::state::{with_world, with_world_opt};

/// Starts a sound and returns the entity playing it, or `0`.
///
/// The sound need not have finished loading; playback begins when it has.
///
/// # Safety
/// `config` must point to a readable [`BcsAudioConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_audio_play(clip: i32, config: *const BcsAudioConfig) -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "render"))]
        {
            let _ = (clip, config);
            0
        }

        #[cfg(feature = "render")]
        {
            use bevy::audio::{AudioPlayer, AudioSource, PlaybackMode, PlaybackSettings, Volume};

            if config.is_null() {
                return 0;
            }
            let config = unsafe { *config };

            with_world_opt(|world| {
                let Some(handle) = crate::assets::clone_handle(world, clip) else {
                    return 0;
                };

                let settings = PlaybackSettings {
                    mode: match config.mode {
                        1 => PlaybackMode::Loop,
                        // Cleans up after itself, which is what a one-shot sound effect wants:
                        // nothing has to remember to despawn it.
                        2 => PlaybackMode::Despawn,
                        _ => PlaybackMode::Once,
                    },
                    volume: Volume::Linear(config.volume),
                    speed: config.speed,
                    paused: config.paused != 0,
                    ..Default::default()
                };

                world
                    .spawn((AudioPlayer(handle.typed::<AudioSource>()), settings))
                    .id()
                    .to_bits()
            })
            .unwrap_or(0)
        }
    })
}

/// Changes a playing sound: its volume, and whether it is paused.
///
/// Reaches the `AudioSink` Bevy attaches once playback has started, so a call in the same frame
/// as [`bcs_audio_play`] reports [`status::NOT_PRESENT`] rather than taking effect.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_audio_control(entity: u64, volume: f32, paused: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, volume, paused);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::audio::{AudioSink, AudioSinkPlayback, Volume};

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };
                let Some(mut sink) = entity_mut.get_mut::<AudioSink>() else {
                    return status::NOT_PRESENT;
                };

                sink.set_volume(Volume::Linear(volume));
                if paused != 0 {
                    sink.pause();
                } else {
                    sink.play();
                }
                status::OK
            })
        }
    })
}

/// Stops a sound and despawns the entity playing it.
///
/// Stopping through the sink first, rather than despawning alone, so the sound ends at once
/// instead of when the audio thread next notices the entity is gone.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_audio_stop(entity: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = entity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::audio::{AudioSink, AudioSinkPlayback};

            with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                if let Some(sink) = entity_mut.get_mut::<AudioSink>() {
                    sink.stop();
                }

                entity_mut.despawn();
                status::OK
            })
        }
    })
}
