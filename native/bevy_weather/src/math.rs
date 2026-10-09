//! Small deterministic noise helpers.
//!
//! These are the CPU counterparts of the hashes used in the shaders, so a
//! given seed produces the same weather on every machine and every run.

/// Integer bit-mix (Wang hash). Deterministic across platforms.
#[inline]
pub fn hash_u32(mut x: u32) -> u32 {
    x ^= x >> 16;
    x = x.wrapping_mul(0x7feb_352d);
    x ^= x >> 15;
    x = x.wrapping_mul(0x846c_a68b);
    x ^= x >> 16;
    x
}

/// Hashes to a float in `[0, 1)`.
#[inline]
pub fn hash_f32(x: u32) -> f32 {
    // 24 bits of mantissa is plenty and keeps the result exactly representable.
    (hash_u32(x) >> 8) as f32 / (1u32 << 24) as f32
}

/// Hashes two values together to a float in `[0, 1)`.
#[inline]
pub fn hash2_f32(a: u32, b: u32) -> f32 {
    hash_f32(a ^ hash_u32(b).wrapping_mul(0x9e37_79b9))
}

/// Smoothstep-interpolated 1D value noise. Period is `u32::MAX`, so in practice
/// it never repeats.
pub fn value_noise(x: f32, seed: u32) -> f32 {
    let i = x.floor();
    let f = x - i;
    // Quintic fade: continuous first *and* second derivative, so weather that
    // is driven by this doesn't visibly "kink" at cell boundaries.
    let u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    let i0 = i as i32 as u32;
    let a = hash2_f32(i0, seed);
    let b = hash2_f32(i0.wrapping_add(1), seed);
    a + (b - a) * u
}

/// Fractal Brownian motion over [`value_noise`], normalised to `[0, 1]`.
pub fn fbm(x: f32, octaves: u32, seed: u32) -> f32 {
    let mut sum = 0.0;
    let mut amp = 0.5;
    let mut freq = 1.0;
    let mut norm = 0.0;
    for o in 0..octaves.max(1) {
        sum += value_noise(x * freq, seed.wrapping_add(o * 7919)) * amp;
        norm += amp;
        amp *= 0.5;
        freq *= 2.0;
    }
    sum / norm
}

/// Remaps `x` from `[a, b]` to `[0, 1]`, clamped. Returns 0 if `a == b`.
#[inline]
pub fn remap01(x: f32, a: f32, b: f32) -> f32 {
    if (b - a).abs() < f32::EPSILON {
        0.0
    } else {
        ((x - a) / (b - a)).clamp(0.0, 1.0)
    }
}

/// Hermite smoothstep between two edges, clamped. Returns 0 if `a == b`.
#[inline]
pub fn smoothstep(a: f32, b: f32, x: f32) -> f32 {
    let t = remap01(x, a, b);
    t * t * (3.0 - 2.0 * t)
}

/// A soft-edged window on the 24-hour clock, wrapping across midnight.
///
/// `t`, `start` and `end` are all fractions of a day in `[0, 1)`. Returns `1.0`
/// well inside the window, `0.0` outside it, and a smooth ramp `feather` wide
/// at each edge. `start > end` is fine and means the window spans midnight,
/// which is exactly the case that a plain comparison gets wrong.
pub fn diurnal_window(t: f32, start: f32, end: f32, feather: f32) -> f32 {
    let width = (end - start).rem_euclid(1.0);
    if width <= 0.0 {
        return 0.0;
    }
    let into = (t - start).rem_euclid(1.0);
    let feather = feather.clamp(1e-4, width * 0.5);
    smoothstep(0.0, feather, into) * (1.0 - smoothstep(width - feather, width, into))
}

/// Frame-rate independent exponential approach.
///
/// `half_life` is the time in seconds for the remaining distance to halve.
/// A `half_life` of zero snaps.
#[inline]
pub fn damp(current: f32, target: f32, half_life: f32, dt: f32) -> f32 {
    if half_life <= 0.0 {
        return target;
    }
    let t = 1.0 - (-dt * core::f32::consts::LN_2 / half_life).exp();
    current + (target - current) * t
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn hash_is_in_unit_range() {
        for i in 0..10_000u32 {
            let v = hash_f32(i);
            assert!((0.0..1.0).contains(&v), "hash_f32({i}) = {v}");
        }
    }

    #[test]
    fn value_noise_is_continuous_across_cells() {
        // Sampling either side of an integer boundary must not jump.
        for cell in -50..50 {
            let x = cell as f32;
            let a = value_noise(x - 1e-4, 7);
            let b = value_noise(x + 1e-4, 7);
            assert!((a - b).abs() < 1e-3, "discontinuity at {x}: {a} vs {b}");
        }
    }

    #[test]
    fn value_noise_is_in_unit_range() {
        for i in 0..5_000 {
            let v = value_noise(i as f32 * 0.137, 3);
            assert!((0.0..=1.0).contains(&v));
        }
    }

    #[test]
    fn fbm_is_in_unit_range() {
        for i in 0..5_000 {
            let v = fbm(i as f32 * 0.061, 4, 11);
            assert!((0.0..=1.0).contains(&v), "fbm out of range: {v}");
        }
    }

    #[test]
    fn smoothstep_is_flat_at_both_ends() {
        assert_eq!(smoothstep(0.0, 1.0, -1.0), 0.0);
        assert_eq!(smoothstep(0.0, 1.0, 2.0), 1.0);
        assert!((smoothstep(0.0, 1.0, 0.5) - 0.5).abs() < 1e-6);
    }

    #[test]
    fn smoothstep_is_monotonic() {
        let mut previous = -1.0;
        for i in 0..=100 {
            let v = smoothstep(0.2, 0.8, i as f32 / 100.0);
            assert!(v >= previous - 1e-6);
            previous = v;
        }
    }

    #[test]
    fn diurnal_window_wraps_across_midnight() {
        // 21:00 to 10:00.
        let w = |t: f32| diurnal_window(t, 0.875, 0.417, 0.05);
        assert!(w(0.0) > 0.99, "midnight should be inside: {}", w(0.0));
        assert!(w(0.2) > 0.99, "05:00 should be inside: {}", w(0.2));
        assert_eq!(w(0.583), 0.0, "14:00 should be outside");
        assert_eq!(w(0.75), 0.0, "18:00 should be outside");
    }

    #[test]
    fn diurnal_window_edges_are_soft_and_bounded() {
        for i in 0..=1_000 {
            let t = i as f32 / 1_000.0;
            let v = diurnal_window(t, 0.875, 0.417, 0.05);
            assert!((0.0..=1.0).contains(&v), "window {v} out of range at {t}");
        }
    }

    #[test]
    fn a_zero_width_window_is_always_closed() {
        for i in 0..=100 {
            assert_eq!(diurnal_window(i as f32 / 100.0, 0.3, 0.3, 0.05), 0.0);
        }
    }

    #[test]
    fn damp_reaches_half_way_after_one_half_life() {
        let v = damp(0.0, 1.0, 1.0, 1.0);
        assert!((v - 0.5).abs() < 1e-5, "{v}");
    }

    #[test]
    fn damp_with_zero_half_life_snaps() {
        assert_eq!(damp(0.0, 1.0, 0.0, 0.016), 1.0);
    }
}
