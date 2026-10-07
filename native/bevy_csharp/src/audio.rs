//! Sound, reachable from C#.
//!
//! A sound that is playing is an entity carrying an `AudioPlayer` and the settings it was started
//! with, so it is despawned like anything else and can be parented, tagged or queried. Bevy adds an
//! `AudioSink` to it once playback begins, and volume and pausing go through it.
//!
//! Everything here needs a render build, because that is the profile Bevy's audio is compiled
//! into, which is the one that takes a system library.

use crate::interop::{status, BcsAudioConfig};
#[cfg(feature = "render")]
use crate::state::{with_world, with_world_opt};

/// A sound's own volume, before the global volume.
///
/// Bevy multiplies the global volume in once, when a sink is made, so a sink's volume holds both
/// and neither can be read back apart. Kept here, a changed global volume reaches every sound
/// playing, and a sound's own volume changed later still carries the global one with it.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component, Clone, Copy)]
pub struct OwnVolume(pub f32);

/// The global volume, as a multiplier.
#[cfg(feature = "render")]
fn global_volume(world: &bevy::ecs::world::World) -> f32 {
    world
        .get_resource::<bevy::audio::GlobalVolume>()
        .map_or(1.0, |global| global.volume.to_linear())
}

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
                        // Cleans up after itself, as a one-shot sound effect should, so nothing has
                        // to remember to despawn it.
                        2 => PlaybackMode::Despawn,
                        _ => PlaybackMode::Once,
                    },
                    volume: Volume::Linear(config.volume),
                    speed: config.speed,
                    paused: config.paused != 0,
                    spatial: config.spatial != 0,
                    spatial_scale: (config.spatial_scale > 0.0)
                        .then(|| SpatialScale::new(config.spatial_scale)),

                    // Where in the clip to start and how much of it to play, which is how one
                    // file holds several effects. Both are left alone at zero, because a clip
                    // played for no time is not what a caller means by not saying.
                    start_position: (config.start_seconds > 0.0).then(|| {
                        core::time::Duration::from_secs_f32(config.start_seconds)
                    }),
                    duration: (config.play_seconds > 0.0).then(|| {
                        core::time::Duration::from_secs_f32(config.play_seconds)
                    }),
                    ..Default::default()
                };

                let mut entity = world.spawn((
                    AudioPlayer(handle.typed::<AudioSource>()),
                    settings,
                    OwnVolume(config.volume),
                ));

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
                let global = global_volume(world);

                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                let own = volume;
                let volume = own * global;

                // A spatial sound gets a different component carrying the same trait, so both
                // are tried rather than only the one a plain sound has.
                let answer = if let Some(mut sink) = entity_mut.get_mut::<AudioSink>() {
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
                };

                if answer == status::OK {
                    entity_mut.insert(OwnVolume(own));
                }

                answer
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
/// Usually the camera, so what is heard follows what is seen. Only one entity should carry it at a
/// time; Bevy takes the first it finds otherwise. `gap` is the distance between the two ears in
/// world units, which decides how pronounced the stereo is; `0` takes Bevy's own.
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

/// Makes an entity the ear, with each ear placed exactly.
///
/// The long form of [`bcs_audio_listener`], which puts the two ears on the x axis a gap apart.
/// `left` and `right` each point at three floats, a position relative to the entity's own
/// transform, for a listener attached to a head rather than to a camera.
///
/// # Safety
/// `left` and `right` must each point at three readable floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_audio_listener_ears(
    entity: u64,
    left: *const f32,
    right: *const f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, left, right);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::audio::SpatialListener;
            use bevy::math::Vec3;

            if left.is_null() || right.is_null() {
                return status::NULL_ARG;
            }

            let left = unsafe { core::slice::from_raw_parts(left, 3) };
            let right = unsafe { core::slice::from_raw_parts(right, 3) };

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                entity_mut.insert(SpatialListener {
                    left_ear_offset: Vec3::new(left[0], left[1], left[2]),
                    right_ear_offset: Vec3::new(right[0], right[1], right[2]),
                });

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

/// Writes a playing sound's own volume and whether it is paused.
///
/// The volume is the sound's own, after anything a mixer multiplied in on the managed side and
/// before the global volume, which the sink holds both of. Answers [`status::NOT_PRESENT`] before
/// the sink exists, which is the frame the sound was started in.
///
/// # Safety
/// `volume` and `paused` must each be writable or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_audio_state(entity: u64, volume: *mut f32, paused: *mut i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, volume, paused);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::audio::{AudioSink, AudioSinkPlayback, SpatialAudioSink};

            with_world(|world| {
                let Ok(entity_ref) = world.get_entity(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };

                let held = if let Some(sink) = entity_ref.get::<AudioSink>() {
                    sink.is_paused()
                } else if let Some(sink) = entity_ref.get::<SpatialAudioSink>() {
                    sink.is_paused()
                } else {
                    return status::NOT_PRESENT;
                };

                let heard = entity_ref.get::<OwnVolume>().map_or(1.0, |own| own.0);

                if !volume.is_null() {
                    unsafe { *volume = heard };
                }
                if !paused.is_null() {
                    unsafe { *paused = held as i32 };
                }

                status::OK
            })
        }
    })
}

/// Runs `f` over a playing sound's sink, a plain one or a spatial one, which carry the same trait.
///
/// Answers [`status::NO_ENTITY`] for an entity that is gone and [`status::NOT_PRESENT`] before the
/// sink exists, which is the frame the sound was started in.
#[cfg(feature = "render")]
fn with_sink(entity: u64, f: impl FnOnce(&mut dyn bevy::audio::AudioSinkPlayback) -> i32) -> i32 {
    use bevy::audio::{AudioSink, SpatialAudioSink};

    with_world(|world| {
        let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
            return status::NO_ENTITY;
        };

        if let Some(mut sink) = entity_mut.get_mut::<AudioSink>() {
            f(&mut *sink)
        } else if let Some(mut sink) = entity_mut.get_mut::<SpatialAudioSink>() {
            f(&mut *sink)
        } else {
            status::NOT_PRESENT
        }
    })
}

/// Sets how fast a playing sound plays, one as recorded, Bevy's `AudioSink::set_speed`.
///
/// A speed changes the pitch with it, as a record played fast does. Zero or less is refused, since
/// a sound held still is a paused one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_audio_speed(entity: u64, speed: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, speed);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if !(speed > 0.0) || !speed.is_finite() {
                return status::INVALID_STATE;
            }

            with_sink(entity, |sink| {
                sink.set_speed(speed);
                status::OK
            })
        }
    })
}

/// Mutes a playing sound or lets it be heard again, Bevy's `AudioSink::mute` and `unmute`.
///
/// Muting keeps the sound's volume, so unmuting brings it back as loud as it was, and a volume set
/// while muted is the one heard once unmuted.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_audio_mute(entity: u64, muted: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, muted);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_sink(entity, |sink| {
                if muted != 0 {
                    sink.mute();
                } else {
                    sink.unmute();
                }
                status::OK
            })
        }
    })
}

/// Writes how fast a playing sound plays and whether it is muted.
///
/// # Safety
/// `speed` and `muted` must each be writable or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_audio_playback(entity: u64, speed: *mut f32, muted: *mut i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, speed, muted);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_sink(entity, |sink| {
                if !speed.is_null() {
                    unsafe { *speed = sink.speed() };
                }
                if !muted.is_null() {
                    unsafe { *muted = sink.is_muted() as i32 };
                }
                status::OK
            })
        }
    })
}

/// Moves playback to a point in the clip, in seconds from its start.
///
/// A looping sound cannot be sought and reports [`status::INVALID_STATE`], because looping is
/// rodio's `Repeat` over a `Buffered` source, which keeps the decoded samples so the clip can start
/// again and refuses to move within them. Nothing here can work around that.
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

/// Scales every sound at once, as a settings screen does.
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
                use bevy::audio::{AudioSink, AudioSinkPlayback, SpatialAudioSink};

                world.insert_resource(GlobalVolume::new(Volume::Linear(volume)));

                // Bevy reads the global volume only when a sink is made, so the sounds already
                // playing are set here, each from its own volume.
                let mut plain = world.query::<(&OwnVolume, &mut AudioSink)>();
                for (own, mut sink) in plain.iter_mut(world) {
                    sink.set_volume(Volume::Linear(own.0 * volume));
                }

                let mut spatial = world.query::<(&OwnVolume, &mut SpatialAudioSink)>();
                for (own, mut sink) in spatial.iter_mut(world) {
                    sink.set_volume(Volume::Linear(own.0 * volume));
                }

                status::OK
            })
        }
    })
}
