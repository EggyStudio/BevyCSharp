// Each of sixty four threads writes how many threads its subgroup has, the sum of one over that
// subgroup, and whether it is the subgroup's first thread, so a test can tell the subgroup
// operations ran as subgroup operations.

import bcs_compute;

RWStructuredBuffer<uint> into;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    let total = WaveActiveSum(1u);
    into[id.x] = total + WaveGetLaneCount() * 1000 + (WaveGetLaneIndex() == 0 ? 100000 : 0);
}
