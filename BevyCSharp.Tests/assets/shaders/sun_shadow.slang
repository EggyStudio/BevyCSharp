// Works out, for each pixel, how much of the first directional light reaches the surface there,
// through the camera's own shadow maps.

import bcs_pass;

[format("r32f")] RWTexture2D<float> sunlit;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    let size = bcs_pass::picture_size();

    if (id.x >= size.x || id.y >= size.y)
        return;

    let pixel = int2(id.xy);
    let depth_value = bcs_pass::load_depth(pixel);

    // Nothing drawn here: marked apart from both lit and shadowed.
    if (depth_value <= 0.0)
    {
        sunlit[pixel] = -1.0;
        return;
    }

    let world = bcs_pass::world_position(bcs_pass::uv_of(pixel), depth_value);
    let normal = bcs_pass::load_normal(pixel);
    sunlit[pixel] = bcs_pass::directional_shadow(0, world, normal, bcs_pass::view_depth(world));
}
