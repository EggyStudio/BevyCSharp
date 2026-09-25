// A compute shader that fills the image it writes with its first row of floats.

struct Params {
    values: array<vec4<f32>, 16>,
};

@group(0) @binding(0) var<uniform> params: Params;
@group(0) @binding(6) var image: texture_storage_2d<rgba8unorm, write>;

@compute @workgroup_size(8, 8)
fn main(@builtin(global_invocation_id) id: vec3<u32>) {
    let size = textureDimensions(image);

    if (id.x >= size.x || id.y >= size.y) {
        return;
    }

    textureStore(image, id.xy, params.values[0]);
}
