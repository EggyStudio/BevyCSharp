// Copies every texel of an eight by eight float image, and the first texel of its second level,
// into a buffer, for the test to read back.

import bcs_compute;

Texture2D<float> image;
RWStructuredBuffer<float> into;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    into[id.y * 8 + id.x] = image.Load(int3(id.xy, 0));

    if (all(id.xy == 0))
        into[64] = image.Load(int3(0, 0, 1));
}
