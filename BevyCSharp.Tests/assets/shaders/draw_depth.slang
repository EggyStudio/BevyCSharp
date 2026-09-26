// Draws a square of `size` at each center in `centers` into the prepass's motion texture, writing no
// motion and the nearest depth there is, as a pass resolving a visibility buffer writes the depth
// of what it resolved.

import bcs_pass;

StructuredBuffer<float4> centers;
uniform float size;

struct Corner
{
    float4 position : SV_Position;
};

struct Written
{
    float4 motion : SV_Target0;
    float depth : SV_Depth;
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
Written fragment(Corner corner)
{
    Written written;
    written.motion = float4(0.0);
    // Nearest in Bevy's reversed depth.
    written.depth = 1.0;
    return written;
}
