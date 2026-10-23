// Raises the mesh and lowers it with the time in both passes, each reading the time from the
// binding its pass gives it, and paints it `color`.
//
// The prepass binds less than the main pass, its time among what moves, so a prepass stage
// reading `meshGlobals` is a pipeline that refuses to build and ends the app.

import bcs;

uniform float4 color;

[shader("vertex")]
MeshVertexOutput vertex(MeshVertex vertex)
{
    vertex.position.y += sin(meshGlobals.time * 4.0) * 0.1;
    return meshStandardVertex(vertex);
}

[shader("fragment")]
float4 fragment(MeshVertexOutput mesh) : SV_Target
{
    return color;
}

[shader("vertex")]
MeshPrepassVertexOutput prepass_vertex(MeshPrepassVertex vertex)
{
    vertex.position.y += sin(meshPrepassGlobals.time * 4.0) * 0.1;
    return meshPrepassOutput(vertex.instance_index, vertex.position, vertex.normal, vertex.uv);
}
