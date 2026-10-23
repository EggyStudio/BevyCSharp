// A pass that moves each channel along by one, so green becomes red, blue green and red blue.

import bcs_pass;

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    let color = passSample(input.uv);
    return float4(color.g, color.b, color.r, 1.0);
}
