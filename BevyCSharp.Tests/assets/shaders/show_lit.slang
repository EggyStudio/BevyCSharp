// A pass that paints what `lit` holds at each pixel as a gray, after tonemapping, so a test reads
// it back as it was written.

import bcs_pass;

Texture2D<float> lit;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let value = lit.Load(int3(int2(input.position.xy), 0));
    return float4(value, value, value, 1.0);
}
