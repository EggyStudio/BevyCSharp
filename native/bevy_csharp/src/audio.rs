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
            use bevy::audio::{
                AudioPlayer, AudioSource, PlaybackMode, PlaybackSettings, SpatialScale, Volume,
            };

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
                    spatial: config.spatial != 0,
                    spatial_scale: (config.spatial_scale > 0.0)
                        .then(|| SpatialScale::new(config.spatial_scale)),
                    ..Default::default()
                };

                let mut entity = world.spawn((AudioPlayer(handle.typed::<AudioSource>()), settings));

                // A spatial sound is placed by its transform, so it is given one to write into.
                // A plain sound has no position and is not burdened with a component that would
                // only mislead.
                if config.spatial != 0 {
                    entity.insert(bevy::transform::components::Transform::default());
                }

                entity.id().to_bits()
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
            use bevy::audio::{AudioSink, AudioSinkPlayback, SpatialAudioSink, Volume};

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                // A spatial sound gets a different component carrying the same trait, so both
                // are tried rather than only the one a plain sound has.
                if let Some(mut sink) = entity_mut.get_mut::<AudioSink>() {
                    sink.set_volume(Volume::Linear(volume));
                    if paused != 0 {
                        sink.pause();
                    } else {
                        sink.play();
                    }
                    status::OK
                } else if let Some(mut sink) = entity_mut.get_mut::<SpatialAudioSink>() {
                    sink.set_volume(Volume::Linear(volume));
                    if paused != 0 {
                        sink.pause();
                    } else {
                        sink.play();
                    }
                    status::OK
                } else {
                    status::NOT_PRESENT
                }
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
            use bevy::audio::{AudioSink, AudioSinkPlayback, SpatialAudioSink};

            with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                if let Some(sink) = entity_mut.get_mut::<AudioSink>() {
                    sink.stop();
                } else if let Some(sink) = entity_mut.get_mut::<SpatialAudioSink>() {
                    sink.stop();
                }

                entity_mut.despawn();
                status::OK
            })
        }
    })
}

/// Makes an entity the ear spatial sound is heard from.
///
/// Usually the camera, so what is heard follows what is seen. Only one entity should carry it at
/// a time; Bevy takes the first it finds otherwise. `gap` is the distance between the two ears in
/// world units, which is what decides how pronounced the stereo is; `0` takes Bevy's own.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_audio_listener(entity: u64, gap: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, gap);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::audio::SpatialListener;

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                let listener = if gap > 0.0 {
                    SpatialListener::new(gap)
                } else {
                    SpatialListener::default()
                };

                // `SpatialListener` requires a `Transform`, which Bevy's insert brings with it,
                // so a camera that already has one keeps the one it has.
                entity_mut.insert(listener);
                status::OK
            })
        }
    })
}

/// Writes how far into its clip a sound has played, in seconds.
///
/// # Safety
/// `seconds` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_audio_position(entity: u64, seconds: *mut f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, seconds);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::audio::{AudioSink, AudioSinkPlayback, SpatialAudioSink};

            if seconds.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let Ok(entity_ref) = world.get_entity(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };

                let position = if let Some(sink) = entity_ref.get::<AudioSink>() {
                    sink.position()
                } else if let Some(sink) = entity_ref.get::<SpatialAudioSink>() {
                    sink.position()
                } else {
                    return status::NOT_PRESENT;
                };

                unsafe { *seconds = position.as_secs_f32() };
                status::OK
            })
        }
    })
}

/// Moves playback to a point in the clip, in seconds from its start.
///
/// A looping sound cannot be sought and reports [`status::INVALID_STATE`]: looping is rodio's
/// `Repeat` over a `Buffered` source, which keeps the decoded samples so the clip can start again
/// and refuses to move within them. Nothing here can work around that.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_audio_seek(entity: u64, seconds: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, seconds);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::audio::{AudioSink, AudioSinkPlayback, SpatialAudioSink};
            use core::time::Duration;

            if !seconds.is_finite() || seconds < 0.0 {
                return status::NULL_ARG;
            }
            let position = Duration::from_secs_f32(seconds);

            with_world(|world| {
                let Ok(entity_ref) = world.get_entity(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };

                let sought = if let Some(sink) = entity_ref.get::<AudioSink>() {
                    sink.try_seek(position)
                } else if let Some(sink) = entity_ref.get::<SpatialAudioSink>() {
                    sink.try_seek(position)
                } else {
                    return status::NOT_PRESENT;
                };

                match sought {
                    Ok(()) => status::OK,
                    Err(_) => status::INVALID_STATE,
                }
            })
        }
    })
}

/// Scales every sound at once, which is what a settings screen changes.
///
/// Multiplied with each sound's own volume rather than replacing it, so the mix a game set up
/// survives the master slider being moved.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_audio_global_volume(volume: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = volume;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::audio::{GlobalVolume, Volume};

            if !volume.is_finite() || volume < 0.0 {
                return status::NULL_ARG;
            }

            with_world(|world| {
                world.insert_resource(GlobalVolume::new(Volume::Linear(volume)));
                status::OK
            })
        }
    })
}
