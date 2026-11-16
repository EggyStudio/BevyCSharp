// A pass that shows the camera's image `copied`, a copy of its picture, where the pixel lies in the
// view.

import bcs_pass;

Texture2D copied;

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    uint width;
    uint height;
    copied.GetDimensions(width, height);
    return float4(copied.Load(int3(min(int2(input.uv * float2(width, height)), int2(width, height) - 1), 0)).rgb, 1.0);
}
