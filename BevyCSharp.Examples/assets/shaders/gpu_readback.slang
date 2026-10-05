// Bevy's gpu_readback shader in Slang, which adds one to each number of a buffer and writes the
// same number into a pixel of an image, so both have something to read back.
import bcs_compute;

RWStructuredBuffer<uint> data;
[format("r32ui")] RWTexture2D<uint> texture;

[shader("compute")]
[numthreads(1, 1, 1)]
void main(uint3 global_id : SV_DispatchThreadID)
{
    data[global_id.x] += 1u;
    texture[int2(int(global_id.x), 0)] = data[global_id.x];
}
