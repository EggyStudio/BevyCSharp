//! A CPU evaluation of the cloud shape field, matching `sky.wgsl`.
//!
//! The shader raymarches this field to draw the cloud deck; this module
//! evaluates the same field on the CPU so that other things can know where the
//! clouds are. At the moment the only caller is
//! [`cloud_shadows`](crate::cloud_shadows), which needs a top-down slice of it
//! to project onto the ground.
//!
//! # Why a second copy
//!
//! Because the alternative is a shadow pattern with no relationship to the sky
//! above it. Shipping a generic tiling noise texture is what most engines do
//! for cloud shadows, and it is fine right up until someone looks up: the deck
//! overhead is broken and the ground is evenly dappled, or the wind shifts and
//! the shadows sit still.
//!
//! Everything here is a transcription. The hashes, the octave count, the
//! lacunarity, the inter-octave offsets and the coverage remap are all the same
//! numbers as the shader, so the two agree on where a cloud is and both move
//! with the same wind. What is *not* here is the erosion pass: it carves
//! features a few hundred metres across, which a shadow map at tens of metres
//! per texel cannot resolve and a soft shadow would not show anyway.

use bevy::math::{Vec2, Vec3};

use crate::clouds::CloudConfig;
use crate::state::WeatherConditions;

/// `fract(x)` with WGSL's semantics.
#[inline]
fn fract(x: f32) -> f32 {
    x - x.floor()
}

#[inline]
fn fract3(v: Vec3) -> Vec3 {
    Vec3::new(fract(v.x), fract(v.y), fract(v.z))
}

/// Matches `hash13` in `sky.wgsl`.
#[inline]
fn hash13(p: Vec3) -> f32 {
    let mut q = fract3(p * 0.1031);
    q += Vec3::splat(q.dot(Vec3::new(q.z, q.y, q.x) + Vec3::splat(31.32)));
    fract((q.x + q.y) * q.z)
}

/// Matches `value_noise3` in `sky.wgsl`.
fn value_noise3(p: Vec3) -> f32 {
    let i = p.floor();
    let f = p - i;
    // The same smoothstep weighting the shader uses.
    let u = f * f * (Vec3::splat(3.0) - 2.0 * f);

    let corner = |dx: f32, dy: f32, dz: f32| hash13(i + Vec3::new(dx, dy, dz));
    let n000 = corner(0.0, 0.0, 0.0);
    let n100 = corner(1.0, 0.0, 0.0);
    let n010 = corner(0.0, 1.0, 0.0);
    let n110 = corner(1.0, 1.0, 0.0);
    let n001 = corner(0.0, 0.0, 1.0);
    let n101 = corner(1.0, 0.0, 1.0);
    let n011 = corner(0.0, 1.0, 1.0);
    let n111 = corner(1.0, 1.0, 1.0);

    let mix = |a: f32, b: f32, t: f32| a * (1.0 - t) + b * t;
    let x00 = mix(n000, n100, u.x);
    let x10 = mix(n010, n110, u.x);
    let x01 = mix(n001, n101, u.x);
    let x11 = mix(n011, n111, u.x);
    mix(mix(x00, x10, u.y), mix(x01, x11, u.y), u.z)
}

/// Matches `fbm3` in `sky.wgsl`.
fn fbm3(p: Vec3, octaves: u32) -> f32 {
    let mut sum = 0.0;
    let mut amplitude = 0.5;
    let mut total = 0.0;
    let mut q = p;
    for _ in 0..octaves {
        sum += value_noise3(q) * amplitude;
        total += amplitude;
        amplitude *= 0.5;
        q = q * 2.03 + Vec3::new(17.3, 5.1, 41.7);
    }
    sum / total.max(1e-6)
}

/// Matches `billow3` in `sky.wgsl`.
fn billow3(p: Vec3, octaves: u32) -> f32 {
    let mut sum = 0.0;
    let mut amplitude = 0.5;
    let mut total = 0.0;
    let mut q = p;
    for _ in 0..octaves {
        sum += (value_noise3(q) * 2.0 - 1.0).abs() * amplitude;
        total += amplitude;
        amplitude *= 0.5;
        q = q * 2.11 + Vec3::new(3.7, 29.1, 11.3);
    }
    sum / total.max(1e-6)
}

/// Matches `remap` in `sky.wgsl`.
#[inline]
fn remap(x: f32, a: f32, b: f32, c: f32, d: f32) -> f32 {
    c + ((x - a) / (b - a).max(1e-6)).clamp(0.0, 1.0) * (d - c)
}

/// Everything the field needs that is not a position.
///
/// Built once per rebuild and then handed to every texel, so the per-texel work
/// is only the noise.
#[derive(Debug, Clone, Copy)]
pub struct CloudField {
    /// Horizontal wind displacement of the deck, in metres.
    pub offset: Vec2,
    /// Shape evolution, which slides the sampling point through the field.
    pub evolution: f32,
    /// Feature size of the base shape, in metres.
    pub shape_scale: f32,
    /// Fraction of sky covered, `0.0..=1.0`.
    pub coverage: f32,
    /// Density multiplier of the cloud interior.
    pub density: f32,
    /// Base of the deck above the ground, in metres.
    pub base_altitude: f32,
    /// Depth of the deck, in metres.
    pub thickness: f32,
}

impl CloudField {
    /// Reads the field parameters out of the live weather.
    ///
    /// `elapsed` is the same wrapped clock the sky material is given, so the
    /// shape evolution stays in step with what is being drawn.
    pub fn new(
        conditions: &WeatherConditions,
        clouds: &CloudConfig,
        wind_offset: Vec2,
        elapsed: f32,
    ) -> Self {
        Self {
            offset: wind_offset * clouds.wind_multiplier,
            evolution: elapsed * clouds.evolution_rate,
            shape_scale: clouds.shape_scale.max(1.0),
            coverage: conditions.cloud_coverage,
            density: conditions.cloud_density,
            base_altitude: conditions.cloud_altitude,
            thickness: conditions.cloud_thickness.max(1.0),
        }
    }

    /// Density at a horizontal position, sampled at `height` through the deck.
    ///
    /// `height` is normalised: `0.0` at the base and `1.0` at the top.
    pub fn density_at(&self, x: f32, z: f32, height: f32) -> f32 {
        if self.coverage <= 0.001 {
            return 0.0;
        }
        let altitude = self.base_altitude + height * self.thickness;
        let sample = Vec3::new(
            (x + self.offset.x) / self.shape_scale + self.evolution,
            altitude / self.shape_scale + self.evolution * 0.7,
            (z + self.offset.y) / self.shape_scale + self.evolution * 1.3,
        );

        let broad = fbm3(sample, 3);
        let cells = billow3(sample * 3.7, 3);
        let mut shape = broad * (1.0 - 0.35) + cells * 0.35;

        let threshold = 1.0 - self.coverage;
        if shape <= threshold {
            return 0.0;
        }
        shape = remap(shape, threshold, 1.0 - self.coverage * 0.4, 0.0, 1.0);

        let bottom = remap(height, 0.0, 0.12, 0.0, 1.0).clamp(0.0, 1.0);
        let top = remap(height, 0.55, 1.0, 1.0, 0.0).clamp(0.0, 1.0);
        let flatten = 1.0 * (1.0 - self.coverage) + (1.0 - height * 0.35) * self.coverage;
        shape *= bottom * top * flatten;

        shape.clamp(0.0, 1.0) * self.density
    }

    /// Optical depth of the whole column above a point, in metres of density.
    ///
    /// Two samples through the deck rather than one. A single mid-height sample
    /// gets the horizontal pattern right but misses the shoulders, where a
    /// thick deck is a good deal narrower than at its waist -- and it is the
    /// shoulders that decide how far a cumulus shadow spreads. Two is as far as
    /// this is worth taking: every extra sample is paid for on every texel.
    ///
    /// The thickness term is what separates a grey day from a dark one. A
    /// four-kilometre cumulonimbus and a seven-hundred-metre stratus deck have
    /// similar densities at their middles and cast completely different
    /// shadows.
    pub fn column_density(&self, x: f32, z: f32) -> f32 {
        let mean = (self.density_at(x, z, 0.3) + self.density_at(x, z, 0.7)) * 0.5;
        mean * self.thickness
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::presets::WeatherPreset;

    fn field(preset: WeatherPreset) -> CloudField {
        CloudField::new(
            &preset.conditions(),
            &CloudConfig::default(),
            Vec2::ZERO,
            0.0,
        )
    }

    #[test]
    fn noise_stays_in_the_unit_range() {
        for i in 0..500 {
            let t = i as f32;
            let p = Vec3::new(t * 0.37, t * -0.11 + 4.0, t * 0.73);
            let n = value_noise3(p);
            assert!((0.0..=1.0).contains(&n), "value noise out of range: {n}");
            let f = fbm3(p, 3);
            assert!((0.0..=1.0).contains(&f), "fbm out of range: {f}");
            let b = billow3(p, 3);
            assert!((0.0..=1.0).contains(&b), "billow out of range: {b}");
        }
    }

    #[test]
    fn a_clear_sky_casts_nothing() {
        let clear = field(WeatherPreset::Clear);
        for i in 0..200 {
            let x = i as f32 * 137.0;
            assert_eq!(clear.column_density(x, x * 0.5), 0.0);
        }
    }

    #[test]
    fn more_coverage_means_more_shadow() {
        // Averaged over a wide area, since any individual point can go either
        // way as the field is re-thresholded.
        let mean = |preset: WeatherPreset| {
            let f = field(preset);
            let mut total = 0.0;
            for i in 0..60 {
                for j in 0..60 {
                    total += f.column_density(i as f32 * 220.0, j as f32 * 220.0);
                }
            }
            total / 3600.0
        };
        let few = mean(WeatherPreset::FewClouds);
        let partly = mean(WeatherPreset::PartlyCloudy);
        let overcast = mean(WeatherPreset::Overcast);
        assert!(few < partly, "{few} !< {partly}");
        assert!(partly < overcast, "{partly} !< {overcast}");
    }

    #[test]
    fn the_field_travels_with_the_wind() {
        // A displaced field sampled at a displaced point is the original field
        // sampled at the original point. This is what keeps the shadows moving
        // with the deck instead of crawling under it.
        let conditions = WeatherPreset::PartlyCloudy.conditions();
        let clouds = CloudConfig::default();
        let still = CloudField::new(&conditions, &clouds, Vec2::ZERO, 0.0);
        let blown = CloudField::new(&conditions, &clouds, Vec2::new(500.0, -300.0), 0.0);
        let shift = Vec2::new(500.0, -300.0) * clouds.wind_multiplier;

        for i in 0..50 {
            let x = i as f32 * 91.0;
            let z = i as f32 * -57.0;
            let a = still.column_density(x, z);
            let b = blown.column_density(x - shift.x, z - shift.y);
            assert!((a - b).abs() < 1e-3, "{a} vs {b}");
        }
    }

    #[test]
    fn a_thicker_deck_casts_a_deeper_shadow() {
        let mut thin = WeatherPreset::Overcast.conditions();
        let mut thick = thin;
        thin.cloud_thickness = 700.0;
        thick.cloud_thickness = 4_000.0;
        let clouds = CloudConfig::default();
        let thin = CloudField::new(&thin, &clouds, Vec2::ZERO, 0.0);
        let thick = CloudField::new(&thick, &clouds, Vec2::ZERO, 0.0);

        let sum = |f: &CloudField| {
            (0..40)
                .map(|i| f.column_density(i as f32 * 310.0, 0.0))
                .sum::<f32>()
        };
        assert!(sum(&thick) > sum(&thin) * 3.0);
    }
}
