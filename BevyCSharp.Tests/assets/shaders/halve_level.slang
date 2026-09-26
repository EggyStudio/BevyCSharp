// Writes the level below from the level above: the largest of each two by two, doubled.

import bcs_compute;

Texture2D<float> above;
[format("r32f")] RWTexture2D<float> below;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width;
    uint height;
    below.GetDimensions(width, height);

    if (id.x >= width || id.y >= height)
        return;

    let at = int2(id.xy) * 2;
    let a = above.Load(int3(at, 0));
    let b = above.Load(int3(at + int2(1, 0), 0));
    let c = above.Load(int3(at + int2(0, 1), 0));
    let d = above.Load(int3(at + int2(1, 1), 0));
    below[id.xy] = max(max(a, b), max(c, d)) * 2.0;
}
