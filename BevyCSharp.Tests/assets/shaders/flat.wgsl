// A material drawn by a shader the test wrote, which is the smallest one worth checking.
//
// The colour comes from the first four floats the material carries, so a picture of it says the
// numbers crossed the boundary, reached the uniform, and were read at the binding the bridge
// documents.

#import bevy_pbr::forward_io::VertexOutput

struct Params {
    values: array<vec4<f32>, 4>,
};

@group(3) @binding(0) var<uniform> params: Params;

@fragment
fn fragment(mesh: VertexOutput) -> @location(0) vec4<f32> {
    return params.values[0];
}
