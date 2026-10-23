// A pass that paints green once time has started, and red before.

import bcs_pass;

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    return passGlobals.time > 0.0 && passGlobals.frame_count > 0 ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
