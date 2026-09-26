// A pass that paints red where the camera's depth says something is drawn this frame, and green
// where last frame's depth says something was.

import bcs_pass;

Texture2D<float> depth_previous;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let now = bcs_pass::depth_at(input.position) > 0.0;
    let then = depth_previous.Load(int3(int2(input.position.xy), 0)) > 0.0;
    return float4(now ? 1.0 : 0.0, then ? 1.0 : 0.0, 0.0, 1.0);
}
