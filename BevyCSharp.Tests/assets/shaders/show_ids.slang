// A pass that paints a visibility buffer: black where it holds nothing, red for the first
// instance, green for the second.

import bcs_pass;

Texture2D<uint> visibility;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let id = visibility.Load(int3(int2(input.position.xy), 0));

    if (id == 1) return float4(1.0, 0.0, 0.0, 1.0);
    if (id == 2) return float4(0.0, 1.0, 0.0, 1.0);
    return float4(0.0, 0.0, 0.0, 1.0);
}
