//! Sound a game makes as it plays, samples written from C# into a queue that a decoder of Bevy's
//! reads.
//!
//! Bevy plays a sound of a game's own through a type implementing its `Decodable`, whose decoder
//! makes each sample on the audio thread as the device asks for it. C# cannot run there, so the game
//! writes its samples ahead into a queue, and the bridge's decoder takes them as the device asks.
//! Where the game has fallen behind it plays silence rather than ending the sound, so a stream
//! carries on for as long as it is played, as Bevy's sine never ends.
//!
//! The decoder takes the queue's samples a stretch at a time into its own, so the audio thread meets
//! the game's writes once a stretch rather than once a sample. A stretch and the silence where the
//! queue has run dry are each whole frames, a sample for every channel, so the game's next frame
//! starts on the first channel however long the stream went without.

#[cfg(feature = "render")]
use std::collections::VecDeque;
#[cfg(feature = "render")]
use std::sync::{Arc, Mutex};

use crate::interop::status;

#[cfg(feature = "render")]
use bevy::audio::{ChannelCount, Decodable, SampleRate, Source};

/// The most samples the decoder takes from the queue at once.
#[cfg(feature = "render")]
const STRETCH: usize = 512;

/// A stream of samples the game writes, played as Bevy plays a sound.
#[cfg(feature = "render")]
#[derive(bevy::asset::Asset, bevy::reflect::TypePath)]
pub struct BcsSamples {
    rate: SampleRate,
    channels: ChannelCount,
    queue: Arc<Mutex<VecDeque<f32>>>,
}

/// What plays a [`BcsSamples`], taking its queue's samples as the device asks for them.
#[cfg(feature = "render")]
pub struct SamplesDecoder {
    rate: SampleRate,
    channels: ChannelCount,
    queue: Arc<Mutex<VecDeque<f32>>>,
    taken: VecDeque<f32>,
}

#[cfg(feature = "render")]
impl Iterator for SamplesDecoder {
    type Item = f32;

    fn next(&mut self) -> Option<f32> {
        if self.taken.is_empty() {
            // The queue holds whole frames, since a write of part of one is refused, so a stretch of
            // whole frames leaves it holding whole frames too.
            let channels = usize::from(self.channels.get());
            let mut queue = self.queue.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
            let stretch = queue.len().min((STRETCH / channels).max(1) * channels);
            self.taken.extend(queue.drain(..stretch));

            // A frame of silence where the game has written nothing yet, so the sound goes on and
            // what the game writes next is heard on the channels it was written for.
            if self.taken.is_empty() {
                self.taken.extend(std::iter::repeat_n(0.0, channels));
            }
        }

        self.taken.pop_front()
    }
}

#[cfg(feature = "render")]
impl Source for SamplesDecoder {
    fn current_span_len(&self) -> Option<usize> {
        None
    }

    fn channels(&self) -> ChannelCount {
        self.channels
    }

    fn sample_rate(&self) -> SampleRate {
        self.rate
    }

    fn total_duration(&self) -> Option<core::time::Duration> {
        None
    }
}

#[cfg(feature = "render")]
impl Decodable for BcsSamples {
    type Decoder = SamplesDecoder;

    fn decoder(&self) -> Self::Decoder {
        SamplesDecoder {
            rate: self.rate,
            channels: self.channels,
            queue: self.queue.clone(),
            taken: VecDeque::new(),
        }
    }
}

/// The queue of the stream behind `key`.
#[cfg(feature = "render")]
fn queue_of(world: &bevy::ecs::world::World, key: i32) -> Result<(Arc<Mutex<VecDeque<f32>>>, u16), i32> {
    let handle = crate::assets::clone_handle(world, key)
        .and_then(|handle| handle.try_typed::<BcsSamples>().ok())
        .ok_or(status::NO_COMPONENT)?;
    let streams = world.get_resource::<bevy::asset::Assets<BcsSamples>>().ok_or(status::UNSUPPORTED)?;
    let stream = streams.get(&handle).ok_or(status::NOT_PRESENT)?;
    Ok((stream.queue.clone(), stream.channels.get()))
}

/// Makes a stream of samples the game writes, `rate` a second and `channels` to a frame, and
/// answers its asset key, which `bcs_audio_play` plays as it plays a clip.
///
/// Answers [`status::NULL_ARG`] for a rate or a count of channels of zero, and
/// [`status::UNSUPPORTED`] where the app has no audio.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_audio_create_samples(rate: u32, channels: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (rate, channels);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let (Some(rate), Some(channels)) =
                (SampleRate::new(rate), u16::try_from(channels).ok().and_then(ChannelCount::new))
            else {
                return status::NULL_ARG;
            };

            crate::state::with_world(|world| {
                let Some(mut streams) = world.get_resource_mut::<bevy::asset::Assets<BcsSamples>>() else {
                    return status::UNSUPPORTED;
                };

                let stream = BcsSamples { rate, channels, queue: Arc::default() };
                let handle = streams.add(stream).untyped();
                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

/// Writes `count` samples at `samples` after those the stream behind `key` holds, each frame's
/// channels one after another, and answers how many it then holds.
///
/// Answers [`status::NULL_ARG`] for a count that is not whole frames, [`status::NO_COMPONENT`] for a
/// key that names no stream, and [`status::UNSUPPORTED`] where the app has no audio.
///
/// # Safety
/// `samples` must hold `count` floats, or be null where `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_audio_queue_samples(key: i32, samples: *const f32, count: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (key, samples, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if count < 0 || (samples.is_null() && count > 0) {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| {
                let (queue, channels) = match queue_of(world, key) {
                    Ok(found) => found,
                    Err(refusal) => return refusal,
                };
                if count as usize % channels as usize != 0 {
                    return status::NULL_ARG;
                }

                let given = unsafe { crate::interop::opt_slice(samples, count) };
                let mut queue = queue.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
                queue.extend(given.iter().copied());
                i32::try_from(queue.len()).unwrap_or(i32::MAX)
            })
        }
    })
}

/// Answers how many samples the stream behind `key` holds that have not been played.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_audio_queued_samples(key: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = key;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        crate::state::with_world(|world| match queue_of(world, key) {
            Ok((queue, _)) => {
                let queue = queue.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
                i32::try_from(queue.len()).unwrap_or(i32::MAX)
            }
            Err(refusal) => refusal,
        })
    })
}

/// Lets Bevy play streams of samples to the machine's device, where it plays to one. A run that
/// plays to none has them from `silent`, beside its other sources.
#[cfg(feature = "render")]
pub(crate) fn install(app: &mut bevy::app::App) {
    use bevy::audio::AddAudioSource;

    if !app.world().contains_resource::<super::silent::SilentOutput>() {
        app.add_audio_source::<BcsSamples>();
    }
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use super::*;

    /// The decoder plays what was queued in order, then silence rather than ending, and takes
    /// what is queued later.
    #[test]
    fn the_decoder_plays_the_queue_then_silence_and_carries_on() {
        let stream = BcsSamples {
            rate: SampleRate::new(4).unwrap(),
            channels: ChannelCount::new(1).unwrap(),
            queue: Arc::new(Mutex::new([0.25, 0.5].into())),
        };
        let mut decoder = stream.decoder();

        assert_eq!(decoder.next(), Some(0.25));
        assert_eq!(decoder.next(), Some(0.5));
        assert_eq!(decoder.next(), Some(0.0));

        stream.queue.lock().unwrap().push_back(0.75);
        assert_eq!(decoder.next(), Some(0.75));
    }

    /// A stereo stream that runs dry part of the way through what the device asks for comes back
    /// with its left samples on the left, its silence a frame at a time.
    #[test]
    fn a_stream_that_runs_dry_comes_back_on_its_first_channel() {
        let stream = BcsSamples {
            rate: SampleRate::new(4).unwrap(),
            channels: ChannelCount::new(2).unwrap(),
            queue: Arc::new(Mutex::new([-1.0, 1.0].into())),
        };
        let mut decoder = stream.decoder();

        let mut heard: Vec<f32> = (0..3).filter_map(|_| decoder.next()).collect();
        stream.queue.lock().unwrap().extend([-1.0, 1.0, -1.0, 1.0]);
        heard.extend((0..7).filter_map(|_| decoder.next()));

        assert_eq!(heard, [-1.0, 1.0, 0.0, 0.0, -1.0, 1.0, -1.0, 1.0, 0.0, 0.0]);
    }
}
