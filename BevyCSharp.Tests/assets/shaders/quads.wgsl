// Draws a mesh of quads, each placed at the center the material's buffer holds for it.
//
// The mesh's own positions are each corner's offset from its quad's center, so the mesh on its own
// is every quad stacked at the origin. What spreads them out is the buffer, which a compute shader
// can move without the mesh changing.

#import bevy_pbr::{
    forward_io::VertexOutput,
    mesh_bindings::mesh,
    view_transformations::position_world_to_clip,
}

@group(#{MATERIAL_BIND_GROUP}) @binding(3) var<storage, read> centers: array<vec4<f32>>;

struct QuadVertex {
    @builtin(instance_index) instance_index: u32,
    @builtin(vertex_index) vertex_index: u32,
    @location(0) corner: vec3<f32>,
};

@vertex
fn vertex(vertex: QuadVertex) -> VertexOutput {
    // Counted from where this mesh begins in the buffer every mesh is packed into.
    let number = vertex.vertex_index - mesh[vertex.instance_index].first_vertex_index;
    let world = centers[number / 4u].xyz + vertex.corner;

    var out: VertexOutput;
    out.world_position = vec4<f32>(world, 1.0);
    out.position = position_world_to_clip(world);
    out.world_normal = vec3<f32>(0.0, 0.0, 1.0);
    return out;
}

@fragment
fn fragment(in: VertexOutput) -> @location(0) vec4<f32> {
    return vec4<f32>(0.0, 1.0, 0.0, 1.0);
}
