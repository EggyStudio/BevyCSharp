// Traces one ray a thread straight down from `height` through `scene`, a ray scene over a geometry
// pool, along a row of points `spacing` apart centered on the origin, and writes what each met into
// `hits`: the slot it hit, the pool mesh, the distance, and one, or all -1 where it met nothing.

import bcs_ray;

uniform float height;
uniform float spacing;
RaytracingAccelerationStructure scene;
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
    let hit = bcs_ray::trace_in(scene, float3(x, height, 0.0), float3(0.0, -1.0, 0.0), 0.001, 1000.0);

    hits[id.x] = hit.hit ? float4(float(hit.instance), float(hit.mesh), hit.t, 1.0) : float4(-1.0);
}
