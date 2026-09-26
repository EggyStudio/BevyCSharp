// Draws a square of `size` facing the camera's z axis at each center in `centers`, one instance a
// square, in `color`, placed entirely from the buffer.

import bcs_pass;

StructuredBuffer<float4> centers;
uniform float size;
uniform float4 color;

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
    let center = centers[instance_id].xyz;
    let world = center + float3(corners[vertex_id % 6] * size, 0.0);

    Corner corner;
    corner.position = bcs_pass::transform(bcs_pass::view.clip_from_world, float4(world, 1.0));
    return corner;
}

[shader("fragment")]
float4 fragment(Corner corner) : SV_Target
{
    return color;
}
