// A material that pushes its vertices out along their normals, in both passes.
//
// The first float is how far. The main pass draws the swollen shape and the prepass draws the
// same one, so the shadow it casts is the shadow of what is on screen. A program without the
// prepass entry casts the shadow of the mesh it started as, which is what the test compares.
//
// The file is compiled whole for each pass, with that pass's defines, so each pass's entry points
// are kept behind `PREPASS_PIPELINE`. Without that the main pass's vertex shader is compiled for
// the shadow pass too, where the mesh's normals are not declared, and the whole file fails there.

#import bevy_pbr::{
    mesh_functions,
    forward_io,
    prepass_io,
    view_transformations::position_world_to_clip,
}

struct Params {
    values: array<vec4<f32>, 16>,
};

@group(#{MATERIAL_BIND_GROUP}) @binding(0) var<uniform> params: Params;

fn swollen(instance_index: u32, position: vec3<f32>, normal: vec3<f32>) -> vec4<f32> {
    let world_from_local = mesh_functions::get_world_from_local(instance_index);
    let moved = position + normal * params.values[0].x;
    return mesh_functions::mesh_position_local_to_world(world_from_local, vec4<f32>(moved, 1.0));
}

#ifndef PREPASS_PIPELINE
@vertex
fn vertex(vertex: forward_io::Vertex) -> forward_io::VertexOutput {
    var out: forward_io::VertexOutput;
    out.world_position = swollen(vertex.instance_index, vertex.position, vertex.normal);
    out.position = position_world_to_clip(out.world_position.xyz);
    out.world_normal = vertex.normal;
    out.uv = vertex.uv;
    return out;
}

@fragment
fn fragment(mesh: forward_io::VertexOutput) -> @location(0) vec4<f32> {
    return params.values[1];
}
#endif

#ifdef PREPASS_PIPELINE
// The prepass hands a vertex shader of a material's own every attribute the mesh has, whichever
// prepass it is, so the normal is at location three even in the shadow pass.
struct PrepassVertex {
    @builtin(instance_index) instance_index: u32,
    @location(0) position: vec3<f32>,
    @location(3) normal: vec3<f32>,
};

@vertex
fn prepass_vertex(vertex: PrepassVertex) -> prepass_io::VertexOutput {
    var out: prepass_io::VertexOutput;
    out.world_position = swollen(vertex.instance_index, vertex.position, vertex.normal);
    out.position = position_world_to_clip(out.world_position.xyz);
#ifdef UNCLIPPED_DEPTH_ORTHO_EMULATION
    out.unclipped_depth = out.position.z;
    out.position.z = min(out.position.z, 1.0);
#endif
    return out;
}
#endif
