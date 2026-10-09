//! Ready-made weather, for when you just want it to rain.

use bevy::reflect::Reflect;

use crate::state::WeatherConditions;

/// Named weather archetypes.
///
/// Each converts into a full [`WeatherConditions`], which you are free to tweak:
///
/// ```
/// # use bevy_weather::prelude::*;
/// let mut storm = WeatherPreset::Thunderstorm.conditions();
/// storm.snow = 0.4; // thundersnow
/// ```
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default, Reflect)]
pub enum WeatherPreset {
    /// Cloudless, dry, light breeze.
    #[default]
    Clear,
    /// A few fair-weather cumulus.
    FewClouds,
    /// Scattered cumulus over about half the sky.
    PartlyCloudy,
    /// Solid grey stratus deck, no rain.
    Overcast,
    /// Thin high cirrus over a mostly blue sky.
    Hazy,
    /// Light, steady drizzle.
    Drizzle,
    /// Proper rain under a thick deck.
    Rain,
    /// Heavy rain, strong wind, low cloud base.
    Storm,
    /// Storm plus frequent lightning and towering cloud.
    Thunderstorm,
    /// Gentle snowfall in still air.
    LightSnow,
    /// Steady snow under an overcast sky.
    Snow,
    /// Heavy snow, gale-force wind, near-zero visibility.
    Blizzard,
    /// Dense ground fog under a flat sky.
    Fog,
    /// Low mist at dawn, clearing above.
    MistyMorning,
    /// Dry, dusty haze with a hot wind.
    Sandstorm,
}

impl WeatherPreset {
    /// Every preset, in a stable order. Useful for editors and cycling hotkeys.
    pub const ALL: [WeatherPreset; 15] = [
        WeatherPreset::Clear,
        WeatherPreset::FewClouds,
        WeatherPreset::PartlyCloudy,
        WeatherPreset::Overcast,
        WeatherPreset::Hazy,
        WeatherPreset::Drizzle,
        WeatherPreset::Rain,
        WeatherPreset::Storm,
        WeatherPreset::Thunderstorm,
        WeatherPreset::LightSnow,
        WeatherPreset::Snow,
        WeatherPreset::Blizzard,
        WeatherPreset::Fog,
        WeatherPreset::MistyMorning,
        WeatherPreset::Sandstorm,
    ];

    /// Human-readable name, for debug overlays.
    pub const fn name(self) -> &'static str {
        match self {
            WeatherPreset::Clear => "Clear",
            WeatherPreset::FewClouds => "Few Clouds",
            WeatherPreset::PartlyCloudy => "Partly Cloudy",
            WeatherPreset::Overcast => "Overcast",
            WeatherPreset::Hazy => "Hazy",
            WeatherPreset::Drizzle => "Drizzle",
            WeatherPreset::Rain => "Rain",
            WeatherPreset::Storm => "Storm",
            WeatherPreset::Thunderstorm => "Thunderstorm",
            WeatherPreset::LightSnow => "Light Snow",
            WeatherPreset::Snow => "Snow",
            WeatherPreset::Blizzard => "Blizzard",
            WeatherPreset::Fog => "Fog",
            WeatherPreset::MistyMorning => "Misty Morning",
            WeatherPreset::Sandstorm => "Sandstorm",
        }
    }

    /// The conditions this preset stands for.
    pub fn conditions(self) -> WeatherConditions {
        // A dry, still, cloudless baseline that each arm edits.
        let base = WeatherConditions {
            cloud_coverage: 0.0,
            cloud_density: 0.6,
            cloud_altitude: 1_800.0,
            cloud_thickness: 600.0,
            rain: 0.0,
            snow: 0.0,
            fog: 0.0,
            thunder: 0.0,
            wind_speed: 2.0,
            wind_direction: 0.0,
            turbulence: 0.15,
            humidity: 0.35,
            temperature: 18.0,
        };

        match self {
            WeatherPreset::Clear => base,

            WeatherPreset::FewClouds => WeatherConditions {
                cloud_coverage: 0.18,
                cloud_density: 0.5,
                cloud_thickness: 500.0,
                wind_speed: 3.0,
                humidity: 0.4,
                ..base
            },

            WeatherPreset::PartlyCloudy => WeatherConditions {
                cloud_coverage: 0.45,
                cloud_density: 0.6,
                cloud_thickness: 900.0,
                wind_speed: 4.5,
                turbulence: 0.25,
                humidity: 0.5,
                ..base
            },

            WeatherPreset::Overcast => WeatherConditions {
                cloud_coverage: 0.95,
                cloud_density: 0.7,
                cloud_altitude: 1_100.0,
                cloud_thickness: 700.0,
                wind_speed: 5.0,
                turbulence: 0.2,
                humidity: 0.7,
                temperature: 13.0,
                ..base
            },

            WeatherPreset::Hazy => WeatherConditions {
                cloud_coverage: 0.30,
                cloud_density: 0.18,
                cloud_altitude: 6_000.0,
                cloud_thickness: 900.0,
                fog: 0.12,
                humidity: 0.45,
                temperature: 24.0,
                ..base
            },

            WeatherPreset::Drizzle => WeatherConditions {
                cloud_coverage: 0.85,
                cloud_density: 0.6,
                cloud_altitude: 900.0,
                cloud_thickness: 600.0,
                rain: 0.2,
                fog: 0.12,
                wind_speed: 4.0,
                humidity: 0.85,
                temperature: 11.0,
                ..base
            },

            WeatherPreset::Rain => WeatherConditions {
                cloud_coverage: 1.0,
                cloud_density: 0.8,
                cloud_altitude: 800.0,
                cloud_thickness: 1_400.0,
                rain: 0.55,
                fog: 0.18,
                wind_speed: 7.0,
                turbulence: 0.35,
                humidity: 0.93,
                temperature: 10.0,
                ..base
            },

            WeatherPreset::Storm => WeatherConditions {
                cloud_coverage: 1.0,
                cloud_density: 0.92,
                cloud_altitude: 600.0,
                cloud_thickness: 2_600.0,
                rain: 0.85,
                fog: 0.28,
                thunder: 0.4,
                wind_speed: 16.0,
                turbulence: 0.7,
                humidity: 0.97,
                temperature: 9.0,
                ..base
            },

            WeatherPreset::Thunderstorm => WeatherConditions {
                cloud_coverage: 1.0,
                cloud_density: 0.97,
                cloud_altitude: 500.0,
                cloud_thickness: 4_500.0,
                rain: 1.0,
                fog: 0.3,
                thunder: 4.0,
                wind_speed: 20.0,
                turbulence: 0.85,
                humidity: 1.0,
                temperature: 12.0,
                ..base
            },

            WeatherPreset::LightSnow => WeatherConditions {
                cloud_coverage: 0.8,
                cloud_density: 0.55,
                cloud_altitude: 900.0,
                cloud_thickness: 700.0,
                snow: 0.25,
                fog: 0.1,
                wind_speed: 2.0,
                turbulence: 0.3,
                humidity: 0.8,
                temperature: -2.0,
                ..base
            },

            WeatherPreset::Snow => WeatherConditions {
                cloud_coverage: 1.0,
                cloud_density: 0.75,
                cloud_altitude: 800.0,
                cloud_thickness: 1_200.0,
                snow: 0.6,
                fog: 0.25,
                wind_speed: 5.0,
                turbulence: 0.45,
                humidity: 0.9,
                temperature: -6.0,
                ..base
            },

            WeatherPreset::Blizzard => WeatherConditions {
                cloud_coverage: 1.0,
                cloud_density: 0.9,
                cloud_altitude: 500.0,
                cloud_thickness: 1_800.0,
                snow: 1.0,
                fog: 0.7,
                wind_speed: 26.0,
                turbulence: 1.0,
                humidity: 0.95,
                temperature: -15.0,
                ..base
            },

            WeatherPreset::Fog => WeatherConditions {
                cloud_coverage: 0.6,
                cloud_density: 0.4,
                cloud_altitude: 1_000.0,
                cloud_thickness: 500.0,
                fog: 0.9,
                wind_speed: 0.6,
                turbulence: 0.05,
                humidity: 1.0,
                temperature: 8.0,
                ..base
            },

            WeatherPreset::MistyMorning => WeatherConditions {
                cloud_coverage: 0.25,
                cloud_density: 0.45,
                cloud_altitude: 2_200.0,
                cloud_thickness: 600.0,
                fog: 0.55,
                wind_speed: 1.0,
                turbulence: 0.08,
                humidity: 0.95,
                temperature: 7.0,
                ..base
            },

            WeatherPreset::Sandstorm => WeatherConditions {
                cloud_coverage: 0.35,
                cloud_density: 0.3,
                cloud_altitude: 3_000.0,
                cloud_thickness: 800.0,
                fog: 0.8,
                wind_speed: 22.0,
                turbulence: 0.9,
                humidity: 0.05,
                temperature: 38.0,
                ..base
            },
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn every_preset_survives_sanitizing_unchanged() {
        for preset in WeatherPreset::ALL {
            let c = preset.conditions();
            assert_eq!(
                c,
                c.sanitized(),
                "{} is outside its own valid range",
                preset.name()
            );
        }
    }

    #[test]
    fn snowy_presets_are_below_freezing() {
        for preset in [
            WeatherPreset::LightSnow,
            WeatherPreset::Snow,
            WeatherPreset::Blizzard,
        ] {
            assert!(
                preset.conditions().temperature < 0.0,
                "{} should be freezing",
                preset.name()
            );
        }
    }

    #[test]
    fn rainy_presets_are_above_freezing_and_cloudy() {
        for preset in [
            WeatherPreset::Drizzle,
            WeatherPreset::Rain,
            WeatherPreset::Storm,
            WeatherPreset::Thunderstorm,
        ] {
            let c = preset.conditions();
            assert!(c.temperature > 0.0, "{}", preset.name());
            assert!(c.rain > 0.0, "{}", preset.name());
            assert!(c.cloud_coverage > 0.5, "{}", preset.name());
        }
    }

    #[test]
    fn severity_is_ordered() {
        let drizzle = WeatherPreset::Drizzle.conditions();
        let rain = WeatherPreset::Rain.conditions();
        let storm = WeatherPreset::Storm.conditions();
        assert!(drizzle.rain < rain.rain);
        assert!(rain.rain < storm.rain);
        assert!(rain.wind_speed < storm.wind_speed);
    }

    #[test]
    fn only_storms_bring_lightning() {
        for preset in WeatherPreset::ALL {
            let expected = matches!(preset, WeatherPreset::Storm | WeatherPreset::Thunderstorm);
            assert_eq!(
                preset.conditions().thunder > 0.0,
                expected,
                "{}",
                preset.name()
            );
        }
    }

    #[test]
    fn all_is_exhaustive_and_unique() {
        let mut seen: Vec<WeatherPreset> = Vec::new();
        for p in WeatherPreset::ALL {
            assert!(!seen.contains(&p), "{} listed twice", p.name());
            seen.push(p);
        }
    }
}
