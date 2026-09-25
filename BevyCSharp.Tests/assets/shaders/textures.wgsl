// Samples the last of the 2D texture slots, or the second layer of the first array texture, so a
// picture of it says the bindings past the first one are where the bridge documents them.

#import bevy_pbr::forward_io::VertexOutput

@group(#{MATERIAL_BIND_GROUP}) @binding(16) var last_texture: texture_2d<f32>;
@group(#{MATERIAL_BIND_GROUP}) @binding(17) var last_sampler: sampler;
@group(#{MATERIAL_BIND_GROUP}) @binding(20) var layers: texture_2d_array<f32>;

@fragment
fn fragment(mesh: VertexOutput) -> @location(0) vec4<f32> {
#ifdef ARRAY
    return textureSample(layers, last_sampler, vec2<f32>(0.5, 0.5), 1);
#else
    return textureSample(last_texture, last_sampler, vec2<f32>(0.5, 0.5));
#endif
}
