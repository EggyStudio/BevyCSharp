// Traces a ray from five units in front of the first mesh of a pool straight at it, through every
// triangle, and writes the nearest hit's distance and the normal there, where its box is entered,
// and how many triangles each of the first two meshes has.

import bcs_compute;
import bcs_scene;

StructuredBuffer<PoolVertex> vertices;
StructuredBuffer<uint> indices;
StructuredBuffer<PoolMesh> meshes;
RWStructuredBuffer<float4> into;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    let mesh = meshes[0];
    let origin = float3(0.0, 0.0, 5.0);
    let direction = float3(0.0, 0.0, -1.0);

    var nearest = 1e30;
    var normal = float3(0.0);

    for (uint triangle = 0; triangle < poolTriangleCount(mesh); triangle++)
    {
        let a = poolCorner(vertices, indices, mesh, triangle, 0);
        let b = poolCorner(vertices, indices, mesh, triangle, 1);
        let c = poolCorner(vertices, indices, mesh, triangle, 2);
        let hit = intersectTriangle(origin, direction, a.position.xyz, b.position.xyz, c.position.xyz);

        if (hit.w > 0.0 && hit.x < nearest)
        {
            nearest = hit.x;
            normal = a.normal.xyz * (1.0 - hit.y - hit.z) + b.normal.xyz * hit.y + c.normal.xyz * hit.z;
        }
    }

    into[0] = float4(nearest, normal);
    into[1] = float4(intersectBox(origin, direction, mesh.bounds_min.xyz, mesh.bounds_max.xyz), 0.0, 0.0, 0.0);
    into[2] = float4(float(poolTriangleCount(meshes[0])), float(poolTriangleCount(meshes[1])), float(meshes[1].first_vertex), 0.0);
}
