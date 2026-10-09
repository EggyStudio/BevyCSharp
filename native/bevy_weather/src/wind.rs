//! Wind, including gusts.
//!
//! One resource, sampled by the clouds, the fog scroll and the precipitation,
//! so everything blows the same way.

use bevy::app::{App, Plugin, Update};
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Res, ResMut};
use bevy::math::{Vec2, Vec3};
use bevy::reflect::Reflect;
use bevy::time::Time;

use crate::WeatherSystems;
use crate::math::fbm;
use crate::state::Weather;
use bevy::ecs::reflect::ReflectResource;
use bevy::reflect::std_traits::ReflectDefault;

/// The current wind, derived from [`Weather`] plus a gust simulation.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct Wind {
    /// Horizontal wind velocity in metres per second, gusts included.
    ///
    /// `x` is east, `y` is south (Bevy's `+Z`). Use [`velocity3`](Self::velocity3)
    /// for a world-space vector.
    pub velocity: Vec2,
    /// Steady wind velocity, without gusts.
    pub base_velocity: Vec2,
    /// Current gust multiplier around `1.0`.
    pub gust: f32,
    /// Cumulative wind displacement in metres. Clouds and fog scroll by this,
    /// so changing wind speed doesn't make them jump.
    pub offset: Vec2,
    /// Seed for the gust noise.
    pub seed: u32,
    /// Magnitude at which [`offset`](Self::offset) folds back toward zero, in
    /// world units, to keep shader-side `f32` precision usable over a long
    /// session.
    ///
    /// Folding is visible for one frame when it happens — the cloud field
    /// shifts — so this wants to be large enough that it happens rarely. At the
    /// default and a stiff breeze it is once every few hours.
    ///
    /// It also wants to be small enough that a single frame's wind movement is
    /// comfortably larger than the floating-point spacing there; see
    /// [`advance_offset`].
    pub offset_wrap: f32,
}

impl Default for Wind {
    fn default() -> Self {
        Self {
            velocity: Vec2::ZERO,
            base_velocity: Vec2::ZERO,
            gust: 1.0,
            offset: Vec2::ZERO,
            seed: 0x5EED_1234,
            offset_wrap: 1.0e5,
        }
    }
}

impl Wind {
    /// Wind velocity as a world-space vector on the horizontal plane.
    #[inline]
    pub fn velocity3(&self) -> Vec3 {
        Vec3::new(self.velocity.x, 0.0, self.velocity.y)
    }

    /// Wind speed in metres per second.
    #[inline]
    pub fn speed(&self) -> f32 {
        self.velocity.length()
    }

    /// Accumulated displacement as a world-space vector.
    #[inline]
    pub fn offset3(&self) -> Vec3 {
        Vec3::new(self.offset.x, 0.0, self.offset.y)
    }
}

/// Converts a compass bearing to a horizontal direction vector.
///
/// `bearing` is radians clockwise from north, matching
/// [`WeatherConditions::wind_direction`](crate::state::WeatherConditions::wind_direction).
/// North is `-Z` and east is `+X`.
#[inline]
pub fn bearing_to_vec2(bearing: f32) -> Vec2 {
    let (sin, cos) = bearing.sin_cos();
    Vec2::new(sin, -cos)
}

/// Simulates gusts and integrates the wind offset.
#[derive(Debug, Clone, Copy, Default)]
pub struct WindPlugin;

impl Plugin for WindPlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<Wind>()
            .register_type::<Wind>()
            .add_systems(Update, update_wind.in_set(WeatherSystems::Apply));
    }
}

fn update_wind(mut wind: ResMut<Wind>, weather: Res<Weather>, time: Res<Time>) {
    let conditions = weather.current;
    let direction = bearing_to_vec2(conditions.wind_direction);
    wind.base_velocity = direction * conditions.wind_speed;

    // Two octaves at different rates: a slow swell plus a sharper gust front.
    let t = time.elapsed_secs();
    let seed = wind.seed;
    let swell = fbm(t * 0.07, 3, seed);
    let gust_front = fbm(t * 0.45, 2, seed ^ 0x9E37_79B9);
    // Turbulence controls how far gusts stray from the mean. A calm day barely
    // varies; a squall can double the speed and briefly drop it by half.
    let amount = conditions.turbulence;
    let gust = 1.0 + amount * (0.7 * (swell * 2.0 - 1.0) + 0.5 * (gust_front * 2.0 - 1.0));
    wind.gust = gust.max(0.0);

    wind.velocity = wind.base_velocity * wind.gust;

    let wrap = wind.offset_wrap.max(1.0);
    wind.offset = advance_offset(wind.offset, wind.velocity, time.delta_secs(), wrap);
}

/// Integrates the wind displacement and folds it back toward zero.
///
/// # Why this is not `rem_euclid`
///
/// `rem_euclid` maps its result into `[0, wrap)`, which parks the value hard
/// against the boundary whenever the wind runs backwards: a displacement of
/// `-0.05` comes back as `wrap - 0.05`.
///
/// At a large wrap that is where floating point runs out. The spacing between
/// representable `f32` values near `1e6` is `0.0625`, and a single frame of
/// wind moves less than that, so `wrap - 0.05` rounds to exactly `wrap` — and
/// `wrap.rem_euclid(wrap)` is `0`. The next frame goes negative again and comes
/// back to `wrap`. The offset then alternates between `0` and `wrap` forever,
/// every single frame.
///
/// That offset is what the cloud shader advects its noise field by, so the
/// clouds alternate between two completely unrelated skies at frame rate. It
/// reads as violent flashing, and it is worst in light winds — precisely when
/// the sky should be calmest.
///
/// A signed remainder has no such boundary. Values near zero are left exactly
/// alone, and folding only happens after a full period has genuinely been
/// travelled, which lands the result near zero rather than on the edge.
pub fn advance_offset(offset: Vec2, velocity: Vec2, delta: f32, wrap: f32) -> Vec2 {
    let wrap = wrap.max(1.0);
    let moved = offset + velocity * delta;
    Vec2::new(fold(moved.x, wrap), fold(moved.y, wrap))
}

/// Folds `value` into `(-wrap, wrap)`, leaving anything already inside untouched.
#[inline]
fn fold(value: f32, wrap: f32) -> f32 {
    if value.is_finite() && value.abs() >= wrap {
        value % wrap
    } else if value.is_finite() {
        value
    } else {
        0.0
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn bearing_zero_points_north() {
        let v = bearing_to_vec2(0.0);
        assert!((v.x).abs() < 1e-6);
        assert!((v.y + 1.0).abs() < 1e-6, "north is -Z, got {v:?}");
    }

    #[test]
    fn bearing_ninety_degrees_points_east() {
        let v = bearing_to_vec2(core::f32::consts::FRAC_PI_2);
        assert!((v.x - 1.0).abs() < 1e-6, "{v:?}");
        assert!(v.y.abs() < 1e-6, "{v:?}");
    }

    #[test]
    fn bearings_are_unit_length() {
        for i in 0..64 {
            let b = i as f32 / 64.0 * core::f32::consts::TAU;
            assert!((bearing_to_vec2(b).length() - 1.0).abs() < 1e-6);
        }
    }

    #[test]
    fn a_backwards_wind_does_not_make_the_offset_oscillate() {
        // The bug this guards against: with `rem_euclid`, an offset sitting at
        // zero and a wind blowing in the negative direction lands just below
        // zero, comes back as `wrap`, and then rounds straight back to zero --
        // alternating every frame. The cloud field is advected by this, so the
        // sky flashes between two unrelated skies at frame rate.
        let wrap = 1.0e5;
        let velocity = Vec2::new(-3.0, -0.04);
        let mut offset = Vec2::ZERO;
        for frame in 0..600 {
            let next = advance_offset(offset, velocity, 1.0 / 60.0, wrap);
            let jump = (next - offset).length();
            assert!(
                jump < 1.0,
                "frame {frame}: offset jumped {jump} ({offset:?} -> {next:?})"
            );
            offset = next;
        }
    }

    #[test]
    fn the_offset_stays_bounded() {
        let wrap = 1.0e5;
        let mut offset = Vec2::ZERO;
        for _ in 0..20_000 {
            offset = advance_offset(offset, Vec2::new(40.0, -25.0), 1.0, wrap);
            assert!(offset.x.abs() < wrap && offset.y.abs() < wrap, "{offset:?}");
        }
    }

    #[test]
    fn small_offsets_are_left_exactly_alone() {
        // Nothing near zero should ever be rewritten; that is what kept
        // dragging the value onto the boundary.
        let wrap = 1.0e5;
        for value in [0.0f32, -1e-9, 1e-9, -0.05, 0.05, -500.0, 500.0] {
            let moved = advance_offset(Vec2::new(value, value), Vec2::ZERO, 1.0 / 60.0, wrap);
            assert_eq!(moved.x, value, "{value} was rewritten");
        }
    }

    #[test]
    fn folding_survives_a_wrap_crossing_without_a_second_jump() {
        // One discontinuity when it folds, and then it must settle rather than
        // bouncing off the boundary.
        let wrap = 1.0e3;
        let mut offset = Vec2::new(wrap - 0.5, 0.0);
        let mut jumps = 0;
        for _ in 0..400 {
            let next = advance_offset(offset, Vec2::new(6.0, 0.0), 1.0 / 60.0, wrap);
            if (next.x - offset.x).abs() > 1.0 {
                jumps += 1;
            }
            offset = next;
        }
        assert_eq!(jumps, 1, "expected exactly one fold, got {jumps}");
    }

    #[test]
    fn non_finite_velocity_cannot_poison_the_offset() {
        let moved = advance_offset(Vec2::ZERO, Vec2::splat(f32::INFINITY), 1.0, 1.0e5);
        assert!(moved.x.is_finite() && moved.y.is_finite(), "{moved:?}");
    }

    #[test]
    fn velocity3_lies_in_the_horizontal_plane() {
        let wind = Wind {
            velocity: Vec2::new(3.0, -4.0),
            ..Default::default()
        };
        assert_eq!(wind.velocity3(), Vec3::new(3.0, 0.0, -4.0));
        assert!((wind.speed() - 5.0).abs() < 1e-6);
    }
}
