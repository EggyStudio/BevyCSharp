// Each of sixty four threads writes, in four numbers, the sum of one over the threads before it in
// its subgroup, its index in the subgroup, the first thread's global index read from that thread,
// and the low bits of a ballot every thread votes yes in.

import bcs_compute;

RWStructuredBuffer<uint4> into;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    let before = WavePrefixSum(1u);
    let first = WaveReadLaneAt(id.x, 0);
    let votes = WaveActiveBallot(true);
    into[id.x] = uint4(before, WaveGetLaneIndex(), first, votes.x);
}
