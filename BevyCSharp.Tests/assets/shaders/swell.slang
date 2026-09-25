// Pushes the mesh out along its normals by `swell`, in both passes, and paints it `color`.
//
// The main pass draws the swollen shape and the prepass draws the same one, so the shadow it
// casts is the shadow of what is on screen. A program without the prepass entry casts the shadow
// of the mesh it started as, which is what the test compares.

import bcs;

uniform float swell;
uniform float4 color;

[shader("vertex")]
bcs::VertexOutput vertex(bcs::Vertex vertex)
{
    vertex.position += vertex.normal * swell;
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
    let moved = vertex.position + vertex.normal * swell;
    return bcs::prepass_output(vertex.instance_index, moved, vertex.normal, vertex.uv);
}
