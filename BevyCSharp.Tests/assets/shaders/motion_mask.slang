// A pass that paints green where the camera's motion vectors say something moved since the
// previous frame, and red where nothing did.

import bcs_pass;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let moved = length(bcs_pass::motion_at(input.position)) > 1e-4;
    return moved ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
