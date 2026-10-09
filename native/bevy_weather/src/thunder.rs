//! Lightning and thunder.
//!
//! A strike is scheduled from [`WeatherConditions::thunder`], which is a rate
//! in strikes per minute. Each strike produces:
//!
//! * a multi-pulse flash envelope, because real lightning flickers rather than
//!   flashing once;
//! * a bright point light at the strike position, so the scene is lit from the
//!   right direction;
//! * glow inside the cloud layer, via [`LightningState`] which the sky shader
//!   reads;
//! * a [`LightningStrike`] message, delayed audio included, so your own systems
//!   can react.
//!
//! [`WeatherConditions::thunder`]: crate::state::WeatherConditions::thunder

use bevy::app::{App, Plugin, Update};
use bevy::color::{Color, LinearRgba};
use bevy::ecs::component::Component;
use bevy::ecs::message::{Message, MessageWriter};
use bevy::ecs::query::With;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query, Res, ResMut};
use bevy::light::PointLight;
use bevy::math::Vec3;
use bevy::prelude::Entity;
use bevy::reflect::Reflect;
use bevy::time::Time;
use bevy::transform::components::{GlobalTransform, Transform};
#[cfg(feature = "audio")]
use bevy::{
    asset::Handle,
    audio::{AudioPlayer, AudioSource, PlaybackMode, PlaybackSettings, Volume},
    ecs::message::MessageReader,
};

use crate::WeatherSystems;
use crate::config::{WeatherCamera, WeatherConfig};
use crate::math::hash_f32;
use crate::state::Weather;
use bevy::ecs::reflect::ReflectComponent;
use bevy::ecs::reflect::ReflectResource;
use bevy::reflect::std_traits::ReflectDefault;

/// Speed of sound in dry air at 20 C, in metres per second.
pub const SPEED_OF_SOUND: f32 = 343.0;

/// Tuning for lightning and thunder.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct ThunderConfig {
    /// Colour of the flash.
    pub flash_color: Color,
    /// Peak brightness of the sky flash.
    pub flash_intensity: f32,
    /// How long a strike's flicker lasts, in seconds.
    pub flash_duration: f32,
    /// Nearest a strike will land to the camera, in world units.
    pub min_distance: f32,
    /// Furthest a strike will land from the camera, in world units.
    pub max_distance: f32,
    /// Height above the camera that the strike's light is placed, in world units.
    pub strike_height: f32,
    /// Intensity of the point light spawned at the strike, in lumens.
    pub light_intensity: f32,
    /// Range of that point light, in world units.
    pub light_range: f32,
    /// Spawn a point light at all. Turn off if you only want the sky flash.
    pub spawn_light: bool,
    /// Seed for strike timing and placement.
    pub seed: u32,
    /// Multiplies the strike rate from the weather.
    pub rate_multiplier: f32,

    /// Thunder clap to play, delayed by the time sound takes to arrive.
    ///
    /// You supply the asset; the plugin owns the timing and the falloff. Leave
    /// it `None` to handle audio yourself from [`LightningStrike`].
    #[cfg(feature = "audio")]
    pub sound: Option<Handle<AudioSource>>,

    /// Volume of a thunder clap heard from
    /// [`min_distance`](Self::min_distance).
    #[cfg(feature = "audio")]
    pub volume: f32,

    /// Distance, in world units, over which a clap fades to nothing.
    ///
    /// Real thunder is inaudible past about 15 km, and quieter long before
    /// that.
    #[cfg(feature = "audio")]
    pub audible_distance: f32,
}

impl Default for ThunderConfig {
    fn default() -> Self {
        Self {
            flash_color: Color::srgb(0.82, 0.88, 1.0),
            flash_intensity: 1.0,
            flash_duration: 0.55,
            min_distance: 150.0,
            max_distance: 4_000.0,
            strike_height: 900.0,
            light_intensity: 4.0e9,
            light_range: 3_000.0,
            spawn_light: true,
            seed: 0xB017_5EED,
            rate_multiplier: 1.0,
            #[cfg(feature = "audio")]
            sound: None,
            #[cfg(feature = "audio")]
            volume: 1.0,
            #[cfg(feature = "audio")]
            audible_distance: 15_000.0,
        }
    }
}

/// A lightning strike happened.
///
/// Read this to trigger your own audio, screen shake, damage, or anything else.
/// The delay before the thunder is already computed for you.
#[derive(Message, Debug, Clone, Copy)]
pub struct LightningStrike {
    /// Where the bolt landed, in world space.
    pub position: Vec3,
    /// Distance from the camera, in world units.
    pub distance: f32,
    /// Seconds until the thunder should be heard, from the speed of sound.
    pub thunder_delay: f32,
    /// Relative brightness of this strike, `0.0..=1.0`.
    pub intensity: f32,
}

/// The current flash, for the sky shader and anything else that wants it.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct LightningState {
    /// Flash colour, in linear space.
    pub color: LinearRgba,
    /// Current flash brightness, `0.0` when nothing is happening.
    pub intensity: f32,
    /// Seconds until the next scheduled strike.
    pub next_strike_in: f32,
    /// Seconds since the current strike began.
    pub elapsed: f32,
    /// Total length of the current strike's envelope.
    pub duration: f32,
    /// How many strikes have happened. Also seeds each strike's randomness.
    pub strike_count: u32,
    /// Whether a strike is currently flashing.
    pub active: bool,
}

impl Default for LightningState {
    fn default() -> Self {
        Self {
            color: LinearRgba::WHITE,
            intensity: 0.0,
            next_strike_in: f32::INFINITY,
            elapsed: 0.0,
            duration: 0.0,
            strike_count: 0,
            active: false,
        }
    }
}

/// The brightness envelope of one strike, `0.0..=1.0`.
///
/// Real lightning is several return strokes down the same channel over a
/// couple of hundred milliseconds, so this is a decaying envelope with a
/// flicker on top rather than a single smooth pulse.
pub fn flash_envelope(elapsed: f32, duration: f32, seed: u32) -> f32 {
    if duration <= 0.0 || elapsed < 0.0 || elapsed > duration {
        return 0.0;
    }
    let t = elapsed / duration;

    // Fast attack, exponential decay.
    let attack = (t / 0.04).min(1.0);
    let decay = (-4.5 * t).exp();

    // Two or three return strokes, at seeded offsets.
    let mut flicker = 1.0f32;
    for stroke in 0..3u32 {
        let offset = 0.12 + hash_f32(seed.wrapping_add(stroke * 977)) * 0.55;
        let width = 0.05;
        let d = (t - offset) / width;
        flicker += 0.9 * (-d * d).exp();
    }

    (attack * decay * flicker).clamp(0.0, 1.0)
}

/// A thunder clap waiting for its sound to reach the listener.
#[cfg(feature = "audio")]
#[derive(Debug, Clone, Copy)]
struct PendingThunder {
    /// Seconds until the clap should be heard.
    remaining: f32,
    /// Volume to play it at, already attenuated for distance.
    volume: f32,
}

/// Claps in flight.
///
/// Light arrives instantly and sound does not, which is the whole reason you
/// can count the gap to tell how far away a storm is. Reproducing that means
/// holding each clap until its sound catches up.
#[cfg(feature = "audio")]
#[derive(Resource, Debug, Default)]
pub struct ThunderQueue {
    pending: Vec<PendingThunder>,
}

#[cfg(feature = "audio")]
impl ThunderQueue {
    /// How many claps are still on their way.
    pub fn len(&self) -> usize {
        self.pending.len()
    }

    /// Whether any clap is still on its way.
    pub fn is_empty(&self) -> bool {
        self.pending.is_empty()
    }
}

/// Marks a point light spawned for a strike, so it can be cleaned up.
#[derive(Component, Debug, Clone, Copy, Reflect)]
#[reflect(Component)]
pub struct LightningLight {
    /// Seconds remaining before this light is despawned.
    pub remaining: f32,
    /// Peak intensity, scaled by the flash envelope each frame.
    pub peak_intensity: f32,
    /// Total lifetime, for evaluating the envelope.
    pub duration: f32,
    /// Seed for this strike's flicker.
    pub seed: u32,
}

/// Schedules strikes, drives the flash, and emits [`LightningStrike`].
#[derive(Debug, Clone, Copy, Default)]
pub struct ThunderPlugin;

impl Plugin for ThunderPlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<ThunderConfig>()
            .init_resource::<LightningState>()
            .register_type::<ThunderConfig>()
            .register_type::<LightningState>()
            .register_type::<LightningLight>()
            .add_message::<LightningStrike>()
            .add_systems(
                Update,
                (schedule_strikes, animate_lightning_lights)
                    .chain()
                    .in_set(WeatherSystems::Apply),
            );

        #[cfg(feature = "audio")]
        app.init_resource::<ThunderQueue>().add_systems(
            Update,
            play_thunder
                .in_set(WeatherSystems::Apply)
                .after(schedule_strikes),
        );
    }
}

/// Time until the next strike, sampled from an exponential distribution.
///
/// Strikes are a Poisson process: independent events at a constant average
/// rate. Sampling gaps exponentially reproduces that, which is what makes the
/// storm feel unpredictable rather than metronomic.
fn next_gap(strikes_per_minute: f32, uniform: f32) -> f32 {
    if strikes_per_minute <= 0.0 {
        return f32::INFINITY;
    }
    let rate_per_second = strikes_per_minute / 60.0;
    // Clamp away from zero so the log stays finite.
    let u = uniform.clamp(1e-6, 1.0 - 1e-6);
    -u.ln() / rate_per_second
}

#[expect(
    clippy::too_many_arguments,
    reason = "a Bevy system reading every resource a strike depends on"
)]
fn schedule_strikes(
    mut commands: Commands,
    mut state: ResMut<LightningState>,
    mut strikes: MessageWriter<LightningStrike>,
    config: Res<WeatherConfig>,
    thunder: Res<ThunderConfig>,
    weather: Res<Weather>,
    time: Res<Time>,
    cameras: Query<&GlobalTransform, With<WeatherCamera>>,
) {
    state.color = thunder.flash_color.to_linear();

    if !config.thunder {
        state.intensity = 0.0;
        state.active = false;
        state.next_strike_in = f32::INFINITY;
        return;
    }

    let dt = time.delta_secs();
    let rate = weather.current.thunder * thunder.rate_multiplier.max(0.0);

    // Advance the current flash.
    if state.active {
        state.elapsed += dt;
        let seed = thunder.seed.wrapping_add(state.strike_count);
        state.intensity =
            flash_envelope(state.elapsed, state.duration, seed) * thunder.flash_intensity;
        if state.elapsed >= state.duration {
            state.active = false;
            state.intensity = 0.0;
        }
    } else {
        state.intensity = 0.0;
    }

    if rate <= 0.0 {
        state.next_strike_in = f32::INFINITY;
        return;
    }

    // Seed a countdown the first time the storm starts.
    if !state.next_strike_in.is_finite() {
        let u = hash_f32(thunder.seed ^ state.strike_count.wrapping_mul(31));
        state.next_strike_in = next_gap(rate, u);
    }

    state.next_strike_in -= dt;
    if state.next_strike_in > 0.0 {
        return;
    }

    // ---- Fire a strike -----------------------------------------------------
    let index = state.strike_count;
    state.strike_count = state.strike_count.wrapping_add(1);
    let seed = thunder.seed.wrapping_add(index);

    let camera = cameras
        .iter()
        .next()
        .map(|transform| transform.translation())
        .unwrap_or(Vec3::ZERO);

    let angle = hash_f32(seed ^ 0xAAAA) * core::f32::consts::TAU;
    // Square-rooting a uniform variate gives a uniform *area* distribution, so
    // strikes are not bunched near the camera.
    let radial = hash_f32(seed ^ 0xBBBB).sqrt();
    let distance =
        thunder.min_distance + radial * (thunder.max_distance - thunder.min_distance).max(0.0);
    let position = camera
        + Vec3::new(
            angle.cos() * distance,
            thunder.strike_height,
            angle.sin() * distance,
        );

    // Nearby strikes are brighter, both in the sky and as scene lighting.
    let proximity = 1.0 - (distance / thunder.max_distance.max(1.0)).clamp(0.0, 1.0);
    let intensity = 0.35 + 0.65 * proximity * (0.6 + 0.4 * hash_f32(seed ^ 0xCCCC));

    state.active = true;
    state.elapsed = 0.0;
    state.duration = thunder.flash_duration.max(0.05);
    state.intensity = 0.0;

    let ground_distance = position.distance(camera);
    strikes.write(LightningStrike {
        position,
        distance: ground_distance,
        thunder_delay: ground_distance / SPEED_OF_SOUND,
        intensity,
    });

    if thunder.spawn_light {
        commands.spawn((
            LightningLight {
                remaining: state.duration,
                peak_intensity: thunder.light_intensity * intensity,
                duration: state.duration,
                seed,
            },
            PointLight {
                color: thunder.flash_color,
                intensity: 0.0,
                range: thunder.light_range,
                shadow_maps_enabled: false,
                ..Default::default()
            },
            Transform::from_translation(position),
        ));
    }

    // Schedule the next one.
    let u = hash_f32(seed ^ 0xDDDD);
    state.next_strike_in = next_gap(rate, u);
}

fn animate_lightning_lights(
    mut commands: Commands,
    time: Res<Time>,
    mut lights: Query<(Entity, &mut LightningLight, &mut PointLight)>,
) {
    let dt = time.delta_secs();
    for (entity, mut flash, mut light) in &mut lights {
        flash.remaining -= dt;
        if flash.remaining <= 0.0 {
            commands.entity(entity).despawn();
            continue;
        }
        let elapsed = flash.duration - flash.remaining;
        light.intensity =
            flash.peak_intensity * flash_envelope(elapsed, flash.duration, flash.seed);
    }
}

/// Attenuation of a thunder clap heard from `distance`.
///
/// Sound spreads over a growing sphere and the air absorbs the high frequencies
/// as it goes, so a distant strike is not just quieter but duller. The `1/r`
/// spreading term is the dominant effect and is what this models; the extra
/// exponential accounts, roughly, for atmospheric absorption.
pub fn thunder_attenuation(distance: f32, reference: f32, audible: f32) -> f32 {
    if distance <= 0.0 || audible <= 0.0 {
        return 1.0;
    }
    let spreading = (reference.max(1.0) / distance.max(reference.max(1.0))).min(1.0);
    let absorption = (-distance / audible).exp();
    (spreading * absorption).clamp(0.0, 1.0)
}

#[cfg(feature = "audio")]
fn play_thunder(
    mut commands: Commands,
    mut queue: ResMut<ThunderQueue>,
    mut strikes: MessageReader<LightningStrike>,
    thunder: Res<ThunderConfig>,
    time: Res<Time>,
) {
    let Some(sound) = thunder.sound.clone() else {
        // Still drain the reader, so turning the sound on later does not
        // suddenly fire a backlog of claps.
        strikes.clear();
        queue.pending.clear();
        return;
    };

    for strike in strikes.read() {
        let volume = thunder.volume
            * thunder_attenuation(
                strike.distance,
                thunder.min_distance,
                thunder.audible_distance,
            );
        if volume <= 1e-3 {
            continue;
        }
        queue.pending.push(PendingThunder {
            remaining: strike.thunder_delay,
            volume,
        });
    }

    let dt = time.delta_secs();
    queue.pending.retain_mut(|clap| {
        clap.remaining -= dt;
        if clap.remaining > 0.0 {
            return true;
        }
        commands.spawn((
            AudioPlayer::new(sound.clone()),
            PlaybackSettings {
                mode: PlaybackMode::Despawn,
                volume: Volume::Linear(clap.volume),
                ..Default::default()
            },
        ));
        false
    });
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn envelope_starts_and_ends_dark() {
        assert_eq!(flash_envelope(-0.1, 0.5, 7), 0.0);
        assert_eq!(flash_envelope(0.6, 0.5, 7), 0.0);
        assert!(
            flash_envelope(0.0, 0.5, 7) < 0.05,
            "it should attack, not pop"
        );
    }

    #[test]
    fn envelope_stays_in_unit_range() {
        for seed in 0..64u32 {
            for i in 0..=200 {
                let t = i as f32 / 200.0 * 0.6;
                let v = flash_envelope(t, 0.55, seed);
                assert!((0.0..=1.0).contains(&v), "envelope {v} out of range");
            }
        }
    }

    #[test]
    fn envelope_actually_flashes() {
        let peak = (0..=200)
            .map(|i| flash_envelope(i as f32 / 200.0 * 0.55, 0.55, 3))
            .fold(0.0f32, f32::max);
        assert!(peak > 0.5, "peak was only {peak}");
    }

    #[test]
    fn envelope_handles_a_zero_duration() {
        assert_eq!(flash_envelope(0.0, 0.0, 1), 0.0);
    }

    #[test]
    fn no_strikes_when_the_rate_is_zero() {
        assert_eq!(next_gap(0.0, 0.5), f32::INFINITY);
        assert_eq!(next_gap(-1.0, 0.5), f32::INFINITY);
    }

    #[test]
    fn gaps_are_finite_and_positive_for_any_uniform() {
        for i in 0..=100 {
            let u = i as f32 / 100.0;
            let gap = next_gap(6.0, u);
            assert!(gap.is_finite() && gap > 0.0, "u = {u} gave {gap}");
        }
    }

    #[test]
    fn average_gap_matches_the_requested_rate() {
        // A Poisson process with rate r has mean gap 1/r.
        let strikes_per_minute = 12.0;
        let samples = 20_000;
        let total: f64 = (0..samples)
            .map(|i| next_gap(strikes_per_minute, hash_f32(i)) as f64)
            .sum();
        let mean = total / samples as f64;
        let expected = 60.0 / strikes_per_minute as f64;
        assert!(
            (mean - expected).abs() / expected < 0.06,
            "mean gap {mean}, expected about {expected}"
        );
    }

    #[test]
    fn attenuation_falls_off_with_distance() {
        let mut previous = f32::INFINITY;
        for kilometres in 0..15 {
            let v = thunder_attenuation(kilometres as f32 * 1_000.0 + 150.0, 150.0, 15_000.0);
            assert!(v <= previous, "attenuation rose at {kilometres} km");
            assert!((0.0..=1.0).contains(&v));
            previous = v;
        }
        assert!(previous < 0.05, "distant thunder should be near silent");
    }

    #[test]
    fn a_strike_at_the_reference_distance_is_at_full_volume() {
        // Only the atmospheric absorption term applies this close.
        let v = thunder_attenuation(150.0, 150.0, 15_000.0);
        assert!(v > 0.98, "{v}");
    }

    #[test]
    fn attenuation_handles_degenerate_inputs() {
        assert_eq!(thunder_attenuation(0.0, 150.0, 15_000.0), 1.0);
        assert_eq!(thunder_attenuation(-5.0, 150.0, 15_000.0), 1.0);
        assert_eq!(thunder_attenuation(100.0, 150.0, 0.0), 1.0);
    }

    #[test]
    fn thunder_delay_follows_the_speed_of_sound() {
        // The old rule: about three seconds per kilometre.
        let delay = 1_000.0 / SPEED_OF_SOUND;
        assert!((delay - 2.92).abs() < 0.05, "{delay}");
    }
}
