// Builds the second level of a camera's pyramid from the first: the largest of each two by two,
// doubled, so a picture of it says the level was written from the one before.

import bcs_pass;

Texture2D<float> pyramid_mip0;
[format("r32f")] RWTexture2D<float> pyramid_mip1;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width;
    uint height;
    pyramid_mip1.GetDimensions(width, height);

    if (id.x >= width || id.y >= height)
        return;

    let at = int2(id.xy) * 2;
    let a = pyramid_mip0.Load(int3(at, 0));
    let b = pyramid_mip0.Load(int3(at + int2(1, 0), 0));
    let c = pyramid_mip0.Load(int3(at + int2(0, 1), 0));
    let d = pyramid_mip0.Load(int3(at + int2(1, 1), 0));
    pyramid_mip1[id.xy] = max(max(a, b), max(c, d)) * 2.0;
}
