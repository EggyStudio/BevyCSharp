// Adds one to a counter per workgroup that runs, so reading it back says how many ran.

import bcs_compute;

RWStructuredBuffer<Atomic<uint>> counter;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    counter[0].add(1u);
}
