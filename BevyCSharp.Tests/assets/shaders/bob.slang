// Raises the mesh and lowers it with the time in both passes, each reading the time from the
// binding its pass gives it, and paints it `color`.
//
// The prepass binds less than the main pass, its time among what moves, so a prepass stage
// reading `bcs::globals` is a pipeline that refuses to build and ends the app.

import bcs;

uniform float4 color;

[shader("vertex")]
bcs::VertexOutput vertex(bcs::Vertex vertex)
{
    vertex.position.y += sin(bcs::globals.time * 4.0) * 0.1;
    return bcs::standard_vertex(vertex);
}

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return color;
}

[shader("vertex")]
bcs::PrepassVertexOutput prepass_vertex(bcs::PrepassVertex vertex)
{
    vertex.position.y += sin(bcs::prepass_globals.time * 4.0) * 0.1;
    return bcs::prepass_output(vertex.instance_index, vertex.position, vertex.normal, vertex.uv);
}
