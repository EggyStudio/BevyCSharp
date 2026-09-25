// A thousand numbers, of which the shader reads the last, times a color.
//
// Sixteen kilobytes of uniform, since every element of an array of floats in a uniform takes a
// row of sixteen bytes. Reading the last one says the whole block arrived rather than a prefix.

import bcs;

uniform float weights[1000];
uniform float4 color;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return float4(color.rgb * weights[999], 1.0);
}
