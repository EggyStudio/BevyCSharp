// Bevy's automatic_instancing shader in Slang, each of many small cubes colored by the pixel of a
// picture its MeshTag counts to, row by row.
import bcs;

Texture2D texture;
SamplerState texture_sampler;

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

    uint width, height;
    texture.GetDimensions(width, height);
    let texel = uint2(tag % width, tag / width);
    output.color = texture.Load(int3(int2(texel), 0));
    return output;
}

[shader("fragment")]
float4 fragment(VertexOutput mesh) : SV_Target
{
    return mesh.color;
}
