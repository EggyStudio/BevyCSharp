// Pushes the mesh out along its normals by `swell`, in both passes, and paints it `color`.
//
// The main pass draws the swollen shape and the prepass draws the same one, so the shadow it casts
// is the shadow of what is on screen. A program without the prepass entry casts the shadow of the
// mesh it started as, which the test compares.

import bcs;

uniform float swell;
uniform float4 color;

[shader("vertex")]
MeshVertexOutput vertex(MeshVertex vertex)
{
    vertex.position += vertex.normal * swell;
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
    let moved = vertex.position + vertex.normal * swell;
    return meshPrepassOutput(vertex.instance_index, moved, vertex.normal, vertex.uv);
}
