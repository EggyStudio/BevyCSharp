// Paints the middle of the first texture, which is where a compute shader's image is bound.

#import bevy_pbr::forward_io::VertexOutput

@group(#{MATERIAL_BIND_GROUP}) @binding(1) var picture: texture_2d<f32>;
@group(#{MATERIAL_BIND_GROUP}) @binding(2) var picture_sampler: sampler;

@fragment
fn fragment(mesh: VertexOutput) -> @location(0) vec4<f32> {
    return textureSample(picture, picture_sampler, vec2<f32>(0.5, 0.5));
}
