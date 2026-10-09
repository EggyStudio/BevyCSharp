//! The sky dome: stars, the galaxy, the moon and volumetric clouds.
//!
//! # How this fits together with Bevy's atmosphere
//!
//! Bevy 0.19 renders its physically-based atmosphere as a full-screen pass
//! between the opaque and transparent passes, compositing as
//! `destination * transmittance + inscattering`.
//!
//! This module draws deep space *into the opaque pass*, at the far plane, with
//! depth writes disabled. The atmosphere pass then runs over the top. The
//! result is that the sky is extinguished by exactly as much air as physics
//! says it should be, and drowned out by exactly as much daylight scattering:
//! stars appear at dusk and disappear at dawn with no fade parameter anywhere.
//!
//! The whole sky is one full-screen triangle with no model transform, so a
//! single entity serves every camera in the scene.

use bevy::app::{App, Plugin, Startup, Update};
use bevy::asset::RenderAssetUsages;
use bevy::asset::{Asset, Assets, embedded_asset};
use bevy::camera::visibility::NoFrustumCulling;
use bevy::color::{Color, ColorToComponents};
use bevy::ecs::component::Component;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query, Res, ResMut};
use bevy::material::AlphaMode;
use bevy::math::{Mat3, Vec3, Vec4};
use bevy::mesh::{Indices, Mesh, Mesh3d, PrimitiveTopology};
use bevy::pbr::{Material, MaterialPipeline, MaterialPipelineKey, MaterialPlugin, MeshMaterial3d};
use bevy::reflect::{Reflect, TypePath};
use bevy::render::mesh::MeshVertexBufferLayoutRef;
use bevy::render::render_resource::{
    AsBindGroup, CompareFunction, RenderPipelineDescriptor, ShaderType,
    SpecializedMeshPipelineError,
};
use bevy::shader::ShaderRef;
use bevy::time::Time;
use bevy::transform::components::Transform;

use crate::WeatherSystems;
use crate::celestial::CelestialBodies;
use crate::clouds::CloudConfig;
use crate::config::WeatherConfig;
use crate::fog::FogConfig;
use crate::state::Weather;
use crate::thunder::LightningState;
use crate::time::{DAYS_PER_YEAR, WeatherTime};
use crate::wind::Wind;
use bevy::ecs::reflect::ReflectComponent;
use bevy::ecs::reflect::ReflectResource;
use bevy::reflect::std_traits::ReflectDefault;

const SHADER_PATH: &str = "embedded://bevy_weather/sky/sky.wgsl";

/// Mean planetary radius in metres, used for the curvature of the cloud layer.
pub const EARTH_RADIUS_M: f32 = 6_371_000.0;

/// Night-sky values are scaled by this before exposure so that the defaults
/// look right at the `ev100 = 13` daylight exposure the atmosphere wants.
///
/// Point stars have no meaningful physical radiance at raster resolution -- the
/// value depends entirely on the solid angle of a pixel -- so some exaggeration
/// is unavoidable no matter how the rest is authored.
const NIGHT_SCALE: f32 = 3_000.0;

/// The night-sky scale, corrected for a camera exposure that is not the one it
/// was authored against.
///
/// The night sky is the one part of the frame whose radiance is invented rather
/// than measured, so it is the one part that should not move when the exposure
/// does. Leave it alone and
/// [`twilight_exposure_lift`](crate::atmosphere::AtmosphereConfig::twilight_exposure_lift)
/// makes the stars brightest in late twilight and then dims them as the night
/// deepens -- precisely backwards, and very obvious, because the sky behind
/// them is getting darker at the same time.
///
/// Cancelling the lift holds the stars still while the sky drains away
/// underneath them, which is what actually happens outdoors.
fn night_scale(exposure_lift_stops: f32) -> f32 {
    NIGHT_SCALE * (-exposure_lift_stops).exp2()
}

const FLAG_STARS: u32 = 1;
const FLAG_GALAXY: u32 = 2;
const FLAG_MOON: u32 = 4;
const FLAG_CLOUDS: u32 = 8;
const FLAG_ADAPTIVE_MARCH: u32 = 16;
const FLAG_METEORS: u32 = 32;

/// Procedural star field settings.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct StarConfig {
    /// Draw stars at all.
    pub enabled: bool,
    /// Grid cells per cube face. Higher means more, smaller stars.
    pub density: f32,
    /// Overall brightness multiplier.
    pub brightness: f32,
    /// Fraction of cells that actually contain a star, `0.0..=1.0`.
    pub occupancy: f32,
    /// How steeply brightness falls off across the population.
    ///
    /// Real star counts rise steeply toward the faint end, and a uniform
    /// variate raised to this power reproduces that. Too high and every star is
    /// near-invisible, so the sky reads as grey dust instead of as stars; too
    /// low and they are all equally bright, which looks like a texture.
    pub magnitude_falloff: f32,
    /// Apparent size of a star, as a fraction of a grid cell.
    pub size: f32,
    /// How strongly stars are tinted by temperature, `0.0..=1.0`.
    pub color_variation: f32,
    /// Twinkle rate in radians per second.
    pub twinkle_speed: f32,
    /// Twinkle depth, `0.0..=1.0`. Applied most strongly near the horizon,
    /// where you are looking through the most atmosphere.
    pub twinkle_amount: f32,
}

impl Default for StarConfig {
    fn default() -> Self {
        Self {
            enabled: true,
            density: 180.0,
            brightness: 4.4,
            occupancy: 0.19,
            size: 0.052,
            color_variation: 0.6,
            magnitude_falloff: 2.2,
            twinkle_speed: 2.4,
            twinkle_amount: 0.45,
        }
    }
}

/// Procedural Milky Way settings.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct GalaxyConfig {
    /// Draw the galaxy at all.
    pub enabled: bool,
    /// Overall brightness multiplier.
    pub brightness: f32,
    /// Frequency of the unresolved-starlight noise.
    pub noise_scale: f32,
    /// How dark the dust lanes cut, `0.0..=1.0`.
    pub dust: f32,
    /// How tightly the band hugs the galactic plane. Higher is narrower.
    pub band_tightness: f32,
    /// How concentrated the central bulge is. Higher is tighter.
    pub core_concentration: f32,
    /// Colour of the galactic core.
    pub core_color: Color,
    /// Colour of the outer arms.
    pub edge_color: Color,
    /// Galactic pole, in the fixed star frame. The default approximates the
    /// real one, which is why the band sits at a realistic angle to the
    /// celestial equator.
    pub pole: Vec3,
    /// Direction of the galactic centre, in the fixed star frame.
    pub center: Vec3,
}

impl Default for GalaxyConfig {
    fn default() -> Self {
        Self {
            enabled: true,
            brightness: 0.55,
            noise_scale: 26.0,
            dust: 0.75,
            band_tightness: 9.0,
            core_concentration: 3.0,
            core_color: Color::srgb(1.0, 0.92, 0.76),
            edge_color: Color::srgb(0.62, 0.72, 1.0),
            // North galactic pole, roughly RA 12h51m / dec +27.1 degrees.
            pole: Vec3::new(-0.874, -0.484, 0.460),
            // Sagittarius A*, roughly RA 17h46m / dec -29.0 degrees.
            center: Vec3::new(-0.055, -0.873, -0.484),
        }
    }
}

/// Shooting stars.
///
/// Meteors are drawn straight from the clock, with nothing simulated and
/// nothing stored: the slot number and the tick number are hashed into an entry
/// point, a heading and a brightness. Two cameras watching the same sky at the
/// same instant therefore see the same meteor in the same place, and rewinding
/// the clock replays the same shower.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct MeteorConfig {
    /// Draw meteors at all.
    pub enabled: bool,

    /// Meteors per minute across the whole sky.
    ///
    /// A dark-sky site on an ordinary night gives you five or ten an hour, so
    /// the honest value is about `0.1`. The default is a good deal more
    /// generous than that, on the grounds that a player who never sees one may
    /// as well not have them; raise it into the tens for a meteor storm.
    pub rate: f32,

    /// How far a meteor travels across the sky, in radians.
    ///
    /// Individual meteors vary either side of this, with the long ones rare.
    pub arc_length: f32,

    /// Angular half-width of the trail, in radians.
    pub width: f32,

    /// Overall brightness multiplier.
    pub brightness: f32,
}

impl Default for MeteorConfig {
    fn default() -> Self {
        Self {
            enabled: true,
            rate: 1.2,
            arc_length: 0.35,
            width: 0.0024,
            brightness: 7.0,
        }
    }
}

/// Moon rendering settings. Its position and phase come from
/// [`WeatherTime`], not from here.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct MoonConfig {
    /// Draw the moon at all.
    pub enabled: bool,
    /// Brightness of the lit surface.
    ///
    /// Tuned so the sunlit side lands just under the clipping point at the
    /// exposure [`AtmosphereConfig`](crate::atmosphere::AtmosphereConfig) sets.
    /// Pushing it higher does not make the moon look brighter -- it is already
    /// the brightest thing in a night frame -- it just crushes the whole disc
    /// to flat white and takes the phase, the terminator and the maria with it.
    /// Bloom is what should carry the sense of brightness instead.
    pub brightness: f32,
    /// Apparent radius in radians.
    ///
    /// The real moon is `0.00465` -- about half a degree across, which is far
    /// smaller than anyone remembers it being, and at a typical field of view
    /// covers barely a dozen pixels: too few to show a phase at all.
    ///
    /// The default is enlarged to a little over twice that, which is enough for
    /// the terminator and the maria to read while still looking like the moon.
    /// It matches [`SunConfig::angular_radius`](crate::celestial::SunConfig::angular_radius),
    /// as the real pair very nearly do -- which is the coincidence that makes
    /// total eclipses possible. Set it to
    /// [`DEFAULT_ANGULAR_RADIUS`](crate::celestial::DEFAULT_ANGULAR_RADIUS) for
    /// the true size.
    pub angular_radius: f32,
    /// Strength of earthshine on the unlit part of the disc, `0.0..=1.0`.
    /// This is what makes "the old moon in the new moon's arms".
    pub earthshine: f32,
    /// Contrast of the procedural maria and craters, `0.0..=1.0`.
    pub surface_detail: f32,
    /// Tint of the moon's surface. Roughly the real thing: a warm pale grey.
    pub tint: Color,

    /// How strongly to cancel the atmosphere's reddening. `0.0` disables it.
    ///
    /// The atmosphere pass reddens the moon by the correct amount for its
    /// altitude, and at the twenty to forty degrees where the moon actually
    /// spends most of its time that is a *lot* -- two to three air masses, more
    /// than enough to halve the blue channel. Rendered without correction, the
    /// moon is orange nearly every night.
    ///
    /// A real observer does not see that, because the eye white-balances to
    /// what it is looking at. This is the stand-in for an adaptation the
    /// renderer has no way to perform: the moon's tint is pre-divided by an
    /// estimate of the extinction it is about to suffer, so it comes out neutral
    /// at the altitudes it is normally seen at.
    ///
    /// The correction saturates a little below twenty degrees of elevation, so
    /// a moon actually sitting on the horizon still goes the deep orange it
    /// should. `1.0` is the tuned default; higher over-corrects toward a cold
    /// moon, and `0.0` gives you the unmodified physics.
    pub white_balance: f32,
}

impl Default for MoonConfig {
    fn default() -> Self {
        Self {
            enabled: true,
            brightness: 4.6,
            angular_radius: 0.0105,
            earthshine: 0.12,
            surface_detail: 0.7,
            tint: Color::srgb(0.96, 0.95, 0.93),
            white_balance: 1.0,
        }
    }
}

/// Marks the sky entity this plugin spawns.
#[derive(Component, Debug, Clone, Copy, Default, Reflect)]
#[reflect(Component, Default)]
pub struct SkyDome;

/// Everything the sky shader needs, in one uniform.
///
/// Field order and types must match `SkyUniform` in `sky.wgsl` exactly.
#[derive(ShaderType, Debug, Clone)]
pub struct SkyUniform {
    /// `xyz`: direction to the sun. `w`: its angular radius.
    pub sun_direction: Vec4,
    /// `xyz`: direction to the moon. `w`: its angular radius.
    pub moon_direction: Vec4,
    /// `rgb`: sun colour. `a`: daylight factor.
    pub sun_color: Vec4,
    /// `rgb`: moonlight colour. `a`: lit fraction.
    pub moon_color: Vec4,
    /// `x`: density. `y`: brightness. `z`: twinkle speed. `w`: size.
    pub star_params: Vec4,
    /// `x`: colour spread. `y`: occupancy. `z`: twinkle depth.
    /// `w`: magnitude falloff.
    pub star_params2: Vec4,
    /// `x`: brightness. `y`: noise scale. `z`: dust. `w`: band tightness.
    pub galaxy_params: Vec4,
    /// Linear colour of the galactic core.
    pub galaxy_core_color: Vec4,
    /// Linear colour of the outer arms.
    pub galaxy_edge_color: Vec4,
    /// `xyz`: galactic pole. `w`: core concentration.
    pub galaxy_axis: Vec4,
    /// `xyz`: galactic centre. `w`: unused.
    pub galaxy_center: Vec4,
    /// `x`: brightness. `y`: earthshine. `z`: detail. `w`: phase.
    pub moon_params: Vec4,
    /// Linear tint of the moon's surface.
    pub moon_tint: Vec4,
    /// `x`: meteors per minute. `y`: trail arc length. `z`: trail width.
    /// `w`: brightness.
    pub meteor_params: Vec4,
    /// `x`: coverage. `y`: density. `z`: base altitude. `w`: thickness.
    pub cloud_params0: Vec4,
    /// `x`: shape scale. `y`: detail scale. `z`: detail strength. `w`: extinction.
    pub cloud_params1: Vec4,
    /// `x`: forward g. `y`: back g. `z`: powder. `w`: ambient.
    pub cloud_params2: Vec4,
    /// `x`: exposure. `y`: horizon fade. `z`: steps. `w`: light steps.
    pub cloud_params3: Vec4,
    /// `x`: distance where erosion starts fading. `y`: where it is gone.
    pub cloud_params4: Vec4,
    /// Linear cloud albedo.
    pub cloud_albedo: Vec4,
    /// `xyz`: wind displacement in metres. `w`: shape evolution.
    pub cloud_offset: Vec4,
    /// `rgb`: lightning flash colour and strength. `a`: interior response.
    pub lightning: Vec4,
    /// `rgb`: ground-fog colour, in post-exposure units.
    pub fog_color: Vec4,
    /// `x`: fog extinction per world unit. `y`: fog layer height.
    /// `z`: forward-scattering glow. `w`: glow exponent.
    pub fog_params: Vec4,
    /// `x`: time. `y`: planet radius. `z`: units per metre. `w`: feature flags.
    pub misc: Vec4,
    /// Rotates a world direction into the fixed frame the stars live in.
    pub star_from_world: Mat3,
}

impl Default for SkyUniform {
    fn default() -> Self {
        Self {
            sun_direction: Vec4::new(0.0, 1.0, 0.0, crate::celestial::DEFAULT_ANGULAR_RADIUS),
            moon_direction: Vec4::new(0.0, -1.0, 0.0, crate::celestial::DEFAULT_ANGULAR_RADIUS),
            sun_color: Vec4::new(1.0, 1.0, 1.0, 1.0),
            moon_color: Vec4::new(0.7, 0.8, 1.0, 0.0),
            star_params: Vec4::ZERO,
            star_params2: Vec4::ZERO,
            galaxy_params: Vec4::ZERO,
            galaxy_core_color: Vec4::ONE,
            galaxy_edge_color: Vec4::ONE,
            galaxy_axis: Vec4::new(0.0, 1.0, 0.0, 3.0),
            galaxy_center: Vec4::new(0.0, 0.0, 1.0, 0.0),
            moon_params: Vec4::ZERO,
            moon_tint: Vec4::ONE,
            meteor_params: Vec4::ZERO,
            cloud_params0: Vec4::ZERO,
            cloud_params1: Vec4::new(14_000.0, 1_400.0, 0.35, 0.045),
            cloud_params2: Vec4::new(0.8, -0.25, 0.6, 0.35),
            cloud_params3: Vec4::new(1.0, 0.06, 48.0, 4.0),
            cloud_params4: Vec4::new(10_000.0, 30_000.0, 0.0, 0.0),
            cloud_albedo: Vec4::ONE,
            cloud_offset: Vec4::ZERO,
            lightning: Vec4::ZERO,
            fog_color: Vec4::ZERO,
            fog_params: Vec4::ZERO,
            misc: Vec4::new(0.0, EARTH_RADIUS_M, 1.0, 0.0),
            star_from_world: Mat3::IDENTITY,
        }
    }
}

/// The material behind the sky dome.
///
/// You normally never touch this: [`SkyPlugin`] rebuilds
/// [`uniform`](Self::uniform) from the weather resources every frame.
#[derive(Asset, TypePath, AsBindGroup, Debug, Clone, Default)]
pub struct SkyMaterial {
    /// Everything the shader reads.
    #[uniform(0)]
    pub uniform: SkyUniform,
}

/// Shared pipeline setup for both sky passes.
fn specialize_sky(
    descriptor: &mut RenderPipelineDescriptor,
    layout: &MeshVertexBufferLayoutRef,
) -> Result<(), SpecializedMeshPipelineError> {
    // The triangle is already in clip space and is viewed from "inside".
    descriptor.primitive.cull_mode = None;

    if let Some(depth_stencil) = descriptor.depth_stencil.as_mut() {
        // Never write depth: the atmosphere pass keys off a far-plane depth of
        // zero to decide it is looking at sky rather than at geometry.
        depth_stencil.depth_write_enabled = Some(false);
        // `GreaterEqual` rather than `Greater`, because in reverse-Z the sky
        // sits *at* the far plane and has to pass against an untouched depth
        // buffer -- while still losing to any real geometry in front of it.
        depth_stencil.depth_compare = Some(CompareFunction::GreaterEqual);
    }

    // Position is the only attribute these shaders read.
    descriptor.vertex.buffers = vec![
        layout
            .0
            .get_layout(&[Mesh::ATTRIBUTE_POSITION.at_shader_location(0)])?,
    ];

    Ok(())
}

impl Material for SkyMaterial {
    fn vertex_shader() -> ShaderRef {
        SHADER_PATH.into()
    }

    fn fragment_shader() -> ShaderRef {
        SHADER_PATH.into()
    }

    fn alpha_mode(&self) -> AlphaMode {
        // Opaque, so it lands in the opaque pass and Bevy's atmosphere
        // composites over it. The depth state in `specialize_sky` is what stops
        // it behaving like ordinary opaque geometry.
        AlphaMode::Opaque
    }

    fn enable_prepass() -> bool {
        // A prepass would write the sky's depth and convince the atmosphere
        // there is solid geometry at the far plane.
        false
    }

    fn enable_shadows() -> bool {
        false
    }

    fn specialize(
        _pipeline: &MaterialPipeline,
        descriptor: &mut RenderPipelineDescriptor,
        layout: &MeshVertexBufferLayoutRef,
        _key: MaterialPipelineKey<Self>,
    ) -> Result<(), SpecializedMeshPipelineError> {
        specialize_sky(descriptor, layout)
    }
}

/// The volumetric cloud layer.
///
/// A separate material from [`SkyMaterial`] purely so it can be alpha-blended
/// in the transparent pass, which runs *after* Bevy's atmosphere. See the
/// header comment in `sky.wgsl` for why that ordering is not optional.
#[derive(Asset, TypePath, AsBindGroup, Debug, Clone, Default)]
pub struct CloudMaterial {
    /// The same uniform [`SkyMaterial`] uses; both passes are driven together.
    #[uniform(0)]
    pub uniform: SkyUniform,
}

impl Material for CloudMaterial {
    fn vertex_shader() -> ShaderRef {
        SHADER_PATH.into()
    }

    fn fragment_shader() -> ShaderRef {
        SHADER_PATH.into()
    }

    fn alpha_mode(&self) -> AlphaMode {
        AlphaMode::Blend
    }

    fn enable_prepass() -> bool {
        false
    }

    fn enable_shadows() -> bool {
        false
    }

    fn depth_bias(&self) -> f32 {
        // Push the clouds to the back of the transparent queue so that rain,
        // snow and the player's own transparent geometry all draw in front of
        // them.
        -1.0e6
    }

    fn specialize(
        _pipeline: &MaterialPipeline,
        descriptor: &mut RenderPipelineDescriptor,
        layout: &MeshVertexBufferLayoutRef,
        _key: MaterialPipelineKey<Self>,
    ) -> Result<(), SpecializedMeshPipelineError> {
        if let Some(fragment) = descriptor.fragment.as_mut() {
            fragment.shader_defs.push("CLOUD_PASS".into());
        }
        specialize_sky(descriptor, layout)
    }
}

/// A single oversized triangle covering the viewport in normalised device
/// coordinates.
///
/// One triangle rather than two so there is no diagonal seam, and no
/// quad-shading inefficiency down the middle of the screen.
fn fullscreen_triangle() -> Mesh {
    Mesh::new(
        PrimitiveTopology::TriangleList,
        RenderAssetUsages::RENDER_WORLD,
    )
    .with_inserted_attribute(
        Mesh::ATTRIBUTE_POSITION,
        vec![[-1.0, -1.0, 0.0], [3.0, -1.0, 0.0], [-1.0, 3.0, 0.0]],
    )
    .with_inserted_indices(Indices::U32(vec![0, 1, 2]))
}

/// A per-channel gain that cancels most of the atmospheric reddening a body at
/// `altitude` is about to pick up.
///
/// `altitude` is the sine of the elevation angle, matching
/// [`CelestialBodies::moon_altitude`]. `strength` scales the correction: `1.0`
/// is the tuned default and `0.0` disables it.
///
/// The result is normalised against luminance rather than against its largest
/// channel, so it shifts hue without changing how bright the body appears.
/// Normalising on the peak instead would leave every channel at or below one
/// and quietly darken the moon as the correction grew.
pub fn atmospheric_white_balance(altitude: f32, strength: f32) -> Vec3 {
    let strength = strength.clamp(0.0, 4.0);
    if strength <= 0.0 {
        return Vec3::ONE;
    }

    // Effective optical depth of the whole column at the zenith, per channel.
    //
    // Fitted against what Bevy's atmosphere actually does rather than taken
    // from the Rayleigh coefficients directly: the pass also carries a Mie term
    // and an ozone term, and integrates a real density profile, so the textbook
    // Rayleigh figures come out about thirty percent short.
    const ZENITH_OPTICAL_DEPTH: Vec3 = Vec3::new(0.057, 0.134, 0.329);

    // A softened `1 / sin(elevation)`, capped. The cap is the important part:
    // real air mass runs away toward the horizon -- better than twenty at the
    // horizon itself -- and a correction that tracked it would turn a moonrise
    // blue. Saturating at three air masses corrects the altitudes the moon is
    // usually at and leaves the deep reddening of a low moon intact, which is
    // the one time it is worth seeing.
    const MAX_CORRECTED_AIR_MASS: f32 = 3.0;
    let air_mass = (1.0 / (altitude.max(0.0) + 0.045)).min(MAX_CORRECTED_AIR_MASS);
    let optical_depth = ZENITH_OPTICAL_DEPTH * air_mass;

    // The inverse of the transmittance it is about to be multiplied by.
    let gain = Vec3::new(
        (optical_depth.x * strength).exp(),
        (optical_depth.y * strength).exp(),
        (optical_depth.z * strength).exp(),
    );
    // Rec. 709 luminance weights.
    const LUMA: Vec3 = Vec3::new(0.2126, 0.7152, 0.0722);
    gain / gain.dot(LUMA).max(1e-6)
}

/// Builds the star frame rotation for a given clock.
///
/// Returns the matrix that takes a world-space direction into the frame the
/// stars are fixed in.
///
/// The angle is *local sidereal time*, not solar time, and it accumulates
/// continuously. That one detail is what sets the star field's rate: the sky
/// turns once per sidereal day (23h56m), so stars sweep at 15.041 degrees an
/// hour, very slightly faster than the sun's 15. The moon, meanwhile, is
/// falling behind at about 13 degrees a day, so it drifts eastward through the
/// constellations and rises roughly fifty minutes later each night.
///
/// Advancing this by whole days only -- stepping it at midnight rather than
/// integrating it -- would pin the stars to the *solar* rate and lock them to
/// the moon, which is exactly the giveaway that a sky is faked.
pub fn star_frame_from_world(time: &WeatherTime) -> Mat3 {
    let latitude = time.latitude.to_radians();
    // Solar hour angle plus the sun's own continuous yearly march around the
    // sky. `f64` for the year term: after a few in-game years an `f32` can no
    // longer resolve a single minute of it.
    let year_fraction = (time.elapsed_days() / DAYS_PER_YEAR as f64).rem_euclid(1.0) as f32;
    let sidereal = core::f32::consts::TAU * ((time.time_of_day - 0.5) + year_fraction);
    let (sin_lst, cos_lst) = sidereal.sin_cos();
    let (sin_lat, cos_lat) = latitude.sin_cos();

    // Rows of the fixed-frame-to-world rotation, matching the horizon basis
    // used by `celestial::equatorial_to_world`: east, up, and south (Bevy's
    // `+Z`, since north is `-Z`).
    let east = Vec3::new(-sin_lst, cos_lst, 0.0);
    let up = Vec3::new(cos_lat * cos_lst, cos_lat * sin_lst, sin_lat);
    let south = Vec3::new(sin_lat * cos_lst, sin_lat * sin_lst, -cos_lat);

    // Those three are the *rows* of world-from-star, so a matrix built from
    // them as columns is its transpose -- which, for a rotation, is its
    // inverse. That is exactly the world-to-star direction we want.
    Mat3::from_cols(east, up, south)
}

/// Renders the sky dome and keeps its uniform up to date.
#[derive(Debug, Clone, Copy, Default)]
pub struct SkyPlugin;

impl Plugin for SkyPlugin {
    fn build(&self, app: &mut App) {
        embedded_asset!(app, "sky.wgsl");

        app.init_resource::<StarConfig>()
            .init_resource::<GalaxyConfig>()
            .init_resource::<MoonConfig>()
            .init_resource::<MeteorConfig>()
            .init_resource::<CloudConfig>()
            .register_type::<StarConfig>()
            .register_type::<GalaxyConfig>()
            .register_type::<MoonConfig>()
            .register_type::<MeteorConfig>()
            .register_type::<CloudConfig>()
            .register_type::<SkyDome>()
            .add_plugins((
                MaterialPlugin::<SkyMaterial>::default(),
                MaterialPlugin::<CloudMaterial>::default(),
            ))
            .add_systems(Startup, spawn_sky)
            .add_systems(Update, update_sky.in_set(WeatherSystems::Apply));
    }
}

fn spawn_sky(
    mut commands: Commands,
    mut meshes: ResMut<Assets<Mesh>>,
    mut sky_materials: ResMut<Assets<SkyMaterial>>,
    mut cloud_materials: ResMut<Assets<CloudMaterial>>,
) {
    let mesh = meshes.add(fullscreen_triangle());

    // The triangle's vertices are clip-space coordinates, so its bounding box
    // is meaningless and culling on it would be wrong.
    commands.spawn((
        SkyDome,
        Mesh3d(mesh.clone()),
        MeshMaterial3d(sky_materials.add(SkyMaterial::default())),
        NoFrustumCulling,
        Transform::default(),
    ));
    commands.spawn((
        SkyDome,
        Mesh3d(mesh),
        MeshMaterial3d(cloud_materials.add(CloudMaterial::default())),
        NoFrustumCulling,
        Transform::default(),
    ));
}

/// Everything the sky's appearance is a function of.
///
/// Grouped only because a Bevy system takes at most sixteen parameters and the
/// sky needs more than that; the list is otherwise exactly what
/// [`build_uniform`] reads.
#[derive(bevy::ecs::system::SystemParam)]
pub struct SkyInputs<'w> {
    config: Res<'w, WeatherConfig>,
    stars: Res<'w, StarConfig>,
    galaxy: Res<'w, GalaxyConfig>,
    moon: Res<'w, MoonConfig>,
    meteors: Res<'w, MeteorConfig>,
    clouds: Res<'w, CloudConfig>,
    fog: Res<'w, FogConfig>,
    weather: Res<'w, Weather>,
    bodies: Res<'w, CelestialBodies>,
    weather_time: Res<'w, WeatherTime>,
    wind: Res<'w, Wind>,
    lightning: Res<'w, LightningState>,
    atmosphere: Res<'w, crate::atmosphere::AtmosphereConfig>,
}

/// How often the sky material's uniform is rebuilt: every frame.
fn update_sky(
    mut sky_materials: ResMut<Assets<SkyMaterial>>,
    mut cloud_materials: ResMut<Assets<CloudMaterial>>,
    space_domes: Query<&MeshMaterial3d<SkyMaterial>, bevy::ecs::query::With<SkyDome>>,
    cloud_domes: Query<&MeshMaterial3d<CloudMaterial>, bevy::ecs::query::With<SkyDome>>,
    sky: SkyInputs,
    time: Res<Time>,
) {
    let uniform = build_uniform(
        &sky.config,
        &sky.stars,
        &sky.galaxy,
        &sky.moon,
        &sky.meteors,
        &sky.clouds,
        &sky.fog,
        &sky.weather,
        &sky.bodies,
        &sky.weather_time,
        &sky.wind,
        &sky.lightning,
        // Only when the plugin is actually driving exposure; a game managing
        // its own is not having anything cancelled behind its back.
        if sky.atmosphere.exposure_ev100.is_some() && sky.config.atmosphere {
            sky.atmosphere.exposure_lift(sky.bodies.sun_altitude)
        } else {
            0.0
        },
        time.elapsed_secs_wrapped(),
    );

    for handle in &space_domes {
        if let Some(mut material) = sky_materials.get_mut(&handle.0) {
            material.uniform = uniform.clone();
        }
    }
    for handle in &cloud_domes {
        if let Some(mut material) = cloud_materials.get_mut(&handle.0) {
            material.uniform = uniform.clone();
        }
    }
}

/// Assembles the shader uniform from the weather resources.
///
/// Split out from the system so it can be unit tested without a render world.
#[expect(
    clippy::too_many_arguments,
    reason = "mirrors the system's parameters; see the note there"
)]
pub fn build_uniform(
    config: &WeatherConfig,
    stars: &StarConfig,
    galaxy: &GalaxyConfig,
    moon: &MoonConfig,
    meteors: &MeteorConfig,
    clouds: &CloudConfig,
    fog: &FogConfig,
    weather: &Weather,
    bodies: &CelestialBodies,
    weather_time: &WeatherTime,
    wind: &Wind,
    lightning: &LightningState,
    exposure_lift_stops: f32,
    elapsed: f32,
) -> SkyUniform {
    let conditions = weather.current;
    let night_scale = night_scale(exposure_lift_stops);

    let mut flags = 0u32;
    if config.sky {
        if stars.enabled {
            flags |= FLAG_STARS;
        }
        if galaxy.enabled {
            flags |= FLAG_GALAXY;
        }
        if moon.enabled {
            flags |= FLAG_MOON;
        }
        if meteors.enabled && meteors.rate > 0.0 {
            flags |= FLAG_METEORS;
        }
        if clouds_enabled(config, &conditions) {
            flags |= FLAG_CLOUDS;
            if clouds.adaptive_marching {
                flags |= FLAG_ADAPTIVE_MARCH;
            }
        }
    }

    let steps = clouds.resolved_steps(config.quality);
    let light_steps = clouds.resolved_light_steps(config.quality);
    let detail_strength = clouds.resolved_detail_strength(config.quality);

    // Clouds ride higher and faster than surface wind.
    let cloud_offset = wind.offset3() * clouds.wind_multiplier;

    let sun = bodies.sun_direction;
    let moon_dir = bodies.moon_direction;
    let sun_color = bodies.sun_color.to_vec3();
    let moon_tint = moon.tint.to_linear().to_vec3()
        * atmospheric_white_balance(bodies.moon_altitude, moon.white_balance);

    SkyUniform {
        sun_direction: sun.extend(bodies.sun_angular_radius),
        moon_direction: moon_dir.extend(moon.angular_radius.max(1e-5)),
        sun_color: sun_color.extend(bodies.daylight),
        moon_color: (moon_tint * night_scale).extend(bodies.moon_illumination),

        star_params: Vec4::new(
            stars.density.max(1.0),
            stars.brightness * night_scale,
            stars.twinkle_speed,
            stars.size.max(1e-4),
        ),
        star_params2: Vec4::new(
            stars.color_variation.clamp(0.0, 1.0),
            stars.occupancy.clamp(0.0, 1.0),
            stars.twinkle_amount.clamp(0.0, 1.0),
            stars.magnitude_falloff.max(0.1),
        ),

        galaxy_params: Vec4::new(
            galaxy.brightness * night_scale,
            galaxy.noise_scale,
            galaxy.dust.clamp(0.0, 1.0),
            galaxy.band_tightness.max(0.1),
        ),
        galaxy_core_color: galaxy.core_color.to_linear().to_vec3().extend(1.0),
        galaxy_edge_color: galaxy.edge_color.to_linear().to_vec3().extend(1.0),
        galaxy_axis: galaxy
            .pole
            .normalize_or(Vec3::Y)
            .extend(galaxy.core_concentration.max(0.01)),
        galaxy_center: galaxy.center.normalize_or(Vec3::Z).extend(0.0),

        moon_params: Vec4::new(
            moon.brightness * night_scale,
            moon.earthshine.max(0.0),
            moon.surface_detail.clamp(0.0, 1.0),
            bodies.moon_phase,
        ),
        moon_tint: moon_tint.extend(1.0),

        meteor_params: Vec4::new(
            meteors.rate.max(0.0),
            meteors.arc_length.max(1e-3),
            meteors.width.max(1e-4),
            meteors.brightness.max(0.0) * night_scale,
        ),

        cloud_params0: Vec4::new(
            conditions.cloud_coverage,
            conditions.cloud_density,
            conditions.cloud_altitude,
            conditions.cloud_thickness,
        ),
        cloud_params1: Vec4::new(
            clouds.shape_scale.max(1.0),
            clouds.detail_scale.max(1.0),
            detail_strength,
            clouds.extinction.max(1e-6),
        ),
        cloud_params2: Vec4::new(
            clouds.forward_scattering.clamp(-0.99, 0.99),
            clouds.back_scattering.clamp(-0.99, 0.99),
            clouds.powder.clamp(0.0, 1.0),
            clouds.ambient.max(0.0),
        ),
        cloud_params3: Vec4::new(
            clouds.exposure.max(0.0),
            clouds.horizon_fade.max(1e-4),
            steps as f32,
            light_steps as f32,
        ),
        cloud_params4: Vec4::new(
            clouds.detail_distance.max(0.0),
            clouds.detail_distance.max(0.0) * 3.0 + 1.0,
            0.0,
            0.0,
        ),
        cloud_albedo: clouds.albedo_linear().to_vec3().extend(1.0),
        cloud_offset: cloud_offset.extend(elapsed * clouds.evolution_rate),

        lightning: (lightning.color.to_vec3() * lightning.intensity)
            .extend(clouds.lightning_response.max(0.0)),

        fog_color: fog
            .color_at(
                bodies.daylight,
                conditions.cloud_coverage * conditions.cloud_density,
            )
            .to_vec3()
            .extend(1.0),
        fog_params: Vec4::new(
            fog.sky_extinction_at(conditions.fog),
            fog.volume_height.max(0.1),
            fog.sun_glow.max(0.0),
            fog.sun_glow_exponent.max(1.0),
        ),

        misc: Vec4::new(
            elapsed,
            EARTH_RADIUS_M,
            config.units_per_meter.max(1e-6),
            flags as f32,
        ),

        star_from_world: star_frame_from_world(weather_time),
    }
}

fn clouds_enabled(config: &WeatherConfig, conditions: &crate::state::WeatherConditions) -> bool {
    // Skip the raymarch entirely on a genuinely cloudless sky; it is by far the
    // most expensive thing this shader does.
    config.clouds && conditions.cloud_coverage > 0.001
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::celestial::compute_celestial;

    #[test]
    fn white_balance_is_neutral_when_disabled() {
        for altitude in [-1.0f32, 0.0, 0.3, 1.0] {
            assert_eq!(atmospheric_white_balance(altitude, 0.0), Vec3::ONE);
        }
    }

    #[test]
    fn white_balance_preserves_luminance() {
        // It is a hue shift, not a dimmer. If the correction changed overall
        // brightness, the moon would fade as it sank -- on top of the fading
        // the atmosphere is already doing.
        const LUMA: Vec3 = Vec3::new(0.2126, 0.7152, 0.0722);
        for i in 0..=20 {
            let altitude = i as f32 / 20.0;
            let gain = atmospheric_white_balance(altitude, 1.0);
            assert!(
                (gain.dot(LUMA) - 1.0).abs() < 1e-4,
                "luminance drifted: {gain:?}"
            );
            assert!(gain.min_element() > 0.0);
        }
    }

    #[test]
    fn white_balance_pushes_blue_up_not_red() {
        // It has to counteract reddening, so blue must be the channel that is
        // held at full and red the one that is pulled down.
        let gain = atmospheric_white_balance(0.35, 1.0);
        assert!(gain.z > gain.y, "{gain:?}");
        assert!(gain.y > gain.x, "{gain:?}");
    }

    #[test]
    fn white_balance_works_hardest_where_the_air_is_thickest() {
        let high = atmospheric_white_balance(1.0, 1.0);
        let low = atmospheric_white_balance(0.2, 1.0);
        // A lower body needs more correction, so the gap it opens between the
        // blue and red channels is wider.
        assert!(
            low.z / low.x > high.z / high.x,
            "high {high:?}, low {low:?}"
        );
    }

    #[test]
    fn white_balance_lets_a_horizon_moon_stay_orange() {
        // The correction understates the air mass near the horizon on purpose.
        // If it cancelled everything there, a moonrise would be white, and the
        // one time the reddening is worth seeing is the one time it would be
        // gone.
        let horizon = atmospheric_white_balance(0.0, 1.0);
        let typical = atmospheric_white_balance(0.35, 1.0);
        // Saturated: a body on the horizon gets no more correction than one
        // comfortably above it, so its extra reddening survives.
        assert!(
            horizon.z / horizon.x < 3.0,
            "the horizon correction is too aggressive: {horizon:?}"
        );
        assert!(
            (horizon.z / horizon.x) >= (typical.z / typical.x) - 1e-4,
            "the correction should not shrink toward the horizon"
        );
    }

    #[test]
    fn star_frame_is_a_rotation() {
        let mut time = WeatherTime::default();
        for _ in 0..200 {
            time.advance(0.0173);
            let m = star_frame_from_world(&time);
            let identity = m * m.transpose();
            for row in 0..3 {
                for column in 0..3 {
                    let expected = if row == column { 1.0 } else { 0.0 };
                    assert!(
                        (identity.col(column)[row] - expected).abs() < 1e-4,
                        "not orthonormal: {identity:?}"
                    );
                }
            }
            assert!((m.determinant() - 1.0).abs() < 1e-4, "not a rotation");
        }
    }

    /// Angle swept about the celestial pole between two directions.
    fn swept_about_pole(a: Vec3, b: Vec3, pole: Vec3) -> f32 {
        let flatten = |v: Vec3| (v - pole * v.dot(pole)).normalize();
        flatten(a).angle_between(flatten(b))
    }

    fn celestial_pole(latitude_degrees: f32) -> Vec3 {
        let latitude = latitude_degrees.to_radians();
        Vec3::new(0.0, latitude.sin(), -latitude.cos())
    }

    #[test]
    fn stars_sweep_at_the_sidereal_rate() {
        // A sidereal day is 23h56m, so the sky turns 15.041 degrees an hour --
        // not the 15.0 of solar time.
        let mut time = WeatherTime {
            latitude: 45.0,
            ..Default::default()
        };
        time.set_hour(22.0);

        let star = Vec3::new(0.6, 0.3, -0.74).normalize();
        let before = star_frame_from_world(&time).transpose() * star;
        time.advance(1.0 / 24.0);
        let after = star_frame_from_world(&time).transpose() * star;

        let swept = swept_about_pole(before, after, celestial_pole(45.0)).to_degrees();
        assert!(
            (swept - 15.041).abs() < 0.02,
            "stars swept {swept} deg/hour, expected 15.041"
        );
    }

    #[test]
    fn the_moon_falls_behind_the_stars() {
        // The moon orbits eastward, so it lags the star field by about half a
        // degree an hour. If the two sweep at the same rate, the moon is nailed
        // to the constellations and the sky reads as fake.
        let mut time = WeatherTime {
            latitude: 45.0,
            moon_phase_offset: 0.5,
            ..Default::default()
        };
        time.set_hour(22.0);

        let star = Vec3::new(0.6, 0.3, -0.74).normalize();
        let star_before = star_frame_from_world(&time).transpose() * star;
        let moon_before = compute_celestial(&time).moon_direction;

        time.advance(1.0 / 24.0);

        let star_after = star_frame_from_world(&time).transpose() * star;
        let moon_after = compute_celestial(&time).moon_direction;

        let pole = celestial_pole(45.0);
        let star_swept = swept_about_pole(star_before, star_after, pole).to_degrees();
        let moon_swept = swept_about_pole(moon_before, moon_after, pole).to_degrees();

        assert!(
            star_swept > moon_swept + 0.4,
            "stars swept {star_swept}, moon swept {moon_swept} -- too close together"
        );
        assert!(
            (moon_swept - 14.492).abs() < 0.05,
            "moon swept {moon_swept} deg/hour, expected about 14.49"
        );
    }

    #[test]
    fn the_moon_drifts_a_full_lap_through_the_stars_each_month() {
        // Over one synodic month the moon should come back to the same place
        // relative to the sun, having lapped the constellations once.
        let mut time = WeatherTime {
            latitude: 45.0,
            moon_phase_offset: 0.0,
            ..Default::default()
        };
        let pole = celestial_pole(45.0);

        let star = Vec3::new(0.6, 0.3, -0.74).normalize();
        let star_start = star_frame_from_world(&time).transpose() * star;
        let moon_start = compute_celestial(&time).moon_direction;
        let start_gap = swept_about_pole(star_start, moon_start, pole);

        // A quarter of a month is enough to show a large, unambiguous drift.
        time.advance(crate::time::SYNODIC_MONTH_DAYS / 4.0);
        let star_end = star_frame_from_world(&time).transpose() * star;
        let moon_end = compute_celestial(&time).moon_direction;
        let end_gap = swept_about_pole(star_end, moon_end, pole);

        assert!(
            (end_gap - start_gap).abs().to_degrees() > 45.0,
            "moon barely moved against the stars: {} -> {} deg",
            start_gap.to_degrees(),
            end_gap.to_degrees()
        );
    }

    #[test]
    fn sidereal_time_has_no_midnight_jump() {
        // Stepping the year term once a day rather than integrating it puts a
        // visible lurch in the sky at midnight.
        let mut time = WeatherTime {
            latitude: 45.0,
            day: 40,
            ..Default::default()
        };
        time.set_hour(23.999);
        let star = Vec3::new(0.6, 0.3, -0.74).normalize();
        let before = star_frame_from_world(&time).transpose() * star;

        time.advance(0.002 / 24.0);
        let after = star_frame_from_world(&time).transpose() * star;

        // Two thousandths of an hour of genuine sidereal rotation is 0.03
        // degrees. A day's worth of the year term arriving in one step would be
        // nearly a whole degree, which is the failure this guards against.
        let moved = before.angle_between(after).to_degrees();
        assert!(
            moved < 0.2,
            "the sky lurched {moved} degrees across midnight"
        );
    }

    #[test]
    fn stars_are_fixed_while_the_sky_turns() {
        // A fixed star direction must map to different world directions as the
        // night goes on -- that rotation is the whole point.
        let mut early = WeatherTime::default();
        early.set_hour(21.0);
        let mut late = WeatherTime::default();
        late.set_hour(3.0);

        let world_dir = Vec3::new(0.3, 0.6, -0.74).normalize();
        let a = star_frame_from_world(&early) * world_dir;
        let b = star_frame_from_world(&late) * world_dir;
        assert!(
            a.dot(b) < 0.99,
            "the star field did not rotate: {}",
            a.dot(b)
        );
    }

    #[test]
    fn celestial_pole_stays_put_all_night() {
        // Everything rotates about the pole, so the pole itself must not move.
        let latitude = 45f32.to_radians();
        let pole = Vec3::new(0.0, latitude.sin(), -latitude.cos());

        let mut reference: Option<Vec3> = None;
        let mut time = WeatherTime {
            latitude: 45.0,
            ..Default::default()
        };
        for _ in 0..24 {
            time.advance(1.0 / 24.0);
            let star_pole = star_frame_from_world(&time) * pole;
            match reference {
                None => reference = Some(star_pole),
                Some(first) => assert!(
                    first.dot(star_pole) > 0.9999,
                    "the pole drifted: {first:?} vs {star_pole:?}"
                ),
            }
        }
    }

    fn uniform_for(weather: Weather, config: WeatherConfig) -> SkyUniform {
        let time = WeatherTime::default();
        build_uniform(
            &config,
            &StarConfig::default(),
            &GalaxyConfig::default(),
            &MoonConfig::default(),
            &MeteorConfig::default(),
            &CloudConfig::default(),
            &FogConfig::default(),
            &weather,
            &compute_celestial(&time),
            &time,
            &Wind::default(),
            &LightningState::default(),
            0.0,
            0.0,
        )
    }

    #[test]
    fn feature_flags_follow_the_config() {
        let cloudy = Weather::new(crate::presets::WeatherPreset::Overcast.conditions());
        let all_on = uniform_for(cloudy.clone(), WeatherConfig::default());
        let flags = all_on.misc.w as u32;
        assert_eq!(flags & FLAG_STARS, FLAG_STARS);
        assert_eq!(flags & FLAG_GALAXY, FLAG_GALAXY);
        assert_eq!(flags & FLAG_MOON, FLAG_MOON);
        assert_eq!(flags & FLAG_CLOUDS, FLAG_CLOUDS);

        let sky_off = uniform_for(
            cloudy.clone(),
            WeatherConfig {
                sky: false,
                ..Default::default()
            },
        );
        assert_eq!(sky_off.misc.w as u32, 0);

        let clouds_off = uniform_for(
            cloudy,
            WeatherConfig {
                clouds: false,
                ..Default::default()
            },
        );
        assert_eq!((clouds_off.misc.w as u32) & FLAG_CLOUDS, 0);
    }

    #[test]
    fn a_clear_sky_skips_the_cloud_raymarch() {
        let clear = Weather::new(crate::presets::WeatherPreset::Clear.conditions());
        let uniform = uniform_for(clear, WeatherConfig::default());
        assert_eq!((uniform.misc.w as u32) & FLAG_CLOUDS, 0);
    }

    #[test]
    fn directions_reach_the_shader_normalised() {
        let uniform = uniform_for(Weather::default(), WeatherConfig::default());
        assert!((uniform.sun_direction.truncate().length() - 1.0).abs() < 1e-4);
        assert!((uniform.moon_direction.truncate().length() - 1.0).abs() < 1e-4);
        assert!((uniform.galaxy_axis.truncate().length() - 1.0).abs() < 1e-4);
        assert!((uniform.galaxy_center.truncate().length() - 1.0).abs() < 1e-4);
    }

    #[test]
    fn erosion_fades_out_rather_than_stopping() {
        // A hard cutoff would draw a ring in the sky at a fixed distance, and
        // it would slide around as the camera moved. The far edge has to sit
        // well beyond the near one.
        let cloudy = Weather::new(crate::presets::WeatherPreset::Overcast.conditions());
        let uniform = uniform_for(cloudy, WeatherConfig::default());
        let near = uniform.cloud_params4.x;
        let far = uniform.cloud_params4.y;
        assert!(near > 0.0, "erosion should survive nearby");
        assert!(far > near * 2.0, "the fade is too abrupt: {near} to {far}");
    }

    #[test]
    fn quality_drives_the_cloud_step_count() {
        let cloudy = Weather::new(crate::presets::WeatherPreset::Overcast.conditions());
        for quality in [
            crate::config::Quality::Low,
            crate::config::Quality::Medium,
            crate::config::Quality::High,
            crate::config::Quality::Ultra,
        ] {
            let uniform = uniform_for(
                cloudy.clone(),
                WeatherConfig {
                    quality,
                    ..Default::default()
                },
            );
            assert_eq!(uniform.cloud_params3.z as u32, quality.cloud_steps());
            assert_eq!(uniform.cloud_params3.w as u32, quality.cloud_light_steps());
        }
    }

    #[test]
    fn the_fullscreen_triangle_covers_the_viewport() {
        let mesh = fullscreen_triangle();
        assert_eq!(mesh.count_vertices(), 3);
        // Every corner of NDC space must fall inside the triangle.
        let positions = match mesh.attribute(Mesh::ATTRIBUTE_POSITION).unwrap() {
            bevy::mesh::VertexAttributeValues::Float32x3(values) => values.clone(),
            other => panic!("unexpected position format: {other:?}"),
        };
        let point_in_triangle = |p: [f32; 2]| {
            let sign = |a: &[f32; 3], b: &[f32; 3], c: [f32; 2]| {
                (c[0] - b[0]) * (a[1] - b[1]) - (a[0] - b[0]) * (c[1] - b[1])
            };
            let d1 = sign(&positions[0], &positions[1], p);
            let d2 = sign(&positions[1], &positions[2], p);
            let d3 = sign(&positions[2], &positions[0], p);
            let has_negative = d1 < 0.0 || d2 < 0.0 || d3 < 0.0;
            let has_positive = d1 > 0.0 || d2 > 0.0 || d3 > 0.0;
            !(has_negative && has_positive)
        };
        for corner in [[-1.0, -1.0], [1.0, -1.0], [-1.0, 1.0], [1.0, 1.0]] {
            assert!(point_in_triangle(corner), "corner {corner:?} not covered");
        }
    }
}
