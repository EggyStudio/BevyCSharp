// A 2D material of one color, its square moved across by `offset` where its own 2D vertex shader
// draws it.
import bcs2d;

uniform float4 color;
uniform float2 offset;

[shader("vertex")]
bcs2d::VertexOutput vertex(bcs2d::Vertex v)
{
    var output = bcs2d::standard_vertex(v);
    output.world_position.xy += offset;
    output.position = bcs2d::world_to_clip(output.world_position.xyz);
    return output;
}

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput mesh) : SV_Target
{
    return color;
}
