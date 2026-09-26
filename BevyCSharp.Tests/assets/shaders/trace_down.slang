// Traces one ray a thread straight down from `height` onto the scene Solari keeps, along a row of
// points `spacing` apart centered on the origin, and writes what each met into `hits`: the
// surface's base color and the distance to it, or all -1 where the ray met nothing.

import bcs_ray;

uniform float height;
uniform float spacing;
RWStructuredBuffer<float4> hits;

[shader("compute")]
[numthreads(16, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint count;
    uint stride;
    hits.GetDimensions(count, stride);

    if (id.x >= count)
        return;

    let x = (float(id.x) - float(count - 1) * 0.5) * spacing;
    let hit = bcs_ray::trace(float3(x, height, 0.0), float3(0.0, -1.0, 0.0), 0.001, 1000.0);

    if (!hit.hit)
    {
        hits[id.x] = float4(-1.0);
        return;
    }

    let surface = bcs_ray::surface_at(hit);
    hits[id.x] = float4(surface.base_color, hit.t);
}
