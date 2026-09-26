// Draws a level square of `size` at each center in `centers`, in whatever view it runs with, which
// for a draw that casts shadows is each of the camera's shadow cascades as well as the camera.

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
    let corner2 = corners[vertex_id % 6] * size;
    let world = centers[instance_id].xyz + float3(corner2.x, 0.0, corner2.y);

    Corner corner;
    corner.position = bcs_pass::transform(bcs_pass::view.clip_from_world, float4(world, 1.0));
    return corner;
}

[shader("fragment")]
float4 fragment(Corner corner) : SV_Target
{
    return float4(1.0, 1.0, 1.0, 1.0);
}
