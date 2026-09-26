// The layout of what the engine and a shader exchange about the scene, which any shader can reach
// with `import bcs_scene;`, whatever it runs as.
//
// Nothing here is bound. A buffer the engine keeps (see `Shaders.CreateInstanceBuffer`) or an image
// Bevy's lighting reads (see `Render.SetIrradianceVolume`) is handed to a shader by name like any
// other, and this says how it is laid out.

module bcs_scene;

public namespace bcs_scene
{

/// One slot of an instance buffer: an entity's transform this frame and on the previous one.
///
/// Matrices are four columns, which `transform` multiplies out. A slot with no entity in it holds
/// zeros, which transform everything to the origin.
public struct Instance
{
    public float4 world_from_local[4];
    public float4 previous_world_from_local[4];
};

/// One slot of a material buffer: what an entity's standard material says it is made of.
///
/// All zeros for a slot with no entity, or one whose entity has no standard material. Textures are
/// not in it; The material multiplies its texture by `base_color`.
public struct Material
{
    /// Linear, with alpha.
    public float4 base_color;
    /// Linear, in the material's own units.
    public float4 emissive;
    public float perceptual_roughness;
    public float metallic;
    /// Specular reflectance at normal incidence.
    public float reflectance;
    /// One for an unlit material, which shows its color whatever lights it.
    public float unlit;
};

/// Multiplies a matrix held as four columns by a vector.
public float4 transform(float4 columns[4], float4 v)
{
    return columns[0] * v.x + columns[1] * v.y + columns[2] * v.z + columns[3] * v.w;
}

/// Where a point on an instance is in the world this frame.
public float3 to_world(Instance instance, float3 local)
{
    return transform(instance.world_from_local, float4(local, 1.0)).xyz;
}

/// Where the same point was on the previous frame, for motion and reprojection.
public float3 to_previous_world(Instance instance, float3 local)
{
    return transform(instance.previous_world_from_local, float4(local, 1.0)).xyz;
}

/// Where an instance's origin is this frame.
public float3 position(Instance instance)
{
    return instance.world_from_local[3].xyz;
}

/// Where an instance's origin was on the previous frame.
public float3 previous_position(Instance instance)
{
    return instance.previous_world_from_local[3].xyz;
}

/// The six directions an irradiance volume keeps light for, as `irradiance_texel` takes them.
///
/// Each is the way a surface faces, and it keeps the light that surface receives, so `POSITIVE_Y`
/// lights a floor.
public static const uint POSITIVE_X = 0;
public static const uint NEGATIVE_X = 1;
public static const uint POSITIVE_Y = 2;
public static const uint NEGATIVE_Y = 3;
public static const uint POSITIVE_Z = 4;
public static const uint NEGATIVE_Z = 5;

/// How large the image of an irradiance volume `grid` points across has to be.
///
/// Twice as high for the two signs of an axis and three times as deep for the three axes, which is
/// the packing Bevy samples.
public uint3 irradiance_image_size(uint3 grid)
{
    return uint3(grid.x, grid.y * 2, grid.z * 3);
}

/// Where in an irradiance volume's image the light is kept that a surface facing `side` receives
/// at grid point `point`, for a volume `grid` points across.
///
/// Point zero is the corner of the probe's box at its negative end on every axis. The positive
/// half of each axis is the first `grid.y` rows and the negative half the next, which is the way
/// Bevy's shader reads it; its documentation of the layout has the halves the other way round.
public uint3 irradiance_texel(uint3 point, uint side, uint3 grid)
{
    uint axis = side / 2;
    uint negative = side % 2;
    return uint3(point.x, point.y + negative * grid.y, point.z + axis * grid.z);
}

/// Where grid point `point` of an irradiance volume `grid` points across sits inside the probe's
/// box, from minus a half to a half on each axis, before the probe's transform.
///
/// The points are the centers of the cells the box is cut into, which is where Bevy's filtering
/// puts each one at full weight. The probe's `world_from_local` turns this into a world position.
public float3 irradiance_point_in_box(uint3 point, uint3 grid)
{
    return (float3(point) + 0.5) / float3(grid) - 0.5;
}

/// One vertex of a geometry pool.
public struct PoolVertex
{
    /// Where it is on its mesh, with one in `w`.
    public float4 position;
    /// Which way its surface faces, with zero in `w`; all zero for a mesh that had no normals.
    public float4 normal;
    /// Its texture coordinates in `xy`.
    public float4 uv;
};

/// One mesh of a geometry pool: where its vertices and indices start in the pool's buffers, how
/// many there are, and the box it fits in, on the mesh before any transform.
public struct PoolMesh
{
    public uint first_vertex;
    public uint vertex_count;
    public uint first_index;
    public uint index_count;
    public float4 bounds_min;
    public float4 bounds_max;
};

/// How many triangles a pool's mesh has.
public uint triangle_count(PoolMesh mesh)
{
    return mesh.index_count / 3;
}

/// Corner `corner`, from zero to two, of triangle `triangle` of a pool's mesh.
public PoolVertex pool_corner(
    StructuredBuffer<PoolVertex> vertices,
    StructuredBuffer<uint> indices,
    PoolMesh mesh,
    uint triangle,
    uint corner)
{
    let index = indices[mesh.first_index + triangle * 3 + corner];
    return vertices[mesh.first_vertex + index];
}

/// Where a ray from `origin` along `direction` meets the triangle `a`, `b`, `c`, as its distance
/// along the ray in `x` (in units of `direction`'s length) and the barycentric weights of `b` and
/// `c` in `y` and `z`, with one in `w` for a hit and zero for a miss.
///
/// Both faces count, since a ray gathering light should see a thin wall from either side. The
/// weights interpolate anything the corners carry: the normal at the hit is
/// `a * (1 - y - z) + b * y + c * z`.
public float4 intersect_triangle(float3 origin, float3 direction, float3 a, float3 b, float3 c)
{
    let edge1 = b - a;
    let edge2 = c - a;
    let p = cross(direction, edge2);
    let determinant = dot(edge1, p);

    if (abs(determinant) < 1e-9)
        return float4(0.0);

    let inverse = 1.0 / determinant;
    let offset = origin - a;
    let u = dot(offset, p) * inverse;

    if (u < 0.0 || u > 1.0)
        return float4(0.0);

    let q = cross(offset, edge1);
    let v = dot(direction, q) * inverse;

    if (v < 0.0 || u + v > 1.0)
        return float4(0.0);

    let t = dot(edge2, q) * inverse;
    return t > 0.0 ? float4(t, u, v, 1.0) : float4(0.0);
}

/// Where a ray from `origin` along `direction` enters a box, as the distance along it, or a
/// negative number where it misses, which skips a mesh whose bounds a ray does not reach.
public float intersect_box(float3 origin, float3 direction, float3 low, float3 high)
{
    // A ray along an axis has zeros in its direction, and WGSL leaves one over zero undefined
    // rather than infinite, so a zero is made tiny and the slab it would divide is effectively
    // endless, as infinity would have made it.
    let tiny = 1e-20;
    let steady = float3(
        abs(direction.x) < tiny ? tiny : direction.x,
        abs(direction.y) < tiny ? tiny : direction.y,
        abs(direction.z) < tiny ? tiny : direction.z);
    let inverse = 1.0 / steady;
    let near = (low - origin) * inverse;
    let far = (high - origin) * inverse;
    let enter = max(max(min(near.x, far.x), min(near.y, far.y)), min(near.z, far.z));
    let leave = min(min(max(near.x, far.x), max(near.y, far.y)), max(near.z, far.z));
    return leave >= max(enter, 0.0) ? max(enter, 0.0) : -1.0;
}

}
