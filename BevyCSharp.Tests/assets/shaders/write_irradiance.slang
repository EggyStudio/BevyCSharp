// Fills every point of an irradiance volume with one color per direction a surface can face, so a
// picture of a lit cube says which face read which direction.

import bcs_compute;
import bcs_scene;

uniform uint size;
uniform float4 light[6];
[format("rgba16f")] WTexture3D<float4> volume;

[shader("compute")]
[numthreads(4, 4, 4)]
void main(uint3 id : SV_DispatchThreadID)
{
    let grid = uint3(size);

    if (any(id >= grid)) return;

    for (uint side = 0; side < 6; side++)
    {
        volume.Store(bcs_scene::irradiance_texel(id, side, grid), light[side]);
    }
}
