//! Volumetric cloud tuning.
//!
//! The clouds themselves are raymarched in the sky dome shader
//! ([`crate::sky`]); this is the knob box. Shape comes from
//! [`WeatherConditions`](crate::state::WeatherConditions) — coverage, density,
//! altitude and thickness are weather, not configuration — while everything
//! here is art direction and performance.

use bevy::color::{Color, LinearRgba};
use bevy::ecs::reflect::ReflectResource;
use bevy::ecs::resource::Resource;
use bevy::reflect::Reflect;

use crate::config::Quality;
use bevy::reflect::std_traits::ReflectDefault;

/// Look and cost of the volumetric cloud layer.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct CloudConfig {
    /// Pin this subsystem to its own quality tier, overriding
    /// [`WeatherConfig::quality`](crate::config::WeatherConfig::quality).
    ///
    /// For a settings menu that exposes cloud detail separately from everything
    /// else. `None` follows the global dial. Explicit counts on this struct
    /// still win over both.
    pub quality: Option<Quality>,

    /// Raymarch steps through the cloud slab. `None` follows
    /// [`WeatherConfig::quality`](crate::config::WeatherConfig::quality).
    pub steps: Option<u32>,

    /// Steps taken toward the sun per sample to estimate self-shadowing.
    /// `0` swaps in a cheap analytic approximation. `None` follows quality.
    pub light_steps: Option<u32>,

    /// Size of the base cloud shape, in metres, on every axis.
    ///
    /// Roughly the width of one cumulus. Much above a few kilometres and the
    /// whole overhead sky falls inside a single noise cell, which reads as
    /// "either totally clear or totally covered" rather than as weather.
    pub shape_scale: f32,

    /// Size of the erosion detail carved out of the cloud edges, in metres.
    pub detail_scale: f32,

    /// Distance, in metres, beyond which erosion detail starts to fade out.
    ///
    /// Erosion carves features a few hundred metres across. Far enough away
    /// those are smaller than a pixel and the aerial perspective has washed out
    /// what remains, so computing them is cost with nothing to show for it —
    /// and evaluating that noise is the most expensive part of a cloud sample.
    ///
    /// It fades out over the next twice this distance again rather than
    /// stopping, because a hard cutoff draws a ring in the sky that moves with
    /// the camera. Raise it if you can see the transition; lower it to buy
    /// back time on a broken sky, where much of the screen is distant cloud
    /// near the horizon.
    pub detail_distance: f32,

    /// How hard the detail noise bites into the cloud edges, `0.0..=1.0`.
    pub detail_strength: f32,

    /// Extinction per metre at full density. Higher is darker and more opaque.
    pub extinction: f32,

    /// Forward-scattering asymmetry of the Henyey–Greenstein phase function,
    /// `-1.0..=1.0`. Positive values give the bright silver lining you see
    /// looking toward the sun.
    pub forward_scattering: f32,

    /// Back-scattering lobe, blended with the forward one. Keeps clouds from
    /// going flat and dark when the sun is behind you.
    pub back_scattering: f32,

    /// Strength of the "powder" effect that darkens cloud interiors, giving
    /// the crisp look of sunlit cumulus. `0.0..=1.0`.
    pub powder: f32,

    /// Ambient light the clouds pick up from the sky, multiplied into the base
    /// colour.
    pub ambient: f32,

    /// Overall brightness multiplier for the clouds.
    pub exposure: f32,

    /// Tint of the cloud body.
    pub albedo: Color,

    /// How fast clouds evolve relative to the wind, in "shape changes per
    /// second". Zero makes them rigid shapes that only translate.
    pub evolution_rate: f32,

    /// Multiplies wind speed for cloud advection. Clouds at altitude move
    /// faster than surface wind, so this defaults above `1.0`.
    pub wind_multiplier: f32,

    /// Angular radius of the horizon fade, as a fraction of the sky. Stops the
    /// slab intersection producing a hard line at the horizon.
    pub horizon_fade: f32,

    /// How much a lightning flash lights the cloud from inside.
    pub lightning_response: f32,

    /// Skip through empty air instead of sampling every step of it.
    ///
    /// The raymarch spends most of its samples in clear sky, where the full
    /// cloud evaluation is expensive and always returns zero. With this on it
    /// first asks a much cheaper question -- "could there be anything here?" --
    /// and only pays for the real answer when that comes back yes, taking
    /// quadruple-length steps until it does.
    ///
    /// The cheap test is conservative: it can say "maybe" where the full
    /// evaluation says "no", but never the reverse, so nothing is skipped that
    /// should have been drawn. This is on by default and there is no visual
    /// reason to turn it off; the switch exists so the cost of the fast path
    /// can be measured against the slow one.
    pub adaptive_marching: bool,
}

impl Default for CloudConfig {
    fn default() -> Self {
        Self {
            quality: None,
            steps: None,
            light_steps: None,
            shape_scale: 1_500.0,
            detail_scale: 320.0,
            detail_distance: 10_000.0,
            detail_strength: 0.35,
            extinction: 0.045,
            forward_scattering: 0.8,
            back_scattering: -0.25,
            powder: 0.6,
            ambient: 0.35,
            exposure: 1.0,
            albedo: Color::srgb(1.0, 1.0, 1.0),
            evolution_rate: 0.006,
            wind_multiplier: 2.5,
            horizon_fade: 0.06,
            lightning_response: 1.0,
            adaptive_marching: true,
        }
    }
}

impl CloudConfig {
    /// Cloud albedo in linear space, ready for the shader.
    pub fn albedo_linear(&self) -> LinearRgba {
        self.albedo.to_linear()
    }
}

impl CloudConfig {
    /// The quality tier the clouds run at, resolving
    /// [`quality`](Self::quality) against the global dial.
    pub fn quality(&self, global: Quality) -> Quality {
        self.quality.unwrap_or(global)
    }

    /// Raymarch steps to use, resolving the explicit override, then the tier.
    pub fn resolved_steps(&self, global: Quality) -> u32 {
        self.steps
            .unwrap_or_else(|| self.quality(global).cloud_steps())
    }

    /// Light-march steps to use, resolved the same way.
    pub fn resolved_light_steps(&self, global: Quality) -> u32 {
        self.light_steps
            .unwrap_or_else(|| self.quality(global).cloud_light_steps())
    }

    /// Erosion strength to use. The cheapest tier drops it entirely, which is
    /// the single biggest saving available in a cloud sample.
    pub fn resolved_detail_strength(&self, global: Quality) -> f32 {
        if self.quality(global).cloud_erosion() {
            self.detail_strength.clamp(0.0, 1.0)
        } else {
            0.0
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn an_explicit_count_beats_both_tiers() {
        let clouds = CloudConfig {
            steps: Some(7),
            quality: Some(Quality::Ultra),
            ..Default::default()
        };
        assert_eq!(clouds.resolved_steps(Quality::Potato), 7);
    }

    #[test]
    fn a_pinned_subsystem_ignores_the_global_dial() {
        let clouds = CloudConfig {
            quality: Some(Quality::Ultra),
            ..Default::default()
        };
        assert_eq!(
            clouds.resolved_steps(Quality::Potato),
            Quality::Ultra.cloud_steps()
        );
    }

    #[test]
    fn an_unpinned_subsystem_follows_the_global_dial() {
        let clouds = CloudConfig::default();
        for quality in Quality::ALL {
            assert_eq!(clouds.resolved_steps(quality), quality.cloud_steps());
            assert_eq!(
                clouds.resolved_light_steps(quality),
                quality.cloud_light_steps()
            );
        }
    }

    #[test]
    fn the_cheapest_tier_turns_erosion_off() {
        let clouds = CloudConfig::default();
        assert_eq!(clouds.resolved_detail_strength(Quality::Potato), 0.0);
        assert!(clouds.resolved_detail_strength(Quality::Low) > 0.0);
    }

    #[test]
    fn detail_strength_is_always_in_range() {
        let clouds = CloudConfig {
            detail_strength: 5.0,
            ..Default::default()
        };
        assert_eq!(clouds.resolved_detail_strength(Quality::High), 1.0);

        let clouds = CloudConfig {
            detail_strength: -2.0,
            ..Default::default()
        };
        assert_eq!(clouds.resolved_detail_strength(Quality::High), 0.0);
    }
}
