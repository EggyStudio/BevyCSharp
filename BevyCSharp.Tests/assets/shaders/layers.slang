// Sixty-four textures, of which the shader samples the one `pick` names, through a sampler of its
// own.

import bcs;

Texture2D layers[64];
SamplerState linear;
uniform uint pick;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return layers[pick].Sample(linear, float2(0.5, 0.5));
}
