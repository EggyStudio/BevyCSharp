// A camera's picture brought to a window larger than it, in one copy at the frame's end.
//
// The vertex stage is Bevy's full-screen triangle, drawn over the part of the window the whole
// picture spans, so each window pixel reads the picture at its own place. The sampler decides how,
// filtered for a soft picture, or the nearest picture pixel for a sharp one. A picture kept in sRGB
// or Oklab to be blended in is turned linear here, as Bevy's own copy does, by an entry point of
// each, since the bridge hands its WGSL to wgpu as it is written, with no preprocessor between.

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
