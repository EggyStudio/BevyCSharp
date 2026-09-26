// Copies the first channel of every texel of an eight by eight image into a buffer, for the test to
// read back, which for a compressed image is what the GPU decoded.

import bcs_compute;

Texture2D image;
RWStructuredBuffer<float> into;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    into[id.y * 8 + id.x] = image.Load(int3(id.xy, 0)).r;
}
