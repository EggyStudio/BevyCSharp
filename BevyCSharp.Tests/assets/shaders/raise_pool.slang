// Moves every vertex of a geometry pool up by `by`, as a shader deforming a mesh in place would.

import bcs_compute;
import bcs_scene;

RWStructuredBuffer<bcs_scene::PoolVertex> vertices;
uniform float by;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint count;
    uint stride;
    vertices.GetDimensions(count, stride);

    if (id.x < count)
        vertices[id.x].position.y += by;
}
