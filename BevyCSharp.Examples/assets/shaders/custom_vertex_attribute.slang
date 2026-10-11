// Bevy's custom_vertex_attribute shader in Slang, each vertex's blend color, a stream of the mesh's
// own at location 8, times the material's color.
import bcs;

struct CustomMaterial
{
    float4 color;
};

uniform CustomMaterial material;

struct Vertex
{
    float3 position : TEXCOORD0;
    float4 blend_color : TEXCOORD8;
    uint instance_index : SV_InstanceID;
};

struct VertexOutput
{
    float4 clip_position : SV_Position;
    float4 blend_color : TEXCOORD0;
};

[shader("vertex")]
VertexOutput vertex(Vertex vertex)
{
    VertexOutput output;
    output.clip_position = bcs::local_to_clip(vertex.instance_index, vertex.position);
    output.blend_color = vertex.blend_color;
    return output;
}

[shader("fragment")]
float4 fragment(VertexOutput input) : SV_Target
{
    return material.color * input.blend_color;
}
