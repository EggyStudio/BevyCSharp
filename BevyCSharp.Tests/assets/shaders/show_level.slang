// A pass that paints green where the second level of the camera's pyramid holds `expected`, and red
// anywhere else.
//
// Loaded rather than sampled, because a 32-bit float image cannot be filtered, and a texture a
// shader samples is bound as one that can.

import bcs_pass;

Texture2D<float> pyramid;
uniform float expected;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let level = pyramid.Load(int3(int2(input.position.xy) / 2, 1));
    return abs(level - expected) < 0.01 ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
