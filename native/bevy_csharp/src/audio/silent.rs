//! Sound played to no device, for a run that shows no window.
//!
//! A run with no window to look at is a test, a soak, a capture or a tool, and a sound it plays
//! reaches somebody beside the machine who never asked for it. Bevy's audio plugin opens the
//! machine's device whenever there is one and keeps it in a resource nothing outside Bevy can name
//! or set, so a run cannot hand it no device. The bridge adds this plugin in its place instead,
//! which registers the same assets and the same settings and opens nothing.
//!
//! A sound still runs its course. Each is decoded from its clip with the window, the loop and the
//! settings Bevy would play it with, and drawn from as fast as a device draws, on the app's real
//! clock, so it pauses, changes speed, seeks, loops and ends when it would be heard to, and a sound
//! told to despawn at its end does so on time. A game that waits on a sound's end works the same in
//! such a run. Nothing is mixed and nothing is written anywhere. The sink is the bridge's own,
//! carrying Bevy's `AudioSinkPlayback`, so the calls that reach a playing sound reach it as they
//! reach Bevy's.
//!
//! Where a sound is reads as the time into its clip, as `bcs_audio_position` promises. A device's
//! sink at a speed other than one counts the time it was heard for instead.

use std::sync::{Mutex, MutexGuard, PoisonError};
use std::time::Duration;

use bevy::app::{App, Plugin, PostUpdate};
use bevy::asset::{Asset, AssetApp, Assets};
use bevy::audio::{
    AudioLoader, AudioPlayer, AudioSinkPlayback, AudioSource, Decodable, DefaultSpatialScale,
    GlobalVolume, PlaybackMode, PlaybackSettings, Pitch, SeekError, Source, SpatialScale, Volume,
};
use bevy::ecs::prelude::*;
use bevy::time::{Real, Time};

/// Plays every sound to no device. See the module.
pub struct SilentAudio {
    /// What a world unit is for a spatial sound that does not say, kept so a reader of the setting
    /// finds what Bevy's plugin would have put there.
    pub default_spatial_scale: SpatialScale,
}

/// Present while sounds go to no device, which `bcs_audio_silent` reads.
#[derive(Resource)]
pub struct SilentOutput;

impl Plugin for SilentAudio {
    fn build(&self, app: &mut App) {
        app.insert_resource(GlobalVolume::default())
            .insert_resource(DefaultSpatialScale(self.default_spatial_scale))
            .insert_resource(SilentOutput)
            .init_asset::<AudioSource>()
            .init_asset_loader::<AudioLoader>()
            .init_asset::<Pitch>();

        // Sounds playing move on before this frame's are started, so a new sound starts at its
        // first sample, and one that runs out is despawned in the frame it did. After transforms
        // are propagated, where Bevy plays its own, which is after `audio::checked` has refused a
        // file no decoder reads, since building a decoder from one panics.
        app.add_systems(
            PostUpdate,
            (
                advance,
                (start::<AudioSource>, start::<Pitch>),
                (finish::<AudioSource>, finish::<Pitch>),
            )
                .chain()
                .after(bevy::transform::TransformSystems::Propagate),
        );
    }
}

/// What a sound playing to no device holds.
#[derive(Component)]
pub struct SilentSink(Mutex<Playing>);

/// A sound's samples and the state Bevy's sink keeps beside them.
struct Playing {
    source: Box<dyn Source + Send>,
    volume: Volume,
    speed: f32,
    paused: bool,
    muted: bool,
    ended: bool,
    /// Seconds into the clip.
    position: f64,
    /// Seconds of the clip the clock has passed and no whole sample has taken up yet.
    owed: f64,
}

impl Playing {
    /// Draws the samples `seconds` of the clock cover at the sound's speed, as a device would.
    fn advance(&mut self, seconds: f64) {
        if self.paused || self.ended {
            return;
        }

        self.owed += seconds * f64::from(self.speed);

        // A sample's length is asked of the source each time, since a clip may change its rate or
        // its channels between spans.
        loop {
            let rate = f64::from(self.source.sample_rate().get());
            let channels = f64::from(self.source.channels().get());
            let sample = 1.0 / (rate * channels);

            if self.owed < sample {
                return;
            }

            if self.source.next().is_none() {
                self.ended = true;
                self.owed = 0.0;
                return;
            }

            self.owed -= sample;
            self.position += sample;
        }
    }
}

impl SilentSink {
    fn playing(&self) -> MutexGuard<'_, Playing> {
        self.0.lock().unwrap_or_else(PoisonError::into_inner)
    }
}

impl AudioSinkPlayback for SilentSink {
    fn volume(&self) -> Volume {
        self.playing().volume
    }

    fn set_volume(&mut self, volume: Volume) {
        self.playing().volume = volume;
    }

    fn speed(&self) -> f32 {
        self.playing().speed
    }

    fn set_speed(&self, speed: f32) {
        self.playing().speed = speed;
    }

    fn play(&self) {
        self.playing().paused = false;
    }

    fn position(&self) -> Duration {
        Duration::from_secs_f64(self.playing().position)
    }

    /// Moves within the clip as Bevy's sink does, a looping one refusing, and does nothing to a
    /// sound that has ended, as rodio's sink does nothing once its sound is gone.
    fn try_seek(&self, position: Duration) -> Result<(), SeekError> {
        let mut playing = self.playing();
        if playing.ended {
            return Ok(());
        }

        playing.source.try_seek(position)?;
        playing.position = position.as_secs_f64();
        playing.owed = 0.0;
        Ok(())
    }

    fn pause(&self) {
        self.playing().paused = true;
    }

    fn is_paused(&self) -> bool {
        self.playing().paused
    }

    fn stop(&self) {
        let mut playing = self.playing();
        playing.ended = true;
        playing.position = 0.0;
    }

    fn empty(&self) -> bool {
        self.playing().ended
    }

    fn is_muted(&self) -> bool {
        self.playing().muted
    }

    fn mute(&mut self) {
        self.playing().muted = true;
    }

    fn unmute(&mut self) {
        self.playing().muted = false;
    }
}

/// Moves every sound playing on by the frame's real time, as a device plays on while the game's
/// own clock is paused or scaled.
fn advance(time: Res<Time<Real>>, sinks: Query<&SilentSink>) {
    let seconds = time.delta_secs_f64();
    for sink in &sinks {
        sink.playing().advance(seconds);
    }
}

/// Gives a sink to every sound whose clip has loaded, with what Bevy would have given its own.
fn start<T: Asset + Decodable>(
    mut commands: Commands,
    clips: Res<Assets<T>>,
    global: Res<GlobalVolume>,
    waiting: Query<(Entity, &AudioPlayer<T>, &PlaybackSettings), Without<SilentSink>>,
) {
    for (entity, player, settings) in &waiting {
        let Some(clip) = clips.get(&player.0) else {
            continue;
        };

        commands.entity(entity).insert(SilentSink(Mutex::new(Playing {
            source: shaped(clip.decoder(), settings),
            volume: settings.volume * global.volume,
            speed: settings.speed,
            paused: settings.paused,
            muted: settings.muted,
            ended: false,
            position: 0.0,
            owed: 0.0,
        })));
    }
}

/// The clip cut to its window and looped where asked, by the same steps Bevy's plugin takes, so a
/// looping sound keeps its samples and refuses a seek here too.
fn shaped(
    decoder: impl Source + Send + 'static,
    settings: &PlaybackSettings,
) -> Box<dyn Source + Send> {
    let looped = matches!(settings.mode, PlaybackMode::Loop);

    match (settings.start_position, settings.duration) {
        (Some(start), Some(length)) => {
            ending(decoder.skip_duration(start).take_duration(length), looped)
        }
        (Some(start), None) => ending(decoder.skip_duration(start), looped),
        (None, Some(length)) => ending(decoder.take_duration(length), looped),
        (None, None) => ending(decoder, looped),
    }
}

fn ending(source: impl Source + Send + 'static, looped: bool) -> Box<dyn Source + Send> {
    if looped {
        Box::new(source.repeat_infinite())
    } else {
        Box::new(source)
    }
}

/// Despawns a sound that has ended, or strips it of what played it, as its mode asks and as Bevy's
/// plugin does with its own.
fn finish<T: Asset + Decodable>(
    mut commands: Commands,
    sounds: Query<(Entity, &SilentSink, &PlaybackSettings), With<AudioPlayer<T>>>,
) {
    for (entity, sink, settings) in &sounds {
        if !sink.empty() {
            continue;
        }

        match settings.mode {
            PlaybackMode::Despawn => commands.entity(entity).try_despawn(),
            PlaybackMode::Remove => {
                commands
                    .entity(entity)
                    .try_remove::<(AudioPlayer<T>, SilentSink, PlaybackSettings)>();
            }
            PlaybackMode::Once | PlaybackMode::Loop => {}
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::audio::{ChannelCount, SampleRate};

    /// A second of silence at a rate small enough to count by hand.
    struct Second {
        left: u32,
    }

    impl Iterator for Second {
        type Item = f32;

        fn next(&mut self) -> Option<f32> {
            self.left = self.left.checked_sub(1)?;
            Some(0.0)
        }
    }

    impl Source for Second {
        fn current_span_len(&self) -> Option<usize> {
            None
        }

        fn channels(&self) -> ChannelCount {
            ChannelCount::new(1).unwrap()
        }

        fn sample_rate(&self) -> SampleRate {
            SampleRate::new(100).unwrap()
        }

        fn total_duration(&self) -> Option<Duration> {
            Some(Duration::from_secs(1))
        }
    }

    fn playing(speed: f32) -> Playing {
        Playing {
            source: Box::new(Second { left: 100 }),
            volume: Volume::Linear(1.0),
            speed,
            paused: false,
            muted: false,
            ended: false,
            position: 0.0,
            owed: 0.0,
        }
    }

    #[test]
    fn a_sound_ends_when_its_clip_has_been_heard() {
        let mut sound = playing(1.0);

        sound.advance(0.9);
        assert!(!sound.ended);
        assert!((sound.position - 0.9).abs() < 0.011);

        sound.advance(0.2);
        assert!(sound.ended);
    }

    #[test]
    fn a_faster_sound_ends_sooner_and_a_paused_one_waits() {
        let mut fast = playing(2.0);
        fast.advance(0.55);
        assert!(fast.ended);

        let mut held = playing(1.0);
        held.paused = true;
        held.advance(5.0);
        assert!(!held.ended);
        assert_eq!(held.position, 0.0);
    }
}
