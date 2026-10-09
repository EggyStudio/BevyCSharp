// Rain and snow.
//
// Every particle lives entirely on the GPU. The mesh is a static buffer of
// quads whose "position" attribute is not a position at all but a per-particle
// random seed; the vertex shader turns that seed plus the clock into a
// world-anchored falling particle and billboards the quad around it.
//
// Nothing is simulated on the CPU and no buffer is ever re-uploaded, so the
// cost of a hundred thousand raindrops is one draw call.

#import bevy_pbr::mesh_view_bindings::view

struct PrecipitationUniform {
    // xyz: wind velocity, world units per second. w: fall speed.
    wind: vec4<f32>,
    // x: box size. y: intensity 0..1. z: time, wrapped. w: mode (0 rain, 1 snow).
    params: vec4<f32>,
    // x: particle width. y: particle length. z: flutter. w: opacity.
    shape: vec4<f32>,
    // rgb: colour. a: unused.
    tint: vec4<f32>,
    // rgb: light reaching the particles. a: ambient fraction.
    light: vec4<f32>,
}

@group(#{MATERIAL_BIND_GROUP}) @binding(0) var<uniform> precipitation: PrecipitationUniform;

const MODE_SNOW: f32 = 0.5;

// Particles are composited into a physically-calibrated HDR buffer and then
// multiplied by `view.exposure`, so their colour has to be a real radiance.
// A sunlit water drop or snowflake scatters roughly like a small white
// diffuser, so it lands near the same value as a sunlit white surface.
const LIT_RADIANCE: f32 = 30000.0;

struct Vertex {
    // Per-particle random seed in [0, 1)^3, reinterpreted as a cell position.
    @location(0) seed: vec3<f32>,
    // Corner of the quad, (0,0) to (1,1).
    @location(2) corner: vec2<f32>,
    // x: size scale. y: speed scale. z: flutter phase. w: index in [0, 1).
    @location(5) random: vec4<f32>,
}

struct VertexOutput {
    @builtin(position) position: vec4<f32>,
    @location(0) uv: vec2<f32>,
    @location(1) alpha: f32,
    @location(2) shade: f32,
}

/// Wraps `v` into `[-size/2, size/2)`.
///
/// This is what anchors particles to the world instead of to the camera: the
/// base position is computed in absolute world space and then folded into the
/// box around the viewer, so walking forward moves you *through* the rain
/// rather than dragging it along.
fn wrap(v: vec3<f32>, size: f32) -> vec3<f32> {
    return v - size * floor(v / size + 0.5);
}

/// A clip position that is guaranteed to be culled.
fn discarded() -> vec4<f32> {
    return vec4(2.0, 2.0, 0.0, 1.0);
}

@vertex
fn vertex(vertex: Vertex) -> VertexOutput {
    var out: VertexOutput;
    out.uv = vertex.corner;

    let intensity = precipitation.params.y;
    // Thinning the field by index rather than by scaling the mesh means
    // intensity can change every frame with no reallocation, and the particles
    // that remain are the same ones -- so rain eases off instead of shuffling.
    if vertex.random.w > intensity {
        out.position = discarded();
        out.alpha = 0.0;
        out.shade = 0.0;
        return out;
    }

    let box = max(precipitation.params.x, 1.0);
    let time = precipitation.params.z;
    let is_snow = precipitation.params.w > MODE_SNOW;
    let eye = view.world_position;

    let speed_scale = 0.7 + vertex.random.y * 0.6;
    let fall_speed = precipitation.wind.w * speed_scale;

    // World-anchored base position, advanced by gravity and wind.
    var base = vertex.seed * box;
    base.y -= fall_speed * time;
    base += precipitation.wind.xyz * time;

    if is_snow {
        // Flakes are light enough to be pushed around by turbulence, which is
        // what makes snow drift rather than fall.
        let phase = vertex.random.z * 100.0;
        let flutter = precipitation.shape.z;
        base.x += sin(time * 1.7 + phase) * flutter;
        base.z += cos(time * 1.3 + phase * 1.7) * flutter;
        base.y += sin(time * 0.9 + phase * 0.5) * flutter * 0.3;
    }

    let center = eye + wrap(base - eye, box);

    // Fade at the edge of the box so particles do not pop as they wrap, and
    // again very close to the eye, where a single drop would otherwise smear a
    // streak across the whole screen.
    let offset = center - eye;
    let distance = length(offset);
    let edge_fade = (1.0 - smoothstep(0.75, 1.0, distance / (box * 0.5)))
        * smoothstep(0.0, 1.0, distance / max(precipitation.shape.y * 3.0, 0.3));
    if edge_fade <= 0.0 {
        out.position = discarded();
        out.alpha = 0.0;
        out.shade = 0.0;
        return out;
    }

    let size_scale = 0.6 + vertex.random.x * 0.8;
    let width = precipitation.shape.x * size_scale;
    var length_axis = precipitation.shape.y * size_scale;

    let to_eye = normalize(eye - center);

    // Rain is a motion-blurred streak, so its quad is aligned to the direction
    // of travel. Snow is slow enough to read as a shape, so it faces the camera
    // squarely.
    var up_axis: vec3<f32>;
    if is_snow {
        up_axis = normalize(view.world_from_view[1].xyz);
        length_axis = width;
    } else {
        let velocity = precipitation.wind.xyz - vec3(0.0, fall_speed, 0.0);
        up_axis = normalize(velocity);
        // A drop's apparent length grows with how fast it crosses the frame.
        length_axis *= clamp(length(velocity) / 12.0, 0.35, 1.6);
    }

    var right = cross(up_axis, to_eye);
    let right_length = length(right);
    if right_length < 1e-4 {
        // Looking straight down the streak: it collapses to a point, so drop it
        // rather than dividing by zero.
        out.position = discarded();
        out.alpha = 0.0;
        out.shade = 0.0;
        return out;
    }
    right = right / right_length;

    let local = vertex.corner - vec2(0.5, 0.5);
    let world = center + right * (local.x * width) + up_axis * (local.y * length_axis);

    out.position = view.clip_from_world * vec4(world, 1.0);
    // Vary opacity per drop. A field of identically-opaque streaks reads as a
    // mesh laid over the screen; real rain has drops at every depth and every
    // angle, and most of them you barely register.
    let drop_variation = select(1.0, 0.25 + 0.75 * vertex.random.z, !is_snow);
    out.alpha = precipitation.shape.w * edge_fade * drop_variation;
    // Face the streak toward the light a little, so rain glints when lit from
    // the side.
    out.shade = 1.0;
    return out;
}

fn hash12(p: vec2<f32>) -> f32 {
    var q = fract(vec3(p.xyx) * 0.1031);
    q += dot(q, q.yzx + 33.33);
    return fract((q.x + q.y) * q.z);
}

@fragment
fn fragment(in: VertexOutput) -> @location(0) vec4<f32> {
    if in.alpha <= 0.0 {
        discard;
    }

    let is_snow = precipitation.params.w > MODE_SNOW;
    let centred = in.uv * 2.0 - 1.0;

    var coverage: f32;
    if is_snow {
        // A soft round flake with a slightly ragged edge.
        let r = length(centred);
        let ragged = 0.86 + 0.14 * hash12(floor(in.uv * 7.0));
        coverage = 1.0 - smoothstep(ragged * 0.45, ragged, r);
        coverage *= coverage;
    } else {
        // A streak: a thin bright core that falls off fast to either side, and
        // tapers away at both ends. The cubic is what keeps it reading as a
        // line of light rather than as a solid quad with soft edges.
        let across = 1.0 - abs(centred.x);
        let along = 1.0 - abs(centred.y);
        coverage = across * across * across * smoothstep(0.0, 0.5, along);
    }

    if coverage <= 0.002 {
        discard;
    }

    let lit = (precipitation.light.rgb + precipitation.light.a) * LIT_RADIANCE;
    let color = precipitation.tint.rgb * lit * in.shade;
    return vec4(color * view.exposure, coverage * in.alpha);
}
