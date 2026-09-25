// A pass that paints green where the surface is between its first two floats away from the camera,
// in world units, and red everywhere else.

import bcs_pass;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let away = bcs_pass::distance_at(input.position);
    let inside = away > bcs_pass::number(0) && away < bcs_pass::number(1);
    return inside ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
