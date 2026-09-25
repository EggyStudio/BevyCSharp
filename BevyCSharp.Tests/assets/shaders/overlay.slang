// A pass with textures and a buffer of its own: it paints the picture over with the texture
// `stamp`, tinted by the last color in `tints`.

import bcs_pass;

Texture2D stamp;
SamplerState stamp_sampler;
StructuredBuffer<float4> tints;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    uint count;
    uint stride;
    tints.GetDimensions(count, stride);
    return float4(stamp.Sample(stamp_sampler, input.uv).rgb * tints[count - 1].rgb, 1.0);
}
