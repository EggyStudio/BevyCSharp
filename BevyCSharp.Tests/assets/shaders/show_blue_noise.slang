// A pass that paints the camera's blue noise for this frame as gray.

import bcs_pass;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let value = bcs_pass::blue_noise(int2(input.position.xy)).r;
    return float4(value, value, value, 1.0);
}
