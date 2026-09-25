// Draws a mesh of quads, each placed at the center `centers` holds for it.
//
// The mesh's own positions are each corner's offset from its quad's center, so the mesh on its own
// is every quad stacked at the origin. What spreads them out is the buffer, which a compute shader
// can move without the mesh changing.

import bcs;

StructuredBuffer<float4> centers;

struct Corner
{
    float3 offset : TEXCOORD0;
    uint instance_index : SV_InstanceID;
    uint vertex_id : SV_VertexID;
};

[shader("vertex")]
bcs::VertexOutput vertex(Corner corner)
{
    let world = centers[bcs::vertex_number(corner.instance_index, corner.vertex_id) / 4].xyz + corner.offset;

    bcs::VertexOutput output;
    output.world_position = float4(world, 1.0);
    output.position = bcs::world_to_clip(world);
    output.world_normal = float3(0.0, 0.0, 1.0);
    output.uv = float2(0.0);
    return output;
}

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return float4(0.0, 1.0, 0.0, 1.0);
}
