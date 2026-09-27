// Copies the texel at the middle of `picture` into the first element of `into`, as it is stored,
// for a test to read back a value a capture would clamp.

import bcs_compute;

Texture2D<float4> picture;
RWStructuredBuffer<float4> into;

[shader("compute")]
[numthreads(1, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width;
    uint height;
    picture.GetDimensions(width, height);
    into[0] = picture.Load(int3(width / 2, height / 2, 0));
}
