// Moves every center in `centers` by `by`.

import bcs_compute;

uniform float3 by;
RWStructuredBuffer<float4> centers;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint count;
    uint stride;
    centers.GetDimensions(count, stride);

    if (id.x < count)
        centers[id.x] += float4(by, 0.0);
}
