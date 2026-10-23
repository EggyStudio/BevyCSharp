// A pass that paints green where the surface is between `nearest` and `farthest` from the camera,
// in world units, and red everywhere else.

import bcs_pass;

uniform float nearest;
uniform float farthest;

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    let away = passDistanceAt(input.position);
    return away > nearest && away < farthest ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
