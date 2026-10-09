//! Ground fog and haze.
//!
//! Two mechanisms, both driven from [`WeatherConditions::fog`]:
//!
//! * Bevy's [`FogVolume`] raymarched volumetric fog, which catches light shafts
//!   from the sun and moon. A single volume follows the camera.
//! * The much cheaper per-pixel [`DistanceFog`], for when the volumetric pass
//!   is too expensive or unavailable.
//!
//! [`WeatherConditions::fog`]: crate::state::WeatherConditions::fog

use bevy::app::{App, Plugin, Startup, Update};
use bevy::color::{Color, ColorToComponents, LinearRgba};
use bevy::ecs::component::Component;
use bevy::ecs::query::With;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query, Res};
use bevy::light::{FogVolume, VolumetricFog};
use bevy::math::Vec3;
use bevy::pbr::{DistanceFog, FogFalloff};
use bevy::prelude::Entity;
use bevy::reflect::Reflect;
use bevy::transform::components::{GlobalTransform, Transform};

use crate::WeatherSystems;
use crate::celestial::CelestialBodies;
use crate::config::{Quality, WeatherCamera, WeatherConfig};
use crate::state::Weather;
use crate::wind::Wind;
use bevy::ecs::reflect::ReflectComponent;
use bevy::ecs::reflect::ReflectResource;
use bevy::reflect::std_traits::ReflectDefault;

/// Fog appearance and cost.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct FogConfig {
    /// Pin this subsystem to its own quality tier, overriding
    /// [`WeatherConfig::quality`](crate::config::WeatherConfig::quality).
    ///
    /// For a settings menu that exposes fog separately from everything
    /// else. `None` follows the global dial. Explicit counts on this struct
    /// still win over both.
    pub quality: Option<Quality>,

    /// Edge length of the fog volume, in world units. It follows the camera.
    ///
    /// Make this comfortably larger than the visibility distance you expect.
    /// The volume has hard faces, and if one falls inside the visible range you
    /// can see the rectangle where the volumetric fog stops and only
    /// [`DistanceFog`] continues. Beyond the visibility distance the two agree
    /// closely enough that the seam disappears.
    pub volume_size: f32,

    /// Height of the fog volume, in world units. Ground fog is a shallow layer,
    /// so this is much smaller than [`volume_size`](Self::volume_size).
    pub volume_height: f32,

    /// Vertical centre of the fog volume relative to the camera, in world
    /// units. Negative keeps the layer near the ground as the camera climbs.
    pub volume_offset: f32,

    /// Ceiling on the optical depth across the fog volume.
    ///
    /// Deliberately small, because the volumetric pass is only here for god
    /// rays. The visible loss of contrast is done by [`DistanceFog`] and the
    /// sky fade, which agree with each other exactly.
    ///
    /// # Why the volumetric pass cannot carry the fog itself
    ///
    /// Bevy computes a fog volume's ambient in-scattering as
    /// `exp(-ray_length * (absorption + scattering))`, with no density term in
    /// it. That makes the term shrink with distance: a long ray to the sky
    /// picks up *less* ambient than a short ray to nearby geometry, which is
    /// backwards. Turn it up and the horizon develops a dark band with a bright
    /// foreground beneath it -- the opposite of fog. So the ambient is left at
    /// zero and the volume contributes only the directional-light term, which
    /// is computed correctly.
    ///
    /// That term is pure absorption on its own, so the volume still dims what
    /// is behind it. Keeping the optical depth low keeps that dimming below
    /// the level where it fights the distance fog.
    pub max_optical_depth: f32,

    /// Fraction of the fog's extinction that is absorption rather than
    /// scattering, `0.0..=1.0`. Higher is darker and moodier.
    pub absorption_fraction: f32,

    /// Fog colour in daylight.
    pub day_color: Color,

    /// Fog colour at night. Real fog is lit by whatever is around it, so a
    /// separate night colour reads far better than tinting one value.
    pub night_color: Color,

    /// How strongly fog scatters light toward the camera, `-1.0..=1.0`.
    /// High values make god rays snap into view when you face the sun.
    pub scattering_asymmetry: f32,

    /// How much brighter fog is looking toward the sun than away from it.
    ///
    /// Fog scatters light forward, so it glows in the sun's direction. Zero
    /// gives a flat, uniform colour in every direction, which is what makes
    /// fog look like a grey card rather than like air.
    pub sun_glow: f32,

    /// How tightly that glow is concentrated around the sun. Higher is tighter.
    pub sun_glow_exponent: f32,

    /// Random offset applied to each ray's start, to trade banding for noise.
    /// Worth raising when temporal antialiasing is on to smooth it back out.
    pub jitter: f32,

    /// Visibility in world units in the thickest fog, used for
    /// [`DistanceFog`].
    ///
    /// This is the value at a fog density of `1.0`; see
    /// [`visibility_at`](Self::visibility_at) for how the range between this
    /// and [`max_visibility`](Self::max_visibility) is walked.
    pub min_visibility: f32,

    /// Visibility in world units with no fog at all.
    ///
    /// This is *clear air*, so it should be a long way: the meteorological
    /// figure for a clear day is twenty to fifty kilometres, and what closes
    /// the horizon before that is the atmosphere's own scattering, which
    /// [`AtmosphereConfig`](crate::atmosphere::AtmosphereConfig) already
    /// renders. Setting it short makes clear weather permanently hazy.
    pub max_visibility: f32,
}

impl Default for FogConfig {
    fn default() -> Self {
        Self {
            quality: None,
            volume_size: 700.0,
            volume_height: 30.0,
            volume_offset: -10.0,
            max_optical_depth: 0.25,
            absorption_fraction: 0.25,
            day_color: Color::srgb(0.78, 0.82, 0.88),
            night_color: Color::srgb(0.10, 0.13, 0.20),
            scattering_asymmetry: 0.7,
            sun_glow: 0.9,
            sun_glow_exponent: 8.0,
            jitter: 0.0,
            min_visibility: 25.0,
            max_visibility: 25_000.0,
        }
    }
}

impl FogConfig {
    /// The quality tier the fog runs at, resolving [`quality`](Self::quality)
    /// against the global dial.
    pub fn quality(&self, global: Quality) -> Quality {
        self.quality.unwrap_or(global)
    }

    /// Total extinction coefficient handed to Bevy's fog volume.
    ///
    /// Pinned to the reciprocal of [`volume_size`](Self::volume_size), which is
    /// what lets [`volume_optical_depth`](Self::volume_optical_depth) mean
    /// something absolute. Bevy multiplies this coefficient by the volume's
    /// density factor, so a ray crossing the whole volume accumulates exactly
    /// that factor's worth of optical depth however large the box happens to
    /// be. Choose the extinction independently and the two are coupled: resize
    /// the volume and the fog inside it silently changes strength.
    pub fn volume_extinction(&self) -> f32 {
        1.0 / self.volume_size.max(1.0)
    }

    /// Visibility in world units at a given fog density.
    ///
    /// Interpolated *geometrically*, not linearly: visibility spans two orders
    /// of magnitude between clear air and a pea-souper, so a linear ramp spends
    /// almost its whole range down at the thick end. With a straight lerp, a
    /// storm's light murk at `0.3` already closes the horizon to a hundred
    /// metres. Geometric interpolation gives every part of the range a
    /// meaningful share of the effect.
    pub fn visibility_at(&self, density: f32) -> f32 {
        let clear = self.max_visibility.max(1.0);
        let thick = self.min_visibility.max(1.0);
        clear * (thick / clear).powf(density.clamp(0.0, 1.0))
    }

    /// Extinction per world unit at a given fog density.
    ///
    /// Defined so that transmittance has fallen to about 5% at
    /// [`visibility_at`](Self::visibility_at), the usual meteorological
    /// convention for "visibility".
    pub fn extinction_at(&self, density: f32) -> f32 {
        3.0 / self.visibility_at(density).max(1e-3)
    }

    /// Extinction per world unit that the *sky* should pick up from fog.
    ///
    /// This is [`extinction_at`](Self::extinction_at) with the clear-air floor
    /// taken out, and the distinction matters more than it looks.
    ///
    /// `extinction_at` never returns zero -- it cannot, since it is three over
    /// a finite visibility -- and the sky shader multiplies it by a path length
    /// that runs away toward the horizon, where a ray skimming a shallow layer
    /// travels through it almost indefinitely. Feed it the clear-air value and
    /// the bottom of the sky is washed flat grey on a cloudless day: the band
    /// where a sunset actually happens, painted over with a colour that has no
    /// idea where the sun is.
    ///
    /// Subtracting the floor means no fog gives no wash, and the horizon haze
    /// on a clear day is left to the atmosphere pass, which computes it from
    /// real scattering and gets the colour right.
    pub fn sky_extinction_at(&self, density: f32) -> f32 {
        (self.extinction_at(density) - self.extinction_at(0.0)).max(0.0)
    }

    /// Fog colour for the current sun altitude and cloud cover.
    ///
    /// Fog is not a light source; it only shows you the light already around
    /// it. So as well as shifting from the night colour to the day colour, it
    /// dims under cloud -- which is the difference between a bright white-out
    /// and the low grey murk under a thunderhead.
    pub fn color_at(&self, daylight: f32, cloudiness: f32) -> LinearRgba {
        let night = self.night_color.to_linear().to_vec3();
        let day = self.day_color.to_linear().to_vec3();
        let mixed = night.lerp(day, daylight.clamp(0.0, 1.0));
        let lit = 1.0 - 0.45 * cloudiness.clamp(0.0, 1.0);
        let scaled = mixed * lit;
        LinearRgba::rgb(scaled.x, scaled.y, scaled.z)
    }

    /// Optical depth the fog volume should have, for a given fog density.
    ///
    /// Straight proportionality rather than anything derived from visibility.
    /// The volumetric pass is only carrying god rays now, so what matters is
    /// that it is zero in clear weather and stays well below the level where
    /// its absorption fights the distance fog. Deriving it from visibility
    /// instead pins it to the cap across almost the entire range -- including
    /// at zero fog, which leaves a permanent haze on a cloudless day.
    pub fn volume_optical_depth(&self, density: f32) -> f32 {
        density.clamp(0.0, 1.0) * self.max_optical_depth.max(0.0)
    }
}

/// Marks the fog volume this plugin spawns and moves.
#[derive(Component, Debug, Clone, Copy, Default, Reflect)]
#[reflect(Component, Default)]
pub struct WeatherFogVolume;

/// Drives Bevy's volumetric and distance fog from the weather.
#[derive(Debug, Clone, Copy, Default)]
pub struct WeatherFogPlugin;

impl Plugin for WeatherFogPlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<FogConfig>()
            .register_type::<FogConfig>()
            .register_type::<WeatherFogVolume>()
            .add_systems(Startup, spawn_fog_volume)
            .add_systems(
                Update,
                (configure_cameras, drive_fog_volume).in_set(WeatherSystems::Apply),
            );
    }
}

fn spawn_fog_volume(mut commands: Commands) {
    commands.spawn((WeatherFogVolume, FogVolume::default(), Transform::default()));
}

fn configure_cameras(
    mut commands: Commands,
    config: Res<WeatherConfig>,
    fog: Res<FogConfig>,
    weather: Res<Weather>,
    bodies: Res<CelestialBodies>,
    cameras: Query<Entity, With<WeatherCamera>>,
) {
    let density = weather.current.fog;
    let cloudiness = weather.current.cloud_coverage * weather.current.cloud_density;
    let color = Color::LinearRgba(fog.color_at(bodies.daylight, cloudiness));

    for entity in &cameras {
        let mut entity_commands = commands.entity(entity);

        if config.volumetric_fog {
            entity_commands.insert(VolumetricFog {
                step_count: fog.quality(config.quality).fog_steps(),
                jitter: fog.jitter,
                ambient_color: color,
                // See `FogConfig::max_optical_depth` for why this stays at zero.
                ambient_intensity: 0.0,
            });
        } else {
            entity_commands.remove::<VolumetricFog>();
        }

        if config.distance_fog {
            let visibility = fog.visibility_at(density);
            entity_commands.insert(DistanceFog {
                color,
                directional_light_color: Color::srgba(1.0, 0.95, 0.85, 0.4),
                directional_light_exponent: 30.0,
                falloff: FogFalloff::from_visibility(visibility),
            });
        } else {
            entity_commands.remove::<DistanceFog>();
        }
    }
}

fn drive_fog_volume(
    config: Res<WeatherConfig>,
    fog: Res<FogConfig>,
    weather: Res<Weather>,
    bodies: Res<CelestialBodies>,
    wind: Res<Wind>,
    cameras: Query<&GlobalTransform, With<WeatherCamera>>,
    mut volumes: Query<(&mut FogVolume, &mut Transform), With<WeatherFogVolume>>,
) {
    let density = weather.current.fog;
    let cloudiness = weather.current.cloud_coverage * weather.current.cloud_density;
    let color = Color::LinearRgba(fog.color_at(bodies.daylight, cloudiness));
    // Follow the first weather camera; the volume is a local effect around the
    // viewer, not a world-sized object.
    let center = cameras
        .iter()
        .next()
        .map(|transform| transform.translation())
        .unwrap_or(Vec3::ZERO);

    for (mut volume, mut transform) in &mut volumes {
        if !config.volumetric_fog || density <= 0.001 {
            // Zero density is cheaper than despawning and respawning, and
            // avoids a one-frame pop when the fog rolls back in.
            volume.density_factor = 0.0;
            continue;
        }

        let extinction = fog.volume_extinction();
        let absorption_fraction = fog.absorption_fraction.clamp(0.0, 1.0);
        volume.density_factor = fog.volume_optical_depth(density);
        volume.fog_color = color;
        volume.absorption = extinction * absorption_fraction;
        volume.scattering = extinction * (1.0 - absorption_fraction);
        volume.scattering_asymmetry = fog.scattering_asymmetry.clamp(-0.99, 0.99);
        // Scroll the (optional) density texture with the wind.
        volume.density_texture_offset = wind.offset3() * 0.001;

        transform.translation = center + Vec3::Y * fog.volume_offset;
        transform.scale = Vec3::new(fog.volume_size, fog.volume_height, fog.volume_size);
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn fog_colour_blends_between_night_and_day() {
        let config = FogConfig::default();
        let night = config.color_at(0.0, 0.0);
        let day = config.color_at(1.0, 0.0);
        assert!(day.red > night.red, "day fog should be brighter");

        let mid = config.color_at(0.5, 0.0);
        assert!(mid.red > night.red && mid.red < day.red);
    }

    #[test]
    fn fog_colour_clamps_out_of_range_inputs() {
        let config = FogConfig::default();
        assert_eq!(config.color_at(-5.0, 0.0), config.color_at(0.0, 0.0));
        assert_eq!(config.color_at(5.0, 0.0), config.color_at(1.0, 0.0));
        assert_eq!(config.color_at(1.0, -2.0), config.color_at(1.0, 0.0));
        assert_eq!(config.color_at(1.0, 3.0), config.color_at(1.0, 1.0));
    }

    #[test]
    fn cloud_cover_darkens_the_fog() {
        let config = FogConfig::default();
        let clear = config.color_at(1.0, 0.0);
        let overcast = config.color_at(1.0, 1.0);
        assert!(
            overcast.red < clear.red,
            "{} vs {}",
            overcast.red,
            clear.red
        );
    }

    #[test]
    fn light_murk_still_leaves_a_usable_view_distance() {
        // A thunderstorm sets fog to about 0.3. That should read as murk, not
        // as a hundred-metre white-out.
        let config = FogConfig::default();
        assert!(
            config.visibility_at(0.3) > 400.0,
            "0.3 gave {}",
            config.visibility_at(0.3)
        );
        assert!(
            config.visibility_at(0.9) < 100.0,
            "0.9 gave {}",
            config.visibility_at(0.9)
        );
    }

    #[test]
    fn volume_optical_depth_is_capped_and_vanishes_in_clear_weather() {
        let config = FogConfig::default();
        let mut previous = -1.0;
        for i in 0..=20 {
            let depth = config.volume_optical_depth(i as f32 / 20.0);
            assert!(
                depth <= config.max_optical_depth + 1e-6,
                "depth {depth} exceeded the cap"
            );
            assert!(depth > previous, "depth should rise with fog");
            previous = depth;
        }
        // No fog means no volume at all, or a clear day picks up a haze it
        // should not have.
        assert_eq!(config.volume_optical_depth(0.0), 0.0);
        assert_eq!(config.volume_optical_depth(-1.0), 0.0);
        assert_eq!(config.volume_optical_depth(5.0), config.max_optical_depth);
    }

    #[test]
    fn visibility_shrinks_as_fog_thickens() {
        let config = FogConfig::default();
        assert!((config.visibility_at(0.0) - config.max_visibility).abs() < 1.0);
        assert!((config.visibility_at(1.0) - config.min_visibility).abs() < 1.0);

        let mut previous = f32::INFINITY;
        for i in 0..=20 {
            let v = config.visibility_at(i as f32 / 20.0);
            assert!(v < previous, "visibility should only shrink");
            previous = v;
        }
    }

    #[test]
    fn visibility_clamps_out_of_range_density() {
        let config = FogConfig::default();
        assert_eq!(config.visibility_at(-1.0), config.visibility_at(0.0));
        assert_eq!(config.visibility_at(5.0), config.visibility_at(1.0));
    }

    #[test]
    fn extinction_reaches_five_percent_at_the_visibility_distance() {
        let config = FogConfig::default();
        for density in [0.1f32, 0.5, 1.0] {
            let visibility = config.visibility_at(density);
            let transmittance = (-config.extinction_at(density) * visibility).exp();
            assert!(
                (transmittance - 0.0498).abs() < 1e-3,
                "density {density} gave {transmittance}"
            );
        }
    }
}

#[cfg(test)]
mod volume_tests {
    use super::*;

    #[test]
    fn extinction_across_the_volume_is_one_nepers_worth() {
        // Keeps the volume's own coefficients in a sane range relative to its
        // size, so the density carried by `max_optical_depth` means what it
        // says: optical depth across the volume is the density.
        for size in [50.0f32, 250.0, 1_000.0] {
            let config = FogConfig {
                volume_size: size,
                ..Default::default()
            };
            let survival = (-size * config.volume_extinction()).exp();
            assert!(
                (survival - core::f32::consts::E.recip()).abs() < 1e-5,
                "size {size} gave {survival}"
            );
        }
    }

    #[test]
    fn absorption_and_scattering_sum_to_the_extinction() {
        let config = FogConfig::default();
        let extinction = config.volume_extinction();
        let absorption = extinction * config.absorption_fraction;
        let scattering = extinction * (1.0 - config.absorption_fraction);
        assert!((absorption + scattering - extinction).abs() < 1e-9);
    }
}
