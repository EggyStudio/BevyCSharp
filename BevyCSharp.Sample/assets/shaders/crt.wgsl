// A pass that makes the picture look like it came through an old screen: the channels pulled
// apart toward the edges, and dark lines across it. Its first float is how strong.
//
// Run after tonemapping, because it is about the picture as a picture rather than about light.

#import bevy_core_pipeline::fullscreen_vertex_shader::FullscreenVertexOutput

struct Params {
    values: array<vec4<f32>, 16>,
};

@group(0) @binding(0) var picture: texture_2d<f32>;
@group(0) @binding(1) var picture_sampler: sampler;
@group(0) @binding(2) var<uniform> params: Params;

@fragment
fn fragment(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    let strength = params.values[0].x;
    let from_middle = in.uv - vec2<f32>(0.5);
    let shift = from_middle * dot(from_middle, from_middle) * 0.05 * strength;

    let color = vec3<f32>(
        textureSample(picture, picture_sampler, in.uv + shift).r,
        textureSample(picture, picture_sampler, in.uv).g,
        textureSample(picture, picture_sampler, in.uv - shift).b,
    );

    let size = vec2<f32>(textureDimensions(picture));
    let lines = 1.0 - strength * 0.25 * (0.5 + 0.5 * sin(in.uv.y * size.y * 3.14159));

    return vec4<f32>(color * lines, 1.0);
}
