// A camera's picture brought to its window, in one copy at the frame's end.
//
// The vertex stage is Bevy's full-screen triangle, drawn over the part of the window the whole
// picture spans, so each window pixel reads the picture at its own place. For the linear and the
// nearest scaling the sampler decides how, filtered for a soft picture or the nearest picture pixel
// for a sharp one, the sharp scaling has entry points of its own that work out the blend at a
// picture pixel's edge, and a picture larger than the window has entry points that bring it down
// smoothly. A picture kept in sRGB or Oklab to be blended in is turned linear here, as
// Bevy's own copy does, by an entry point of each, since the bridge hands its WGSL to wgpu as it is
// written, with no preprocessor between.

struct FullscreenVertexOutput {
    @builtin(position) position: vec4<f32>,
    @location(0) uv: vec2<f32>,
}

@group(0) @binding(0) var picture: texture_2d<f32>;
@group(0) @binding(1) var picture_sampler: sampler;

fn srgb_to_linear(color: vec3<f32>) -> vec3<f32> {
    let low = color / 12.92;
    let high = pow((max(color, vec3(0.0)) + vec3(0.055)) / 1.055, vec3(2.4));
    return select(high, low, color <= vec3(0.04045));
}

// https://bottosson.github.io/posts/oklab/
fn oklab_to_linear_rgb(c: vec3<f32>) -> vec3<f32> {
    let l_ = c.x + 0.39633778 * c.y + 0.21580376 * c.z;
    let m_ = c.x - 0.105561346 * c.y - 0.06385417 * c.z;
    let s_ = c.x - 0.08948418 * c.y - 1.2914855 * c.z;
    let l = l_ * l_ * l_;
    let m = m_ * m_ * m_;
    let s = s_ * s_ * s_;
    return vec3(
        4.0767417 * l - 3.3077116 * m + 0.23096994 * s,
        -1.268438 * l + 2.6097574 * m - 0.34131938 * s,
        -0.0041960863 * l - 0.7034186 * m + 1.7076147 * s,
    );
}

@fragment
fn fragment(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    return textureSample(picture, picture_sampler, in.uv);
}

@fragment
fn fragment_srgb(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    let color = textureSample(picture, picture_sampler, in.uv);
    return vec4(srgb_to_linear(color.rgb), color.a);
}

@fragment
fn fragment_oklab(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    let color = textureSample(picture, picture_sampler, in.uv);
    return vec4(oklab_to_linear_rgb(color.rgb), color.a);
}

// The sharp scaling's color at uv. A window pixel takes the picture pixel its middle lies in, and
// where it reaches past that pixel's edge it takes the next one's share of its own width, so a
// window pixel inside a picture pixel takes it whole and one across an edge blends the two. It is
// what the linear filter gives with each sample moved toward its picture pixel's middle by the
// ratio, worked out here and read with four loads, since the filter's weights are fixed point and
// a sample a float's step from a picture pixel's middle would take a 256th of the next one, where
// a whole ratio takes none and so equals the nearest scaling to the bit.
fn sharp_color(uv: vec2<f32>) -> vec4<f32> {
    let size = vec2<f32>(textureDimensions(picture));
    let texel = uv * size;
    // Window pixels to a picture pixel along each axis, from how far uv moves across one.
    let ratio = 1.0 / (abs(vec2(dpdx(uv.x), dpdy(uv.y))) * size);
    let middle = floor(texel);
    let from_middle = texel - middle - 0.5;
    // The ratio read from the step across a pixel carries a float's error, so a share under half
    // the filter's step is taken as none, as the filter would take it.
    var share = max(abs(from_middle) - (0.5 - 0.5 / ratio), vec2(0.0)) * ratio;
    share = select(share, vec2(0.0), share < vec2(1.0 / 512.0));
    let last = vec2<i32>(size) - 1;
    let at = clamp(vec2<i32>(middle), vec2(0), last);
    let next = clamp(at + vec2<i32>(sign(from_middle)), vec2(0), last);
    let across = vec2(next.x, at.y);
    let down = vec2(at.x, next.y);
    let top = mix(textureLoad(picture, at, 0), textureLoad(picture, across, 0), share.x);
    let bottom = mix(textureLoad(picture, down, 0), textureLoad(picture, next, 0), share.x);
    return mix(top, bottom, share.y);
}

@fragment
fn sharp(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    return sharp_color(in.uv);
}

@fragment
fn sharp_srgb(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    let color = sharp_color(in.uv);
    return vec4(srgb_to_linear(color.rgb), color.a);
}

@fragment
fn sharp_oklab(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    let color = sharp_color(in.uv);
    return vec4(oklab_to_linear_rgb(color.rgb), color.a);
}

// A picture brought down to the window, at up to twice its size along a side. The four linear
// samples stand a quarter of the window pixel's span from its middle along each side, so at twice
// each lands on the middle of one of the four picture pixels under the window pixel and reads it
// alone, which takes in every one of them evenly, and nearer one the four draw together into the
// linear filter's one sample.
fn down_color(uv: vec2<f32>) -> vec4<f32> {
    let step = abs(vec2(dpdx(uv.x), dpdy(uv.y))) * 0.25;
    let a = textureSample(picture, picture_sampler, uv + vec2(-step.x, -step.y));
    let b = textureSample(picture, picture_sampler, uv + vec2(step.x, -step.y));
    let c = textureSample(picture, picture_sampler, uv + vec2(-step.x, step.y));
    let d = textureSample(picture, picture_sampler, uv + vec2(step.x, step.y));
    return (a + b + c + d) * 0.25;
}

@fragment
fn down(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    return down_color(in.uv);
}

@fragment
fn down_srgb(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    let color = down_color(in.uv);
    return vec4(srgb_to_linear(color.rgb), color.a);
}

@fragment
fn down_oklab(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    let color = down_color(in.uv);
    return vec4(oklab_to_linear_rgb(color.rgb), color.a);
}
