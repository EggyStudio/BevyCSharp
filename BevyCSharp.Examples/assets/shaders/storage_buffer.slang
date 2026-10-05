// Bevy's storage_buffer shader in Slang, each cube colored by the entry of a buffer of five colors
// its MeshTag names, read in the vertex shader and handed on.
import bcs;

StructuredBuffer<float4> colors;

struct Vertex
{
    float3 position : TEXCOORD0;
    uint instance_index : SV_InstanceID;
};

struct VertexOutput
{
    float4 clip_position : SV_Position;
    float4 world_position : TEXCOORD0;
    float4 color : TEXCOORD1;
};

[shader("vertex")]
VertexOutput vertex(Vertex vertex)
{
    VertexOutput output;
    let tag = bcs::tag(vertex.instance_index);
    output.world_position = float4(bcs::local_to_world(vertex.instance_index, vertex.position), 1.0);
    output.clip_position = bcs::world_to_clip(output.world_position.xyz);
    output.color = colors[tag];
    return output;
}

[shader("fragment")]
float4 fragment(VertexOutput mesh) : SV_Target
{
    return mesh.color;
}
