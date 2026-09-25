// A pass that inverts the picture by as much as its first float says, from none at zero to all
// of it at one.

#import bevy_core_pipeline::fullscreen_vertex_shader::FullscreenVertexOutput

struct Params {
    values: array<vec4<f32>, 16>,
};

@group(0) @binding(0) var picture: texture_2d<f32>;
@group(0) @binding(1) var picture_sampler: sampler;
@group(0) @binding(2) var<uniform> params: Params;

@fragment
fn fragment(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    let color = textureSample(picture, picture_sampler, in.uv);
    return vec4<f32>(mix(color.rgb, vec3<f32>(1.0) - color.rgb, params.values[0].x), 1.0);
}
