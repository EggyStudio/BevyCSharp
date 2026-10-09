//! Shadows cast on the ground by the volumetric cloud deck.
//!
//! # How this works
//!
//! Bevy's [`DirectionalLightTexture`] is a light cookie: a texture projected
//! along a directional light, multiplying its intensity per fragment. That is
//! exactly the shape of a cloud shadow, so no extra render pass is needed --
//! this module only has to keep the texture up to date.
//!
//! The projection happens in the light's own local frame, which means the
//! sun-parallel component of a world position is discarded before the lookup.
//! That discarding *is* the shadow projection: two points that differ by a step
//! along the sun direction sample the same texel, so a ground point samples
//! whatever the cloud deck is doing directly up-sun of it, with the long
//! stretching of low-angle shadows falling out for free.
//!
//! The texture itself is built on the CPU from [`CloudField`], the same field
//! the sky shader raymarches, so the shadows are the shadows of the clouds that
//! are actually up there and they travel with the same wind.
//!
//! # Costs and caveats
//!
//! Building the texture is the expensive part, so it is rebuilt a few times a
//! second and spread across frames a slice at a time; between rebuilds the wind
//! is carried by translating the light, which is free and continuous.
//!
//! Light cookies ride on Bevy's clustered decal machinery, which needs both the
//! `pbr_light_textures` feature and a GPU with texture binding arrays. Where
//! either is missing the texture is simply never sampled -- lighting is
//! completely normal, just unshadowed by cloud.

use bevy::app::{App, Plugin, Startup, Update};
use bevy::asset::{Assets, Handle, RenderAssetUsages};
use bevy::ecs::query::With;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query, Res, ResMut};
use bevy::image::{Image, ImageSampler};
use bevy::light::DirectionalLightTexture;
use bevy::math::{Vec2, Vec3};
use bevy::prelude::Entity;
use bevy::reflect::Reflect;
use bevy::render::render_resource::{Extent3d, TextureDimension, TextureFormat};
use bevy::time::Time;
use bevy::transform::components::Transform;

use crate::WeatherSystems;
use crate::celestial::{CelestialBodies, SunLight};
use crate::cloud_field::CloudField;
use crate::clouds::CloudConfig;
use crate::config::WeatherConfig;
use crate::state::Weather;
use crate::wind::Wind;
use bevy::ecs::reflect::ReflectResource;
use bevy::reflect::std_traits::ReflectDefault;

/// Settings for ground shadows cast by the cloud deck.
#[derive(Resource, Debug, Clone, Reflect)]
#[reflect(Resource, Default)]
pub struct CloudShadowConfig {
    /// Cast cloud shadows at all.
    pub enabled: bool,

    /// Edge length of the shadow texture, in texels.
    ///
    /// The whole texture is rebuilt on the CPU, so this is quadratic: a rebuild
    /// costs about 1.5 ms at 64, 6 ms at 128 and 20 ms at 256, spread across
    /// the frames of [`update_seconds`](Self::update_seconds) rather than
    /// landing on one of them.
    ///
    /// There is little point going high. Cloud shadow edges are soft over a
    /// hundred metres or more in any case -- that is the penumbra of a
    /// half-degree sun two kilometres up -- so past a texel or so per hundred
    /// metres of [`tile_size`](Self::tile_size) the extra detail is blurred
    /// away again by the thing it is trying to resolve.
    pub resolution: u32,

    /// Width of one tile of the shadow pattern, in world units.
    ///
    /// The pattern repeats at this spacing. Larger hides the repetition and
    /// costs nothing, but spends the same texels over more ground; a few
    /// kilometres is the usual compromise.
    pub tile_size: f32,

    /// How dark the deepest shadow goes, `0.0..=1.0`.
    ///
    /// Never all the way to zero in reality: a shaded patch of ground is still
    /// lit by the whole rest of the sky, and this plugin feeds that in through
    /// the ambient term -- but the cookie multiplies the *direct* light only, so
    /// leaving headroom here is what keeps a cloud shadow from reading as a
    /// hole cut in the world.
    pub strength: f32,

    /// Seconds between rebuilds of the shadow texture.
    ///
    /// Between rebuilds the pattern still slides with the wind, which is what
    /// the eye actually reads; a rebuild is only needed as the deck changes
    /// shape and as the sun moves. The work is spread across the frames in the
    /// interval rather than landing on one of them.
    pub update_seconds: f32,
}

impl Default for CloudShadowConfig {
    fn default() -> Self {
        Self {
            enabled: true,
            resolution: 128,
            tile_size: 6_000.0,
            strength: 0.8,
            update_seconds: 0.5,
        }
    }
}

/// Below this much sun elevation, shadows stop being projected.
///
/// The projection divides by the sine of the elevation, so it runs away to
/// infinity at the horizon. It is also pointless down there: the sun's own
/// illuminance has already been faded out, so there is nothing left to shadow.
const MIN_SUN_ELEVATION: f32 = 0.09;

/// The live shadow texture, and the state of the rebuild filling it.
#[derive(Resource)]
pub struct CloudShadowMap {
    /// The texture handed to the sun's [`DirectionalLightTexture`].
    pub image: Handle<Image>,
    /// Scratch buffer the rebuild writes into, swapped in when complete.
    scratch: Vec<u8>,
    /// Next row to fill, or `resolution` when the rebuild has finished.
    row: u32,
    /// Edge length the scratch buffer was allocated for.
    resolution: u32,
    /// Everything the rebuild in flight is using, frozen when it started.
    ///
    /// Frozen rather than read live, because a rebuild spans many frames and
    /// the weather does not hold still for it. Read the sun direction per row
    /// and the top of the texture is lit from somewhere the bottom is not;
    /// read the tile size per row and the two halves are at different scales.
    settings: RebuildInputs,
    /// Seconds until the next rebuild starts.
    countdown: f32,
}

/// The inputs to one rebuild of the shadow texture.
#[derive(Debug, Clone, Copy)]
struct RebuildInputs {
    field: CloudField,
    sun: Vec3,
    /// Half the tile width, in metres.
    half_tile: f32,
    strength: f32,
    extinction: f32,
}

/// Keeps [`CloudShadowMap`] up to date and attached to the sun.
#[derive(Debug, Clone, Copy, Default)]
pub struct CloudShadowPlugin;

impl Plugin for CloudShadowPlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<CloudShadowConfig>()
            .register_type::<CloudShadowConfig>()
            .add_systems(Startup, spawn_shadow_map)
            .add_systems(Update, drive_cloud_shadows.in_set(WeatherSystems::Apply));
    }
}

/// Bytes per texel of the shadow texture.
///
/// The light itself only reads the red channel, so a single-channel texture
/// would do -- except that Bevy routes light cookies through the same machinery
/// as clustered decals, and anything in that list is *also* composited onto
/// surfaces as a decal, over the base colour, using its alpha.
///
/// A one-channel texture samples as `(r, 0, 0, 1)`: fully opaque pure red. The
/// result is that every surface within range of the light is painted flat red.
/// Four channels with a zero alpha make that composite a no-op while the light
/// still reads exactly the same red channel.
const CHANNELS: usize = 4;

/// Builds a blank, fully-lit shadow texture.
fn blank_image(resolution: u32) -> Image {
    let mut image = Image::new_fill(
        Extent3d {
            width: resolution,
            height: resolution,
            depth_or_array_layers: 1,
        },
        TextureDimension::D2,
        // Full brightness and zero alpha: no shadow until the first rebuild
        // lands, and nothing painted onto any surface ever.
        &[255, 255, 255, 0],
        TextureFormat::Rgba8Unorm,
        // Kept in the main world as well as the render world, because this is
        // rewritten every rebuild rather than uploaded once.
        RenderAssetUsages::MAIN_WORLD | RenderAssetUsages::RENDER_WORLD,
    );
    image.sampler = ImageSampler::linear();
    image
}

/// A buffer of fully-lit texels.
fn blank_scratch(resolution: u32) -> Vec<u8> {
    let mut scratch = vec![255; (resolution * resolution) as usize * CHANNELS];
    for texel in scratch.as_chunks_mut::<CHANNELS>().0 {
        texel[3] = 0;
    }
    scratch
}

fn spawn_shadow_map(
    mut commands: Commands,
    config: Res<CloudShadowConfig>,
    mut images: ResMut<Assets<Image>>,
) {
    let resolution = config.resolution.clamp(8, 1024);
    let image = images.add(blank_image(resolution));
    commands.insert_resource(CloudShadowMap {
        image,
        scratch: blank_scratch(resolution),
        row: resolution,
        resolution,
        settings: RebuildInputs {
            field: CloudField::new(
                &crate::state::WeatherConditions::default(),
                &CloudConfig::default(),
                Vec2::ZERO,
                0.0,
            ),
            sun: Vec3::Y,
            half_tile: CloudShadowConfig::default().tile_size * 0.5,
            strength: 0.0,
            extinction: CloudConfig::default().extinction,
        },
        countdown: 0.0,
    });
}

#[expect(
    clippy::too_many_arguments,
    reason = "the shadow map is a function of the sky, the wind and the sun"
)]
fn drive_cloud_shadows(
    mut commands: Commands,
    config: Res<WeatherConfig>,
    shadows: Res<CloudShadowConfig>,
    clouds: Res<CloudConfig>,
    weather: Res<Weather>,
    wind: Res<Wind>,
    bodies: Res<CelestialBodies>,
    time: Res<Time>,
    mut images: ResMut<Assets<Image>>,
    mut map: ResMut<CloudShadowMap>,
    sun: Query<(Entity, &mut Transform, Option<&DirectionalLightTexture>), With<SunLight>>,
) {
    let on = shadows.enabled && config.clouds && config.celestial_lights;

    let mut sun = sun;
    let Ok((entity, mut transform, existing)) = sun.single_mut() else {
        return;
    };

    if !on {
        if existing.is_some() {
            commands.entity(entity).remove::<DirectionalLightTexture>();
            transform.scale = Vec3::ONE;
            transform.translation = Vec3::ZERO;
        }
        return;
    }

    // Resize on demand rather than at startup only, so the resolution can be
    // driven from a settings menu.
    //
    // Only a resize abandons the rebuild in flight. Restarting on any change to
    // the config at all looks tidier and is a trap: a settings screen that
    // writes its resource every frame -- which is what an immediate-mode slider
    // does whether or not you touched it -- would restart the rebuild every
    // frame, so it would never reach the end and the texture would never be
    // uploaded at all. Every other setting is picked up by the next rebuild,
    // which is at most `update_seconds` away.
    let resolution = shadows.resolution.clamp(8, 1024);
    if resolution != map.resolution {
        map.resolution = resolution;
        map.scratch = blank_scratch(resolution);
        map.image = images.add(blank_image(resolution));
        commands.entity(entity).remove::<DirectionalLightTexture>();
        map.row = resolution;
        map.countdown = 0.0;
    }

    if existing.is_none() {
        commands.entity(entity).insert(DirectionalLightTexture {
            image: map.image.clone(),
            tiled: true,
        });
    }

    // The cookie spans local xy in [-1, 1], so one tile is twice the scale.
    let scale = (shadows.tile_size.max(1.0)) * 0.5;
    // Scrolling the whole projection by the deck's displacement is what carries
    // the wind between rebuilds. In world units, so a scene authored in
    // centimetres still tracks.
    let metres_to_world = config.units_per_meter.max(1e-6);
    let drift = wind.offset3() * clouds.wind_multiplier * metres_to_world;
    transform.scale = Vec3::splat(scale);
    transform.translation = Vec3::new(drift.x, 0.0, drift.z);

    // --- rebuild, a slice of rows at a time ---------------------------------
    if map.row >= map.resolution {
        map.countdown -= time.delta_secs();
        if map.countdown > 0.0 {
            return;
        }
        // Freeze the inputs for the whole rebuild.
        map.settings = RebuildInputs {
            field: CloudField::new(
                &weather.current,
                &clouds,
                // No wind: the displacement is carried by the transform above,
                // so baking it in as well would move the shadows at twice the
                // speed.
                Vec2::ZERO,
                time.elapsed_secs_wrapped(),
            ),
            sun: bodies.sun_direction,
            half_tile: scale / metres_to_world,
            strength: shadows.strength,
            extinction: clouds.extinction.max(1e-6),
        };
        map.row = 0;
    }

    let resolution = map.resolution;
    let interval = shadows.update_seconds.max(1.0 / 240.0);
    // Spread the work over the interval, so a rebuild never lands on one frame.
    let frames = (interval / time.delta_secs().max(1e-4)).clamp(1.0, 240.0);
    let rows_per_frame = ((resolution as f32 / frames).ceil() as u32).max(1);

    let end = (map.row + rows_per_frame).min(resolution);
    fill_rows(&mut map, end);
    map.row = end;

    if map.row >= resolution {
        if let Some(mut image) = images.get_mut(&map.image) {
            image.data = Some(map.scratch.clone());
        }
        map.countdown = interval;
    }
}

/// Fills rows `[map.row, end)` of the scratch buffer, from
/// [`CloudShadowMap::settings`].
fn fill_rows(map: &mut CloudShadowMap, end: u32) {
    let resolution = map.resolution;
    let RebuildInputs {
        field,
        sun,
        half_tile,
        strength,
        extinction,
    } = map.settings;
    let strength = strength.clamp(0.0, 1.0);

    // The projection divides by the sun's elevation, so a sun on the horizon
    // would stretch a tile across the county. Clamped, and faded out as well,
    // because shadows from a sun that low are not a thing anyone sees.
    let elevation = sun.y;
    let fade = ((elevation - MIN_SUN_ELEVATION * 0.5) / (MIN_SUN_ELEVATION * 0.5)).clamp(0.0, 1.0);
    let elevation = elevation.max(MIN_SUN_ELEVATION);
    let deck = field.base_altitude + field.thickness * 0.5;

    // The light's local frame: two axes across the beam, one along it. This
    // matches `Transform::looking_to(-sun)`, which is how the sun light is
    // oriented.
    let forward = -sun;
    let right = Vec3::Y.cross(forward).normalize_or(Vec3::X);
    let up = forward.cross(right);

    for y in map.row..end {
        for x in 0..resolution {
            // Texel centre in [0, 1), then into the light's local square.
            let u = (x as f32 + 0.5) / resolution as f32;
            let v = (y as f32 + 0.5) / resolution as f32;

            // The pattern has to repeat -- it is one texture standing in for
            // an unbounded sky -- and a field sampled straight would not line
            // up where it wraps, leaving a hard line ruled across the ground
            // every few kilometres. So near each edge the field is cross-faded
            // into the copy from the opposite edge, with a weight that reaches
            // the same value at both ends of the tile.
            //
            // Only *near* each edge, though. Fading across the whole tile is
            // the textbook way to make a texture wrap, and it is the wrong one
            // here: averaging two uncorrelated copies of a field everywhere
            // halves its variance, and a cloud shadow map with half its
            // contrast is an evenly grey field with a suggestion of cloud in
            // it. Confining the blend to a narrow band leaves the great
            // majority of the tile at full contrast, and costs about a quarter
            // of an extra evaluation per texel rather than three.
            let wu = seam_weight(u, resolution);
            let wv = seam_weight(v, resolution);
            let mut total = 0.0;
            for (du, wu) in [(0.0, 1.0 - wu), (1.0, wu)] {
                for (dv, wv) in [(0.0, 1.0 - wv), (1.0, wv)] {
                    let weight = wu * wv;
                    if weight <= 0.0 {
                        continue;
                    }
                    let point =
                        ray_hits_deck(u - du, v - dv, half_tile, right, up, sun, elevation, deck);
                    total += weight * field.column_density(point.x, point.y);
                }
            }

            // Beer-Lambert on the column, using the same extinction the sky
            // shader marches with, so a deck that looks opaque from below casts
            // a shadow that is opaque from above. Cloud is far more optically
            // thick than it looks: a few hundred metres of even moderate
            // density puts the ground firmly in shade, which is why the edge of
            // a cumulus shadow is sharp rather than a gentle gradient.
            let transmittance = (-total * extinction).exp();
            let shade = 1.0 - strength * fade * (1.0 - transmittance);
            let byte = (shade.clamp(0.0, 1.0) * 255.0).round() as u8;
            let index = (y * resolution + x) as usize * CHANNELS;
            map.scratch[index] = byte;
            map.scratch[index + 1] = byte;
            map.scratch[index + 2] = byte;
            map.scratch[index + 3] = 0;
        }
    }
}

/// Fraction of the tile's edge that cross-fades into the opposite edge.
const SEAM_BAND: f32 = 0.14;

/// How much of the wrapped copy to mix in at a given coordinate.
///
/// Zero across the interior, rising smoothly to one by the last texel centre.
///
/// Reaching one at the last *texel* rather than at the mathematical edge is the
/// detail that makes the wrap exact. Texel centres sit half a texel inside the
/// tile, so a ramp that only finishes at `1.0` leaves the outermost column
/// still part original and part copy -- a small step, but a step, drawn as a
/// line across the world. Finishing early means the first and last columns are
/// the field itself sampled half a texel either side of the wrap point, which
/// is exactly what a continuous field would have put there.
#[inline]
fn seam_weight(t: f32, resolution: u32) -> f32 {
    let last = 1.0 - 0.5 / resolution as f32;
    let x = ((t - (last - SEAM_BAND)) / SEAM_BAND).clamp(0.0, 1.0);
    x * x * (3.0 - 2.0 * x)
}

/// Where the light ray through a cookie texel crosses the middle of the deck.
///
/// Returns horizontal metres as `(x, z)`.
#[expect(
    clippy::too_many_arguments,
    reason = "an inner loop helper; every argument is hoisted out of the loop"
)]
#[inline]
fn ray_hits_deck(
    u: f32,
    v: f32,
    half_tile: f32,
    right: Vec3,
    up: Vec3,
    sun: Vec3,
    elevation: f32,
    deck: f32,
) -> Vec2 {
    // Undo `decal_uv = local.xy * vec2(-0.5, 0.5) + 0.5`.
    let local_x = 1.0 - 2.0 * u;
    let local_y = 2.0 * v - 1.0;
    let base = (right * local_x + up * local_y) * half_tile;
    // Walk up the sun direction until the deck's mid-height.
    let travel = (deck - base.y) / elevation;
    let hit = base + sun * travel;
    Vec2::new(hit.x, hit.z)
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::presets::WeatherPreset;

    /// A map ready to have its rows filled, with the rebuild inputs frozen the
    /// way the driver would have frozen them.
    fn map_for(
        size: u32,
        preset: WeatherPreset,
        sun: Vec3,
        half_tile: f32,
        strength: f32,
    ) -> CloudShadowMap {
        CloudShadowMap {
            image: Handle::default(),
            scratch: blank_scratch(size),
            row: 0,
            resolution: size,
            settings: RebuildInputs {
                field: CloudField::new(
                    &preset.conditions(),
                    &CloudConfig::default(),
                    Vec2::ZERO,
                    0.0,
                ),
                sun,
                half_tile,
                strength,
                extinction: CloudConfig::default().extinction,
            },
            countdown: 0.0,
        }
    }

    #[test]
    fn the_projection_is_constant_along_the_sun() {
        // Two texels that differ only by a step along the sun direction must
        // land on the same patch of cloud -- that identity is the whole reason
        // a light cookie can stand in for a shadow map.
        let sun = Vec3::new(0.3, 0.6, -0.74).normalize();
        let forward = -sun;
        let right = Vec3::Y.cross(forward).normalize_or(Vec3::X);
        let up = forward.cross(right);

        let a = ray_hits_deck(0.3, 0.7, 2000.0, right, up, sun, sun.y, 1500.0);
        // The same point on the cookie always resolves to the same cloud,
        // whatever height the deck is queried from, up to the shear.
        let b = ray_hits_deck(0.3, 0.7, 2000.0, right, up, sun, sun.y, 1500.0);
        assert!((a - b).length() < 1e-3);
    }

    #[test]
    fn a_higher_deck_moves_its_shadow_up_sun() {
        // A cloud twice as high throws its shadow twice as far, in the
        // direction the light is coming from.
        let sun = Vec3::new(0.0, 0.5, -0.866).normalize();
        let forward = -sun;
        let right = Vec3::Y.cross(forward).normalize_or(Vec3::X);
        let up = forward.cross(right);

        let low = ray_hits_deck(0.5, 0.5, 2000.0, right, up, sun, sun.y, 1000.0);
        let high = ray_hits_deck(0.5, 0.5, 2000.0, right, up, sun, sun.y, 2000.0);
        let step = high - low;
        // Displaced along the sun's compass bearing.
        let bearing = Vec2::new(sun.x, sun.z).normalize();
        assert!(step.length() > 1.0);
        assert!(
            step.normalize().dot(bearing) > 0.999,
            "shadow moved the wrong way: {step:?} against {bearing:?}"
        );
    }

    #[test]
    fn the_tile_matches_itself_at_its_edges() {
        // The cross-fade exists so the pattern wraps. If the two edges disagree
        // there is a line ruled across the world, repeating every tile.
        //
        // What is checked is not that the edges are *equal* -- they are one
        // texel apart, and a cloud edge falling there should show a step, the
        // same as anywhere else. It is that the step across the wrap is no
        // bigger than the steps inside the tile. A seam is a seam precisely
        // because it stands out from its surroundings.
        const SIZE: u32 = 48;
        let mut map = map_for(
            SIZE,
            WeatherPreset::PartlyCloudy,
            Vec3::new(0.2, 0.7, -0.68).normalize(),
            4_000.0,
            1.0,
        );
        fill_rows(&mut map, SIZE);

        let n = SIZE as usize;
        let shade = |x: usize, y: usize| map.scratch[(y * n + x) * CHANNELS] as f32;

        let mut interior = 0.0;
        for y in 0..n {
            for x in 1..n {
                interior += (shade(x, y) - shade(x - 1, y)).abs();
                interior += (shade(y, x) - shade(y, x - 1)).abs();
            }
        }
        interior /= (2 * n * (n - 1)) as f32;

        let mut seam = 0.0;
        for i in 0..n {
            seam += (shade(0, i) - shade(n - 1, i)).abs();
            seam += (shade(i, 0) - shade(i, n - 1)).abs();
        }
        seam /= (2 * n) as f32;

        assert!(
            seam <= interior * 2.0 + 2.0,
            "the tile seam stands out: {seam} across the wrap against {interior} inside"
        );
    }

    #[test]
    fn the_blend_keeps_the_pattern_s_contrast() {
        // Cross-fading two uncorrelated copies of a field over the whole tile
        // is the textbook way to make a texture wrap, and it halves the
        // variance -- which for a shadow map means an evenly grey field with a
        // hint of cloud in it rather than cloud shadows. The blend is confined
        // to a band for this reason, so the interior has to still have its
        // contrast.
        const SIZE: u32 = 48;
        let mut map = map_for(SIZE, WeatherPreset::PartlyCloudy, Vec3::Y, 4_000.0, 0.8);
        fill_rows(&mut map, SIZE);

        let shades: Vec<f32> = map
            .scratch
            .as_chunks::<CHANNELS>()
            .0
            .iter()
            .map(|texel| texel[0] as f32)
            .collect();
        let min = shades.iter().cloned().fold(f32::MAX, f32::min);
        let max = shades.iter().cloned().fold(f32::MIN, f32::max);
        assert!(
            max - min > 90.0,
            "the shadow pattern is washed out: {min} to {max}"
        );
    }

    #[test]
    #[ignore = "a timing measurement, not an assertion"]
    fn measure_rebuild_cost() {
        for size in [64u32, 128, 256] {
            let mut map = map_for(
                size,
                WeatherPreset::PartlyCloudy,
                Vec3::new(0.2, 0.7, -0.68).normalize(),
                3_000.0,
                0.8,
            );
            let start = std::time::Instant::now();
            fill_rows(&mut map, size);
            println!("{size}x{size}: {:?}", start.elapsed());
        }
    }

    #[test]
    fn a_clear_sky_casts_no_shadow() {
        let mut map = map_for(16, WeatherPreset::Clear, Vec3::Y, 2_000.0, 1.0);
        fill_rows(&mut map, 16);
        assert!(
            map.scratch
                .as_chunks::<CHANNELS>()
                .0
                .iter()
                .all(|texel| texel[0] == 255)
        );
    }

    #[test]
    fn an_overcast_sky_casts_a_deep_one() {
        let mut map = map_for(24, WeatherPreset::Overcast, Vec3::Y, 2_000.0, 0.8);
        fill_rows(&mut map, 24);
        let shades: Vec<f32> = map
            .scratch
            .as_chunks::<CHANNELS>()
            .0
            .iter()
            .map(|texel| texel[0] as f32)
            .collect();
        let mean = shades.iter().sum::<f32>() / shades.len() as f32;
        assert!(mean < 160.0, "overcast barely shaded anything: {mean}");
        // `strength` is a floor, not a target: nothing may go past it.
        assert!(
            shades.iter().all(|&t| t >= 0.2 * 255.0 - 1.0),
            "a shadow went past the strength floor"
        );
        // And the alpha has to stay at zero, or every surface in range is
        // painted with the shadow map instead of lit by it.
        assert!(
            map.scratch
                .as_chunks::<CHANNELS>()
                .0
                .iter()
                .all(|texel| texel[3] == 0)
        );
    }

    #[test]
    fn a_low_sun_fades_the_shadows_out() {
        let build = |sun: Vec3| {
            let mut map = map_for(16, WeatherPreset::Overcast, sun, 2_000.0, 0.8);
            fill_rows(&mut map, 16);
            map.scratch
                .as_chunks::<CHANNELS>()
                .0
                .iter()
                .map(|texel| texel[0] as f32)
                .sum::<f32>()
                / 256.0
        };
        let overhead = build(Vec3::new(0.0, 1.0, 0.0));
        let setting = build(Vec3::new(0.0, 0.01, -1.0).normalize());
        assert!(overhead < 200.0);
        assert!(
            setting > 250.0,
            "a setting sun still cast shadows: {setting}"
        );
    }
}
