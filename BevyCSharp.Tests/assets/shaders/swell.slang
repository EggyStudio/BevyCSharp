// The Slang twin of swell.wgsl, pushed out along the normal by the first float in both passes
// and painted with the second row.

import bcs;

[shader("vertex")]
bcs::VertexOutput vertex(bcs::Vertex vertex)
{
    vertex.position += vertex.normal * bcs::number(0);
    return bcs::standard_vertex(vertex);
}

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return bcs::row(1);
}

[shader("vertex")]
bcs::PrepassVertexOutput prepass_vertex(bcs::PrepassVertex vertex)
{
    let moved = vertex.position + vertex.normal * bcs::number(0);
    return bcs::prepass_output(vertex.instance_index, moved, vertex.normal, vertex.uv);
}
