// Paints the middle of `picture`, which is where a compute shader's image is put.

import bcs;

Texture2D picture;
SamplerState linear;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return picture.Sample(linear, float2(0.5, 0.5));
}
