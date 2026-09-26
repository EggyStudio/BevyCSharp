// A pass showing two images a draw wrote together: red where the first holds the first instance,
// and the second's value as green.

import bcs_pass;

Texture2D<uint> ids;
Texture2D<float> values;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let at = int3(int2(input.position.xy), 0);
    return float4(ids.Load(at) == 1 ? 1.0 : 0.0, values.Load(at), 0.0, 1.0);
}
