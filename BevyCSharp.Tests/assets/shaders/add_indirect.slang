// Adds light to the picture where the G-buffer has a surface, times that surface's color, the way a
// screen-space GI result is composited: a triangle over the whole picture, drawn added, after
// opaque geometry and before transparent geometry and tonemapping.

import bcs_pass;

Texture2D<uint4> gbuffer;
uniform float4 light;

struct Corner
{
    float4 position : SV_Position;
};

[shader("vertex")]
Corner vertex(uint vertex_id : SV_VertexID)
{
    let corner = float2(float((vertex_id << 1) & 2), float(vertex_id & 2));

    Corner output;
    // Nearest there is in Bevy's reversed depth, so the depth test passes everywhere.
    output.position = float4(corner * 2.0 - 1.0, 1.0, 1.0);
    return output;
}

[shader("fragment")]
float4 fragment(Corner corner) : SV_Target
{
    let surface = bcs_pass::surface_of(gbuffer.Load(int3(int2(corner.position.xy), 0)));
    return float4(surface.base_color * light.rgb, 0.0);
}
