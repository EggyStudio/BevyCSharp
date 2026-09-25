// Fills the first level of a camera's pyramid with a number.

import bcs_pass;

[format("r32f")] RWTexture2D<float> pyramid;
uniform float value;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width;
    uint height;
    pyramid.GetDimensions(width, height);

    if (id.x < width && id.y < height)
        pyramid[id.xy] = value;
}
