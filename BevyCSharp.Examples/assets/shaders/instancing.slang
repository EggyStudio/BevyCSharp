// Bevy's instancing shader in Slang, a cube drawn once for each entry of a buffer, moved and sized
// by it and colored by it. Bevy feeds the cube's vertices from its mesh and the entries as instance
// attributes, and a camera's draw here is handed only the vertex and instance numbers, so the
// cube's corners are worked out from the first and the entry read by the second.
import bcs_pass;

struct InstanceData
{
    float4 position_scale;
    float4 color;
};

StructuredBuffer<InstanceData> instances;

struct VertexOutput
{
    float4 clip_position : SV_Position;
    float4 color : TEXCOORD0;
};

// The eight corners of a cube half a unit across, Bevy's Cuboid::new(0.5, 0.5, 0.5).
static const float3 corners[8] = {
    float3(-0.25, -0.25, -0.25), float3(0.25, -0.25, -0.25), float3(0.25, 0.25, -0.25), float3(-0.25, 0.25, -0.25),
    float3(-0.25, -0.25, 0.25), float3(0.25, -0.25, 0.25), float3(0.25, 0.25, 0.25), float3(-0.25, 0.25, 0.25),
};

// Two triangles for each of the six faces, turning the same way seen from outside.
static const uint triangles[36] = {
    4, 5, 6, 6, 7, 4,
    1, 0, 3, 3, 2, 1,
    5, 1, 2, 2, 6, 5,
    0, 4, 7, 7, 3, 0,
    7, 6, 2, 2, 3, 7,
    0, 1, 5, 5, 4, 0,
};

[shader("vertex")]
VertexOutput vertex(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
{
    let instance = instances[instance_id];
    let position = corners[triangles[vertex_id % 36]] * instance.position_scale.w + instance.position_scale.xyz;

    VertexOutput output;
    output.clip_position = bcs_pass::transform(bcs_pass::view.clip_from_world, float4(position, 1.0));
    output.color = instance.color;
    return output;
}

[shader("fragment")]
float4 fragment(VertexOutput input) : SV_Target
{
    return input.color;
}
