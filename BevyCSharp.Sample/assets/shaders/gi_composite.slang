// Screen-space global illumination, the last step: one triangle over the whole picture, drawn
// added after opaque geometry is lit, adding each pixel's gathered light times the color of the
// surface there. Before transparent geometry and tonemapping, so glass is drawn over it and the
// tonemapper sees it as light like any other.

import bcs_pass;

Texture2D<uint4> gbuffer;
Texture2D gi;

uniform float strength;

struct Corner
{
    float4 position : SV_Position;
};

[shader("vertex")]
Corner vertex(uint vertex_id : SV_VertexID)
{
    let corner = float2(float((vertex_id << 1) & 2), float(vertex_id & 2));

    Corner output;
    // Nearest there is in Bevy's reversed depth, so every pixel passes the depth test.
    output.position = float4(corner * 2.0 - 1.0, 1.0, 1.0);
    return output;
}

[shader("fragment")]
float4 fragment(Corner corner) : SV_Target
{
    let pixel = int2(corner.position.xy);
    let surface = bcs_pass::surface_of(gbuffer.Load(int3(pixel, 0)));

    // A metal reflects rather than scatters, so it takes none of this diffuse light.
    let color = surface.base_color * (1.0 - surface.metallic);
    let uv = corner.position.xy / float2(bcs_pass::picture_size());
    let light = gi.SampleLevel(bcs_pass::picture_sampler, uv, 0.0).rgb;

    return float4(color * light * strength, 0.0);
}
