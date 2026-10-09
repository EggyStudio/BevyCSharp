//! The clock everything else is a function of.

use bevy::app::{App, Plugin, Update};
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Res, ResMut};
use bevy::reflect::Reflect;
use bevy::time::Time;

use crate::WeatherSystems;
use bevy::ecs::reflect::ReflectResource;
use bevy::reflect::std_traits::ReflectDefault;

/// Length of the synodic (new-moon to new-moon) month, in days.
pub const SYNODIC_MONTH_DAYS: f32 = 29.530_588;

/// Days in a tropical year.
pub const DAYS_PER_YEAR: f32 = 365.242_2;

/// The in-game clock: time of day, calendar day, and how fast they advance.
///
/// The whole plugin is a pure function of this resource plus [`Weather`]. That
/// means you can scrub it, pause it, network it, or drive it from your own
/// clock and everything stays consistent.
///
/// [`Weather`]: crate::state::Weather
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct WeatherTime {
    /// Time of day in `[0, 1)`. `0.0` is local midnight, `0.25` sunrise-ish,
    /// `0.5` solar noon, `0.75` sunset-ish.
    ///
    /// This is the "time progression ratio": set it directly (with
    /// [`paused`](Self::paused) on) to drive the entire sky and weather system
    /// from your own timeline.
    pub time_of_day: f32,

    /// Whole days elapsed since the epoch. Drives seasons and moon phase.
    pub day: u32,

    /// Real seconds per in-game day. `120.0` gives a brisk two-minute cycle;
    /// `86_400.0` runs in real time.
    pub day_length_secs: f32,

    /// When true, [`time_of_day`](Self::time_of_day) and [`day`](Self::day) are
    /// left entirely to you.
    pub paused: bool,

    /// Observer latitude in degrees, `-90..=90`. Controls how steeply the sun
    /// arcs and how extreme the seasons are.
    pub latitude: f32,

    /// Planet axial tilt in degrees. Earth is `23.44`; `0.0` removes seasons.
    pub axial_tilt: f32,

    /// Day of the year (`0.0..DAYS_PER_YEAR`) that [`day`] `0` corresponds to.
    /// Use it to start the game in a particular season.
    ///
    /// [`day`]: Self::day
    pub year_offset: f32,

    /// Moon phase at [`day`] `0`, in `[0, 1)`. `0.0` is a new moon, `0.5` full.
    ///
    /// [`day`]: Self::day
    pub moon_phase_offset: f32,
}

impl Default for WeatherTime {
    fn default() -> Self {
        Self {
            time_of_day: 0.30,
            day: 0,
            day_length_secs: 300.0,
            paused: false,
            latitude: 45.0,
            axial_tilt: 23.44,
            year_offset: 172.0, // northern summer solstice
            moon_phase_offset: 0.5,
        }
    }
}

impl WeatherTime {
    /// Total elapsed days as a continuous value, `day + time_of_day`.
    ///
    /// `f64` because at long day counts an `f32` can no longer resolve a single
    /// minute, which would make the procedural weather stutter.
    #[inline]
    pub fn elapsed_days(&self) -> f64 {
        self.day as f64 + self.time_of_day as f64
    }

    /// Advances the clock by `days`, rolling [`time_of_day`] into [`day`].
    ///
    /// Negative values run the clock backwards.
    ///
    /// [`time_of_day`]: Self::time_of_day
    /// [`day`]: Self::day
    pub fn advance(&mut self, days: f32) {
        let t = self.time_of_day + days;
        let whole = t.floor();
        self.time_of_day = t - whole;
        // Saturating so running the clock backwards past the epoch parks at
        // day 0 rather than wrapping to ~4 billion.
        if whole >= 0.0 {
            self.day = self.day.saturating_add(whole as u32);
        } else {
            self.day = self.day.saturating_sub((-whole) as u32);
        }
    }

    /// Sets the clock from an hour in `[0, 24)`.
    pub fn set_hour(&mut self, hour: f32) {
        self.time_of_day = (hour / 24.0).rem_euclid(1.0);
    }

    /// Time of day expressed as an hour in `[0, 24)`.
    #[inline]
    pub fn hour(&self) -> f32 {
        self.time_of_day * 24.0
    }

    /// Position in the year, `[0, 1)`. `0.0` is the vernal equinox reference
    /// point used by [`solar_declination`](Self::solar_declination).
    ///
    /// Continuous, not stepped once a day. Everything seasonal is a function of
    /// this, so a version that only moved at midnight would take the sun's
    /// declination with it -- and the moon's, which is derived from it. Near an
    /// equinox that is nearly half a degree of jump, most of a moon's width,
    /// applied at an hour when the moon is very often the thing you are looking
    /// at.
    #[inline]
    pub fn year_fraction(&self) -> f32 {
        ((self.elapsed_days() + self.year_offset as f64) / DAYS_PER_YEAR as f64).rem_euclid(1.0)
            as f32
    }

    /// Sun declination in radians: how far north or south of the equator the
    /// sun is directly overhead today.
    pub fn solar_declination(&self) -> f32 {
        let tilt = self.axial_tilt.to_radians();
        // Day 0 of the year is ~10 days after the December solstice.
        let angle = core::f32::consts::TAU * (self.year_fraction() + 10.0 / DAYS_PER_YEAR);
        -tilt * angle.cos()
    }

    /// Moon phase in `[0, 1)`: `0.0` new, `0.25` first quarter, `0.5` full,
    /// `0.75` last quarter.
    pub fn moon_phase(&self) -> f32 {
        let d = self.elapsed_days() + self.moon_phase_offset as f64 * SYNODIC_MONTH_DAYS as f64;
        (d / SYNODIC_MONTH_DAYS as f64).rem_euclid(1.0) as f32
    }

    /// Fraction of the moon's disc that is lit, `0.0` new to `1.0` full.
    pub fn moon_illumination(&self) -> f32 {
        0.5 * (1.0 - (core::f32::consts::TAU * self.moon_phase()).cos())
    }

    /// Half the length of today's day, as a fraction of a full rotation.
    ///
    /// `None` when the sun does not cross the horizon at all today, which is
    /// what happens inside the polar circles: either it never sets or it never
    /// rises, and [`daylight_hours`](Self::daylight_hours) says which.
    fn half_day(&self) -> Option<f32> {
        let latitude = self.latitude.to_radians();
        // The sun is on the horizon where cos(H) = -tan(lat) tan(declination).
        // Outside [-1, 1] there is no such hour angle and the sun stays on one
        // side of the horizon all day.
        let cos_hour_angle = -latitude.tan() * self.solar_declination().tan();
        if !(-1.0..=1.0).contains(&cos_hour_angle) {
            return None;
        }
        Some(cos_hour_angle.acos() / core::f32::consts::TAU)
    }

    /// Hour of local sunrise today, or `None` during a polar day or night.
    ///
    /// Geometric: the moment the centre of the sun crosses the horizon, which
    /// is what the rest of this plugin renders. Almanacs quote a figure a few
    /// minutes earlier, because they allow for refraction lifting the sun and
    /// for its upper limb arriving before its centre.
    pub fn sunrise_hour(&self) -> Option<f32> {
        self.half_day().map(|half| 12.0 - half * 24.0)
    }

    /// Hour of local sunset today, or `None` during a polar day or night.
    ///
    /// See [`sunrise_hour`](Self::sunrise_hour) on what "sunset" means here.
    pub fn sunset_hour(&self) -> Option<f32> {
        self.half_day().map(|half| 12.0 + half * 24.0)
    }

    /// Hours between sunrise and sunset today, `0.0..=24.0`.
    ///
    /// Unlike [`sunrise_hour`](Self::sunrise_hour) this always has an answer:
    /// inside the polar circles it is `24.0` under the midnight sun and `0.0`
    /// through the polar night.
    pub fn daylight_hours(&self) -> f32 {
        match self.half_day() {
            Some(half) => half * 48.0,
            // No crossing: which side of the horizon the sun is stuck on
            // depends on whether the hemisphere is tilted toward it.
            None => {
                let lit = self.latitude.to_radians().signum() == self.solar_declination().signum();
                if lit { 24.0 } else { 0.0 }
            }
        }
    }

    /// True while the sun is above the horizon.
    pub fn is_daytime(&self) -> bool {
        match (self.sunrise_hour(), self.sunset_hour()) {
            (Some(rise), Some(set)) => {
                let hour = self.hour();
                hour >= rise && hour < set
            }
            _ => self.daylight_hours() > 12.0,
        }
    }
}

/// Advances [`WeatherTime`] from Bevy's [`Time`].
#[derive(Debug, Clone, Copy, Default)]
pub struct WeatherTimePlugin;

impl Plugin for WeatherTimePlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<WeatherTime>()
            .register_type::<WeatherTime>()
            .add_systems(Update, tick_weather_time.in_set(WeatherSystems::Tick));
    }
}

fn tick_weather_time(mut weather_time: ResMut<WeatherTime>, time: Res<Time>) {
    if weather_time.paused || weather_time.day_length_secs <= 0.0 {
        return;
    }
    let days = time.delta_secs() / weather_time.day_length_secs;
    weather_time.advance(days);
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn advance_rolls_over_into_days() {
        let mut t = WeatherTime {
            time_of_day: 0.9,
            day: 3,
            ..Default::default()
        };
        t.advance(0.2);
        assert_eq!(t.day, 4);
        assert!((t.time_of_day - 0.1).abs() < 1e-6, "{}", t.time_of_day);
    }

    #[test]
    fn advance_backwards_rolls_days_down() {
        let mut t = WeatherTime {
            time_of_day: 0.1,
            day: 3,
            ..Default::default()
        };
        t.advance(-0.2);
        assert_eq!(t.day, 2);
        assert!((t.time_of_day - 0.9).abs() < 1e-5, "{}", t.time_of_day);
    }

    #[test]
    fn advance_backwards_past_epoch_saturates() {
        let mut t = WeatherTime {
            time_of_day: 0.1,
            day: 0,
            ..Default::default()
        };
        t.advance(-5.0);
        assert_eq!(t.day, 0);
    }

    #[test]
    fn time_of_day_stays_in_unit_range() {
        let mut t = WeatherTime::default();
        for _ in 0..1_000 {
            t.advance(0.037);
            assert!((0.0..1.0).contains(&t.time_of_day));
        }
    }

    #[test]
    fn moon_illumination_tracks_phase() {
        let mut t = WeatherTime {
            day: 0,
            time_of_day: 0.0,
            moon_phase_offset: 0.0,
            ..Default::default()
        };
        assert!(t.moon_illumination() < 1e-4, "new moon should be dark");

        t.day = (SYNODIC_MONTH_DAYS / 2.0).round() as u32;
        assert!(
            t.moon_illumination() > 0.99,
            "half a synodic month later should be full, got {}",
            t.moon_illumination()
        );
    }

    #[test]
    fn the_seasons_do_not_lurch_at_midnight() {
        // Everything seasonal hangs off `year_fraction`, so if it only moved
        // once a day the sun's declination would step at midnight -- and the
        // moon's with it, since the moon's is derived from the sun's. Near an
        // equinox that is most of a moon's width, at an hour when the moon is
        // very often what you are looking at.
        let mut before = WeatherTime {
            day: 40,
            // An equinox, where the declination is changing fastest.
            year_offset: 80.0,
            ..Default::default()
        };
        before.set_hour(23.999);
        let mut after = before.clone();
        after.advance(0.002 / 24.0);

        let step = (after.solar_declination() - before.solar_declination())
            .abs()
            .to_degrees();
        // Two thousandths of an hour of real seasonal drift is about a
        // hundred-thousandth of a degree. A whole day's worth arriving at once
        // would be nearly half a degree.
        assert!(step < 0.001, "the sun's declination lurched {step} degrees");
    }

    #[test]
    fn year_fraction_advances_within_a_day() {
        let mut morning = WeatherTime::default();
        morning.set_hour(6.0);
        let mut evening = WeatherTime::default();
        evening.set_hour(18.0);
        assert!(evening.year_fraction() > morning.year_fraction());
    }

    #[test]
    fn declination_peaks_at_the_solstices() {
        // year_offset 172 is the northern summer solstice: declination ~ +tilt.
        let summer = WeatherTime {
            year_offset: 172.0,
            ..Default::default()
        };
        let winter = WeatherTime {
            year_offset: 172.0 + DAYS_PER_YEAR / 2.0,
            ..Default::default()
        };
        let tilt = 23.44f32.to_radians();
        assert!(
            (summer.solar_declination() - tilt).abs() < 0.05,
            "{}",
            summer.solar_declination()
        );
        assert!(
            (winter.solar_declination() + tilt).abs() < 0.05,
            "{}",
            winter.solar_declination()
        );
    }

    #[test]
    fn the_equinox_gives_everyone_a_twelve_hour_day() {
        // Declination zero puts the sun on the celestial equator, where it
        // rises due east and sets due west whatever the latitude.
        for latitude in [-60.0f32, -23.0, 0.0, 35.0, 62.0] {
            let t = WeatherTime {
                latitude,
                // A quarter year from the solstice offset the default uses.
                year_offset: 172.0 - DAYS_PER_YEAR / 4.0,
                ..Default::default()
            };
            let rise = t.sunrise_hour().expect("the sun rises at an equinox");
            let set = t.sunset_hour().expect("the sun sets at an equinox");
            assert!((rise - 6.0).abs() < 0.1, "sunrise at {latitude}: {rise}");
            assert!((set - 18.0).abs() < 0.1, "sunset at {latitude}: {set}");
            assert!((t.daylight_hours() - 12.0).abs() < 0.2);
        }
    }

    #[test]
    fn summer_days_are_longer_than_winter_ones() {
        let summer = WeatherTime {
            latitude: 51.5,
            year_offset: 172.0,
            ..Default::default()
        };
        let winter = WeatherTime {
            year_offset: 172.0 + DAYS_PER_YEAR / 2.0,
            ..summer.clone()
        };
        // London gets about sixteen and a half hours in June and eight in
        // December.
        assert!(
            (summer.daylight_hours() - 16.5).abs() < 0.6,
            "{}",
            summer.daylight_hours()
        );
        assert!(
            (winter.daylight_hours() - 7.9).abs() < 0.6,
            "{}",
            winter.daylight_hours()
        );
        // And the southern hemisphere has it the other way round.
        let antipodes = WeatherTime {
            latitude: -51.5,
            ..summer.clone()
        };
        assert!(antipodes.daylight_hours() < 9.0);
    }

    #[test]
    fn the_poles_get_a_midnight_sun_and_a_polar_night() {
        let midsummer = WeatherTime {
            latitude: 78.0,
            year_offset: 172.0,
            ..Default::default()
        };
        assert_eq!(midsummer.sunrise_hour(), None);
        assert_eq!(midsummer.daylight_hours(), 24.0);
        assert!(midsummer.is_daytime());

        let midwinter = WeatherTime {
            year_offset: 172.0 + DAYS_PER_YEAR / 2.0,
            ..midsummer.clone()
        };
        assert_eq!(midwinter.sunset_hour(), None);
        assert_eq!(midwinter.daylight_hours(), 0.0);
        assert!(!midwinter.is_daytime());

        // And the southern polar circle is in the opposite season.
        let southern = WeatherTime {
            latitude: -78.0,
            ..midsummer.clone()
        };
        assert_eq!(southern.daylight_hours(), 0.0);
    }

    #[test]
    fn daylight_agrees_with_where_the_sun_actually_is() {
        // The closed form and the geometry have to be the same answer, or the
        // sky and the clock disagree about when it is dark.
        let mut time = WeatherTime {
            latitude: 45.0,
            ..Default::default()
        };
        for step in 0..96 {
            time.set_hour(step as f32 * 0.25);
            let altitude = crate::celestial::compute_celestial(&time).sun_altitude;
            // Within a few minutes of the crossing the two can disagree on
            // rounding, so leave the boundary out of it.
            if altitude.abs() < 0.02 {
                continue;
            }
            assert_eq!(
                time.is_daytime(),
                altitude > 0.0,
                "at {:.2}h the sun is at {altitude:.3} but is_daytime said {}",
                time.hour(),
                time.is_daytime()
            );
        }
    }

    #[test]
    fn zero_tilt_removes_seasons() {
        for day in [0u32, 40, 90, 200, 300] {
            let t = WeatherTime {
                axial_tilt: 0.0,
                day,
                ..Default::default()
            };
            assert!(t.solar_declination().abs() < 1e-6);
        }
    }
}
