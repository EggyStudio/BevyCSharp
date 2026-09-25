// A pass that paints green once time has started, and red before.

import bcs_pass;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    return bcs_pass::globals.time > 0.0 && bcs_pass::globals.frame_count > 0 ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
