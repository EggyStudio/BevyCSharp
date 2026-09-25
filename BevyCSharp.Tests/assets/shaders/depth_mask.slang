// A pass that paints green wherever the camera's depth says something was drawn, and red where it
// says nothing was, which is depth zero, the far plane.

import bcs_pass;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    return bcs_pass::depth_at(input.position) > 0.0 ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
