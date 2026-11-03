//! Global illumination's probes as a surface drawn forward reads them, the leaves and the grass.
//!
//! The light pass (`gi_light.slang`) lights what the G-buffer holds, taking back the light Bevy's
//! lighting gave a surface from all around and adding the probes' in its place, and passes over a
//! pixel no deferred material drew. A tree's leaves and the grass are drawn forward, since the
//! G-buffer holds one normal and a leaf is lit by two ([`super::leaves`]), so with the day's fill
//! off under global illumination they took the sky's small ambient light alone and the meadow lay
//! near black in its shade. The leaf material binds the probes as the pass reads them and does in
//! its own fragment shader what the pass does for the G-buffer's surfaces, taking back Bevy's
//! light from all around and giving the probes' gathered at the surface.
//!
//! The managed side hands the probes over as it binds them to the pass each frame
//! ([`bcs_render_set_forward_probes`]), and every leaf material takes them, or stand-ins that leave
//! Bevy's lighting as it is while global illumination is off. The shader's reading of the probes is
//! [`PROBES`], a port of `bcs_gi.slang`'s `bouncedAt` and `bouncedHold` and of the light pass's
//! light past the probes, which `GlobalIlluminationTests` holds to the pass's reading of a deferred
//! surface beside it. The light let through a leaf is read from the probes its front reads, with
//! the front's weights, each probe's faces toward the way behind the leaf, since a reading of its
//! own took the forest of `TreeLeavesTests` from 6.8 ms to 9.0 at 1080p, half of a reading's time
//! being its probes' reach. In the feature test's meadow at 1280 by 720 at `Medium` the reading
//! takes the opaque pass from 2.6 ms to 5.6.

use crate::interop::status;

#[cfg(feature = "render")]
use bevy::{asset::Handle, ecs::resource::Resource, image::Image, math::Vec4, render::storage::ShaderBuffer};

/// The probes a leaf material reads, or stand-ins while global illumination is off.
#[cfg(feature = "render")]
#[derive(Resource, Clone, PartialEq)]
pub struct ForwardProbes {
    /// Every cascade's probes' six faces, as `gi_ambient.slang` gathers them.
    pub cubes: Handle<Image>,
    /// How far each probe's rays reached, eight by eight directions a probe.
    pub reach: Handle<Image>,
    /// Where the scene's distance field's cascades lie this frame, `bcs_field`'s `FieldInfo`.
    pub field: Handle<ShaderBuffer>,
    /// The lights the probes are lit by, `bcs_gi`'s `GiLights`, whose light from all around stands
    /// in the sky past the probes.
    pub lights: Handle<ShaderBuffer>,
    /// What `gi_sky.slang` reads at the camera, the camera's air toward the sun first, then the
    /// environment map's light from above and the air toward each directional light.
    pub air: Handle<ShaderBuffer>,
    /// x: the probes along each side of a cascade. y: their spacing in cells. z: how many cascades
    /// of probes. w: 1 while global illumination is on.
    pub counts: Vec4,
}

/// The most directional lights whose light past the probes a leaf takes, as the light pass's
/// `MostSuns`, and whose air `gi_sky.slang` reads after the sky's.
pub const MOST_SUNS: usize = 10;

/// The air buffer's entries, the sun's, the sky's and a directional light's each.
pub const AIR_ENTRIES: usize = 2 + MOST_SUNS;

/// The leaf material's reading of global illumination's probes, its bindings after the wind's at
/// 100 and the functions a leaf's fragment shader gathers their light with, ported from
/// `bcs_field.slang` and `bcs_gi.slang`, whose names each keeps in snake case, and from
/// `gi_light.slang`'s light past the probes. Joined after the shader's imports, which bring Bevy's
/// view bindings and shadows.
#[cfg(feature = "render")]
pub const PROBES: &str = r#"
struct ProbeField {
    counts: vec4<f32>,
    cascades: array<vec4<f32>, 9>,
    eye: vec4<f32>,
}

@group(constants::MATERIAL_BIND_GROUP) @binding(101) var probe_cubes: texture_3d<f32>;
@group(constants::MATERIAL_BIND_GROUP) @binding(102) var probe_reach: texture_3d<f32>;
@group(constants::MATERIAL_BIND_GROUP) @binding(103) var<storage, read> probe_field: array<ProbeField>;
@group(constants::MATERIAL_BIND_GROUP) @binding(104) var<storage, read> probe_lights: array<vec4<f32>>;
@group(constants::MATERIAL_BIND_GROUP) @binding(105) var<storage, read> probe_air: array<vec4<f32>>;
@group(constants::MATERIAL_BIND_GROUP) @binding(106) var<uniform> probe_counts: vec4<f32>;

const PROBE_PI: f32 = 3.14159265358979;
const EYE_CLEAR: f32 = 6.0;
const CASCADE_BAND: f32 = 8.0;
const LEAN: f32 = 0.5;
const REACH_TEXELS: i32 = 8;
const MOST_SUNS: u32 = 10u;

// The field's cascades the probes stand in, as `probesOf` places them, read from the buffer
// where they are needed rather than copied, since an array copied into a function's variables and
// indexed as it runs is kept in the GPU's slow local memory, which took the forest's opaque pass
// from 3.0 ms to 19.8.
fn probe_cascades() -> i32 {
    return min(i32(probe_counts.z), i32(probe_field[0].counts.x));
}

fn probe_cascade(c: i32) -> vec4<f32> {
    let far = i32(probe_field[0].counts.w);
    if far > 0 && c == i32(probe_counts.z) - 1 {
        return probe_field[0].cascades[far];
    }
    return probe_field[0].cascades[c];
}

fn band_hold(c: i32, cell: vec3<f32>, band: f32, margin: f32) -> f32 {
    let oc = probe_cascade(c);
    let size = vec3<f32>(probe_field[0].counts.y);
    let eye = (probe_field[0].eye.xyz - oc.xyz) / oc.w;
    let low = clamp(eye - margin - EYE_CLEAR, vec3<f32>(0.0), vec3<f32>(band));
    let high = clamp(size - eye - margin - EYE_CLEAR, vec3<f32>(0.0), vec3<f32>(band));
    let held = saturate(min((cell - margin) / max(low, vec3<f32>(1e-3)), (size - cell - margin) / max(high, vec3<f32>(1e-3))));
    return min(min(held.x, held.y), held.z);
}

fn probes_coarsest_built(up_to: i32) -> i32 {
    for (var i = min(up_to, probe_cascades() - 1); i >= 0; i--) {
        if probe_cascade(i).w > 0.0 {
            return i;
        }
    }
    return -1;
}

fn field_cascade(p: vec3<f32>, margin: f32) -> i32 {
    let size = probe_field[0].counts.y;
    for (var i = 0; i < probe_cascades(); i++) {
        let oc = probe_cascade(i);
        if oc.w <= 0.0 {
            continue;
        }
        let cell = (p - oc.xyz) / oc.w;
        if all(cell >= vec3<f32>(margin)) && all(cell <= vec3<f32>(size - margin)) {
            return i;
        }
    }
    return -1;
}

fn octahedron(way: vec3<f32>) -> vec2<f32> {
    let d = way / (abs(way.x) + abs(way.y) + abs(way.z));
    var p = d.xy;
    if d.z < 0.0 {
        p = (1.0 - abs(p.yx)) * select(vec2<f32>(-1.0), vec2<f32>(1.0), p >= vec2<f32>(0.0));
    }
    return p * 0.5 + 0.5;
}

fn probe_facing(p: vec3<f32>, n: vec3<f32>, probe: vec3<f32>) -> f32 {
    let toward = probe - p;
    let length2 = dot(toward, toward);
    if length2 <= 1e-8 {
        return 1.0;
    }
    let cosine = dot(toward * inverseSqrt(length2), n);
    let facing = (cosine + 1.0) * 0.5;
    return mix(0.02 * facing * facing, facing * facing + 0.2, smoothstep(-0.15, 0.15, cosine));
}

fn probe_sees(p: vec3<f32>, n: vec3<f32>, probe: vec3<f32>, at: vec3<i32>, c: i32, probes: f32, apart: f32) -> f32 {
    let toward = p + n * (0.3 * apart) - probe;
    let d = length(toward);
    if d < 1e-4 {
        return 1.0;
    }
    let texel = min(vec2<i32>(octahedron(toward / d) * f32(REACH_TEXELS)), vec2<i32>(REACH_TEXELS - 1));
    let reached = textureLoad(probe_reach, vec3<i32>(at.xy * REACH_TEXELS + texel, c * i32(probes) + at.z), 0).r;
    let slack = 0.125 * apart;
    return saturate((reached + slack - d) / slack);
}

fn cascade_hold(c: i32, p: vec3<f32>) -> f32 {
    let oc = probe_cascade(c);
    return band_hold(c, (p - oc.xyz) / oc.w, CASCADE_BAND, 1.0);
}

// The light the probes hold for a leaf's two shares, irradiance, its front facing `n` and the
// light let through it from `t`, the way behind it.
struct ProbesLight {
    front: vec3<f32>,
    through: vec3<f32>,
}

// `bouncedIn` for a surface at `p` facing `n`, and the light from `t` read from the same probes
// with the same weights, each probe's faces toward `t` in place of those toward `n`, a face
// loaded again only where `t` lies on its other side, so the light let through a leaf costs no
// second reading of the probes' reach, which is half of a reading's time.
fn bounced_in(p: vec3<f32>, n: vec3<f32>, t: vec3<f32>, probes: f32, spacing: f32, c: i32) -> ProbesLight {
    let oc = probe_cascade(c);
    let q = clamp((p - oc.xyz) / (oc.w * spacing) - 0.5, vec3<f32>(0.0), vec3<f32>(probes - 1.0));
    let base = vec3<i32>(floor(q));
    let f = q - vec3<f32>(base);
    let n2 = n * n;
    let t2 = t * t;
    let faces = vec3<i32>(0, 2, 4) + select(vec3<i32>(0), vec3<i32>(1), n < vec3<f32>(0.0));
    let behind = vec3<i32>(0, 2, 4) + select(vec3<i32>(0), vec3<i32>(1), t < vec3<f32>(0.0));
    var sum = ProbesLight(vec3<f32>(0.0), vec3<f32>(0.0));
    var weights = 0.0;
    for (var corner = 0; corner < 8; corner++) {
        let offset = vec3<i32>(corner & 1, (corner >> 1u) & 1, corner >> 2u);
        let at = min(base + offset, vec3<i32>(i32(probes) - 1));
        let lerps = mix(1.0 - f, f, vec3<f32>(offset));
        let row = vec2<i32>(at.y, c * i32(probes) + at.z);
        let x = textureLoad(probe_cubes, vec3<i32>(faces.x * i32(probes) + at.x, row), 0);
        let y = textureLoad(probe_cubes, vec3<i32>(faces.y * i32(probes) + at.x, row), 0);
        let z = textureLoad(probe_cubes, vec3<i32>(faces.z * i32(probes) + at.x, row), 0);
        var tx = x;
        var ty = y;
        var tz = z;
        if behind.x != faces.x {
            tx = textureLoad(probe_cubes, vec3<i32>(behind.x * i32(probes) + at.x, row), 0);
        }
        if behind.y != faces.y {
            ty = textureLoad(probe_cubes, vec3<i32>(behind.y * i32(probes) + at.x, row), 0);
        }
        if behind.z != faces.z {
            tz = textureLoad(probe_cubes, vec3<i32>(behind.z * i32(probes) + at.x, row), 0);
        }
        let alphas = vec3<f32>(x.a, y.a, z.a);
        let held = alphas > vec3<f32>(0.5);
        let valid = select(0.0, 1.0, all(held));
        let moved = select(vec3<f32>(0.0), alphas - 2.0, held);
        let probe = oc.xyz + (vec3<f32>(at) + 0.5 + moved) * (oc.w * spacing);
        var weight = lerps.x * lerps.y * lerps.z * valid * probe_facing(p, n, probe);
        weight *= mix(0.05, 1.0, probe_sees(p, n, probe, at, c, probes, oc.w * spacing));
        sum.front += (x.rgb * n2.x + y.rgb * n2.y + z.rgb * n2.z) * weight;
        sum.through += (tx.rgb * t2.x + ty.rgb * t2.y + tz.rgb * t2.z) * weight;
        weights += weight;
    }
    if weights <= 1e-5 {
        return ProbesLight(vec3<f32>(0.0), vec3<f32>(0.0));
    }
    return ProbesLight(sum.front / weights, sum.through / weights);
}

fn bounced_at(p: vec3<f32>, n: vec3<f32>, t: vec3<f32>) -> ProbesLight {
    let cascades = i32(probe_counts.z);
    let c = field_cascade(p, 1.0);
    if c < 0 || c >= cascades {
        return ProbesLight(vec3<f32>(0.0), vec3<f32>(0.0));
    }
    var own = 1.0;
    if c + 1 < cascades && probe_cascade(c + 1).w > 0.0 {
        own = cascade_hold(c, p);
    }
    let first = bounced_in(p, n, t, probe_counts.x, probe_counts.y, c);
    var light = ProbesLight(first.front * own, first.through * own);
    if own < 1.0 {
        let next = bounced_in(p, n, t, probe_counts.x, probe_counts.y, c + 1);
        light.front += next.front * (1.0 - own);
        light.through += next.through * (1.0 - own);
    }
    return light;
}

fn bounced_hold(p: vec3<f32>, n: vec3<f32>) -> f32 {
    let c = probes_coarsest_built(i32(probe_counts.z) - 1);
    if c < 0 {
        return 0.0;
    }
    let oc = probe_cascade(c);
    let cell = (p - oc.xyz) / oc.w;
    return max(band_hold(c, cell + n * (LEAN * probe_counts.y), CASCADE_BAND, 0.0), band_hold(c, cell, CASCADE_BAND, 0.0));
}

// Whether the probes light this frame, global illumination on and the field holding its cascades,
// where the light pass reads them too.
fn probes_light() -> bool {
    return probe_counts.w > 0.5 && probe_field[0].counts.x > 0.5;
}

// The light past the probes from all around a surface of `color` at `p` facing `n`, a sky over a
// ground whose color is the surface's own, lit by the sky and by each directional light through
// the camera's air where its shadow map holds the ground a coarsest cell along `n`, as the light
// pass gives it (`distant`).
fn distant_around(p: vec3<f32>, n: vec3<f32>, color: vec3<f32>, frag_coord: vec2<f32>) -> vec3<f32> {
    let sky = probe_lights[2].rgb + probe_air[1].rgb;
    let above = saturate(n.y * 0.5 + 0.5);
    var distant = sky * (above + color * (1.0 - above));
    let below = saturate(0.5 - n.y * 0.5);
    if below > 0.0 {
        var cell = 0.0;
        for (var c = i32(probe_field[0].counts.x) - 1; c >= 0; c--) {
            if probe_field[0].cascades[c].w > 0.0 {
                cell = probe_field[0].cascades[c].w;
                break;
            }
        }
        let ground = vec4<f32>(p + n * cell, 1.0);
        let ground_z = dot(vec4<f32>(
            view_bindings::view.view_from_world[0].z,
            view_bindings::view.view_from_world[1].z,
            view_bindings::view.view_from_world[2].z,
            view_bindings::view.view_from_world[3].z
        ), ground);
        for (var i = 0u; i < min(view_bindings::lights.n_directional_lights, MOST_SUNS); i++) {
            let light = view_bindings::lights.directional_lights[i];
            let floor = saturate(light.direction_to_light.y);
            if floor <= 0.0 {
                continue;
            }
            let reached = shadows::fetch_directional_shadow(i, ground, vec3<f32>(0.0, 1.0, 0.0), ground_z, frag_coord);
            distant += color * light.color.rgb * probe_air[2u + i].rgb / PROBE_PI * view_bindings::view.exposure * floor * reached * below;
        }
    }
    return distant;
}

// The light the probes give a leaf at `p`, its front of `front` color facing `n` and the share of
// `through` color let through it from `t`, irradiance over pi times the color, as the light pass
// gives the G-buffer's surfaces (`added`), the world's probes around it, faded across the last
// cascade's outer band to the light past them, both shares weighed by the front's view of the
// probes (`bounced_in`).
fn probes_given(p: vec3<f32>, n: vec3<f32>, front: vec3<f32>, t: vec3<f32>, through: vec3<f32>, frag_coord: vec2<f32>) -> ProbesLight {
    let held = bounced_hold(p, n);
    var bounced = ProbesLight(vec3<f32>(0.0), vec3<f32>(0.0));
    if held > 0.0 {
        bounced = bounced_at(p, n, t);
    }
    var given = ProbesLight(front * bounced.front / PROBE_PI * held, through * bounced.through / PROBE_PI * held);
    if held < 1.0 {
        given.front += front * distant_around(p, n, front, frag_coord) * (1.0 - held);
        given.through += through * distant_around(p, t, through, frag_coord) * (1.0 - held);
    }
    return given;
}
"#;

/// Hands the leaf materials global illumination's probes, as the light pass reads them: the faces
/// image, the reach image, the field's info buffer, the lights buffer and the air buffer, by their
/// asset keys, the probes along each side of a cascade, their spacing in cells and how many
/// cascades. A faces key of zero or less turns their reading off, Bevy's lighting left as it is.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_forward_probes(
    cubes: i32,
    reach: i32,
    field: i32,
    lights: i32,
    air: i32,
    probes: f32,
    spacing: f32,
    cascades: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (cubes, reach, field, lights, air, probes, spacing, cascades);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                let given = if cubes <= 0 {
                    let Some(off) = stand_ins(world) else {
                        return status::UNSUPPORTED;
                    };
                    off
                } else {
                    let image = |key| crate::assets::clone_handle(world, key).and_then(|handle| handle.try_typed::<Image>().ok());
                    let (Some(cubes), Some(reach)) = (image(cubes), image(reach)) else {
                        return status::NO_COMPONENT;
                    };
                    let buffer = |key| super::compute::buffer_handle(world, key);
                    let (Some(field), Some(lights), Some(air)) = (buffer(field), buffer(lights), buffer(air)) else {
                        return status::NO_COMPONENT;
                    };
                    ForwardProbes { cubes, reach, field, lights, air, counts: Vec4::new(probes, spacing, cascades, 1.0) }
                };

                if world.get_resource::<ForwardProbes>() != Some(&given) {
                    world.insert_resource(given);
                }
                status::OK
            })
        }
    })
}

/// The images and buffers a leaf material binds while global illumination is off, each the
/// smallest its binding takes, the field's saying it holds no cascades. Made once and kept.
#[cfg(feature = "render")]
fn stand_ins(world: &mut bevy::ecs::world::World) -> Option<ForwardProbes> {
    use bevy::render::render_resource::{Extent3d, TextureDimension, TextureFormat};

    #[derive(Resource)]
    struct StandIns(ForwardProbes);

    if let Some(kept) = world.get_resource::<StandIns>() {
        return Some(kept.0.clone());
    }

    let image = |format: TextureFormat, bytes: usize| {
        Image::new_fill(
            Extent3d { width: 1, height: 1, depth_or_array_layers: 1 },
            TextureDimension::D3,
            &vec![0; bytes],
            format,
            bevy::asset::RenderAssetUsages::RENDER_WORLD,
        )
    };
    let mut images = world.get_resource_mut::<bevy::asset::Assets<Image>>()?;
    let cubes = images.add(image(TextureFormat::Rgba16Float, 8));
    let reach = images.add(image(TextureFormat::R16Float, 2));

    // The field's info, its counts first, then nine cascades and the eye, a vector each.
    let buffer = |bytes: usize| {
        let mut buffer = ShaderBuffer::new(vec![0u8; bytes], bevy::asset::RenderAssetUsages::default());
        buffer.buffer_usage = super::compute::buffer_usage();
        buffer
    };
    let mut buffers = world.get_resource_mut::<bevy::asset::Assets<ShaderBuffer>>()?;
    let field = buffers.add(buffer(11 * 16));
    let lights = buffers.add(buffer(4 * 16));
    let air = buffers.add(buffer(AIR_ENTRIES * 16));

    let made = ForwardProbes { cubes, reach, field, lights, air, counts: Vec4::ZERO };
    world.insert_resource(StandIns(made.clone()));
    Some(made)
}

/// Gives every leaf material the probes as they were last handed over, where they changed.
#[cfg(feature = "render")]
fn keep_leaves_probed(
    probes: Option<bevy::ecs::system::Res<ForwardProbes>>,
    mut materials: bevy::ecs::system::ResMut<bevy::asset::Assets<super::leaves::LeafMaterial>>,
) {
    use bevy::ecs::change_detection::DetectChanges;

    let Some(probes) = probes else {
        return;
    };
    if !probes.is_changed() {
        return;
    }
    let ids: Vec<_> = materials.ids().collect();
    for id in ids {
        if let Some(material) = materials.get_mut_untracked(id)
            && super::leaves::probed(&material.extension, &probes)
        {
            continue;
        }
        if let Some(mut material) = materials.get_mut(id) {
            super::leaves::probe(&mut material.extension, &probes);
        }
    }
}

/// The probes as a new leaf material takes them, the stand-ins before any were handed over.
#[cfg(feature = "render")]
pub fn current(world: &mut bevy::ecs::world::World) -> Option<ForwardProbes> {
    world.get_resource::<ForwardProbes>().cloned().or_else(|| stand_ins(world))
}

/// Keeps the leaf materials reading the probes last handed over.
#[cfg(feature = "render")]
pub(crate) fn install(app: &mut bevy::app::App) {
    app.add_systems(bevy::app::PostUpdate, keep_leaves_probed);
}
