// Writes workgroup counts for the next dispatch into a buffer, so the GPU decides how much work
// follows.

import bcs_compute;

RWByteAddressBuffer counts;
uniform uint groups;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    counts.Store3(0, uint3(groups, 1, 1));
}
