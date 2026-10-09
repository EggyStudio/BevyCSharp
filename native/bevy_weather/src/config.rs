//! Master switches and quality settings.

use bevy::ecs::component::Component;
use bevy::ecs::reflect::ReflectComponent;
use bevy::ecs::reflect::ReflectResource;
use bevy::ecs::resource::Resource;
use bevy::reflect::Reflect;
use bevy::reflect::std_traits::ReflectDefault;

/// Marks the camera(s) that weather should be rendered for.
///
/// The plugin inserts the components each enabled subsystem needs
/// (`AtmosphereSettings`, `VolumetricFog`, `DistanceFog`, …) onto every entity
/// with this marker, and keeps them in sync with [`WeatherConfig`]. Cameras
/// without it are left completely untouched, which is what you want for UI,
/// minimap or render-to-texture cameras.
#[derive(Component, Debug, Clone, Copy, Default, Reflect)]
#[reflect(Component, Default)]
pub struct WeatherCamera;

/// A quality tier, ready to be a dropdown in a settings menu.
///
/// Use [`ALL`](Self::ALL) to populate the menu and [`name`](Self::name) to label
/// the entries. Setting [`WeatherConfig::quality`] moves every subsystem at
/// once; each subsystem can then be pinned to its own tier, and any individual
/// count can still be set outright. See [`WeatherConfig::quality`] for how the
/// three levels resolve against each other.
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Default, Reflect)]
pub enum Quality {
    /// Whatever it takes. Clouds lose their erosion detail and their light
    /// march, so they read as soft blobs rather than sculpted cumulus, and
    /// precipitation thins right out. For hardware that cannot manage [`Low`].
    ///
    /// [`Low`]: Self::Low
    Potato,
    /// Cheap enough for integrated graphics. Clouds keep their shape but lose
    /// self-shadowing.
    Low,
    /// Sensible default.
    #[default]
    Medium,
    /// Roughly double the cloud samples, and the light march that gives them
    /// their internal shadowing.
    High,
    /// For screenshots and cutscenes.
    Ultra,
}

impl Quality {
    /// Every tier, cheapest first. Suitable for driving a settings dropdown.
    pub const ALL: [Quality; 5] = [
        Quality::Potato,
        Quality::Low,
        Quality::Medium,
        Quality::High,
        Quality::Ultra,
    ];

    /// Human-readable name, for a settings menu.
    pub const fn name(self) -> &'static str {
        match self {
            Quality::Potato => "Potato",
            Quality::Low => "Low",
            Quality::Medium => "Medium",
            Quality::High => "High",
            Quality::Ultra => "Ultra",
        }
    }

    /// The next tier up, saturating at [`Ultra`](Self::Ultra).
    pub fn higher(self) -> Quality {
        Quality::ALL
            .get(Quality::ALL.iter().position(|q| *q == self).unwrap_or(0) + 1)
            .copied()
            .unwrap_or(Quality::Ultra)
    }

    /// The next tier down, saturating at [`Potato`](Self::Potato).
    pub fn lower(self) -> Quality {
        let index = Quality::ALL.iter().position(|q| *q == self).unwrap_or(0);
        Quality::ALL
            .get(index.saturating_sub(1))
            .copied()
            .unwrap_or(Quality::Potato)
    }

    /// Whether clouds get their fine erosion detail.
    ///
    /// This is the noise that eats away at cloud edges and gives them their
    /// wispy, torn look. It is the single most expensive part of a cloud
    /// sample, and the first thing to go.
    pub const fn cloud_erosion(self) -> bool {
        !matches!(self, Quality::Potato)
    }

    /// Primary raymarch steps through the cloud layer.
    ///
    /// The most expensive number in the plugin, and the one to reach for first.
    /// Cost is per-pixel, so it is felt hardest looking straight up, where the
    /// cloud layer fills the frame.
    pub fn cloud_steps(self) -> u32 {
        match self {
            Quality::Potato => 12,
            Quality::Low => 20,
            Quality::Medium => 36,
            Quality::High => 64,
            Quality::Ultra => 128,
        }
    }

    /// Steps taken toward the sun per primary sample, to estimate self-shadowing.
    /// Zero means "use the cheap analytic approximation instead".
    pub fn cloud_light_steps(self) -> u32 {
        match self {
            Quality::Potato => 0,
            Quality::Low => 0,
            Quality::Medium => 3,
            Quality::High => 5,
            Quality::Ultra => 8,
        }
    }

    /// Number of precipitation particles at full intensity.
    pub fn particle_count(self) -> u32 {
        match self {
            Quality::Potato => 1_200,
            Quality::Low => 4_000,
            Quality::Medium => 12_000,
            Quality::High => 30_000,
            Quality::Ultra => 60_000,
        }
    }

    /// Edge length of the cubemap the atmosphere generates for ambient light.
    ///
    /// Regenerated every frame, so it is not free. It only carries low-frequency
    /// ambient and reflection, and 512 buys nothing visible over 256 for that.
    pub fn environment_map_size(self) -> u32 {
        match self {
            Quality::Potato => 32,
            Quality::Low => 64,
            Quality::Medium => 128,
            Quality::High => 256,
            Quality::Ultra => 512,
        }
    }

    /// Raymarch steps for Bevy's volumetric fog.
    pub fn fog_steps(self) -> u32 {
        match self {
            Quality::Potato => 12,
            Quality::Low => 24,
            Quality::Medium => 48,
            Quality::High => 80,
            Quality::Ultra => 128,
        }
    }
}

/// Top-level configuration: which subsystems run, and how hard they work.
///
/// This is a [`Resource`]; mutate it at runtime and everything reacts on the
/// next frame.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct WeatherConfig {
    /// Global quality dial: the one a game's graphics menu should be bound to.
    ///
    /// Settings resolve in three levels, most specific first:
    ///
    /// 1. An explicit count, like [`CloudConfig::steps`]. Always wins.
    /// 2. That subsystem's own tier, like [`CloudConfig::quality`], for a menu
    ///    with separate sliders for clouds and precipitation.
    /// 3. This, the global tier.
    ///
    /// So changing this moves everything that has not been pinned, and never
    /// overwrites a number you set by hand.
    ///
    /// [`CloudConfig::steps`]: crate::clouds::CloudConfig::steps
    /// [`CloudConfig::quality`]: crate::clouds::CloudConfig::quality
    pub quality: Quality,

    /// Render the physically-based atmosphere (sky colour, aerial perspective).
    pub atmosphere: bool,
    /// Render the star field, galaxy, moon and clouds sky dome.
    pub sky: bool,
    /// Render volumetric clouds in the sky dome.
    pub clouds: bool,
    /// Drive Bevy's volumetric fog / god rays.
    pub volumetric_fog: bool,
    /// Drive the cheap per-pixel `DistanceFog` as well.
    ///
    /// On by default, and doing most of the visible work: `DistanceFog` blends
    /// toward a colour, which is what makes heavy fog a white-out rather than
    /// a black-out. The volumetric pass supplies the god rays on top.
    pub distance_fog: bool,
    /// Simulate and draw rain and snow.
    pub precipitation: bool,
    /// Lightning flashes and thunder.
    pub thunder: bool,
    /// Spawn and steer the sun and moon directional lights.
    pub celestial_lights: bool,

    /// Multiplies every light this plugin creates. Handy when your scene is
    /// authored against a different exposure.
    pub light_intensity_scale: f32,

    /// World units per metre. Bevy's atmosphere is calibrated in metres; if
    /// your game is authored at, say, 1 unit = 1 cm, set this to `100.0`.
    pub units_per_meter: f32,
}

impl Default for WeatherConfig {
    fn default() -> Self {
        Self {
            quality: Quality::Medium,
            atmosphere: true,
            sky: true,
            clouds: true,
            volumetric_fog: true,
            distance_fog: true,
            precipitation: true,
            thunder: true,
            celestial_lights: true,
            light_intensity_scale: 1.0,
            units_per_meter: 1.0,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn every_tier_is_listed_once_and_in_order() {
        assert_eq!(Quality::ALL.len(), 5);
        for pair in Quality::ALL.windows(2) {
            assert!(
                pair[0] < pair[1],
                "{:?} should precede {:?}",
                pair[0],
                pair[1]
            );
        }
    }

    #[test]
    fn costs_rise_with_the_tier() {
        for pair in Quality::ALL.windows(2) {
            let (cheap, dear) = (pair[0], pair[1]);
            assert!(cheap.cloud_steps() <= dear.cloud_steps());
            assert!(cheap.cloud_light_steps() <= dear.cloud_light_steps());
            assert!(cheap.particle_count() <= dear.particle_count());
            assert!(cheap.fog_steps() <= dear.fog_steps());
            assert!(cheap.environment_map_size() <= dear.environment_map_size());
        }
    }

    #[test]
    fn every_tier_still_draws_something() {
        // A tier that renders nothing is a bug, not a fast setting.
        for quality in Quality::ALL {
            assert!(quality.cloud_steps() >= 8, "{}", quality.name());
            assert!(quality.particle_count() >= 500, "{}", quality.name());
            assert!(quality.environment_map_size() >= 16, "{}", quality.name());
        }
    }

    #[test]
    fn only_the_cheapest_tier_drops_cloud_detail() {
        assert!(!Quality::Potato.cloud_erosion());
        for quality in [Quality::Low, Quality::Medium, Quality::High, Quality::Ultra] {
            assert!(quality.cloud_erosion(), "{}", quality.name());
        }
    }

    #[test]
    fn stepping_through_the_tiers_saturates_at_both_ends() {
        assert_eq!(Quality::Potato.lower(), Quality::Potato);
        assert_eq!(Quality::Ultra.higher(), Quality::Ultra);
        assert_eq!(Quality::Medium.higher(), Quality::High);
        assert_eq!(Quality::Medium.lower(), Quality::Low);

        // Walking all the way up and back lands where it started.
        let mut quality = Quality::Potato;
        for _ in 0..10 {
            quality = quality.higher();
        }
        assert_eq!(quality, Quality::Ultra);
        for _ in 0..10 {
            quality = quality.lower();
        }
        assert_eq!(quality, Quality::Potato);
    }

    #[test]
    fn every_tier_has_a_distinct_name() {
        let mut seen: Vec<&str> = Vec::new();
        for quality in Quality::ALL {
            assert!(
                !seen.contains(&quality.name()),
                "{} listed twice",
                quality.name()
            );
            seen.push(quality.name());
        }
    }
}
