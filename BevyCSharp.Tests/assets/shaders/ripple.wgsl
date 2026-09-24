// A material that moves its own vertices, which is what a vertex shader override is for.
//
// The mesh is pushed along its normal by the first float the material carries, so a picture of it
// says the vertex stage ran and read the same uniform the fragment stage does.

#import bevy_pbr::{
    mesh_functions,
    forward_io::{Vertex, VertexOutput},
    view_transformations::position_world_to_clip,
}

struct Params {
    values: array<vec4<f32>, 4>,
};

@group(3) @binding(0) var<uniform> params: Params;

@vertex
fn vertex(vertex: Vertex) -> VertexOutput {
    var out: VertexOutput;

    let world_from_local = mesh_functions::get_world_from_local(vertex.instance_index);
    let swollen = vertex.position + (vertex.normal * params.values[0].x);

    out.world_position = mesh_functions::mesh_position_local_to_world(
        world_from_local,
        vec4<f32>(swollen, 1.0),
    );

    out.position = position_world_to_clip(out.world_position.xyz);
    out.world_normal = vertex.normal;
    out.uv = vertex.uv;

    return out;
}

@fragment
fn fragment(mesh: VertexOutput) -> @location(0) vec4<f32> {
    return params.values[1];
}
