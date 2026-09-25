// The smallest Slang material, whose color is the first row of the material's floats.

import bcs;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return bcs::row(0);
}
