// A material drawn into Bevy's deferred buffers, a surface of the color, metal and roughness it is
// given, written by its deferred stage. Its prepass vertex shader writes all of what the deferred
// stage reads.

import bcs;

uniform float4 color;
uniform float metallic;
uniform float roughness;

bcs::Surface given(bcs::Surface surface)
{
    surface.base_color = color;
    surface.metallic = metallic;
    surface.roughness = roughness;
    return surface;
}

[shader("vertex")]
bcs::PrepassVertexOutput prepass_vertex(bcs::PrepassVertex vertex)
{
    return bcs::prepass_output(vertex.instance_index, vertex.position, vertex.normal, vertex.uv);
}

[shader("fragment")]
bcs::Deferred deferred(bcs::PrepassVertexOutput mesh)
{
    return bcs::deferred(given(bcs::surface(mesh)), mesh);
}
