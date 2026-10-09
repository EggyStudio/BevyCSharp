//! Rain and snow.
//!
//! The particle field is entirely GPU-resident. A static mesh of quads carries
//! one random seed per particle; the vertex shader turns that seed plus the
//! clock into a falling, wind-blown, world-anchored particle and billboards the
//! quad around it. Nothing is simulated on the CPU and no buffer is re-uploaded
//! per frame, so tens of thousands of drops cost one draw call.
//!
//! Particles are wrapped into a box centred on the camera. Because the wrap
//! happens on an absolute world-space position, walking forward moves you
//! *through* the rain instead of dragging the box along with you.

use bevy::app::{App, Plugin, Startup, Update};
use bevy::asset::RenderAssetUsages;
use bevy::asset::{Asset, Assets, Handle, embedded_asset};
use bevy::camera::visibility::{NoFrustumCulling, Visibility};
use bevy::color::{Color, ColorToComponents};
use bevy::ecs::component::Component;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query, Res, ResMut};
use bevy::material::AlphaMode;
use bevy::math::{Vec3, Vec4};
use bevy::mesh::{Indices, Mesh, Mesh3d, PrimitiveTopology};
use bevy::pbr::{Material, MaterialPipeline, MaterialPipelineKey, MaterialPlugin, MeshMaterial3d};
use bevy::reflect::{Reflect, TypePath};
use bevy::render::mesh::MeshVertexBufferLayoutRef;
use bevy::render::render_resource::{
    AsBindGroup, RenderPipelineDescriptor, ShaderType, SpecializedMeshPipelineError,
};
use bevy::shader::ShaderRef;
use bevy::time::Time;
use bevy::transform::components::Transform;

use crate::WeatherSystems;
use crate::celestial::CelestialBodies;
use crate::config::{Quality, WeatherConfig};
use crate::math::hash2_f32;
use crate::state::Weather;
use crate::wind::Wind;
use bevy::ecs::change_detection::DetectChanges;
use bevy::ecs::reflect::ReflectComponent;
use bevy::ecs::reflect::ReflectResource;
use bevy::reflect::std_traits::ReflectDefault;

const SHADER_PATH: &str = "embedded://bevy_weather/precipitation/precipitation.wesl";

/// Which kind of particle an entity draws.
#[derive(Component, Debug, Clone, Copy, PartialEq, Eq, Reflect)]
#[reflect(Component)]
pub enum PrecipitationKind {
    /// Motion-blurred streaks aligned to their velocity.
    Rain,
    /// Camera-facing flakes that drift on the turbulence.
    Snow,
}

impl PrecipitationKind {
    /// This kind's intensity in the given conditions, `0.0..=1.0`.
    #[inline]
    pub fn intensity(self, conditions: &crate::state::WeatherConditions) -> f32 {
        match self {
            PrecipitationKind::Rain => conditions.rain,
            PrecipitationKind::Snow => conditions.snow,
        }
    }
}

/// Look and cost of rain and snow.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct PrecipitationConfig {
    /// Pin this subsystem to its own quality tier, overriding
    /// [`WeatherConfig::quality`](crate::config::WeatherConfig::quality).
    ///
    /// For a settings menu that exposes rain and snow density separately from everything
    /// else. `None` follows the global dial. Explicit counts on this struct
    /// still win over both.
    pub quality: Option<Quality>,

    /// Particles in the pool. `None` follows
    /// [`WeatherConfig::quality`](crate::config::WeatherConfig::quality).
    ///
    /// This is the count at full intensity; lighter rain draws a subset of the
    /// same pool, so changing intensity never rebuilds the mesh.
    pub particle_count: Option<u32>,

    /// Edge length of the cube of particles around the camera, in world units.
    /// This is effectively how far away you can see individual drops.
    pub box_size: f32,

    /// Terminal velocity of rain, in world units per second.
    pub rain_fall_speed: f32,
    /// Width of a raindrop streak, in world units.
    pub rain_width: f32,
    /// Length of a raindrop streak at the reference speed, in world units.
    pub rain_length: f32,
    /// Opacity of a single raindrop, `0.0..=1.0`.
    ///
    /// Low. Any one drop is almost invisible; rain reads as rain because there
    /// are thousands of them, not because each is solid. Individual drops are
    /// further varied in the shader, so that even at full intensity the field
    /// has faint drops in it rather than being a uniform mesh of lines.
    pub rain_opacity: f32,
    /// Tint of the rain.
    ///
    /// Very close to white. A raindrop is a lens, not a pigment: it shows you a
    /// distorted picture of whatever light is around it. Giving it a colour of
    /// its own turns a downpour into a wall of coloured streaks.
    pub rain_color: Color,

    /// Terminal velocity of snow, in world units per second.
    pub snow_fall_speed: f32,
    /// Diameter of a snowflake, in world units.
    pub snow_size: f32,
    /// Opacity of a single flake, `0.0..=1.0`.
    pub snow_opacity: f32,
    /// How far flakes drift sideways on the turbulence, in world units.
    pub snow_flutter: f32,
    /// Tint of the snow.
    pub snow_color: Color,

    /// Ambient light the particles receive when unlit, so precipitation stays
    /// visible at night.
    pub ambient: f32,

    /// Seed for particle placement.
    pub seed: u32,
}

impl Default for PrecipitationConfig {
    fn default() -> Self {
        Self {
            quality: None,
            particle_count: None,
            box_size: 60.0,
            rain_fall_speed: 22.0,
            rain_width: 0.014,
            rain_length: 0.32,
            rain_opacity: 0.22,
            rain_color: Color::srgb(0.92, 0.94, 0.97),
            snow_fall_speed: 1.6,
            snow_size: 0.055,
            snow_opacity: 0.85,
            snow_flutter: 0.9,
            snow_color: Color::srgb(0.97, 0.98, 1.0),
            ambient: 0.03,
            seed: 0x5_0F70,
        }
    }
}

/// Uniform for the precipitation shader.
///
/// Field order and types must match `PrecipitationUniform` in
/// `precipitation.wesl` exactly.
#[derive(ShaderType, Debug, Clone, Default)]
pub struct PrecipitationUniform {
    /// `xyz`: wind velocity. `w`: fall speed.
    pub wind: Vec4,
    /// `x`: box size. `y`: intensity. `z`: time. `w`: mode (0 rain, 1 snow).
    pub params: Vec4,
    /// `x`: width. `y`: length. `z`: flutter. `w`: opacity.
    pub shape: Vec4,
    /// `rgb`: tint.
    pub tint: Vec4,
    /// `rgb`: incident light. `a`: ambient.
    pub light: Vec4,
}

/// The material behind the particle field.
#[derive(Asset, TypePath, AsBindGroup, Debug, Clone, Default)]
pub struct PrecipitationMaterial {
    /// Everything the shader reads.
    #[uniform(0)]
    pub uniform: PrecipitationUniform,
}

impl Material for PrecipitationMaterial {
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
        // The prepass would run the vertex shader again for a depth buffer that
        // transparent particles must not write to anyway.
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
        // Quads are billboarded in the vertex shader and can end up facing
        // either way once the wind tilts them.
        descriptor.primitive.cull_mode = None;

        descriptor.vertex.buffers = vec![layout.0.get_layout(&[
            Mesh::ATTRIBUTE_POSITION.at_shader_location(0),
            Mesh::ATTRIBUTE_UV_0.at_shader_location(2),
            Mesh::ATTRIBUTE_COLOR.at_shader_location(5),
        ])?];

        Ok(())
    }
}

/// Marks the mesh so it can be rebuilt when the particle count changes.
#[derive(Resource, Debug, Clone)]
pub struct PrecipitationMesh {
    /// The shared quad pool. Rain and snow both draw it.
    pub handle: Handle<Mesh>,
    /// How many particles it holds.
    pub count: u32,
}

/// Builds the static quad pool.
///
/// Each particle contributes four vertices. The position attribute holds the
/// particle's seed in `[0, 1)^3` rather than a real position, the UV holds the
/// quad corner, and the colour holds four more per-particle randoms. The vertex
/// shader turns all of that into a position.
pub fn build_precipitation_mesh(count: u32, seed: u32) -> Mesh {
    let count = count.max(1);
    let vertices = count as usize * 4;

    let mut positions: Vec<[f32; 3]> = Vec::with_capacity(vertices);
    let mut uvs: Vec<[f32; 2]> = Vec::with_capacity(vertices);
    let mut randoms: Vec<[f32; 4]> = Vec::with_capacity(vertices);
    let mut indices: Vec<u32> = Vec::with_capacity(count as usize * 6);

    const CORNERS: [[f32; 2]; 4] = [[0.0, 0.0], [1.0, 0.0], [1.0, 1.0], [0.0, 1.0]];

    for particle in 0..count {
        let cell = [
            hash2_f32(particle, seed),
            hash2_f32(particle, seed ^ 0x9E37_79B9),
            hash2_f32(particle, seed ^ 0x85EB_CA6B),
        ];
        // The fourth random is the particle's index, normalised. The shader
        // compares it against intensity to thin the field out, which is why
        // lighter rain is a subset of heavier rain rather than a reshuffle.
        let random = [
            hash2_f32(particle, seed ^ 0xC2B2_AE35),
            hash2_f32(particle, seed ^ 0x27D4_EB2F),
            hash2_f32(particle, seed ^ 0x1656_67B1),
            particle as f32 / count as f32,
        ];

        let base = particle * 4;
        for corner in CORNERS {
            positions.push(cell);
            uvs.push(corner);
            randoms.push(random);
        }
        indices.extend_from_slice(&[base, base + 1, base + 2, base, base + 2, base + 3]);
    }

    Mesh::new(
        PrimitiveTopology::TriangleList,
        RenderAssetUsages::RENDER_WORLD,
    )
    .with_inserted_attribute(Mesh::ATTRIBUTE_POSITION, positions)
    .with_inserted_attribute(Mesh::ATTRIBUTE_UV_0, uvs)
    .with_inserted_attribute(Mesh::ATTRIBUTE_COLOR, randoms)
    .with_inserted_indices(Indices::U32(indices))
}

/// Spawns and drives the rain and snow particle fields.
#[derive(Debug, Clone, Copy, Default)]
pub struct PrecipitationPlugin;

impl Plugin for PrecipitationPlugin {
    fn build(&self, app: &mut App) {
        embedded_asset!(app, "precipitation.wesl");

        app.init_resource::<PrecipitationConfig>()
            .register_type::<PrecipitationConfig>()
            .register_type::<PrecipitationKind>()
            .add_plugins(MaterialPlugin::<PrecipitationMaterial>::default())
            .add_systems(Startup, spawn_precipitation)
            .add_systems(
                Update,
                (rebuild_mesh, drive_precipitation)
                    .chain()
                    .in_set(WeatherSystems::Apply),
            );
    }
}

fn particle_count(config: &WeatherConfig, precipitation: &PrecipitationConfig) -> u32 {
    precipitation.resolved_particle_count(config.quality)
}

fn spawn_precipitation(
    mut commands: Commands,
    mut meshes: ResMut<Assets<Mesh>>,
    mut materials: ResMut<Assets<PrecipitationMaterial>>,
    config: Res<WeatherConfig>,
    precipitation: Res<PrecipitationConfig>,
) {
    let count = particle_count(&config, &precipitation);
    let handle = meshes.add(build_precipitation_mesh(count, precipitation.seed));

    for kind in [PrecipitationKind::Rain, PrecipitationKind::Snow] {
        commands.spawn((
            kind,
            Mesh3d(handle.clone()),
            MeshMaterial3d(materials.add(PrecipitationMaterial::default())),
            // The mesh's "positions" are seeds, so its bounding box says
            // nothing about where the particles actually are.
            NoFrustumCulling,
            Transform::default(),
        ));
    }

    commands.insert_resource(PrecipitationMesh { handle, count });
}

fn rebuild_mesh(
    mut meshes: ResMut<Assets<Mesh>>,
    mut state: Option<ResMut<PrecipitationMesh>>,
    config: Res<WeatherConfig>,
    precipitation: Res<PrecipitationConfig>,
) {
    if !config.is_changed() && !precipitation.is_changed() {
        return;
    }
    let Some(state) = state.as_mut() else {
        return;
    };
    let wanted = particle_count(&config, &precipitation);
    if wanted == state.count {
        return;
    }
    // Rebuilding the pool is only needed when the *capacity* changes, which is
    // a settings change, not something that happens as the weather shifts.
    if let Err(error) = meshes.insert(
        &state.handle,
        build_precipitation_mesh(wanted, precipitation.seed),
    ) {
        bevy::log::warn!("could not resize the precipitation pool: {error}");
        return;
    }
    state.count = wanted;
}

#[expect(
    clippy::too_many_arguments,
    reason = "a Bevy system reading every resource the particles depend on"
)]
fn drive_precipitation(
    mut materials: ResMut<Assets<PrecipitationMaterial>>,
    mut particles: Query<(
        &PrecipitationKind,
        &MeshMaterial3d<PrecipitationMaterial>,
        &mut Visibility,
    )>,
    config: Res<WeatherConfig>,
    precipitation: Res<PrecipitationConfig>,
    weather: Res<Weather>,
    bodies: Res<CelestialBodies>,
    wind: Res<Wind>,
    time: Res<Time>,
) {
    let conditions = weather.current;
    let elapsed = time.elapsed_secs_wrapped();

    for (kind, handle, mut visibility) in &mut particles {
        // Hide a field that is not falling. Both fields exist at all times so
        // that rain and snow can overlap into sleet, but an idle one would
        // otherwise still run its vertex shader over every quad in the pool
        // every frame just to move them all off-screen -- and for most weather,
        // one of the two is always idle.
        let falling = config.precipitation && kind.intensity(&conditions) > 0.0;
        let wanted = if falling {
            Visibility::Inherited
        } else {
            Visibility::Hidden
        };
        if *visibility != wanted {
            *visibility = wanted;
        }

        // Nothing downstream reads a hidden field's uniform, and writing it
        // anyway is not free: `Assets::get_mut` marks the material modified, so
        // Bevy re-extracts it and re-uploads its buffer every frame for a
        // material that is not being drawn. For most weather that is one of the
        // two fields, all of the time.
        if !falling {
            continue;
        }

        if let Some(mut material) = materials.get_mut(&handle.0) {
            material.uniform = build_uniform(
                *kind,
                &config,
                &precipitation,
                &conditions,
                &bodies,
                &wind,
                elapsed,
            );
        }
    }
}

/// Assembles the shader uniform for one particle field.
///
/// Split out from the system so it can be tested without a render world.
pub fn build_uniform(
    kind: PrecipitationKind,
    config: &WeatherConfig,
    precipitation: &PrecipitationConfig,
    conditions: &crate::state::WeatherConditions,
    bodies: &CelestialBodies,
    wind: &Wind,
    elapsed: f32,
) -> PrecipitationUniform {
    let intensity = if config.precipitation {
        kind.intensity(conditions)
    } else {
        0.0
    };

    // Direct light on the particles: sunlight by day, moonlight by night, with
    // heavy cloud dimming both. Precipitation implies cloud, so this is nearly
    // always the dim case -- which is the point.
    let overcast = 1.0 - 0.75 * conditions.cloud_coverage * conditions.cloud_density;
    let sun = bodies.sun_color.to_vec3() * bodies.daylight;
    let moon = Vec3::new(0.35, 0.42, 0.6) * bodies.moon_illumination * (1.0 - bodies.daylight);
    let lit = (sun + moon) * overcast * config.light_intensity_scale.max(0.0);

    let (fall_speed, width, length, flutter, opacity, tint) = match kind {
        PrecipitationKind::Rain => (
            precipitation.rain_fall_speed,
            precipitation.rain_width,
            precipitation.rain_length,
            0.0,
            precipitation.rain_opacity,
            precipitation.rain_color,
        ),
        PrecipitationKind::Snow => (
            precipitation.snow_fall_speed,
            precipitation.snow_size,
            precipitation.snow_size,
            precipitation.snow_flutter * (0.3 + conditions.turbulence),
            precipitation.snow_opacity,
            precipitation.snow_color,
        ),
    };

    PrecipitationUniform {
        wind: wind.velocity3().extend(fall_speed.max(0.01)),
        params: Vec4::new(
            precipitation.box_size.max(1.0),
            intensity.clamp(0.0, 1.0),
            elapsed,
            match kind {
                PrecipitationKind::Rain => 0.0,
                PrecipitationKind::Snow => 1.0,
            },
        ),
        shape: Vec4::new(
            width.max(1e-4),
            length.max(1e-4),
            flutter,
            opacity.clamp(0.0, 1.0),
        ),
        tint: tint.to_linear().to_vec3().extend(1.0),
        light: lit.extend(precipitation.ambient.max(0.0)),
    }
}

impl PrecipitationConfig {
    /// The quality tier precipitation runs at, resolving
    /// [`quality`](Self::quality) against the global dial.
    pub fn quality(&self, global: Quality) -> Quality {
        self.quality.unwrap_or(global)
    }

    /// Particles to allocate, resolving the explicit override, then the tier.
    pub fn resolved_particle_count(&self, global: Quality) -> u32 {
        self.particle_count
            .unwrap_or_else(|| self.quality(global).particle_count())
            .clamp(1, 1_000_000)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::celestial::compute_celestial;
    use crate::presets::WeatherPreset;
    use crate::time::WeatherTime;

    #[test]
    fn the_mesh_has_four_vertices_and_six_indices_per_particle() {
        let mesh = build_precipitation_mesh(100, 1);
        assert_eq!(mesh.count_vertices(), 400);
        match mesh.indices().unwrap() {
            Indices::U32(indices) => assert_eq!(indices.len(), 600),
            other => panic!("expected 32-bit indices, got {other:?}"),
        }
    }

    #[test]
    fn every_particle_seed_is_inside_the_unit_cube() {
        let mesh = build_precipitation_mesh(500, 42);
        let positions = match mesh.attribute(Mesh::ATTRIBUTE_POSITION).unwrap() {
            bevy::mesh::VertexAttributeValues::Float32x3(values) => values,
            other => panic!("unexpected format: {other:?}"),
        };
        for seed in positions {
            for axis in seed {
                assert!((0.0..1.0).contains(axis), "seed {axis} out of range");
            }
        }
    }

    #[test]
    fn the_four_corners_of_a_quad_share_one_seed() {
        let mesh = build_precipitation_mesh(50, 7);
        let positions = match mesh.attribute(Mesh::ATTRIBUTE_POSITION).unwrap() {
            bevy::mesh::VertexAttributeValues::Float32x3(values) => values,
            other => panic!("unexpected format: {other:?}"),
        };
        for particle in 0..50usize {
            let first = positions[particle * 4];
            for corner in 1..4 {
                assert_eq!(positions[particle * 4 + corner], first);
            }
        }
    }

    #[test]
    fn quad_corners_cover_the_unit_square() {
        let mesh = build_precipitation_mesh(4, 7);
        let uvs = match mesh.attribute(Mesh::ATTRIBUTE_UV_0).unwrap() {
            bevy::mesh::VertexAttributeValues::Float32x2(values) => values,
            other => panic!("unexpected format: {other:?}"),
        };
        assert_eq!(
            &uvs[0..4],
            &[[0.0, 0.0], [1.0, 0.0], [1.0, 1.0], [0.0, 1.0]]
        );
    }

    #[test]
    fn particle_indices_are_evenly_spread_so_intensity_thins_smoothly() {
        let count = 1_000;
        let mesh = build_precipitation_mesh(count, 3);
        let randoms = match mesh.attribute(Mesh::ATTRIBUTE_COLOR).unwrap() {
            bevy::mesh::VertexAttributeValues::Float32x4(values) => values,
            other => panic!("unexpected format: {other:?}"),
        };
        // Half the particles should survive an intensity of 0.5.
        let surviving = randoms
            .iter()
            .step_by(4)
            .filter(|random| random[3] <= 0.5)
            .count();
        assert!(
            (surviving as i32 - count as i32 / 2).unsigned_abs() < 5,
            "{surviving} of {count} survived"
        );
    }

    #[test]
    fn a_zero_particle_request_still_builds_a_valid_mesh() {
        let mesh = build_precipitation_mesh(0, 1);
        assert_eq!(mesh.count_vertices(), 4);
    }

    fn uniform_for(kind: PrecipitationKind, preset: WeatherPreset) -> PrecipitationUniform {
        let time = WeatherTime::default();
        build_uniform(
            kind,
            &WeatherConfig::default(),
            &PrecipitationConfig::default(),
            &preset.conditions(),
            &compute_celestial(&time),
            &Wind::default(),
            0.0,
        )
    }

    #[test]
    fn intensity_comes_from_the_matching_weather_field() {
        let rain = uniform_for(PrecipitationKind::Rain, WeatherPreset::Rain);
        assert!((rain.params.y - WeatherPreset::Rain.conditions().rain).abs() < 1e-6);

        let snow_in_rain = uniform_for(PrecipitationKind::Snow, WeatherPreset::Rain);
        assert_eq!(snow_in_rain.params.y, 0.0);

        let snow = uniform_for(PrecipitationKind::Snow, WeatherPreset::Blizzard);
        assert!(snow.params.y > 0.9);
    }

    #[test]
    fn the_mode_flag_distinguishes_rain_from_snow() {
        assert_eq!(
            uniform_for(PrecipitationKind::Rain, WeatherPreset::Rain)
                .params
                .w,
            0.0
        );
        assert_eq!(
            uniform_for(PrecipitationKind::Snow, WeatherPreset::Snow)
                .params
                .w,
            1.0
        );
    }

    #[test]
    fn disabling_precipitation_zeroes_the_intensity() {
        let time = WeatherTime::default();
        let uniform = build_uniform(
            PrecipitationKind::Rain,
            &WeatherConfig {
                precipitation: false,
                ..Default::default()
            },
            &PrecipitationConfig::default(),
            &WeatherPreset::Storm.conditions(),
            &compute_celestial(&time),
            &Wind::default(),
            0.0,
        );
        assert_eq!(uniform.params.y, 0.0);
    }

    #[test]
    fn snow_flutters_more_in_turbulent_air() {
        let time = WeatherTime::default();
        let bodies = compute_celestial(&time);
        let calm = build_uniform(
            PrecipitationKind::Snow,
            &WeatherConfig::default(),
            &PrecipitationConfig::default(),
            &WeatherPreset::LightSnow.conditions(),
            &bodies,
            &Wind::default(),
            0.0,
        );
        let gale = build_uniform(
            PrecipitationKind::Snow,
            &WeatherConfig::default(),
            &PrecipitationConfig::default(),
            &WeatherPreset::Blizzard.conditions(),
            &bodies,
            &Wind::default(),
            0.0,
        );
        assert!(gale.shape.z > calm.shape.z);
    }

    #[test]
    fn quality_drives_the_particle_count() {
        for quality in [
            crate::config::Quality::Low,
            crate::config::Quality::Medium,
            crate::config::Quality::High,
            crate::config::Quality::Ultra,
        ] {
            let config = WeatherConfig {
                quality,
                ..Default::default()
            };
            assert_eq!(
                particle_count(&config, &PrecipitationConfig::default()),
                quality.particle_count()
            );
        }
    }

    #[test]
    fn a_pinned_precipitation_tier_ignores_the_global_dial() {
        let precipitation = PrecipitationConfig {
            quality: Some(Quality::Ultra),
            ..Default::default()
        };
        assert_eq!(
            precipitation.resolved_particle_count(Quality::Potato),
            Quality::Ultra.particle_count()
        );
    }

    #[test]
    fn an_explicit_particle_count_overrides_quality() {
        let config = WeatherConfig {
            quality: crate::config::Quality::Low,
            ..Default::default()
        };
        let precipitation = PrecipitationConfig {
            particle_count: Some(777),
            ..Default::default()
        };
        assert_eq!(particle_count(&config, &precipitation), 777);
    }
}
