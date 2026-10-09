//! Where the sun and moon are, and the lights that follow them.

use bevy::app::{App, Plugin, Update};
use bevy::color::{Color, LinearRgba, Mix};
use bevy::ecs::change_detection::DetectChanges;
use bevy::ecs::component::Component;
use bevy::ecs::message::{Message, MessageWriter};
use bevy::ecs::query::{With, Without};
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query, Res, ResMut};
use bevy::light::{
    AmbientLight, CascadeShadowConfig, CascadeShadowConfigBuilder, DirectionalLight, SunDisk,
    VolumetricLight, light_consts::lux,
};
use bevy::math::Vec3;
use bevy::prelude::Entity;
use bevy::reflect::Reflect;
use bevy::transform::components::Transform;

use crate::cloud_field::CloudField;
use crate::config::{WeatherCamera, WeatherConfig};
use crate::state::Weather;
use crate::time::WeatherTime;
use crate::{WeatherSystems, math::remap01};
use bevy::ecs::reflect::ReflectComponent;
use bevy::ecs::reflect::ReflectResource;
use bevy::reflect::std_traits::ReflectDefault;

/// Mean lunar orbital inclination to the ecliptic, in degrees.
const LUNAR_INCLINATION_DEG: f32 = 5.145;

/// Draconic month: the period of the moon's declination wobble, in days.
const DRACONIC_MONTH_DAYS: f32 = 27.212_22;

/// Angular radius of the sun and moon as seen from Earth, in radians (~0.26°).
pub const DEFAULT_ANGULAR_RADIUS: f32 = 0.004_65;

/// How the sun's disc is drawn.
///
/// The disc itself is rendered by Bevy's atmosphere pass from a [`SunDisk`]
/// component, so this is really just a convenient place to configure it
/// alongside everything else.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct SunConfig {
    /// Draw a visible disc at all. With this off you still get sunlight and
    /// atmospheric scattering, just no disc.
    pub disk: bool,

    /// Angular *radius* of the disc, in radians.
    ///
    /// The real sun is `0.00465` -- the same half a degree as the moon, which
    /// is why a total eclipse works at all. As with the moon, the default is
    /// enlarged, because a physically-sized sun on a monitor is a speck.
    ///
    /// Note that Bevy's [`SunDisk`] is specified as a *diameter*; this is
    /// halved for you.
    pub angular_radius: f32,

    /// Brightness multiplier for the disc. `1.0` is physically correct.
    ///
    /// The sun's true radiance is several orders of magnitude past anything
    /// else on screen, so this mostly controls how far the glare bleeds once
    /// bloom gets hold of it.
    pub disk_intensity: f32,

    /// How much of the sun's atmospheric reddening to put on the
    /// [`DirectionalLight`] itself, `0.0..=1.0`.
    ///
    /// This one number decides whether sunsets look right, so it is worth
    /// knowing what it does.
    ///
    /// Bevy's atmosphere is a Bruneton model: it integrates the transmittance
    /// from the sun to every point along the view ray itself, and reddening the
    /// sky is precisely what that integral is *for*. It expects to be handed
    /// the raw solar spectrum -- unfiltered white -- exactly as it expects to be
    /// handed [`lux::RAW_SUNLIGHT`] rather than the 100 klx that reaches the
    /// ground.
    ///
    /// Give it a pre-reddened light instead and the extinction is applied
    /// twice, which does not simply deepen the colour: Rayleigh scattering is
    /// about six times stronger in blue than in red, so scattering an already
    /// orange sun leaves the sky a muddy olive grey instead of orange. The
    /// spectacular part of a sunset disappears and what is left looks like
    /// pollution.
    ///
    /// The catch is that the same light also lights your geometry, and Bevy
    /// applies no transmittance to *that* -- so a fully neutral light means
    /// objects stand in white sunlight at dusk while the sky behind them burns.
    /// This splits the difference: enough warmth on the key light for a golden
    /// hour, little enough that the sky is still the atmosphere's to colour.
    /// `0.0` is the physically correct input to the atmosphere; `1.0` restores
    /// the old double-counted behaviour.
    pub light_tint: f32,

    /// How far from the camera the sun casts shadows, in world units.
    ///
    /// Bevy's default is 150, which is a reasonable figure for an indoor or
    /// arena-sized scene and much too short for anything with a horizon: the
    /// shadows simply stop part way across the ground and everything beyond
    /// stands in full sun.
    pub shadow_distance: f32,

    /// Far bound of the first shadow cascade, in world units.
    ///
    /// Cascades are the reason shadows look sharp near the camera and soft far
    /// away: each covers a shell of distance at its own resolution. The seams
    /// between them are blended for surfaces, but *not* inside the volumetric
    /// fog pass, which picks one cascade per sample -- so wherever a boundary
    /// falls, a god ray changes character across a line drawn at that exact
    /// distance, and the line follows the camera around.
    ///
    /// Bevy's default puts the first one ten metres out, which is close enough
    /// to read as a ring on the ground in front of the player. Pushing it out
    /// costs some crispness in the shadows right at your feet and moves the
    /// artefact somewhere it is not being stared at.
    pub shadow_near_distance: f32,

    /// How many shadow cascades the sun uses.
    ///
    /// Four is the usual outdoor choice. Fewer is cheaper and blockier; more
    /// costs another shadow map render per cascade.
    pub shadow_cascades: usize,

    /// How much neighbouring cascades overlap, `0.0..1.0`.
    ///
    /// The overlap is the band the renderer cross-fades across, so a larger
    /// value hides the seam better at the cost of some resolution.
    pub shadow_cascade_overlap: f32,
}

impl Default for SunConfig {
    fn default() -> Self {
        Self {
            disk: true,
            angular_radius: 0.0105,
            disk_intensity: 1.0,
            light_tint: 0.35,
            shadow_distance: 900.0,
            shadow_near_distance: 40.0,
            shadow_cascades: 4,
            shadow_cascade_overlap: 0.3,
        }
    }
}

/// The moonlight: how much of it there is, and whether it casts shadows.
///
/// Separate from [`MoonConfig`](crate::sky::MoonConfig), which is about how the
/// moon *looks*. This is about what it does to the scene below.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct MoonLightConfig {
    /// Illuminance of a full moon at the zenith, in lux.
    ///
    /// The true figure is about a quarter of a lux, which at the daylight
    /// exposure this plugin targets is indistinguishable from black. This is
    /// the conventional "day for night" lift, and it is the same exaggeration
    /// the moon's own disc and the star field already carry.
    pub illuminance: f32,

    /// Colour of moonlight.
    ///
    /// Moonlight is very slightly *warmer* than sunlight in reality -- it is
    /// sunlight off a grey rock. It looks blue because the eye's colour vision
    /// gives out at those levels and the rods take over, and every film ever
    /// shot day-for-night has taught everyone to expect it, so blue it is.
    pub color: Color,

    /// Cast shadows from the moon.
    ///
    /// A full moon really does throw a shadow you can see, and it is one of the
    /// things that makes a night scene read as lit rather than as merely dark.
    /// It costs a second set of cascaded shadow maps, so it is switched off
    /// automatically whenever the moon is down, new, or washed out by daylight.
    pub shadows: bool,
}

impl Default for MoonLightConfig {
    fn default() -> Self {
        Self {
            illuminance: MOONLIGHT_ILLUMINANCE,
            color: Color::srgb(0.72, 0.80, 1.0),
            shadows: true,
        }
    }
}

/// Sine of the solar altitude at the end of civil twilight, about -6 degrees.
///
/// The conventional line between "dusk" and "dark": bright enough to make out
/// the horizon and the first stars, and the point at which outdoor work stops
/// and street lights come on.
const CIVIL_TWILIGHT: f32 = -0.1045;

/// Which part of the day/night cycle it is.
///
/// Read the resource for the current phase, or listen for
/// [`DayPhaseChanged`] to act on the transitions -- lighting street lamps,
/// closing shops, waking whatever comes out after dark.
#[derive(Resource, Debug, Clone, Copy, Default, PartialEq, Eq, Hash, Reflect)]
#[reflect(Resource, Default)]
pub enum DayPhase {
    /// The sun is up.
    #[default]
    Day,
    /// The sun has set but it is not dark yet.
    Dusk,
    /// Past civil twilight: dark, and the stars are out.
    Night,
    /// Getting light, but the sun has not risen.
    Dawn,
}

impl DayPhase {
    /// Every phase, in the order they occur.
    pub const ALL: [DayPhase; 4] = [
        DayPhase::Dawn,
        DayPhase::Day,
        DayPhase::Dusk,
        DayPhase::Night,
    ];

    /// A short lowercase name, for debug overlays.
    pub const fn name(self) -> &'static str {
        match self {
            DayPhase::Day => "day",
            DayPhase::Dusk => "dusk",
            DayPhase::Night => "night",
            DayPhase::Dawn => "dawn",
        }
    }

    /// True for [`Dusk`](Self::Dusk) and [`Dawn`](Self::Dawn).
    pub const fn is_twilight(self) -> bool {
        matches!(self, DayPhase::Dusk | DayPhase::Dawn)
    }

    /// True for anything but [`Day`](Self::Day) -- when artificial light is
    /// wanted.
    pub const fn is_dark(self) -> bool {
        !matches!(self, DayPhase::Day)
    }
}

/// Sent whenever [`DayPhase`] changes.
#[derive(Message, Debug, Clone, Copy, PartialEq, Eq)]
pub struct DayPhaseChanged {
    /// The phase that just ended.
    pub from: DayPhase,
    /// The phase now in effect.
    pub to: DayPhase,
}

/// Classifies the time of day from the sun's altitude.
///
/// `sun_altitude` is the sine of the elevation, matching
/// [`CelestialBodies::sun_altitude`]. `rising` distinguishes the two twilights,
/// which are otherwise identical: the sun is at the same height at dawn and at
/// dusk, and only the direction it is travelling says which.
pub fn day_phase(sun_altitude: f32, rising: bool) -> DayPhase {
    if sun_altitude > 0.0 {
        DayPhase::Day
    } else if sun_altitude < CIVIL_TWILIGHT {
        DayPhase::Night
    } else if rising {
        DayPhase::Dawn
    } else {
        DayPhase::Dusk
    }
}

/// Marks the directional light the plugin steers as the sun.
///
/// Spawned automatically when [`WeatherConfig::celestial_lights`] is on. Add it
/// to your own light instead if you want to control its other properties.
#[derive(Component, Debug, Clone, Copy, Default, Reflect)]
#[reflect(Component, Default)]
pub struct SunLight;

/// Marks the directional light the plugin steers as the moon.
#[derive(Component, Debug, Clone, Copy, Default, Reflect)]
#[reflect(Component, Default)]
pub struct MoonLight;

/// Sun and moon geometry for the current frame, in world space.
///
/// Read this instead of recomputing: the sky shader, the fog, the particles and
/// the lights all share these values, so anything you derive from them stays in
/// step.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct CelestialBodies {
    /// Unit vector pointing *from the observer toward* the sun.
    pub sun_direction: Vec3,
    /// Unit vector pointing *from the observer toward* the moon.
    pub moon_direction: Vec3,
    /// Sine of the sun's altitude: `1.0` at the zenith, `0.0` on the horizon,
    /// negative below it.
    pub sun_altitude: f32,
    /// Sine of the moon's altitude.
    pub moon_altitude: f32,
    /// Moon phase in `[0, 1)`; `0.0` new, `0.5` full.
    pub moon_phase: f32,
    /// Lit fraction of the moon's disc, `0.0..=1.0`.
    pub moon_illumination: f32,
    /// `0.0` at night through `1.0` in full day, with a smooth twilight ramp.
    /// Handy for cross-fading your own day/night content.
    pub daylight: f32,
    /// Colour the sun is tinted to right now, reddening near the horizon.
    pub sun_color: LinearRgba,
    /// Angular radius of the sun's disc, in radians.
    pub sun_angular_radius: f32,
    /// Angular radius of the moon's disc, in radians.
    pub moon_angular_radius: f32,
}

impl Default for CelestialBodies {
    fn default() -> Self {
        Self {
            sun_direction: Vec3::Y,
            moon_direction: Vec3::NEG_Y,
            sun_altitude: 1.0,
            moon_altitude: -1.0,
            moon_phase: 0.5,
            moon_illumination: 1.0,
            daylight: 1.0,
            sun_color: LinearRgba::WHITE,
            sun_angular_radius: DEFAULT_ANGULAR_RADIUS,
            moon_angular_radius: DEFAULT_ANGULAR_RADIUS,
        }
    }
}

/// Converts an equatorial position to a world-space direction.
///
/// `hour_angle` is zero when the body crosses the meridian and advances with
/// the day; `declination` is its angle north of the celestial equator. The
/// result uses Bevy's convention of `+Y` up, `+X` east and `-Z` north.
pub fn equatorial_to_world(hour_angle: f32, declination: f32, latitude_rad: f32) -> Vec3 {
    let (sin_h, cos_h) = hour_angle.sin_cos();
    let (sin_d, cos_d) = declination.sin_cos();
    let (sin_lat, cos_lat) = latitude_rad.sin_cos();

    let east = -cos_d * sin_h;
    let up = sin_lat * sin_d + cos_lat * cos_d * cos_h;
    let north = cos_lat * sin_d - sin_lat * cos_d * cos_h;

    // `-Z` is north in Bevy's right-handed, Y-up world.
    Vec3::new(east, up, -north).normalize_or(Vec3::Y)
}

/// Computes [`CelestialBodies`] for a given clock. Pure, so it is easy to test
/// and to reuse outside the schedule.
pub fn compute_celestial(time: &WeatherTime) -> CelestialBodies {
    let latitude = time.latitude.to_radians();
    let sun_declination = time.solar_declination();
    // Hour angle is zero at solar noon (`time_of_day == 0.5`).
    let sun_hour_angle = (time.time_of_day - 0.5) * core::f32::consts::TAU;

    let sun_direction = equatorial_to_world(sun_hour_angle, sun_declination, latitude);

    let phase = time.moon_phase();
    // The moon trails the sun by a full turn over one synodic month, which is
    // exactly what makes a new moon rise with the sun and a full moon oppose it.
    let moon_hour_angle = sun_hour_angle - phase * core::f32::consts::TAU;
    // A full moon sits opposite the sun in declination too, which is why full
    // moons ride high in winter and low in summer.
    let mut moon_declination = sun_declination * (phase * core::f32::consts::TAU).cos();
    // The moon's orbit is tilted off the ecliptic; this wobble is what makes
    // eclipses rare.
    moon_declination += LUNAR_INCLINATION_DEG.to_radians()
        * (core::f32::consts::TAU * (time.elapsed_days() / DRACONIC_MONTH_DAYS as f64) as f32)
            .sin();

    let moon_direction = equatorial_to_world(moon_hour_angle, moon_declination, latitude);

    let sun_altitude = sun_direction.y;
    // Civil twilight is about -6 degrees; ramp across that band so the
    // transition is not a hard switch at the geometric horizon.
    let daylight = remap01(sun_altitude, -0.105, 0.10);

    CelestialBodies {
        sun_direction,
        moon_direction,
        sun_altitude,
        moon_altitude: moon_direction.y,
        moon_phase: phase,
        moon_illumination: time.moon_illumination(),
        daylight,
        sun_color: sun_tint(sun_altitude, 1.0),
        sun_angular_radius: DEFAULT_ANGULAR_RADIUS,
        moon_angular_radius: DEFAULT_ANGULAR_RADIUS,
    }
}

/// Approximates the colour of direct sunlight at a given altitude.
///
/// This is a cheap stand-in for integrating transmittance along the view ray;
/// the atmosphere pass does the real thing for the sky, but the clouds and the
/// particles need a sun colour before that pass runs.
///
/// `strength` scales the optical depth: `1.0` is the full ground-level colour,
/// `0.0` is the unfiltered solar spectrum. See
/// [`SunConfig::light_tint`] for why the light and the sky want different
/// values.
pub fn sun_tint(sun_altitude: f32, strength: f32) -> LinearRgba {
    // Air mass grows sharply as the sun approaches the horizon.
    let h = sun_altitude.max(0.0);
    let air_mass = 1.0 / (h + 0.05);
    // Rayleigh optical depth is roughly proportional to lambda^-4, so blue is
    // extinguished several times faster than red.
    let tau = Vec3::new(0.021, 0.052, 0.128) * air_mass * strength.max(0.0);
    LinearRgba::rgb((-tau.x).exp(), (-tau.y).exp(), (-tau.z).exp())
}

/// Keeps [`CelestialBodies`] and the sun/moon lights in sync with the clock.
#[derive(Debug, Clone, Copy, Default)]
pub struct CelestialPlugin;

impl Plugin for CelestialPlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<CelestialBodies>()
            .init_resource::<SunConfig>()
            .init_resource::<MoonLightConfig>()
            .init_resource::<DayPhase>()
            .add_message::<DayPhaseChanged>()
            .register_type::<CelestialBodies>()
            .register_type::<SunConfig>()
            .register_type::<MoonLightConfig>()
            .register_type::<DayPhase>()
            .register_type::<SunLight>()
            .register_type::<MoonLight>()
            .add_systems(
                Update,
                (
                    update_celestial_bodies,
                    update_day_phase,
                    spawn_lights,
                    drive_lights,
                )
                    .chain()
                    .in_set(WeatherSystems::Apply),
            );
    }
}

fn update_celestial_bodies(mut bodies: ResMut<CelestialBodies>, time: Res<WeatherTime>) {
    *bodies = compute_celestial(&time);
}

fn update_day_phase(
    mut phase: ResMut<DayPhase>,
    mut changes: MessageWriter<DayPhaseChanged>,
    bodies: Res<CelestialBodies>,
    time: Res<WeatherTime>,
) {
    // Solar noon is the middle of the day by construction, so anything before
    // it is on the way up.
    let rising = time.time_of_day < 0.5;
    let next = day_phase(bodies.sun_altitude, rising);
    if next != *phase {
        changes.write(DayPhaseChanged {
            from: *phase,
            to: next,
        });
        *phase = next;
    }
}

fn spawn_lights(
    mut commands: Commands,
    config: Res<WeatherConfig>,
    sun_config: Res<SunConfig>,
    moon_config: Res<MoonLightConfig>,
    suns: Query<(), With<SunLight>>,
    moons: Query<(), With<MoonLight>>,
) {
    if !config.celestial_lights {
        return;
    }
    if suns.is_empty() {
        commands.spawn((
            SunLight,
            DirectionalLight {
                // `RAW_SUNLIGHT` is sunlight *before* the atmosphere filters it,
                // which is what Bevy's atmosphere expects as input.
                illuminance: lux::RAW_SUNLIGHT,
                shadow_maps_enabled: true,
                ..Default::default()
            },
            VolumetricLight,
            sun_disk(&sun_config, 1.0),
            sun_config.cascades(),
            Transform::default(),
        ));
    }
    if moons.is_empty() {
        commands.spawn((
            MoonLight,
            DirectionalLight {
                illuminance: 0.0,
                shadow_maps_enabled: false,
                color: moon_config.color,
                ..Default::default()
            },
            VolumetricLight,
            // The moon shares the sun's cascade layout: how far shadows reach
            // is a property of the scene, not of which body is casting them.
            sun_config.cascades(),
            // Not optional. Bevy's atmosphere draws a sun disc for every
            // directional light, and a light with no `SunDisk` component falls
            // back to `SunDisk::EARTH` -- so the moon light gets a second,
            // blazing white disc painted at the moon's exact position, sitting
            // on top of the real moon and hiding its phase completely. The
            // moon's own disc is drawn by the sky shader.
            SunDisk::OFF,
            Transform::default(),
        ));
    }
}

impl SunConfig {
    /// The cascade layout this configuration asks for.
    ///
    /// Clamped rather than asserted: `CascadeShadowConfigBuilder::build` panics
    /// on out-of-range values, and a settings slider dragged to zero should not
    /// take the game down.
    pub fn cascades(&self) -> CascadeShadowConfig {
        let near = self.shadow_near_distance.max(1.0);
        CascadeShadowConfigBuilder {
            num_cascades: self.shadow_cascades.clamp(1, 4),
            minimum_distance: 0.1,
            first_cascade_far_bound: near,
            maximum_distance: self.shadow_distance.max(near + 1.0),
            overlap_proportion: self.shadow_cascade_overlap.clamp(0.0, 0.9),
        }
        .build()
    }
}

/// The [`SunDisk`] a [`SunConfig`] asks for, dimmed by `transmittance`.
fn sun_disk(config: &SunConfig, transmittance: f32) -> SunDisk {
    let intensity = config.disk_intensity * transmittance.clamp(0.0, 1.0);
    if !config.disk || intensity <= 1e-4 {
        return SunDisk::OFF;
    }
    SunDisk {
        // Bevy measures the disc as a diameter.
        angular_size: config.angular_radius.max(0.0) * 2.0,
        intensity,
    }
}

/// How much of the sun's disc survives the cloud directly up-sun of the camera.
///
/// The disc is drawn by the atmosphere pass, which runs *before* the cloud
/// layer and knows nothing about it, so the clouds have to hide it by drawing
/// over it -- and they cannot. The disc's radiance is four orders of magnitude
/// past anything else in the frame, so even a deck the raymarch has taken to
/// 99% opacity leaves a residual a hundred times brighter than the cloud it is
/// shining through. Chasing the last percent of opacity does not help; the
/// disc has to be put out at the source.
///
/// So it is, by asking the cloud field itself. One sample of the column the
/// sunlight actually came down, at the point where the ray from the camera
/// crosses the middle of the deck, and Beer-Lambert on the result. The sun goes
/// out behind a cumulus and comes back in the gap behind it, at the moment the
/// cloud overhead says it should, rather than being dimmed by an average of a
/// sky that is half clear.
fn sun_disk_transmittance(field: &CloudField, eye_metres: Vec3, sun: Vec3, extinction: f32) -> f32 {
    if field.coverage <= 0.001 {
        return 1.0;
    }
    let deck = field.base_altitude + field.thickness * 0.5;
    if eye_metres.y >= deck {
        // Above the deck, looking up at clear sky.
        return 1.0;
    }
    // A sun this low is being extinguished by the air rather than by cloud, and
    // the crossing point runs away to the horizon.
    let elevation = sun.y;
    if elevation <= 0.02 {
        return 1.0;
    }
    let travel = (deck - eye_metres.y) / elevation;
    let hit = eye_metres + sun * travel;
    (-field.column_density(hit.x, hit.z) * extinction.max(1e-6)).exp()
}

/// Illuminance of a full moon, exaggerated well past the true ~0.3 lux so that
/// nights are legible at a daylight exposure.
///
/// Balanced against [`NIGHT_AMBIENT_LUX`] rather than chosen on its own. What
/// makes a moonlit night read as *lit* is the ratio between the two: moonlight
/// is one small, hard source and the skyglow around it is faint, which is why a
/// full moon throws a shadow you can read by. Set the two close together and
/// the night is uniformly grey with no shadows in it at all.
const MOONLIGHT_ILLUMINANCE: f32 = 4_000.0;

/// Solar altitude, as a sine, below which the sun stops rendering shadows.
///
/// The atmosphere has already extinguished it completely by this point, so the
/// shadow maps would be drawn for a light that reaches nothing.
const SHADOW_CUTOFF_ALTITUDE: f32 = -0.02;

/// Diffuse illuminance, in lux, added by a fully overcast sky in daylight.
///
/// Bevy's atmosphere drives image-based ambient light from the *clear* sky, so
/// it has no idea our cloud deck exists. Under heavy cloud almost all the light
/// reaching the ground is this diffuse bounce, and without it an overcast scene
/// collapses to black once the direct sun is attenuated.
const OVERCAST_AMBIENT_LUX: f32 = 13_000.0;

/// Diffuse illuminance, in lux, on a clear night.
///
/// Far above the real value. A true moonlit night is a fraction of a lux, which
/// at the daylight exposure this plugin targets is indistinguishable from
/// black; this is the conventional "day for night" lift.
///
/// Deliberately well below [`MOONLIGHT_ILLUMINANCE`]; see the note there.
const NIGHT_AMBIENT_LUX: f32 = 1_100.0;

/// The sun light, excluding the moon so the two can be queried together.
type SunQuery<'w, 's> = Query<
    'w,
    's,
    (
        Entity,
        &'static mut Transform,
        &'static mut DirectionalLight,
        Option<&'static SunDisk>,
    ),
    (With<SunLight>, Without<MoonLight>),
>;

/// The moon light, excluding the sun.
type MoonQuery<'w, 's> = Query<
    'w,
    's,
    (
        Entity,
        &'static mut Transform,
        &'static mut DirectionalLight,
    ),
    (With<MoonLight>, Without<SunLight>),
>;

#[expect(
    clippy::too_many_arguments,
    reason = "a Bevy system steering both lights and the ambient term"
)]
fn drive_lights(
    mut commands: Commands,
    bodies: Res<CelestialBodies>,
    config: Res<WeatherConfig>,
    sun_config: Res<SunConfig>,
    moon_config: Res<MoonLightConfig>,
    weather: Res<Weather>,
    clouds: Res<crate::clouds::CloudConfig>,
    wind: Res<crate::wind::Wind>,
    time: Res<bevy::time::Time>,
    cameras: Query<(Entity, &bevy::transform::components::GlobalTransform), With<WeatherCamera>>,
    mut sun: SunQuery,
    mut moon: MoonQuery,
) {
    if !config.celestial_lights {
        return;
    }

    let cloudiness = weather.current.cloud_coverage * weather.current.cloud_density;
    // How much light survives the deck depends on how far down through it the
    // light has to come, not just on how much of the sky it covers. A seven
    // hundred metre stratus deck is a grey day; a four kilometre cumulonimbus
    // is dark enough to turn the streetlights on, and without this term the two
    // render identically.
    let depth = remap01(weather.current.cloud_thickness, 600.0, 4_000.0);
    let opacity = cloudiness * (0.55 + 0.45 * depth);
    // Thick cloud turns direct sunlight into diffuse skylight rather than
    // destroying it, so what it takes off the directional light is put back on
    // the ambient term below.
    let overcast = 1.0 - 0.95 * opacity;
    let scale = config.light_intensity_scale.max(0.0);

    // One sample of the cloud column the sun is shining down, so the disc can
    // be put out by the cloud that is actually in front of it.
    let field = CloudField::new(
        &weather.current,
        &clouds,
        wind.offset,
        time.elapsed_secs_wrapped(),
    );
    let metres_to_world = config.units_per_meter.max(1e-6);
    let eye = cameras
        .iter()
        .next()
        .map(|(_, transform)| transform.translation() / metres_to_world)
        .unwrap_or(Vec3::ZERO);
    let disk_transmittance = if config.clouds {
        sun_disk_transmittance(&field, eye, bodies.sun_direction, clouds.extinction)
    } else {
        1.0
    };

    let disk = sun_disk(&sun_config, disk_transmittance);
    for (entity, mut transform, mut light, existing_disk) in &mut sun {
        // A directional light shines along its local `-Z`, so it must look
        // *away* from the body it represents.
        //
        // Rotation only. The light's translation and scale mean nothing to the
        // lighting itself, but they position and size the cloud-shadow cookie,
        // so overwriting the whole transform here would fight
        // [`cloud_shadows`](crate::cloud_shadows) every frame -- and it would
        // also stamp on anyone who has parented the light into their own rig.
        if bodies.sun_direction.y > -0.999 {
            transform.rotation = Transform::default()
                .looking_to(-bodies.sun_direction, Vec3::Y)
                .rotation;
        }
        // How much of the sun to hand the renderer.
        //
        // The obvious thing is to cut this to zero the moment the sun sets, and
        // it is wrong, because the same number drives the *sky*. Bevy's
        // atmosphere computes its inscattering as the light's colour times a
        // scattering factor, so zeroing the light at the horizon does not end
        // the day -- it switches twilight off. The sky went from a sunset to
        // black in about fifteen minutes, with nothing in between.
        //
        // Nor is the cut needed to stop the sun lighting the scene from
        // underneath the world. Bevy already multiplies every directional
        // light's contribution to a surface by the atmospheric transmittance
        // toward it and by how much of its disc clears the horizon, so a sun
        // below the horizon reaches no geometry on its own account.
        //
        // What the ramp is really for is everything that does *not* go through
        // that path: `DistanceFog`'s directional scattering and the volumetric
        // fog both read the light's colour raw, and would happily light the air
        // from a sun that set an hour ago. So it stays -- but stretched across
        // real twilight instead of a degree, and squared, so the glow persists
        // through the part of dusk that has one and is gone by the part that
        // does not.
        let visibility = if config.atmosphere {
            let ramp = remap01(bodies.sun_altitude, -0.22, 0.02);
            ramp * ramp
        } else {
            // With no atmosphere pass there is nothing to extinguish the light,
            // so the hard cut is all there is.
            remap01(bodies.sun_altitude, -0.02, 0.06)
        };
        light.illuminance = lux::RAW_SUNLIGHT * visibility * overcast * scale;
        // Shadow maps for a light the atmosphere has already put out are pure
        // cost, so they stop at the horizon even though the light does not.
        light.shadow_maps_enabled = bodies.sun_altitude > SHADOW_CUTOFF_ALTITUDE;
        // Deliberately *not* `bodies.sun_color`. That is the colour sunlight
        // has by the time it reaches the ground, which is what this plugin's
        // own shaders want -- they composite outside the atmosphere pass and
        // have to redden their own input. The atmosphere does its own
        // extinction and wants the raw spectrum; see `SunConfig::light_tint`.
        light.color = Color::LinearRgba(sun_tint(bodies.sun_altitude, sun_config.light_tint));

        // The disc now tracks the cloud overhead, so it is rewritten whenever
        // that has moved rather than only when the config changes.
        let moved = existing_disk.is_none_or(|existing| {
            (existing.intensity - disk.intensity).abs() > 1e-4
                || (existing.angular_size - disk.angular_size).abs() > 1e-6
        });
        if moved {
            commands.entity(entity).insert(disk.clone());
        }
        if existing_disk.is_none() || sun_config.is_changed() {
            commands.entity(entity).insert(sun_config.cascades());
        }
    }

    for (entity, mut transform, mut light) in &mut moon {
        if bodies.moon_direction.y > -0.999 {
            transform.rotation = Transform::default()
                .looking_to(-bodies.moon_direction, Vec3::Y)
                .rotation;
        }
        let visibility = remap01(bodies.moon_altitude, -0.02, 0.06);
        // Moonlight only reads once the sun is out of the way.
        let night = 1.0 - bodies.daylight;
        light.color = moon_config.color;
        light.illuminance = moon_config.illuminance.max(0.0)
            * bodies.moon_illumination
            * visibility
            * night
            * overcast
            * scale;
        // A second set of cascades is not worth rendering for a light that is
        // contributing nothing, which is most of the time: the moon is down, or
        // new, or the sun is up.
        light.shadow_maps_enabled = moon_config.shadows && light.illuminance > 1.0;
        if moon_config.is_changed() || sun_config.is_changed() {
            commands.entity(entity).insert(sun_config.cascades());
        }
    }

    // The diffuse term the atmosphere's environment map cannot know about:
    // light bounced around by the cloud deck, plus a night-time floor.
    let night = 1.0 - bodies.daylight;
    // The same depth term: light that has been through four kilometres of
    // cloud arrives diffuse *and* greatly reduced.
    let overcast_ambient =
        OVERCAST_AMBIENT_LUX * cloudiness * bodies.daylight * (1.0 - 0.6 * depth);
    // Most of what little light a moonless night has is airglow and starlight,
    // so the floor does not scale all the way down with the moon -- but a full
    // moon does brighten the whole sky, so some of it does.
    let night_ambient = NIGHT_AMBIENT_LUX * night * (0.35 + 0.65 * bodies.moon_illumination);
    let brightness = (overcast_ambient + night_ambient) * scale;
    // Overcast light is grey; night is blue, because what little there is has
    // been scattered by the atmosphere.
    let color = Color::LinearRgba(
        LinearRgba::rgb(0.85, 0.90, 1.0).mix(&LinearRgba::WHITE, bodies.daylight),
    );

    for (entity, _) in &cameras {
        commands.entity(entity).insert(AmbientLight {
            color,
            brightness,
            affects_lightmapped_meshes: true,
        });
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::time::{DAYS_PER_YEAR, SYNODIC_MONTH_DAYS};

    fn at(hour: f32) -> WeatherTime {
        let mut t = WeatherTime {
            latitude: 45.0,
            axial_tilt: 0.0,
            year_offset: 0.0,
            moon_phase_offset: 0.0,
            day: 0,
            ..Default::default()
        };
        t.set_hour(hour);
        t
    }

    #[test]
    fn sun_is_highest_at_noon_and_lowest_at_midnight() {
        assert!(compute_celestial(&at(12.0)).sun_altitude > 0.7);
        assert!(compute_celestial(&at(0.0)).sun_altitude < -0.7);
    }

    #[test]
    fn sun_rises_in_the_east_and_sets_in_the_west() {
        // At zero declination the sun crosses the horizon due east/west.
        let dawn = compute_celestial(&at(6.0));
        assert!(dawn.sun_direction.x > 0.99, "{:?}", dawn.sun_direction);
        assert!(dawn.sun_altitude.abs() < 1e-3);

        let dusk = compute_celestial(&at(18.0));
        assert!(dusk.sun_direction.x < -0.99, "{:?}", dusk.sun_direction);
    }

    #[test]
    fn northern_noon_sun_sits_in_the_south() {
        // `-Z` is north, so a southern sun has positive Z.
        let noon = compute_celestial(&at(12.0));
        assert!(noon.sun_direction.z > 0.0, "{:?}", noon.sun_direction);
    }

    #[test]
    fn southern_hemisphere_noon_sun_sits_in_the_north() {
        let mut t = at(12.0);
        t.latitude = -45.0;
        let noon = compute_celestial(&t);
        assert!(noon.sun_direction.z < 0.0, "{:?}", noon.sun_direction);
    }

    #[test]
    fn directions_are_unit_length() {
        let mut t = WeatherTime::default();
        for _ in 0..500 {
            t.advance(0.0137);
            let b = compute_celestial(&t);
            assert!((b.sun_direction.length() - 1.0).abs() < 1e-4);
            assert!((b.moon_direction.length() - 1.0).abs() < 1e-4);
        }
    }

    #[test]
    fn new_moon_sits_near_the_sun_and_full_moon_opposes_it() {
        let mut t = at(12.0);

        t.moon_phase_offset = 0.0; // new
        let new = compute_celestial(&t);
        assert!(
            new.sun_direction.dot(new.moon_direction) > 0.95,
            "new moon should be close to the sun, got {}",
            new.sun_direction.dot(new.moon_direction)
        );

        t.moon_phase_offset = 0.5; // full
        let full = compute_celestial(&t);
        assert!(
            full.sun_direction.dot(full.moon_direction) < -0.95,
            "full moon should oppose the sun, got {}",
            full.sun_direction.dot(full.moon_direction)
        );
    }

    #[test]
    fn full_moon_is_up_at_midnight() {
        let mut t = at(0.0);
        t.moon_phase_offset = 0.5;
        assert!(compute_celestial(&t).moon_altitude > 0.5);
    }

    #[test]
    fn daylight_ramps_monotonically_through_the_morning() {
        let mut previous = -1.0;
        for i in 0..=48 {
            let hour = 4.0 + i as f32 * (8.0 / 48.0); // 04:00 -> 12:00
            let d = compute_celestial(&at(hour)).daylight;
            assert!(d >= previous - 1e-6, "daylight dipped at {hour}h");
            previous = d;
        }
        assert!(previous > 0.99);
    }

    #[test]
    fn sun_reddens_near_the_horizon() {
        let noon = sun_tint(1.0, 1.0);
        let horizon = sun_tint(0.0, 1.0);
        // Blue is knocked down far harder than red at low sun.
        assert!(horizon.blue / horizon.red < noon.blue / noon.red);
        assert!(horizon.blue < 0.2, "{}", horizon.blue);
    }

    #[test]
    fn summer_sun_climbs_higher_than_winter_sun() {
        let mut summer = at(12.0);
        summer.axial_tilt = 23.44;
        summer.year_offset = 172.0;

        let mut winter = at(12.0);
        winter.axial_tilt = 23.44;
        winter.year_offset = 172.0 + DAYS_PER_YEAR / 2.0;

        assert!(compute_celestial(&summer).sun_altitude > compute_celestial(&winter).sun_altitude);
    }

    #[test]
    fn moon_phase_completes_one_cycle_per_synodic_month() {
        let mut t = at(0.0);
        t.moon_phase_offset = 0.0;
        let start = compute_celestial(&t).moon_phase;
        t.advance(SYNODIC_MONTH_DAYS);
        let end = compute_celestial(&t).moon_phase;
        assert!((start - end).abs() < 1e-3, "{start} vs {end}");
    }
}
