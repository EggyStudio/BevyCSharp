// A 2D material of one color, its square moved across by `offset` where its own 2D vertex shader
// draws it.
import bcs2d;

uniform float4 color;
uniform float2 offset;

[shader("vertex")]
Mesh2dVertexOutput vertex(Mesh2dVertex v)
{
    var output = mesh2dStandardVertex(v);
    output.world_position.xy += offset;
    output.position = mesh2dWorldToClip(output.world_position.xyz);
    return output;
}

[shader("fragment")]
float4 fragment(Mesh2dVertexOutput mesh) : SV_Target
{
    return color;
}
