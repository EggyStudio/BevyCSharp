// A material drawn into Bevy's deferred buffers, a surface of the color, metal and roughness it is
// given, written by its deferred stage. Its prepass vertex shader writes all of what the deferred
// stage reads.

import bcs;

uniform float4 color;
uniform float metallic;
uniform float roughness;

Surface given(Surface surface)
{
    surface.base_color = color;
    surface.metallic = metallic;
    surface.roughness = roughness;
    return surface;
}

[shader("vertex")]
MeshPrepassVertexOutput prepass_vertex(MeshPrepassVertex vertex)
{
    return meshPrepassOutput(vertex.instance_index, vertex.position, vertex.normal, vertex.uv);
}

[shader("fragment")]
Deferred deferred(MeshPrepassVertexOutput mesh)
{
    return meshDeferred(given(meshSurface(mesh)), mesh);
}
