//! Sound, reachable from C#.
//!
//! A sound that is playing is an entity carrying an `AudioPlayer` and the settings it was started
//! with, so it is despawned like anything else and can be parented, tagged or queried. Bevy adds an
//! `AudioSink` to it once playback begins, and volume and pausing go through it. In a run with no
//! window the bridge's `SilentSink` stands in its place, which plays to no device. See `silent`.
//!
//! Everything here needs a render build, because that is the profile Bevy's audio is compiled
//! into, which is the one that takes a system library.

use crate::interop::{status, BcsAudioConfig};
#[cfg(feature = "render")]
use crate::interop::BcsConfig;

#[cfg(feature = "render")]
pub mod checked;
#[cfg(feature = "render")]
pub mod silent;
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

/// What a world unit is to every spatial sound that does not say otherwise.
///
/// One answer for the app, because how far away a sound is depends on what the world is measured
/// in, and that is a fact about the game rather than about any one sound. A sound may still say
/// otherwise for itself.
#[cfg(feature = "render")]
fn spatial_scale(config: &BcsConfig) -> bevy::audio::SpatialScale {
    let scale = if config.spatial_scale > 0.0 { config.spatial_scale } else { 1.0 };
    bevy::audio::SpatialScale::new(scale)
}

/// Bevy's audio plugin, which plays to the machine's device where there is one.
#[cfg(feature = "render")]
pub(crate) fn device_plugin(config: &BcsConfig) -> bevy::audio::AudioPlugin {
    bevy::audio::AudioPlugin {
        default_spatial_scale: spatial_scale(config),
        ..Default::default()
    }
}

/// The bridge's audio plugin, which plays to no device. Added after the asset plugin, whose types
/// it registers its own with.
#[cfg(feature = "render")]
pub(crate) fn silent_plugin(config: &BcsConfig) -> silent::SilentAudio {
    silent::SilentAudio {
        default_spatial_scale: spatial_scale(config),
    }
}

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
            use bevy::audio::Volume;

            with_world(|world| {
                let global = global_volume(world);

                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                let own = volume;
                let volume = own * global;

                let controlled = sink_of(&mut entity_mut, |sink| {
                    sink.set_volume(Volume::Linear(volume));
                    if paused != 0 {
                        sink.pause();
                    } else {
                        sink.play();
                    }
                });

                if controlled.is_none() {
                    return status::NOT_PRESENT;
                }

                entity_mut.insert(OwnVolume(own));
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
            with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                sink_of(&mut entity_mut, |sink| sink.stop());
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
            if seconds.is_null() {
                return status::NULL_ARG;
            }

            with_sink(entity, |sink| {
                unsafe { *seconds = sink.position().as_secs_f32() };
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
            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                let Some(held) = sink_of(&mut entity_mut, |sink| sink.is_paused()) else {
                    return status::NOT_PRESENT;
                };

                let heard = entity_mut.get::<OwnVolume>().map_or(1.0, |own| own.0);

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

/// Runs `f` over a playing sound's sink, or answers `None` before it has one.
///
/// Three components carry the same trait: Bevy's sink for a plain sound, its sink for a spatial
/// one, and the bridge's for a sound in a run with no window, so each is tried in turn.
#[cfg(feature = "render")]
fn sink_of<R>(
    entity: &mut bevy::ecs::world::EntityWorldMut,
    f: impl FnOnce(&mut dyn bevy::audio::AudioSinkPlayback) -> R,
) -> Option<R> {
    use bevy::audio::{AudioSink, SpatialAudioSink};

    if let Some(mut sink) = entity.get_mut::<AudioSink>() {
        Some(f(&mut *sink))
    } else if let Some(mut sink) = entity.get_mut::<SpatialAudioSink>() {
        Some(f(&mut *sink))
    } else if let Some(mut sink) = entity.get_mut::<silent::SilentSink>() {
        Some(f(&mut *sink))
    } else {
        None
    }
}

/// Runs `f` over a playing sound's sink, whichever kind it has.
///
/// Answers [`status::NO_ENTITY`] for an entity that is gone and [`status::NOT_PRESENT`] before the
/// sink exists, which is the frame the sound was started in.
#[cfg(feature = "render")]
fn with_sink(entity: u64, f: impl FnOnce(&mut dyn bevy::audio::AudioSinkPlayback) -> i32) -> i32 {
    with_world(|world| {
        let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
            return status::NO_ENTITY;
        };

        sink_of(&mut entity_mut, f).unwrap_or(status::NOT_PRESENT)
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
            if !seconds.is_finite() || seconds < 0.0 {
                return status::NULL_ARG;
            }
            let position = core::time::Duration::from_secs_f32(seconds);

            with_sink(entity, |sink| match sink.try_seek(position) {
                Ok(()) => status::OK,
                Err(_) => status::INVALID_STATE,
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

                let mut unheard = world.query::<(&OwnVolume, &mut silent::SilentSink)>();
                for (own, mut sink) in unheard.iter_mut(world) {
                    sink.set_volume(Volume::Linear(own.0 * volume));
                }

                status::OK
            })
        }
    })
}

/// Writes whether this run plays its sounds to no device, as a run with no window does unless its
/// config asks otherwise. A build without audio has nothing to play to and answers so too.
///
/// # Safety
/// `silent` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_audio_silent(silent: *mut i32) -> i32 {
    crate::interop::guard(|| {
        if silent.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            unsafe { *silent = 1 };
            status::OK
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let none = world.contains_resource::<crate::audio::silent::SilentOutput>();
                unsafe { *silent = none as i32 };
                status::OK
            })
        }
    })
}
