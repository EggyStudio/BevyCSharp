// Samples a cube at the middle of three faces, plus X, minus Y and plus Z, into `into`.

import bcs_compute;

TextureCube<float4> cube;
SamplerState nearest;
RWStructuredBuffer<float4> into;

[shader("compute")]
[numthreads(1, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    into[0] = cube.SampleLevel(nearest, float3(1.0, 0.0, 0.0), 0.0);
    into[1] = cube.SampleLevel(nearest, float3(0.0, -1.0, 0.0), 0.0);
    into[2] = cube.SampleLevel(nearest, float3(0.0, 0.0, 1.0), 0.0);
}
