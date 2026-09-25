// The smallest material worth checking: it paints the color it was given.
//
// A picture of it says a number crossed the boundary, was put where the reflection says `color`
// is, and was read there.

import bcs;

uniform float4 color;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return color;
}
