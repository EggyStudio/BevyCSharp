// Paints green once time has started, from Bevy's globals, and red before, which is what says the
// prelude's view of the globals lines up with what Bevy binds.

import bcs;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return bcs::globals.time > 0.0 && bcs::globals.frame_count > 0 ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
