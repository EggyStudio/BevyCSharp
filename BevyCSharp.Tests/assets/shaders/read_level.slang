// Copies the first texel of each of an image's first two levels into a buffer, for the test to read
// back.

import bcs_compute;

Texture2D<float> pyramid;
RWStructuredBuffer<float> into;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    into[0] = pyramid.Load(int3(0, 0, 0));
    into[1] = pyramid.Load(int3(0, 0, 1));
}
