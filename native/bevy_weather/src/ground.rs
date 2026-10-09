//! What the weather has left on the ground.
//!
//! The rest of this plugin renders the *sky*: what is falling, not what has
//! landed. But almost everything a game wants to do with rain happens on the
//! ground -- puddles, darkened stone, a wet sheen on the road, footprints in
//! snow, a different footstep sound, tyres that let go in the corners.
//!
//! That state cannot be read off the current weather, because it is a running
//! total rather than an instant: ground is wet because it rained ten minutes
//! ago, and stays wet for a while after the rain stops. So it is integrated
//! here, once, rather than left for every game to reimplement.
//!
//! Nothing in this plugin renders from [`GroundConditions`]. It is here for
//! you to drive your own materials, audio and physics from:
//!
//! ```no_run
//! # use bevy::prelude::*;
//! # use bevy_weather::prelude::*;
//! fn puddles(ground: Res<GroundConditions>, mut materials: ResMut<Assets<StandardMaterial>>) {
//!     for (_, material) in materials.iter_mut() {
//!         // Wet ground is darker and much shinier than dry ground.
//!         material.perceptual_roughness = 0.9 - 0.55 * ground.wetness;
//!     }
//! }
//! ```

use bevy::app::{App, Plugin, Update};
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Res, ResMut};
use bevy::reflect::Reflect;
use bevy::time::Time;

use crate::WeatherSystems;
use crate::celestial::CelestialBodies;
use crate::state::{Weather, WeatherConditions};
use crate::wind::Wind;
use bevy::ecs::reflect::ReflectResource;
use bevy::reflect::std_traits::ReflectDefault;

/// How wet the ground is and how much snow is lying on it.
///
/// Both are `0.0..=1.0` and both lag the weather, because that is the point:
/// they are what the weather has *done*, not what it is doing.
#[derive(Resource, Debug, Clone, Copy, Default, PartialEq, Reflect)]
#[reflect(Resource, Default)]
pub struct GroundConditions {
    /// How wet exposed surfaces are, `0.0` bone dry to `1.0` standing water.
    ///
    /// Rises while it rains and while lying snow melts, falls as it dries.
    pub wetness: f32,

    /// How much snow is lying, `0.0` bare to `1.0` covered.
    ///
    /// Only accumulates near or below freezing, and melts above it -- so a
    /// snowfall into a warm afternoon leaves the ground wet rather than white.
    pub snow_cover: f32,
}

impl GroundConditions {
    /// True once there is enough water to pool rather than just darken.
    pub fn is_puddling(&self) -> bool {
        self.wetness > 0.6
    }

    /// Ground colour multiplier: wet surfaces are darker than dry ones.
    ///
    /// Water fills the pores and the light that would have scattered straight
    /// back out is trapped and bounced around instead. Snow goes the other way
    /// and is the brightest natural surface there is, so it wins where it lies.
    pub fn albedo_scale(&self) -> f32 {
        let wet = 1.0 - 0.45 * self.wetness.clamp(0.0, 1.0);
        wet * (1.0 - self.snow_cover.clamp(0.0, 1.0)) + 1.6 * self.snow_cover.clamp(0.0, 1.0)
    }
}

/// How fast the ground responds to the weather.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct GroundConfig {
    /// Track the ground at all.
    pub enabled: bool,

    /// Seconds of heavy rain to soak the ground completely.
    pub wetting_seconds: f32,

    /// Seconds to dry out completely, in warm, breezy, sunny conditions.
    ///
    /// Much longer than [`wetting_seconds`](Self::wetting_seconds), and longer
    /// still when it is cold, calm, humid or dark -- all of which this scales
    /// by. Puddles outlast the shower that made them by a long way.
    pub drying_seconds: f32,

    /// Seconds of heavy snowfall to cover the ground completely.
    pub snowfall_seconds: f32,

    /// Seconds to melt a full cover at ten degrees above freezing.
    pub melt_seconds: f32,

    /// Temperature, in Celsius, above which falling snow stops settling.
    ///
    /// Slightly above zero rather than at it: snow falling through air a
    /// degree or two above freezing still settles on cold ground, which is why
    /// it can lie at temperatures a thermometer says it should not.
    pub settling_temperature: f32,
}

impl Default for GroundConfig {
    fn default() -> Self {
        Self {
            enabled: true,
            wetting_seconds: 45.0,
            drying_seconds: 400.0,
            snowfall_seconds: 300.0,
            melt_seconds: 600.0,
            settling_temperature: 1.5,
        }
    }
}

/// Keeps [`GroundConditions`] in step with the weather.
#[derive(Debug, Clone, Copy, Default)]
pub struct GroundPlugin;

impl Plugin for GroundPlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<GroundConfig>()
            .init_resource::<GroundConditions>()
            .register_type::<GroundConfig>()
            .register_type::<GroundConditions>()
            // Simulation rather than application: this is state the weather
            // produces, not something being pushed at the renderer.
            //
            // Ordered after the blend so it integrates this frame's weather
            // rather than whichever of the two Bevy happened to run first. The
            // `daylight` it reads is a frame old, since the celestial bodies
            // are recomputed later in `Apply` -- sixteen milliseconds of lag on
            // a number that takes minutes to move.
            .add_systems(
                Update,
                accumulate_ground
                    .in_set(WeatherSystems::Simulate)
                    .after(crate::state::blend_weather),
            );
    }
}

fn accumulate_ground(
    mut ground: ResMut<GroundConditions>,
    config: Res<GroundConfig>,
    weather: Res<Weather>,
    bodies: Res<CelestialBodies>,
    wind: Res<Wind>,
    time: Res<Time>,
) {
    if !config.enabled {
        return;
    }
    let next = step(
        *ground,
        &config,
        &weather.current,
        bodies.daylight,
        wind.speed(),
        time.delta_secs(),
    );
    // Only touch the resource when it actually moved, so systems keyed off
    // change detection are not woken every frame by weather that is not doing
    // anything.
    if next != *ground {
        *ground = next;
    }
}

/// Advances the ground state by `delta` seconds.
///
/// Pure, so it can be tested and so a game can run its own copy forward -- to
/// catch up after a fast-travel, say, without stepping the whole schedule.
pub fn step(
    ground: GroundConditions,
    config: &GroundConfig,
    conditions: &WeatherConditions,
    daylight: f32,
    wind_speed: f32,
    delta: f32,
) -> GroundConditions {
    if delta <= 0.0 || !delta.is_finite() {
        return ground;
    }
    let mut wetness = ground.wetness.clamp(0.0, 1.0);
    let mut snow_cover = ground.snow_cover.clamp(0.0, 1.0);

    // ---- Snow ------------------------------------------------------------
    let above_freezing = conditions.temperature - config.settling_temperature;
    if conditions.snow > 0.0 && above_freezing <= 0.0 {
        snow_cover += conditions.snow * delta / config.snowfall_seconds.max(1e-3);
    }

    // Melting is driven by how far above freezing it is, and quickened by the
    // sun. Lying snow is what makes a thaw take hours rather than minutes: it
    // is white, so it reflects most of what would otherwise melt it.
    let melted = if conditions.temperature > 0.0 && snow_cover > 0.0 {
        let warmth = (conditions.temperature / 10.0).min(3.0);
        let sun = 0.6 + 0.8 * daylight.clamp(0.0, 1.0);
        let rate = warmth * sun * delta / config.melt_seconds.max(1e-3);
        let melted = rate.min(snow_cover);
        snow_cover -= melted;
        melted
    } else {
        0.0
    };

    // ---- Water -----------------------------------------------------------
    // Rain wets the ground, and so does the snow that just turned into water.
    let rain = conditions.rain.clamp(0.0, 1.0);
    let wetting = (rain * delta / config.wetting_seconds.max(1e-3)) + melted * 2.0;
    wetness += wetting;

    // Drying only happens once nothing is falling on it and nothing is melting
    // onto it, and how fast depends on everything that carries water away.
    if wetting <= 0.0 {
        // Warm, windy, sunny and dry air all speed it up; cold, still, dark and
        // humid air all slow it down. Snow lying on top stops it entirely --
        // the ground under snow does not dry, it stays saturated.
        let warmth = ((conditions.temperature + 5.0) / 25.0).clamp(0.1, 2.0);
        let breeze = 0.5 + (wind_speed.max(0.0) / 12.0).min(1.5);
        let sun = 0.35 + 0.65 * daylight.clamp(0.0, 1.0);
        let dryness = 1.0 - 0.6 * conditions.humidity.clamp(0.0, 1.0);
        let covered = 1.0 - snow_cover;
        let rate = warmth * breeze * sun * dryness * covered;
        wetness -= rate * delta / config.drying_seconds.max(1e-3);
    }

    GroundConditions {
        wetness: wetness.clamp(0.0, 1.0),
        snow_cover: snow_cover.clamp(0.0, 1.0),
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::presets::WeatherPreset;

    /// Runs `seconds` of weather at sixty frames a second.
    fn run(
        start: GroundConditions,
        conditions: WeatherConditions,
        daylight: f32,
        wind: f32,
        seconds: f32,
    ) -> GroundConditions {
        let config = GroundConfig::default();
        let step_size = 1.0 / 60.0;
        let mut ground = start;
        for _ in 0..(seconds / step_size) as u32 {
            ground = step(ground, &config, &conditions, daylight, wind, step_size);
        }
        ground
    }

    #[test]
    fn rain_wets_the_ground_and_stops_at_saturated() {
        let ground = run(
            GroundConditions::default(),
            WeatherPreset::Storm.conditions(),
            1.0,
            5.0,
            600.0,
        );
        assert_eq!(ground.wetness, 1.0);
        assert_eq!(ground.snow_cover, 0.0);
    }

    #[test]
    fn the_ground_stays_wet_long_after_the_rain_stops() {
        // Puddles outlasting the shower is most of what makes wet ground read
        // as weather rather than as a texture swap.
        let soaked = GroundConditions {
            wetness: 1.0,
            snow_cover: 0.0,
        };
        let after_a_minute = run(soaked, WeatherPreset::Clear.conditions(), 1.0, 4.0, 60.0);
        assert!(
            after_a_minute.wetness > 0.7,
            "dried to {} in a minute",
            after_a_minute.wetness
        );
        let much_later = run(soaked, WeatherPreset::Clear.conditions(), 1.0, 4.0, 3_000.0);
        assert_eq!(much_later.wetness, 0.0);
    }

    #[test]
    fn drying_is_framerate_independent() {
        // A per-frame rate rather than a per-second one dries the ground four
        // times faster on a machine running four times as fast, which is the
        // sort of thing nobody notices until they change monitor.
        let config = GroundConfig::default();
        let conditions = WeatherPreset::Clear.conditions();
        let simulate = |step_size: f32| {
            let mut ground = GroundConditions {
                wetness: 1.0,
                snow_cover: 0.0,
            };
            for _ in 0..(120.0 / step_size) as u32 {
                ground = step(ground, &config, &conditions, 1.0, 4.0, step_size);
            }
            ground.wetness
        };
        let slow = simulate(1.0 / 30.0);
        let fast = simulate(1.0 / 240.0);
        assert!(
            (slow - fast).abs() < 1e-3,
            "{slow} at 30 fps, {fast} at 240"
        );
    }

    #[test]
    fn snow_lies_when_it_is_cold_and_does_not_when_it_is_not() {
        let mut freezing = WeatherPreset::Snow.conditions();
        freezing.temperature = -4.0;
        let cold = run(GroundConditions::default(), freezing, 0.3, 3.0, 600.0);
        assert!(cold.snow_cover > 0.5, "no snow lay: {}", cold.snow_cover);

        let mut mild = freezing;
        mild.temperature = 8.0;
        let warm = run(GroundConditions::default(), mild, 0.8, 3.0, 600.0);
        assert_eq!(warm.snow_cover, 0.0, "snow lay at eight degrees");
    }

    #[test]
    fn a_thaw_leaves_the_ground_wet() {
        // Snow does not simply vanish. It turns into the water that is the
        // reason everything is soaked for hours after it goes.
        let snowed_on = GroundConditions {
            wetness: 0.0,
            snow_cover: 1.0,
        };
        let mut thaw = WeatherPreset::Clear.conditions();
        thaw.temperature = 9.0;
        let after = run(snowed_on, thaw, 1.0, 2.0, 900.0);
        assert!(after.snow_cover < 1.0, "nothing melted");
        assert!(
            after.wetness > 0.3,
            "a thaw left the ground dry: {}",
            after.wetness
        );
    }

    #[test]
    fn snow_cover_holds_the_water_under_it_in() {
        // The ground beneath lying snow is saturated and stays that way; it is
        // not exposed to the sun or the wind that would dry it.
        let config = GroundConfig::default();
        let mut cold = WeatherPreset::Clear.conditions();
        cold.temperature = -6.0;
        let covered = step(
            GroundConditions {
                wetness: 1.0,
                snow_cover: 1.0,
            },
            &config,
            &cold,
            0.5,
            6.0,
            60.0,
        );
        assert_eq!(covered.wetness, 1.0);
    }

    #[test]
    fn nothing_leaves_the_unit_range_or_goes_non_finite() {
        let config = GroundConfig::default();
        let mut ground = GroundConditions::default();
        for preset in WeatherPreset::ALL {
            let conditions = preset.conditions();
            for frame in 0..600 {
                // Absurd deltas included, since a game that hitches or loads a
                // level hands the schedule exactly that.
                let delta = match frame % 4 {
                    0 => 1.0 / 240.0,
                    1 => 1.0 / 60.0,
                    2 => 0.0,
                    _ => 5.0,
                };
                ground = step(ground, &config, &conditions, 0.5, 8.0, delta);
                assert!(ground.wetness.is_finite() && ground.snow_cover.is_finite());
                assert!((0.0..=1.0).contains(&ground.wetness));
                assert!((0.0..=1.0).contains(&ground.snow_cover));
            }
        }
    }

    #[test]
    fn a_zero_or_negative_step_changes_nothing() {
        let config = GroundConfig::default();
        let start = GroundConditions {
            wetness: 0.4,
            snow_cover: 0.2,
        };
        let conditions = WeatherPreset::Rain.conditions();
        assert_eq!(step(start, &config, &conditions, 1.0, 3.0, 0.0), start);
        assert_eq!(step(start, &config, &conditions, 1.0, 3.0, -1.0), start);
        assert_eq!(step(start, &config, &conditions, 1.0, 3.0, f32::NAN), start);
    }

    #[test]
    fn wet_ground_is_darker_and_snow_is_brighter() {
        let dry = GroundConditions::default();
        let wet = GroundConditions {
            wetness: 1.0,
            snow_cover: 0.0,
        };
        let snowy = GroundConditions {
            wetness: 1.0,
            snow_cover: 1.0,
        };
        assert!(wet.albedo_scale() < dry.albedo_scale());
        assert!(snowy.albedo_scale() > dry.albedo_scale());
    }
}
