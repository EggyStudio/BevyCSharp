// Fills the level of an image it is handed with `value`.

import bcs_compute;

[format("r32f")] RWTexture2D<float> level;
uniform float value;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width;
    uint height;
    level.GetDimensions(width, height);

    if (id.x < width && id.y < height)
        level[id.xy] = value;
}
