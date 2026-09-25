// Moves every firefly along a looping path around its home, from the time Bevy keeps.
//
// Run as a compute shader every frame, over a buffer the fireflies' material reads, so the
// positions never leave the GPU. Edit this while the sample runs and the swarm changes shape.

#import bevy_render::globals::Globals

struct Firefly {
    position: vec3<f32>,
    phase: f32,
    home: vec3<f32>,
    speed: f32,
};

struct Params {
    values: array<vec4<f32>, 16>,
};

@group(0) @binding(0) var<uniform> params: Params;
@group(0) @binding(1) var<storage, read_write> flies: array<Firefly>;
@group(0) @binding(5) var<uniform> globals: Globals;

@compute @workgroup_size(64)
fn main(@builtin(global_invocation_id) id: vec3<u32>) {
    if (id.x >= arrayLength(&flies)) {
        return;
    }

    var fly = flies[id.x];
    let t = globals.time * fly.speed + fly.phase;
    let reach = params.values[0].x;

    fly.position = fly.home + vec3<f32>(
        sin(t) * reach,
        sin(t * 1.7) * reach * 0.5,
        cos(t * 1.3) * reach,
    );

    flies[id.x] = fly;
}
