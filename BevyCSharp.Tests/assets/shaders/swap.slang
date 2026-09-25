// A pass that moves each channel along by one, so green becomes red, blue green and red blue.

import bcs_pass;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let color = bcs_pass::sample(input.uv);
    return float4(color.g, color.b, color.r, 1.0);
}
