// Draws a square of `size` at each center in `centers` into two images at once: which instance it
// is, counted from one, into the first, and a half into the second.

import bcs_pass;

StructuredBuffer<float4> centers;
uniform float size;

struct Corner
{
    float4 position : SV_Position;
    nointerpolation uint id : ID;
};

struct Written
{
    uint id : SV_Target0;
    float value : SV_Target1;
};

static const float2 corners[6] = {
    float2(-1.0, -1.0), float2(1.0, -1.0), float2(1.0, 1.0),
    float2(-1.0, -1.0), float2(1.0, 1.0), float2(-1.0, 1.0),
};

[shader("vertex")]
Corner vertex(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
{
    let world = centers[instance_id].xyz + float3(corners[vertex_id % 6] * size, 0.0);

    Corner corner;
    corner.position = bcs_pass::transform(bcs_pass::view.clip_from_world, float4(world, 1.0));
    corner.id = instance_id + 1;
    return corner;
}

[shader("fragment")]
Written fragment(Corner corner)
{
    Written written;
    written.id = corner.id;
    written.value = 0.5;
    return written;
}
