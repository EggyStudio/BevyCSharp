// The denoiser over Solari's lit result, the stages of spatiotemporal variance-guided filtering
// (Schied and others, 2017) as compute passes, which demodulate, accumulate in time, estimate the
// variance, filter in five à-trous passes and remodulate.
//
// The node's time goes to moving the whole picture through memory at each stage, so the stages
// are three entry points over as few bytes as hold them. The first demodulates and
// writes each pixel's guide, its distance and its normal in eight bytes, which every tap after
// reads in place of the depth and the G-buffer's sixteen. The second accumulates and estimates the
// variance in the same pass, and the à-trous passes' last remodulates into the view target. The
// light, its moments and the history are half floats, the moments kept as the mean and the root of
// the variance rather than the mean of the square, whose difference from the square of the mean a
// half float loses wherever the light varies by under a few hundredths, and the filtered light
// carries the root of its variance for the same reason, since every pass takes the variance down
// by a tenth and a half float holds a variance of a dark pixel's light only for a pass or two.
//
// Self-contained, with no imports, so a test of the bridge parses and validates it with naga as it
// is, and every function the Rust side mirrors (the weights and the reprojection's agreement) is
// written the same way there, `denoiser.rs` holding the same numbers.
//
// A pixel is denoised where the deferred G-buffer holds it. One with no depth is the sky, and one
// whose G-buffer texel is all zero was drawn forward over Solari by the main pass, and both are
// left as they are and are never a tap, so nothing but Solari's lighting is filtered.

struct Params {
    world_from_clip: mat4x4<f32>,
    view_from_clip: mat4x4<f32>,
    size: vec2<f32>,
    exposure: f32,
    has_history: f32,
    history_limit: f32,
    sigma_depth: f32,
    sigma_normal: f32,
    sigma_luminance: f32,
    albedo_floor: f32,
    plane_distance: f32,
    normal_threshold: f32,
    variance_frames: f32,
};

// Bevy's previous view, as its prepass keeps it, read only for the previous frame's positions.
struct PreviousView {
    view_from_world: mat4x4<f32>,
    clip_from_world: mat4x4<f32>,
    unjittered_clip_from_world: mat4x4<f32>,
    clip_from_view: mat4x4<f32>,
    world_from_clip: mat4x4<f32>,
    view_from_clip: mat4x4<f32>,
};

struct Step {
    step: u32,
    // Whether this pass's light is the next frame's history, and whether it is the last pass, which
    // remodulates into the view target in place of writing the next pass's light.
    first: u32,
    last: u32,
    unused: u32,
};

@group(0) @binding(0) var<uniform> params: Params;
@group(0) @binding(1) var gbuffer: texture_2d<u32>;
@group(0) @binding(2) var depth_buffer: texture_depth_2d;
@group(0) @binding(3) var motion_vectors: texture_2d<f32>;
@group(0) @binding(4) var previous_gbuffer: texture_2d<u32>;
@group(0) @binding(5) var previous_depth_buffer: texture_depth_2d;
@group(0) @binding(6) var<uniform> previous_view: PreviousView;

// Each stage's own textures, numbered apart so no two stages' bindings meet, but for the guide,
// which the temporal stage and the à-trous passes read alike.
@group(1) @binding(0) var lit: texture_2d<f32>;
@group(1) @binding(1) var demodulated_out: texture_storage_2d<rgba16float, write>;
@group(1) @binding(2) var guide_out: texture_storage_2d<rg32uint, write>;

@group(1) @binding(5) var guide: texture_2d<u32>;

@group(1) @binding(10) var demodulated: texture_2d<f32>;
@group(1) @binding(11) var history_color: texture_2d<f32>;
@group(1) @binding(12) var history_moments: texture_2d<f32>;
@group(1) @binding(13) var filtered_start: texture_storage_2d<rgba16float, write>;
@group(1) @binding(14) var moments_out: texture_storage_2d<rgba16float, write>;

@group(1) @binding(30) var filtered_in: texture_2d<f32>;
@group(1) @binding(31) var filtered_out: texture_storage_2d<rgba16float, write>;
@group(1) @binding(32) var history_out: texture_storage_2d<rgba16float, write>;
@group(1) @binding(33) var<uniform> pass_step: Step;
@group(1) @binding(34) var view_out: texture_storage_2d<rgba16float, write>;

// The most a half float holds short of infinity, which the light is kept under where a dark
// surface divides it up.
const HALF_MAX: f32 = 65000.0;

// The à-trous kernel's taps from the middle out, the B3 spline's as SVGF's.
fn kernel(reach: i32) -> f32 {
    return select(select(0.0625, 0.25, reach == 1), 0.375, reach == 0);
}

// The three by three Gaussian the variance is blurred with, a quarter in the middle.
fn blur(reach: i32) -> f32 {
    return select(0.25, 0.5, reach == 0);
}

fn luminance(color: vec3<f32>) -> f32 {
    return dot(color, vec3<f32>(0.2126, 0.7152, 0.0722));
}

fn inside(pixel: vec2<i32>) -> bool {
    return all(pixel >= vec2<i32>(0)) && all(vec2<f32>(pixel) < params.size);
}

// Bevy's deferred normal, two twelve bit octahedral coordinates in the texel's last word.
fn normal_of(gpixel: vec4<u32>) -> vec3<f32> {
    return normal_from(gpixel.a);
}

fn normal_from(packed: u32) -> vec3<f32> {
    let oct = vec2<f32>(f32(packed & 0xFFFu), f32((packed >> 12u) & 0xFFFu)) / 4095.0 * 2.0 - 1.0;
    var n = vec3<f32>(oct, 1.0 - abs(oct.x) - abs(oct.y));
    let t = saturate(-n.z);
    n = vec3<f32>(n.xy + select(vec2<f32>(t), vec2<f32>(-t), n.xy >= vec2<f32>(0.0)), n.z);
    return normalize(n);
}

// The share of the light reaching a surface that it sends back, which the light is divided by, its
// diffuse color and what its specular reflection returns at the angle it is seen at, as Bevy's
// lighting takes it from the polynomial `F_AB` where it reads no table. Solari's light holds both,
// and a dark surface seen at a slant, the feature test's ground far off, sends back more by its
// specular than by its color, so divided by its color alone it stood far above the light of the
// lighter squares beside it, and the filter spread it among them until the picture's checker was
// further from its mean than a grainy frame (build/denoiser-readings.py). Kept above the floor so
// a black surface divides by something, and multiplied back by the same so a pixel the filter
// leaves alone comes back as it was.
fn albedo_of(gpixel: vec4<u32>, pixel: vec2<i32>, depth: f32) -> vec3<f32> {
    let color = unpack4x8unorm(gpixel.r);
    let base = pow(color.rgb, vec3<f32>(2.2));
    let props = unpack4x8unorm(gpixel.b);
    let metallic = props.g;

    // The way to the eye, from the surface to the same pixel at the near plane.
    let surface = world_position(pixel, depth, params.world_from_clip);
    let toward = normalize(world_position(pixel, 1.0, params.world_from_clip) - surface);
    let n_dot_v = max(dot(normal_of(gpixel), toward), 1e-4);

    let c0 = vec4<f32>(-1.0, -0.0275, -0.572, 0.022);
    let c1 = vec4<f32>(1.0, 0.0425, 1.04, -0.04);
    let r = color.a * c0 + c1;
    let a004 = min(r.x * r.x, exp2(-9.28 * n_dot_v)) * r.x + r.y;
    let f_ab = max(vec2<f32>(-1.04, 1.04) * a004 + r.zw, vec2<f32>(0.0));

    let f0 = mix(vec3<f32>(0.16 * props.r * props.r), base, metallic);
    return max(base * (1.0 - metallic) + f0 * f_ab.x + f_ab.y, vec3<f32>(params.albedo_floor));
}

// The emissive color, rgb9e5 in the texel's second word, which Solari adds before the exposure.
fn emissive_of(gpixel: vec4<u32>) -> vec3<f32> {
    let v = gpixel.g;
    let scale = exp2(f32(i32(extractBits(v, 27u, 5u)) - 15 - 9));
    return vec3<f32>(f32(extractBits(v, 0u, 9u)), f32(extractBits(v, 9u, 9u)), f32(extractBits(v, 18u, 9u))) * scale;
}

// The light with any channel that is not a finite number taken as none, since one such pixel
// would spread through every tap that reads it and, by the history, through every frame after.
fn finite(color: vec3<f32>) -> vec3<f32> {
    let exponent = bitcast<vec3<u32>>(color) & vec3<u32>(0x7F800000u);
    return select(color, vec3<f32>(0.0), exponent == vec3<u32>(0x7F800000u));
}

fn deferred(gpixel: vec4<u32>) -> bool {
    return any(gpixel != vec4<u32>(0u));
}

fn world_position(pixel: vec2<i32>, depth: f32, world_from_clip: mat4x4<f32>) -> vec3<f32> {
    let uv = (vec2<f32>(pixel) + 0.5) / params.size;
    let ndc = (uv - vec2<f32>(0.5)) * vec2<f32>(2.0, -2.0);
    let world = world_from_clip * vec4<f32>(ndc, depth, 1.0);
    return world.xyz / world.w;
}

// How far in front of the camera a depth is, in meters, positive.
fn distance_of(depth: f32) -> f32 {
    let view = params.view_from_clip * vec4<f32>(0.0, 0.0, depth, 1.0);
    return -view.z / view.w;
}

// Whether a previous frame's surface is the current one, mirrored by `agrees` in denoiser.rs.
fn agrees(plane: f32, distance: f32, normal_dot: f32) -> bool {
    return plane <= params.plane_distance * distance && normal_dot >= params.normal_threshold;
}

// A pixel's guide, its distance from the camera in meters as the bits of a float and its normal
// as the G-buffer packs it, or nought for both where the pixel is the sky or a forward material,
// which is never a tap.
fn guide_of(gpixel: vec4<u32>, depth: f32) -> vec2<u32> {
    if depth == 0.0 || !deferred(gpixel) { return vec2<u32>(0u); }
    return vec2<u32>(bitcast<u32>(distance_of(depth)), gpixel.a);
}

// A tap's weight by its depth, normal and luminance against the middle's, mirrored by `weight`
// in denoiser.rs.
fn weight(depth_difference: f32, depth_scale: f32, normal_dot: f32, luminance_difference: f32, luminance_scale: f32) -> f32 {
    let by_depth = exp(-abs(depth_difference) / (params.sigma_depth * depth_scale + 1e-4));
    let by_normal = pow(max(normal_dot, 0.0), params.sigma_normal);
    let by_luminance = exp(-abs(luminance_difference) / (params.sigma_luminance * luminance_scale + 1e-6));
    return by_depth * by_normal * by_luminance;
}

// The first stage, the light with its emissive taken off and divided by the share the surface
// sends back, and the pixel's guide for the stages after.
@compute @workgroup_size(8, 8, 1)
fn demodulate(@builtin(global_invocation_id) id: vec3<u32>) {
    let pixel = vec2<i32>(id.xy);
    if !inside(pixel) { return; }

    let depth = textureLoad(depth_buffer, pixel, 0);
    let gpixel = textureLoad(gbuffer, pixel, 0);
    let pixel_guide = guide_of(gpixel, depth);
    textureStore(guide_out, pixel, vec4<u32>(pixel_guide, 0u, 0u));
    if pixel_guide.x == 0u {
        textureStore(demodulated_out, pixel, vec4<f32>(0.0));
        return;
    }

    let light = finite(textureLoad(lit, pixel, 0).rgb) - emissive_of(gpixel) * params.exposure;
    let demodulated_light = min(max(light, vec3<f32>(0.0)) / albedo_of(gpixel, pixel, depth), vec3<f32>(HALF_MAX));
    textureStore(demodulated_out, pixel, vec4<f32>(demodulated_light, 1.0));
}

// The second stage, the light added to the previous frame's where the motion vectors carry the
// pixel back to the same surface, with the mean of its luminance and the variance about it, and
// the third, the variance the filter takes, from those where the history is long enough to hold it
// and from a seven by seven neighborhood of this frame's light where it is not, its root written
// beside the light for the first à-trous pass.
@compute @workgroup_size(8, 8, 1)
fn temporal(@builtin(global_invocation_id) id: vec3<u32>) {
    let pixel = vec2<i32>(id.xy);
    if !inside(pixel) { return; }

    let depth = textureLoad(depth_buffer, pixel, 0);
    let gpixel = textureLoad(gbuffer, pixel, 0);
    if depth == 0.0 || !deferred(gpixel) {
        textureStore(filtered_start, pixel, vec4<f32>(0.0));
        textureStore(moments_out, pixel, vec4<f32>(0.0));
        return;
    }

    let current = textureLoad(demodulated, pixel, 0).rgb;
    let brightness = luminance(current);
    let normal = normal_of(gpixel);
    let world = world_position(pixel, depth, params.world_from_clip);
    let distance = distance_of(depth);

    var color = vec3<f32>(0.0);
    // The mean of the luminance and its variance about the mean, which a half float keeps where the
    // mean of its square, less the square of the mean, would come to nought.
    var moments = vec2<f32>(0.0);
    var frames = 0.0;
    var total = 0.0;

    if params.has_history > 0.5 {
        // Where the pixel was in the previous frame, in texels, since Bevy's motion vector is the
        // move across the screen since then, and the four texels round it, each weighed bilinearly
        // and kept where its surface is this one's. A texel weighed nothing is not read, so a still
        // pixel reads its own texel alone.
        let motion = textureLoad(motion_vectors, pixel, 0).xy;
        let previous = vec2<f32>(pixel) - motion * params.size;
        let base = floor(previous);
        let fraction = previous - base;

        for (var i = 0; i < 4; i++) {
            let corner = vec2<f32>(f32(i & 1), f32(i >> 1u));
            let bilinear = mix(1.0 - fraction.x, fraction.x, corner.x) * mix(1.0 - fraction.y, fraction.y, corner.y);
            let tap = vec2<i32>(base + corner);
            if bilinear <= 0.0 || !inside(tap) { continue; }

            let previous_depth = textureLoad(previous_depth_buffer, tap, 0);
            let previous_gpixel = textureLoad(previous_gbuffer, tap, 0);
            if previous_depth == 0.0 || !deferred(previous_gpixel) { continue; }

            let previous_world = world_position(tap, previous_depth, previous_view.world_from_clip);
            let plane = abs(dot(normal, previous_world - world));
            if !agrees(plane, distance, dot(normal, normal_of(previous_gpixel))) { continue; }

            let kept = textureLoad(history_moments, tap, 0);
            color += textureLoad(history_color, tap, 0).rgb * bilinear;
            moments += vec2<f32>(kept.x, kept.y * kept.y) * bilinear;
            frames += kept.z * bilinear;
            total += bilinear;
        }
    }

    if total > 1e-3 {
        color /= total;
        moments /= total;
        frames /= total;
    } else {
        frames = 0.0;
    }

    // A frame more, up to the clamp, and the new light weighed as one frame of that many.
    frames = min(frames + 1.0, params.history_limit);
    let alpha = 1.0 / frames;
    // The same running mean as the light's, its variance updated about it with no difference taken.
    let off = brightness - moments.x;
    let mean = moments.x + off * alpha;
    var spread = (1.0 - alpha) * moments.y + alpha * (1.0 - alpha) * off * off;
    textureStore(moments_out, pixel, vec4<f32>(mean, sqrt(spread), frames, 0.0));

    if frames < params.variance_frames { spread = nearby_variance(pixel, normal, distance); }
    textureStore(filtered_start, pixel, vec4<f32>(mix(color, current, alpha), sqrt(spread)));
}

// The luminance's variance over a seven by seven neighborhood of this frame's light, each pixel
// weighed by its depth and its normal against the middle's, for a pixel whose history is too short
// to hold its own.
fn nearby_variance(pixel: vec2<i32>, normal: vec3<f32>, distance: f32) -> f32 {
    var first = 0.0;
    var second = 0.0;
    var total = 0.0;
    for (var y = -3; y <= 3; y++) {
        for (var x = -3; x <= 3; x++) {
            let tap = pixel + vec2<i32>(x, y);
            if !inside(tap) { continue; }

            let tap_guide = textureLoad(guide, tap, 0).xy;
            if tap_guide.x == 0u { continue; }

            let reach = length(vec2<f32>(f32(x), f32(y)));
            let w = weight(bitcast<f32>(tap_guide.x) - distance, max(reach * distance * 0.01, 1e-3), dot(normal, normal_from(tap_guide.y)), 0.0, 1.0);
            let brightness = luminance(textureLoad(demodulated, tap, 0).rgb);
            first += brightness * w;
            second += brightness * brightness * w;
            total += w;
        }
    }

    let mean = first / max(total, 1e-6);
    return max(second / max(total, 1e-6) - mean * mean, 0.0);
}

// The share of a pixel's light the root of its variance stands for, under which a workgroup's
// pixels all are settled, and an à-trous pass over them carries their light through as it is. A
// tap's luminance is weighed against the root of the variance, so where that is under a hundredth
// of the light, every tap a level apart from the middle weighs next to nothing and the pass would
// change the light by less than a level of eight bits.
const SETTLED_NOISE: f32 = 0.01;

// The largest share of its light any of a workgroup's pixels has for the root of its variance, as
// the bits of a float, which order as the floats do since none is below nought.
var<workgroup> tile_noise: atomic<u32>;

// How far a neighbor's surface is from the middle's, in meters, or nothing where the neighbor is
// off the picture or is the sky. The sky's depth is at an infinite distance, and a change taken
// from it made the middle's own weight infinity times nought, which spread through the passes and
// the history until the whole picture was black.
fn change(neighbor: vec2<i32>, distance: f32) -> f32 {
    if !inside(neighbor) { return 0.0; }
    let neighbor_guide = textureLoad(guide, neighbor, 0).xy;
    if neighbor_guide.x == 0u { return 0.0; }
    return abs(bitcast<f32>(neighbor_guide.x) - distance);
}

// The fourth stage, one à-trous pass at a step, each tap of the five by five kernel weighed by its
// depth, normal and luminance against the middle's, the variance filtered with the squares of the
// weights, and the first pass's light kept as the next frame's history. A workgroup whose pixels
// are all settled (`SETTLED_NOISE`) passes its light through, which the later passes do over most of
// a still picture, each having taken the variance down. The last pass is the fifth stage as well,
// the filtered light times the share the surface sends back with the emissive added back, into the
// view target where Solari wrote it.
@compute @workgroup_size(8, 8, 1)
fn atrous(@builtin(global_invocation_id) id: vec3<u32>, @builtin(local_invocation_index) index: u32) {
    let pixel = vec2<i32>(id.xy);
    let within = inside(pixel);

    // Every invocation reaches both barriers, those off the picture as well.
    if index == 0u { atomicStore(&tile_noise, 0u); }
    workgroupBarrier();

    var middle = vec4<f32>(0.0);
    var pixel_guide = vec2<u32>(0u);
    if within {
        middle = textureLoad(filtered_in, pixel, 0);
        pixel_guide = textureLoad(guide, pixel, 0).xy;
        if pixel_guide.x != 0u {
            let noise = middle.a / max(luminance(middle.rgb), 1e-4);
            atomicMax(&tile_noise, bitcast<u32>(min(noise, HALF_MAX)));
        }
    }

    workgroupBarrier();
    if !within { return; }

    var result = middle;
    if pixel_guide.x != 0u && bitcast<f32>(atomicLoad(&tile_noise)) >= SETTLED_NOISE {
        result = filtered(pixel, middle, pixel_guide);
    }

    if pass_step.first != 0u { textureStore(history_out, pixel, vec4<f32>(result.rgb, 1.0)); }
    if pass_step.last == 0u {
        textureStore(filtered_out, pixel, result);
        return;
    }

    if pixel_guide.x == 0u { return; }
    let depth = textureLoad(depth_buffer, pixel, 0);
    let gpixel = textureLoad(gbuffer, pixel, 0);
    let light = result.rgb * albedo_of(gpixel, pixel, depth) + emissive_of(gpixel) * params.exposure;
    textureStore(view_out, pixel, vec4<f32>(light, 1.0));
}

// One pixel's light and the root of its variance after a pass's taps, for a pixel the G-buffer
// holds.
fn filtered(pixel: vec2<i32>, middle: vec4<f32>, pixel_guide: vec2<u32>) -> vec4<f32> {
    let normal = normal_from(pixel_guide.y);
    let distance = bitcast<f32>(pixel_guide.x);
    let brightness = luminance(middle.rgb);

    // The variance blurred over three by three, against whose root a tap's luminance is weighed.
    var blurred = 0.0;
    for (var y = -1; y <= 1; y++) {
        for (var x = -1; x <= 1; x++) {
            let tap = clamp(pixel + vec2<i32>(x, y), vec2<i32>(0), vec2<i32>(params.size) - 1);
            let root = textureLoad(filtered_in, tap, 0).a;
            blurred += root * root * blur(abs(x)) * blur(abs(y));
        }
    }

    // The depth's change over a pixel, which a tap's difference is measured against at its reach.
    let gradient = max(
        max(change(pixel + vec2<i32>(1, 0), distance), change(pixel - vec2<i32>(1, 0), distance)),
        max(change(pixel + vec2<i32>(0, 1), distance), change(pixel - vec2<i32>(0, 1), distance)));

    let step = i32(pass_step.step);
    var color = vec3<f32>(0.0);
    var spread = 0.0;
    var total = 0.0;
    for (var y = -2; y <= 2; y++) {
        for (var x = -2; x <= 2; x++) {
            let tap = pixel + vec2<i32>(x, y) * step;
            if !inside(tap) { continue; }

            let tap_guide = textureLoad(guide, tap, 0).xy;
            if tap_guide.x == 0u { continue; }

            let value = textureLoad(filtered_in, tap, 0);
            let reach = length(vec2<f32>(f32(x * step), f32(y * step)));
            let w = kernel(abs(x)) * kernel(abs(y)) * weight(
                bitcast<f32>(tap_guide.x) - distance,
                gradient * reach,
                dot(normal, normal_from(tap_guide.y)),
                luminance(value.rgb) - brightness,
                sqrt(max(blurred, 0.0)));

            color += value.rgb * w;
            spread += value.a * value.a * w * w;
            total += w;
        }
    }

    return vec4<f32>(color / max(total, 1e-6), sqrt(spread / max(total * total, 1e-12)));
}
