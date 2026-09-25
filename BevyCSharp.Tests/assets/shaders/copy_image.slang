// Copies a float image into a buffer, which is how a test reads back what another dispatch wrote
// into it.

import bcs_compute;

Texture2D<float4> source;
RWStructuredBuffer<float4> into;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    into[0] = source.Load(int3(3, 0, 0));
}
