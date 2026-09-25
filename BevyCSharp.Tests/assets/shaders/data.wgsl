// Paints the last of however many colors the material's data holds.
//
// The last rather than the first, so a picture of it says the whole buffer arrived rather than a
// prefix of it the size of a uniform.

#import bevy_pbr::forward_io::VertexOutput

@group(#{MATERIAL_BIND_GROUP}) @binding(3) var<storage, read> colors: array<vec4<f32>>;

@fragment
fn fragment(mesh: VertexOutput) -> @location(0) vec4<f32> {
    return colors[arrayLength(&colors) - 1u];
}
