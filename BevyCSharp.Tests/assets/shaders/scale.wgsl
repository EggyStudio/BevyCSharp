// A compute shader that multiplies every number in its buffer by its first float.

struct Params {
    values: array<vec4<f32>, 16>,
};

@group(0) @binding(0) var<uniform> params: Params;
@group(0) @binding(1) var<storage, read_write> numbers: array<f32>;

@compute @workgroup_size(64)
fn main(@builtin(global_invocation_id) id: vec3<u32>) {
    if (id.x >= arrayLength(&numbers)) {
        return;
    }

    numbers[id.x] = numbers[id.x] * params.values[0].x;
}
