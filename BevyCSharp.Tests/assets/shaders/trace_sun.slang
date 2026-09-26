// Ray-traced shadows on a camera: for every pixel with a surface, a ray toward the sun through the
// scene Solari keeps, writing one into `lit` where nothing is in the way and zero where something
// is, and a half where the pixel shows nothing.

import bcs_pass;
import bcs_ray;

uniform float3 toward_sun;
[format("r32f")] RWTexture2D<float> lit;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    let size = bcs_pass::picture_size();

    if (id.x >= size.x || id.y >= size.y)
        return;

    let uv = (float2(id.xy) + 0.5) / float2(size);
    let depth = bcs_pass::load_depth(int2(id.xy));

    if (depth <= 0.0)
    {
        lit[id.xy] = 0.5;
        return;
    }

    let world = bcs_pass::world_position(uv, depth);
    let normal = bcs_pass::load_normal(int2(id.xy));

    // Lifted off the surface along its normal, so the ray does not meet the triangle it left.
    let from = world + normal * 0.01;
    lit[id.xy] = bcs_ray::visible(from, from + normalize(toward_sun) * 100.0) ? 1.0 : 0.0;
}
