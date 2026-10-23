// Works out, for each pixel inside the first point-type light's reach, how much of it arrives
// through the camera's shadow maps, which for a spot light is its layer of the directional ones.

import bcs_pass;

[format("r32f")] RWTexture2D<float> sunlit;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    let size = passPictureSize();

    if (id.x >= size.x || id.y >= size.y)
        return;

    let pixel = int2(id.xy);
    let depth_value = passLoadDepth(pixel);
    let world = passWorldPosition(passUv(pixel), depth_value);

    // Nothing drawn, or outside the cone, marked apart from both lit and shadowed.
    if (depth_value <= 0.0 || passPointLightCount() == 0 || all(passPointLightRadiance(0, world) <= 0.0))
    {
        sunlit[pixel] = -1.0;
        return;
    }

    sunlit[pixel] = passPointShadow(0, world, passLoadNormal(pixel));
}
