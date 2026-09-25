// A compute shader that writes its first row of floats into the first element of its buffer,
// which is what a material bound to the same buffer then paints with.

struct Params {
    values: array<vec4<f32>, 16>,
};

@group(0) @binding(0) var<uniform> params: Params;
@group(0) @binding(1) var<storage, read_write> colors: array<vec4<f32>>;

@compute @workgroup_size(1)
fn main() {
    colors[0] = params.values[0];
}
