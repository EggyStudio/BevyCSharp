// A pass that paints red where the sun reaches, green where it is shadowed, and black where
// nothing was drawn.

import bcs_pass;

Texture2D<float> sunlit;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let value = sunlit.Load(int3(int2(input.position.xy), 0));

    if (value < 0.0)
        return float4(0.0, 0.0, 0.0, 1.0);

    return value > 0.5 ? float4(1.0, 0.0, 0.0, 1.0) : float4(0.0, 1.0, 0.0, 1.0);
}
