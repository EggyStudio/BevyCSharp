// Moves every center in its buffer by the first three floats.

struct Params {
    values: array<vec4<f32>, 16>,
};

@group(0) @binding(0) var<uniform> params: Params;
@group(0) @binding(1) var<storage, read_write> centers: array<vec4<f32>>;

@compute @workgroup_size(64)
fn main(@builtin(global_invocation_id) id: vec3<u32>) {
    if (id.x >= arrayLength(&centers)) {
        return;
    }

    centers[id.x] = centers[id.x] + vec4<f32>(params.values[0].xyz, 0.0);
}
