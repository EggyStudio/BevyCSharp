// Draws each firefly as a glowing square turned to face the camera, placed from the buffer the
// compute shader moves.
//
// The mesh is one square per firefly with its corners at the origin, so where each one is comes
// entirely from the buffer. The glow is far brighter than white, which is what the camera's bloom
// spreads into a halo. The material blends by adding, so it is never drawn in the prepass, and the
// vertex and fragment shaders can hand each other a struct of their own.

#import bevy_pbr::{
    mesh_bindings::mesh,
    mesh_view_bindings::{view, globals},
    view_transformations::position_world_to_clip,
}

struct Firefly {
    position: vec3<f32>,
    phase: f32,
    home: vec3<f32>,
    speed: f32,
};

@group(#{MATERIAL_BIND_GROUP}) @binding(3) var<storage, read> flies: array<Firefly>;

struct Corner {
    @builtin(instance_index) instance_index: u32,
    @builtin(vertex_index) vertex_index: u32,
    @location(0) offset: vec3<f32>,
    @location(2) uv: vec2<f32>,
};

struct Glow {
    @builtin(position) position: vec4<f32>,
    @location(0) uv: vec2<f32>,
    @location(1) @interpolate(flat) phase: f32,
};

@vertex
fn vertex(corner: Corner) -> Glow {
    // Counted from where this mesh begins in the buffer every mesh is packed into.
    let number = corner.vertex_index - mesh[corner.instance_index].first_vertex_index;
    let fly = flies[number / 4u];

    // The camera's own right and up, so the square always faces it.
    let right = view.world_from_view[0].xyz;
    let up = view.world_from_view[1].xyz;
    let world = fly.position + (right * corner.offset.x + up * corner.offset.y) * 0.05;

    var out: Glow;
    out.position = position_world_to_clip(world);
    out.uv = corner.uv;
    out.phase = fly.phase;
    return out;
}

@fragment
fn fragment(in: Glow) -> @location(0) vec4<f32> {
    let flicker = 0.6 + 0.4 * sin(globals.time * 7.0 + in.phase * 13.0);
    let glow = pow(saturate(1.0 - distance(in.uv, vec2<f32>(0.5)) * 2.0), 2.0) * flicker;

    return vec4<f32>(vec3<f32>(1.0, 0.75, 0.25) * glow * 12.0, glow);
}
