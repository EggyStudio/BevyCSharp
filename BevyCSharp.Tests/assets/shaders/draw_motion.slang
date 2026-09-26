// Draws a square of `size` at each center in `centers` into the prepass's motion texture, writing
// a fixed motion, as a pass resolving a visibility buffer writes the motion of what it resolved.

import bcs_pass;

StructuredBuffer<float4> centers;
uniform float size;

struct Corner
{
    float4 position : SV_Position;
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
    return corner;
}

[shader("fragment")]
float4 fragment(Corner corner) : SV_Target
{
    return float4(0.25, 0.0, 0.0, 0.0);
}
