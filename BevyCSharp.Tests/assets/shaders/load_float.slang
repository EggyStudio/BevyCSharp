// Paints green where the second texel of `heights` holds `expected`, a number no eight-bit image
// could keep, and red anywhere else.

import bcs;

Texture2D<float> heights;
uniform float expected;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    let value = heights.Load(int3(1, 0, 0));
    return abs(value - expected) < 0.01 ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
