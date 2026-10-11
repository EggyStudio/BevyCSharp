// A mesh colored by a stream of its own, one word a vertex at location 8 holding a color's four
// bytes, red first, unpacked in the vertex stage and drawn as it is.
import bcs;

struct Vertex
{
    float3 position : TEXCOORD0;
    uint word : TEXCOORD8;
    uint instance_index : SV_InstanceID;
};

struct VertexOutput
{
    float4 position : SV_Position;
    float4 color : TEXCOORD0;
};

[shader("vertex")]
VertexOutput vertex(Vertex vertex)
{
    VertexOutput output;
    output.position = bcs::local_to_clip(vertex.instance_index, vertex.position);
    let word = vertex.word;
    output.color = float4(float(word & 0xff), float((word >> 8) & 0xff), float((word >> 16) & 0xff), float(word >> 24)) / 255.0;
    return output;
}

[shader("fragment")]
float4 fragment(VertexOutput input) : SV_Target
{
    return input.color;
}
