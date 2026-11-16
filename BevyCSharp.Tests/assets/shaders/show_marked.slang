// A pass that paints green where the camera's image `marked` holds one and red where it holds
// zero, read where the pixel lies in the view.

import bcs_pass;

Texture2D<float> marked;

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    uint width;
    uint height;
    marked.GetDimensions(width, height);
    let value = marked.Load(int3(min(int2(input.uv * float2(width, height)), int2(width, height) - 1), 0));
    return value > 0.5 ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
