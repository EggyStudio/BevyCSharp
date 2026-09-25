// A fragment shader that compiles but disagrees with the material's bind group, by reading
// binding zero as a texture where the material holds its floats in a uniform.
//
// Nothing wrong with it is visible to naga, since it only sees the shader. wgpu sees it when the
// pipeline is built against the layout, which is the error a hot-reloading session has to survive.

#import bevy_pbr::forward_io::VertexOutput

@group(#{MATERIAL_BIND_GROUP}) @binding(0) var wrong: texture_2d<f32>;

@fragment
fn fragment(mesh: VertexOutput) -> @location(0) vec4<f32> {
    return textureLoad(wrong, vec2<i32>(0, 0), 0);
}
