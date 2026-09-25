// A pass that inverts the picture by `amount`, from none at zero to all of it at one.

import bcs_pass;

uniform float amount;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let color = bcs_pass::sample(input.uv);
    return float4(lerp(color.rgb, 1.0 - color.rgb, amount), 1.0);
}
