// Paints green once time has started, from Bevy's globals, and red before, which says the prelude's
// view of the globals lines up with what Bevy binds.

import bcs;

[shader("fragment")]
float4 fragment(MeshVertexOutput mesh) : SV_Target
{
    return meshGlobals.time > 0.0 && meshGlobals.frame_count > 0 ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
