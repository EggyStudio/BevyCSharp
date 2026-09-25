// Sixteen cubemaps, of which the shader samples the last one looking straight up.

import bcs;

TextureCube skies[16];
SamplerState linear;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return skies[15].Sample(linear, float3(0.0, 1.0, 0.0));
}
