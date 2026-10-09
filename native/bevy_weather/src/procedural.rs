//! The procedural weather driver.
//!
//! Weather is a pure function of the clock. Nothing is accumulated frame to
//! frame, so the same [`WeatherTime`] and seed always give the same sky — on
//! every machine, after a save/load, and on every client in a multiplayer game.
//! That also means you can ask what the weather *will* be, which is what
//! [`ProceduralWeather::forecast`] does.

use bevy::app::{App, Plugin, Update};
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Res, ResMut};
use bevy::reflect::Reflect;

use crate::WeatherSystems;
use crate::math::{diurnal_window, fbm, remap01, smoothstep};
use crate::state::{Weather, WeatherConditions};
use crate::time::WeatherTime;
use bevy::ecs::reflect::ReflectResource;
use bevy::reflect::std_traits::ReflectDefault;

/// The long-run character of a region's weather.
///
/// The driver produces variation; the climate decides what it varies *around*.
#[derive(Debug, Clone, Copy, PartialEq, Reflect)]
pub struct Climate {
    /// Annual mean temperature in degrees Celsius.
    pub mean_temperature: f32,
    /// Half the difference between the warmest and coldest season, in degrees.
    /// Zero (or a zero axial tilt) removes seasons.
    pub seasonal_swing: f32,
    /// Half the difference between afternoon and pre-dawn, in degrees.
    pub diurnal_swing: f32,
    /// Baseline relative humidity, `0.0..=1.0`.
    pub base_humidity: f32,
    /// How readily fronts turn into storms, `0.0..=1.0`.
    pub storminess: f32,
    /// Pushes cloud cover up or down, `-1.0..=1.0`.
    pub cloudiness_bias: f32,
    /// How prone the region is to ground fog, `0.0..=1.0`.
    pub fogginess: f32,
    /// Prevailing wind bearing in radians clockwise from north.
    pub prevailing_wind: f32,
    /// Mean wind speed in metres per second.
    pub mean_wind_speed: f32,
}

impl Default for Climate {
    fn default() -> Self {
        Self::TEMPERATE
    }
}

impl Climate {
    /// Four distinct seasons, changeable weather, frequent rain.
    pub const TEMPERATE: Climate = Climate {
        mean_temperature: 11.0,
        seasonal_swing: 11.0,
        diurnal_swing: 5.0,
        base_humidity: 0.62,
        storminess: 0.35,
        cloudiness_bias: 0.05,
        fogginess: 0.45,
        prevailing_wind: 3.93, // ~225 degrees, south-westerly
        mean_wind_speed: 5.0,
    };

    /// Mild, wet and grey, with a small seasonal range.
    pub const OCEANIC: Climate = Climate {
        mean_temperature: 11.0,
        seasonal_swing: 6.0,
        diurnal_swing: 3.5,
        base_humidity: 0.78,
        storminess: 0.45,
        cloudiness_bias: 0.30,
        fogginess: 0.6,
        prevailing_wind: 4.36,
        mean_wind_speed: 8.0,
    };

    /// Hot, dry and almost always clear. Rain arrives rarely and violently.
    pub const ARID: Climate = Climate {
        mean_temperature: 26.0,
        seasonal_swing: 12.0,
        diurnal_swing: 13.0,
        base_humidity: 0.14,
        storminess: 0.25,
        cloudiness_bias: -0.45,
        fogginess: 0.02,
        prevailing_wind: 1.57,
        mean_wind_speed: 4.0,
    };

    /// Hot and humid year round, with towering afternoon thunderstorms.
    pub const TROPICAL: Climate = Climate {
        mean_temperature: 27.0,
        seasonal_swing: 2.5,
        diurnal_swing: 5.0,
        base_humidity: 0.85,
        storminess: 0.8,
        cloudiness_bias: 0.2,
        fogginess: 0.25,
        prevailing_wind: 1.05,
        mean_wind_speed: 4.0,
    };

    /// Dry hot summers, mild wet winters.
    pub const MEDITERRANEAN: Climate = Climate {
        mean_temperature: 18.0,
        seasonal_swing: 9.0,
        diurnal_swing: 8.0,
        base_humidity: 0.5,
        storminess: 0.3,
        cloudiness_bias: -0.2,
        fogginess: 0.2,
        prevailing_wind: 5.24,
        mean_wind_speed: 4.5,
    };

    /// Cold enough that most precipitation falls as snow.
    pub const POLAR: Climate = Climate {
        mean_temperature: -14.0,
        seasonal_swing: 16.0,
        diurnal_swing: 3.0,
        base_humidity: 0.7,
        storminess: 0.4,
        cloudiness_bias: 0.15,
        fogginess: 0.35,
        prevailing_wind: 0.52,
        mean_wind_speed: 9.0,
    };

    /// Warm, dry, and endlessly clear. A good "nothing happens" baseline.
    pub const DESERT_NIGHTS: Climate = Climate {
        mean_temperature: 22.0,
        seasonal_swing: 8.0,
        diurnal_swing: 18.0,
        base_humidity: 0.08,
        storminess: 0.05,
        cloudiness_bias: -0.7,
        fogginess: 0.0,
        prevailing_wind: 2.6,
        mean_wind_speed: 3.0,
    };
}

/// Generates weather from the clock.
///
/// While [`enabled`](Self::enabled) is true this overwrites
/// [`Weather::target`] every frame, so manual [`Weather::set`] calls will be
/// undone. Turn it off to take manual control.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct ProceduralWeather {
    /// Whether the driver runs at all.
    pub enabled: bool,
    /// Changes the weather without changing the climate.
    pub seed: u32,
    /// Roughly how many distinct weather systems pass per in-game day.
    ///
    /// `1.0` gives one front per day. Lower values give long settled spells;
    /// higher values give restless, showery weather.
    pub systems_per_day: f32,
    /// The region's baseline.
    pub climate: Climate,
    /// Written into [`Weather::transition_half_life`] so generated weather
    /// eases in rather than snapping.
    pub transition_half_life: f32,
    /// Scales how far conditions swing from the climate mean, `0.0..`.
    /// `0.0` pins the weather to a bland average; `2.0` makes it dramatic.
    pub variability: f32,
}

impl Default for ProceduralWeather {
    fn default() -> Self {
        Self {
            enabled: true,
            seed: 0x_A11C_E5EE,
            systems_per_day: 1.0,
            climate: Climate::TEMPERATE,
            transition_half_life: 20.0,
            variability: 1.0,
        }
    }
}

impl ProceduralWeather {
    /// The weather this driver produces at `time`.
    ///
    /// Pure: no interior state, no randomness beyond the seed.
    pub fn evaluate(&self, time: &WeatherTime) -> WeatherConditions {
        let climate = self.climate;
        let variability = self.variability.max(0.0);

        // ---- The synoptic driver -------------------------------------------
        // One noise field standing in for the passage of pressure systems.
        // `f32` here is fine because `phase` is small even after in-game years.
        let phase = (time.elapsed_days() * self.systems_per_day.max(0.0) as f64) as f32;
        let front = fbm(phase, 4, self.seed);
        let moisture = fbm(phase * 0.6 + 13.7, 3, self.seed ^ 0x1234_5678);
        let thermal = fbm(phase * 0.35 + 41.2, 3, self.seed ^ 0x2468_ACE0);
        let wind_noise = fbm(phase * 0.8 + 7.1, 2, self.seed ^ 0x0F0F_0F0F);
        let dir_noise = fbm(phase * 0.25 + 91.5, 2, self.seed ^ 0x7777_1111);

        // Centred, variability-scaled versions of the same fields.
        let front_c = (front - 0.5) * 2.0 * variability;
        let moisture_c = (moisture - 0.5) * 2.0 * variability;

        // ---- Temperature ---------------------------------------------------
        // The seasonal signal is the sun's own declination, normalised, so
        // seasons always agree with how high the sun climbs. A zero axial tilt
        // therefore produces no seasons, which is the honest answer.
        let season = seasonal_signal(time);
        // Peak warmth lags the afternoon sun a little.
        let diurnal = (core::f32::consts::TAU * (time.time_of_day - 0.62)).cos();

        let temperature = climate.mean_temperature
            + season * climate.seasonal_swing
            + diurnal * climate.diurnal_swing
            + (thermal - 0.5) * 8.0 * variability;

        // ---- Humidity ------------------------------------------------------
        // Cool air holds less water, so *relative* humidity climbs overnight
        // even when no new moisture arrives. This is the mechanism behind dew
        // and morning fog, so it has to be here rather than bolted onto the
        // fog term.
        let humidity = (climate.base_humidity + moisture_c * 0.28 + front_c * 0.14
            - diurnal * 0.12)
            .clamp(0.02, 1.0);

        // ---- Cloud ---------------------------------------------------------
        // Warm, humid afternoons build convective cloud; this is what gives
        // tropical climates their daily thunderstorm rhythm.
        let convective = smoothstep(15.0, 28.0, temperature)
            * smoothstep(0.45, 0.85, humidity)
            * smoothstep(0.30, 0.62, time.time_of_day)
            * (1.0 - smoothstep(0.72, 0.95, time.time_of_day));

        let cloud_drive = front * 0.75 + humidity * 0.55 - 0.42
            + climate.cloudiness_bias * 0.35
            + convective * 0.35;
        // A wide smoothstep rather than a clamp, so clear days are genuinely
        // clear instead of everything sitting at a permanent half-cover.
        let cloud_coverage = smoothstep(0.02, 0.78, cloud_drive);

        let cloud_density = (0.35 + cloud_coverage * 0.45 + humidity * 0.25).clamp(0.0, 1.0);

        // ---- Precipitation --------------------------------------------------
        // Two independent gates, multiplied. Full cloud cover is not the same
        // as rain -- most overcast days stay dry -- so near-total cover is
        // necessary but nowhere near sufficient; the airmass has to be close to
        // saturated as well. Combining the two into a single blended term
        // instead lets high cover alone carry the threshold, and then every
        // grey day is a wet one, which is both wrong and miserable to play in.
        let precipitation =
            smoothstep(0.86, 1.0, cloud_coverage) * smoothstep(0.80, 0.97, humidity);

        // Below -2 C everything falls as snow, above +2 C everything as rain,
        // and in between you get sleet: both at once.
        let snow_fraction = remap01(2.0 - temperature, 0.0, 4.0);
        let rain = precipitation * (1.0 - snow_fraction);
        let snow = precipitation * snow_fraction;

        // ---- Thunder ---------------------------------------------------------
        // Lightning needs deep, warm, wet convection, so it tracks the
        // convective term rather than plain rainfall.
        let thunder = climate.storminess
            * smoothstep(0.55, 1.0, precipitation)
            * smoothstep(8.0, 22.0, temperature)
            * (0.35 + convective)
            * 7.0;

        // ---- Wind -------------------------------------------------------------
        // At zero variability this is exactly the climate's mean wind; at one
        // it ranges from roughly a third of it to nearly double.
        let wind_speed = (climate.mean_wind_speed
            * (1.0 + (wind_noise - 0.5) * 1.4 * variability.clamp(0.0, 2.0))
            + precipitation * 12.0 * climate.storminess)
            .max(0.0);
        let wind_direction =
            climate.prevailing_wind + (dir_noise - 0.5) * core::f32::consts::PI * 0.9 * variability;
        let turbulence =
            (0.08 + smoothstep(2.0, 22.0, wind_speed) * 0.7 + precipitation * 0.3).clamp(0.0, 1.0);

        // ---- Fog ---------------------------------------------------------------
        // Radiation fog: the ground radiates its heat away overnight, the air
        // just above it cools to the dew point, and the sun burns the result
        // off during the morning. So the window runs from mid-evening to
        // mid-morning, wrapping across midnight.
        let night_window = diurnal_window(time.time_of_day, 0.875, 0.417, 0.06);
        // Fog needs still air: even a light breeze mixes the cold surface
        // layer back into the air above and the fog lifts.
        let calm = 1.0 - smoothstep(1.5, 7.0, wind_speed);
        // Fog needs air at or very near saturation.
        let damp = smoothstep(0.70, 0.98, humidity);
        let radiation_fog = climate.fogginess * damp * calm * night_window;
        // Rain and snow drag their own murk along with them.
        let precipitation_haze = precipitation * 0.3 + snow * 0.35;
        let fog = (radiation_fog + precipitation_haze).clamp(0.0, 1.0);

        // ---- Cloud geometry -----------------------------------------------------
        // A wet, cold airmass has a low base; a dry warm one is high. Storms
        // build tall.
        let cloud_altitude =
            600.0 + (1.0 - humidity) * 2_800.0 + smoothstep(-5.0, 30.0, temperature) * 900.0;
        let cloud_thickness = 400.0 + precipitation * 2_200.0 + (thunder / 7.0).min(1.0) * 2_600.0;

        WeatherConditions {
            cloud_coverage,
            cloud_density,
            cloud_altitude,
            cloud_thickness,
            rain,
            snow,
            fog,
            thunder,
            wind_speed,
            wind_direction,
            turbulence,
            humidity,
            temperature,
        }
        .sanitized()
    }

    /// The weather `hours` from now. Negative values look backwards.
    ///
    /// Because the driver is a pure function of the clock, this is exactly what
    /// you will get when the time arrives — good enough to build an in-game
    /// forecast out of.
    pub fn forecast(&self, time: &WeatherTime, hours: f32) -> WeatherConditions {
        let mut future = time.clone();
        future.advance(hours / 24.0);
        self.evaluate(&future)
    }
}

/// The seasonal signal in `-1.0..=1.0`: `+1` at midsummer, `-1` at midwinter.
///
/// Derived from the same phase as [`WeatherTime::solar_declination`] but
/// normalised, and lagged by about three weeks because ground and sea take
/// time to warm up — which is why the hottest month is not the sunniest one.
fn seasonal_signal(time: &WeatherTime) -> f32 {
    use crate::time::DAYS_PER_YEAR;
    const THERMAL_LAG_DAYS: f32 = 21.0;
    if time.axial_tilt.abs() < 1e-4 {
        return 0.0;
    }
    let lagged = time.year_fraction() - THERMAL_LAG_DAYS / DAYS_PER_YEAR;
    let angle = core::f32::consts::TAU * (lagged + 10.0 / DAYS_PER_YEAR);
    // Positive in the northern summer; flipped for southern latitudes.
    let signal = -angle.cos();
    if time.latitude < 0.0 { -signal } else { signal }
}

/// Runs [`ProceduralWeather::evaluate`] into [`Weather::target`].
#[derive(Debug, Clone, Copy, Default)]
pub struct ProceduralWeatherPlugin;

impl Plugin for ProceduralWeatherPlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<ProceduralWeather>()
            .register_type::<ProceduralWeather>()
            .add_systems(
                Update,
                drive_procedural_weather
                    .in_set(WeatherSystems::Simulate)
                    // Before the blend, so a freshly generated target is acted
                    // on in the same frame it was produced.
                    .before(crate::state::blend_weather),
            );
    }
}

fn drive_procedural_weather(
    procedural: Res<ProceduralWeather>,
    time: Res<WeatherTime>,
    mut weather: ResMut<Weather>,
) {
    if !procedural.enabled {
        return;
    }
    weather.transition_half_life = procedural.transition_half_life;
    let target = procedural.evaluate(&time);
    weather.target = target;
}

#[cfg(test)]
mod tests {
    use super::*;

    fn temperate() -> ProceduralWeather {
        ProceduralWeather::default()
    }

    #[test]
    fn output_is_always_valid() {
        let driver = temperate();
        let mut time = WeatherTime::default();
        for _ in 0..3_000 {
            time.advance(0.011);
            let c = driver.evaluate(&time);
            assert_eq!(c, c.sanitized(), "invalid conditions at {:?}", time.hour());
        }
    }

    #[test]
    fn every_climate_produces_valid_output() {
        for climate in [
            Climate::TEMPERATE,
            Climate::OCEANIC,
            Climate::ARID,
            Climate::TROPICAL,
            Climate::MEDITERRANEAN,
            Climate::POLAR,
            Climate::DESERT_NIGHTS,
        ] {
            let driver = ProceduralWeather {
                climate,
                ..Default::default()
            };
            let mut time = WeatherTime::default();
            for _ in 0..500 {
                time.advance(0.023);
                let c = driver.evaluate(&time);
                assert_eq!(c, c.sanitized());
            }
        }
    }

    #[test]
    fn evaluation_is_deterministic() {
        let driver = temperate();
        let time = WeatherTime {
            day: 137,
            time_of_day: 0.42,
            ..Default::default()
        };
        assert_eq!(driver.evaluate(&time), driver.evaluate(&time));
    }

    #[test]
    fn different_seeds_give_different_weather() {
        let a = ProceduralWeather {
            seed: 1,
            ..Default::default()
        };
        let b = ProceduralWeather {
            seed: 2,
            ..Default::default()
        };
        let time = WeatherTime {
            day: 40,
            time_of_day: 0.5,
            ..Default::default()
        };
        assert_ne!(a.evaluate(&time), b.evaluate(&time));
    }

    #[test]
    fn forecast_matches_the_weather_when_it_arrives() {
        let driver = temperate();
        let mut now = WeatherTime::default();
        let predicted = driver.forecast(&now, 6.0);
        now.advance(6.0 / 24.0);
        assert_eq!(predicted, driver.evaluate(&now));
    }

    #[test]
    fn weather_changes_smoothly_over_time() {
        // No sudden jumps: an hour of in-game time must not swing cover wildly.
        let driver = temperate();
        let mut time = WeatherTime::default();
        let mut previous = driver.evaluate(&time).cloud_coverage;
        for _ in 0..24 * 30 {
            time.advance(1.0 / 24.0);
            let now = driver.evaluate(&time).cloud_coverage;
            assert!(
                (now - previous).abs() < 0.35,
                "cloud cover jumped {previous} -> {now}"
            );
            previous = now;
        }
    }

    #[test]
    fn polar_climate_snows_rather_than_rains() {
        let driver = ProceduralWeather {
            climate: Climate::POLAR,
            ..Default::default()
        };
        // Sample a whole year: the high Arctic genuinely does rain in
        // midsummer, so a single month would not be a fair test.
        let mut time = WeatherTime::default();
        let (mut rain, mut snow) = (0.0f32, 0.0f32);
        for _ in 0..4_000 {
            time.advance(0.1);
            let c = driver.evaluate(&time);
            rain += c.rain;
            snow += c.snow;
        }
        // Not an overwhelming ratio, because a high-Arctic summer really does
        // rain -- but snow should dominate the year comfortably.
        assert!(snow > rain * 3.0, "rain {rain}, snow {snow}");
    }

    #[test]
    fn tropical_climate_rains_rather_than_snows() {
        let driver = ProceduralWeather {
            climate: Climate::TROPICAL,
            ..Default::default()
        };
        let mut time = WeatherTime::default();
        let (mut rain, mut snow) = (0.0f32, 0.0f32);
        for _ in 0..2_000 {
            time.advance(0.017);
            let c = driver.evaluate(&time);
            rain += c.rain;
            snow += c.snow;
        }
        assert!(rain > 0.0);
        assert_eq!(snow, 0.0, "the tropics should never see snow");
    }

    #[test]
    fn most_overcast_days_stay_dry() {
        // Rain should be an event, not the default state of a cloudy sky.
        let driver = temperate();
        let mut time = WeatherTime::default();
        let (mut overcast, mut wet) = (0, 0);
        for _ in 0..20_000 {
            time.advance(0.01);
            let c = driver.evaluate(&time);
            if c.cloud_coverage > 0.85 {
                overcast += 1;
                if c.precipitation() > 0.1 {
                    wet += 1;
                }
            }
        }
        assert!(overcast > 500, "not enough overcast samples: {overcast}");
        let fraction = wet as f32 / overcast as f32;
        assert!(
            fraction < 0.5,
            "{:.0}% of overcast days were wet",
            fraction * 100.0
        );
    }

    #[test]
    fn a_saturated_airmass_still_rains_properly() {
        // The bar is higher, not unreachable: a wet climate must still deliver
        // real rain rather than a permanent drizzle.
        let driver = ProceduralWeather {
            climate: Climate::OCEANIC,
            ..Default::default()
        };
        let mut time = WeatherTime::default();
        let mut heaviest = 0.0f32;
        for _ in 0..20_000 {
            time.advance(0.01);
            heaviest = heaviest.max(driver.evaluate(&time).precipitation());
        }
        assert!(heaviest > 0.7, "heaviest rainfall was only {heaviest}");
    }

    #[test]
    fn arid_climate_is_clearer_than_oceanic() {
        let mut arid_total = 0.0;
        let mut oceanic_total = 0.0;
        let mut time = WeatherTime::default();
        let arid = ProceduralWeather {
            climate: Climate::ARID,
            ..Default::default()
        };
        let oceanic = ProceduralWeather {
            climate: Climate::OCEANIC,
            ..Default::default()
        };
        for _ in 0..2_000 {
            time.advance(0.017);
            arid_total += arid.evaluate(&time).cloud_coverage;
            oceanic_total += oceanic.evaluate(&time).cloud_coverage;
        }
        assert!(
            arid_total < oceanic_total * 0.6,
            "arid {arid_total}, oceanic {oceanic_total}"
        );
    }

    #[test]
    fn seasons_follow_the_axial_tilt() {
        let summer = WeatherTime {
            year_offset: 172.0 + 21.0, // solstice plus the thermal lag
            ..Default::default()
        };
        let winter = WeatherTime {
            year_offset: 172.0 + 21.0 + crate::time::DAYS_PER_YEAR / 2.0,
            ..Default::default()
        };
        assert!(
            seasonal_signal(&summer) > 0.99,
            "{}",
            seasonal_signal(&summer)
        );
        assert!(seasonal_signal(&winter) < -0.99);
    }

    #[test]
    fn southern_hemisphere_seasons_are_inverted() {
        let north = WeatherTime {
            year_offset: 193.0,
            latitude: 45.0,
            ..Default::default()
        };
        let south = WeatherTime {
            latitude: -45.0,
            ..north.clone()
        };
        assert!(seasonal_signal(&north) > 0.0);
        assert!(seasonal_signal(&south) < 0.0);
    }

    #[test]
    fn zero_tilt_removes_the_seasonal_signal() {
        for day in [0u32, 50, 120, 300] {
            let t = WeatherTime {
                axial_tilt: 0.0,
                day,
                ..Default::default()
            };
            assert_eq!(seasonal_signal(&t), 0.0);
        }
    }

    #[test]
    fn zero_variability_still_produces_valid_weather() {
        let driver = ProceduralWeather {
            variability: 0.0,
            ..Default::default()
        };
        let mut time = WeatherTime::default();
        for _ in 0..200 {
            time.advance(0.05);
            let c = driver.evaluate(&time);
            assert_eq!(c, c.sanitized());
        }
    }

    #[test]
    fn fog_prefers_calm_humid_nights_to_windy_afternoons() {
        // Averaged over a year, so the synoptic noise cancels and only the
        // diurnal term is left. Comparing two single moments would just be
        // comparing two unrelated samples of the weather noise.
        // A still, damp, valley-bottom climate, so radiation fog is the
        // dominant term rather than being buried under rain haze.
        let driver = ProceduralWeather {
            climate: Climate {
                fogginess: 1.0,
                base_humidity: 0.85,
                mean_wind_speed: 1.5,
                storminess: 0.1,
                ..Climate::TEMPERATE
            },
            ..Default::default()
        };

        let average_at = |hour: f32| {
            let mut total = 0.0;
            let days = 365;
            for day in 0..days {
                let mut time = WeatherTime {
                    day,
                    ..Default::default()
                };
                time.set_hour(hour);
                total += driver.evaluate(&time).fog;
            }
            total / days as f32
        };

        let dawn = average_at(5.0);
        let afternoon = average_at(14.0);
        assert!(
            dawn > afternoon * 1.5,
            "dawn {dawn} should be much foggier than afternoon {afternoon}"
        );
    }

    #[test]
    fn zero_variability_gives_the_climate_mean_wind() {
        let driver = ProceduralWeather {
            climate: Climate::TEMPERATE,
            variability: 0.0,
            ..Default::default()
        };
        let mut time = WeatherTime::default();
        for _ in 0..200 {
            time.advance(0.05);
            let c = driver.evaluate(&time);
            // Only precipitation can push it off the mean.
            let expected = Climate::TEMPERATE.mean_wind_speed
                + c.precipitation() * 12.0 * Climate::TEMPERATE.storminess;
            assert!(
                (c.wind_speed - expected).abs() < 0.5,
                "{} vs {expected}",
                c.wind_speed
            );
        }
    }
}
