// Paints the color a matrix makes of a unit vector, which says a C# matrix reaches the shader the
// right way round.

import bcs;

uniform float4x4 twist;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return float4(mul(twist, float4(1.0, 0.0, 0.0, 0.0)).xyz, 1.0);
}
