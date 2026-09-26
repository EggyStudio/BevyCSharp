// Rays for a compute shader of the game's own, which it reaches with `import bcs_ray;`: against the
// scene Bevy's Solari traces, or against a ray scene the game built over a geometry pool.
//
// Solari keeps an acceleration structure over every mesh given to `Render.SetRayTraced`, and
// beside it the vertices, indices, transforms and materials a hit is shaded from. This declares
// the same group Solari binds for its own shaders, in the same order, and the bridge binds Solari's
// bind group to it, so a package's shader traces the scene Solari keeps current rather than
// building a second copy of it.
//
// Ray queries exist only in SPIR-V, so a program importing this compiles its compute stage to
// SPIR-V (`ShaderProgramSettings.ComputeTarget = ShaderTarget.SpirV`), which the bridge hands to the
// driver untouched. It runs where Solari runs, in a bridge built with `--solari` and an app that
// set `Config.RayTracedLighting`, on an adapter with ray queries. The group is space 2, which is
// group two of a dispatch, after the shader's own and the bridge's inputs.
//
// The layout below follows Bevy's `bevy_solari::scene_bindings` for the Bevy version this bridge
// builds against, since a declaration that differs from what Solari binds reads the wrong bytes.
//
// A ray scene of the game's own (`Shaders.CreateRayScene`) needs none of that. A shader declares a
// `RaytracingAccelerationStructure` of its own, is handed the scene by name, and traces it with
// `trace_in` and `visible_in`, which run anywhere the device has ray queries, Solari or not. Only
// the functions a shader calls bind anything, so importing this for them binds nothing of
// Solari's.

module bcs_ray;

public namespace bcs_ray
{

/// One vertex as Solari packs it: the position with the normal's x, the normal's y and z with the
/// texture coordinates, and the tangent.
public struct PackedVertex
{
    public float4 a;
    public float4 b;
    public float4 tangent;
};

/// Where an instance's triangles are, among the vertex and index buffers.
public struct GeometryIds
{
    public uint vertex_buffer;
    public uint vertex_offset;
    public uint index_buffer;
    public uint index_offset;
    public uint triangle_count;
};

/// A standard material as Solari keeps it. A texture number of `NO_TEXTURE` means the material
/// has none of that kind.
public struct Material
{
    public uint normal_map_texture;
    public uint base_color_texture;
    public uint emissive_texture;
    public uint metallic_roughness_texture;
    public float3 base_color;
    public float perceptual_roughness;
    public float3 emissive;
    public float metallic;
    public float3 padding;
    public float reflectance;
};

/// A matrix held as four columns, the way Bevy writes one.
public struct Columns
{
    public float4 columns[4];
};

/// A light Solari samples. The low bit of `kind` says which kind, and the rest is extra data.
public struct LightSource
{
    public uint kind;
    public uint id;
};

/// A directional light, as Solari keeps it for sampling.
public struct DirectionalLight
{
    public float3 direction_to_light;
    public float cos_theta_max;
    public float3 luminance;
    public float inverse_pdf;
};

public static const uint NO_TEXTURE = 0xFFFFFFFF;
public static const uint LIGHT_EMISSIVE_MESH = 0;
public static const uint LIGHT_DIRECTIONAL = 1;

// The counts are Solari's, and an array declared shorter or longer than the group it is bound to
// is a different layout.
[[vk::binding(0, 2)]] public StructuredBuffer<PackedVertex> vertex_buffers[500];
[[vk::binding(1, 2)]] public StructuredBuffer<uint> index_buffers[500];
[[vk::binding(2, 2)]] public Texture2D<float4> textures[5000];
[[vk::binding(3, 2)]] public SamplerState samplers[5000];
[[vk::binding(4, 2)]] public StructuredBuffer<Material> materials;
[[vk::binding(5, 2)]] public RaytracingAccelerationStructure tlas;
[[vk::binding(6, 2)]] public StructuredBuffer<Columns> transforms;
[[vk::binding(7, 2)]] public StructuredBuffer<Columns> previous_transforms;
[[vk::binding(8, 2)]] public StructuredBuffer<GeometryIds> geometry_ids;
[[vk::binding(9, 2)]] public StructuredBuffer<uint> material_ids;
[[vk::binding(10, 2)]] public StructuredBuffer<LightSource> light_sources;
[[vk::binding(11, 2)]] public StructuredBuffer<DirectionalLight> directional_lights;
[[vk::binding(12, 2)]] public StructuredBuffer<uint> previous_light_ids;
[[vk::binding(13, 2)]] public Texture2D<float4> brdf_lut;
[[vk::binding(14, 2)]] public SamplerState brdf_lut_sampler;

/// What a ray met, or `hit` false where it met nothing between its two distances.
public struct Hit
{
    public bool hit;
    /// How far along the ray, in units of its direction's length.
    public float t;
    /// Which instance, which is the slot every per-instance buffer above is read at, and the slot an
    /// entity was put in for a ray scene of the game's own.
    public uint instance;
    /// The instance's own number: the pool mesh it is made of, for a ray scene of the game's own.
    public uint mesh;
    /// Which triangle of that instance's mesh.
    public uint triangle;
    /// How far toward the triangle's second and third corners the hit is.
    public float2 barycentrics;
    /// Whether the ray met the side the triangle's winding faces.
    public bool front_face;
};

/// Traces a ray against Solari's scene, and answers the nearest triangle it meets.
///
/// Every instance is considered and nothing is culled by facing, which is how Solari traces.
public Hit trace(float3 origin, float3 direction, float t_min, float t_max)
{
    return trace_in(tlas, origin, direction, t_min, t_max);
}

/// Traces a ray against `scene`, and answers the nearest triangle it meets.
public Hit trace_in(RaytracingAccelerationStructure scene, float3 origin, float3 direction, float t_min, float t_max)
{
    RayDesc ray;
    ray.Origin = origin;
    ray.Direction = direction;
    ray.TMin = t_min;
    ray.TMax = t_max;

    RayQuery<RAY_FLAG_NONE> query;
    query.TraceRayInline(scene, RAY_FLAG_NONE, 0xFF, ray);

    // Every candidate is a triangle built opaque, which the query commits on its own.
    while (query.Proceed()) {}

    Hit found;
    found.hit = query.CommittedStatus() == COMMITTED_TRIANGLE_HIT;
    found.t = found.hit ? query.CommittedRayT() : t_max;
    found.instance = found.hit ? query.CommittedInstanceIndex() : 0;
    found.mesh = found.hit ? query.CommittedInstanceID() : 0;
    found.triangle = found.hit ? query.CommittedPrimitiveIndex() : 0;
    found.barycentrics = found.hit ? query.CommittedTriangleBarycentrics() : float2(0.0);
    found.front_face = found.hit ? query.CommittedTriangleFrontFace() : false;
    return found;
}

/// Whether nothing in Solari's scene is in the way between two points, for a shadow ray.
public bool visible(float3 from, float3 to)
{
    return visible_in(tlas, from, to);
}

/// Whether nothing in `scene` is in the way between two points, for a shadow ray.
///
/// Stops at the first triangle met rather than looking for the nearest, since any one of them
/// answers the question.
public bool visible_in(RaytracingAccelerationStructure scene, float3 from, float3 to)
{
    let offset = to - from;
    let distance = length(offset);

    if (distance <= 0.0)
    {
        return true;
    }

    RayDesc ray;
    ray.Origin = from;
    ray.Direction = offset / distance;
    ray.TMin = 0.001;
    ray.TMax = distance * 0.999;

    RayQuery<RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH> query;
    query.TraceRayInline(scene, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH, 0xFF, ray);

    while (query.Proceed()) {}

    return query.CommittedStatus() != COMMITTED_TRIANGLE_HIT;
}

/// Multiplies a matrix held as four columns by a vector.
public float4 transform(Columns matrix, float4 v)
{
    return matrix.columns[0] * v.x + matrix.columns[1] * v.y + matrix.columns[2] * v.z
        + matrix.columns[3] * v.w;
}

/// Multiplies a direction by a matrix's upper three columns, which leaves out its translation.
public float3 transform_direction(Columns matrix, float3 v)
{
    return matrix.columns[0].xyz * v.x + matrix.columns[1].xyz * v.y + matrix.columns[2].xyz * v.z;
}

/// A texture of the scene's, read at `uv` from its largest level.
public float4 sample_texture(uint id, float2 uv)
{
    return textures[NonUniformResourceIndex(id)].SampleLevel(
        samplers[NonUniformResourceIndex(id)], uv, 0.0);
}

/// What is at a hit, with the material's textures applied.
public struct Surface
{
    public float3 position;
    /// Where the same point was on the previous frame, for reprojecting what a ray found.
    public float3 previous_position;
    /// With the normal map applied, where the material has one.
    public float3 normal;
    /// Interpolated from the vertices alone.
    public float3 geometric_normal;
    public float4 tangent;
    public float2 uv;
    public float3 base_color;
    public float3 emissive;
    public float perceptual_roughness;
    public float roughness;
    public float metallic;
    public float reflectance;
    /// The triangle's area in the world, which a technique sampling emissive triangles weighs by.
    public float triangle_area;
};

/// Unpacks what `hit` met: where it is, which way it faces, and what it is made of there.
///
/// The vertices are read from the instance's slice of the vertex and index buffers, interpolated
/// by the hit's barycentrics and carried into the world by its transform, and the material's
/// textures are read at the interpolated coordinates. This matches how Solari resolves its own
/// hits, including taking the normal through the transform's upper three columns.
public Surface surface_at(Hit hit)
{
    let weights = float3(1.0 - hit.barycentrics.x - hit.barycentrics.y, hit.barycentrics);

    let ids = geometry_ids[hit.instance];
    let first = hit.triangle * 3 + ids.index_offset;
    let index_buffer = NonUniformResourceIndex(ids.index_buffer);
    let vertex_buffer = NonUniformResourceIndex(ids.vertex_buffer);

    PackedVertex corners[3];

    for (uint corner = 0; corner < 3; corner++)
    {
        let index = index_buffers[index_buffer][first + corner] + ids.vertex_offset;
        corners[corner] = vertex_buffers[vertex_buffer][index];
    }

    let world = transforms[hit.instance];
    let previous = previous_transforms[hit.instance];

    float3 local = float3(0.0);
    float3 local_normal = float3(0.0);
    float3 local_tangent = float3(0.0);
    float2 uv = float2(0.0);
    float3 world_corners[3];

    for (uint corner = 0; corner < 3; corner++)
    {
        let packed = corners[corner];
        local += packed.a.xyz * weights[corner];
        local_normal += float3(packed.a.w, packed.b.xy) * weights[corner];
        local_tangent += packed.tangent.xyz * weights[corner];
        uv += packed.b.zw * weights[corner];
        world_corners[corner] = transform(world, float4(packed.a.xyz, 1.0)).xyz;
    }

    Surface surface;
    surface.position = transform(world, float4(local, 1.0)).xyz;
    surface.previous_position = transform(previous, float4(local, 1.0)).xyz;
    surface.uv = uv;
    surface.tangent = float4(normalize(transform_direction(world, local_tangent)), corners[0].tangent.w);
    surface.geometric_normal = normalize(transform_direction(world, local_normal));
    surface.normal = surface.geometric_normal;

    let material = materials[material_ids[hit.instance]];

    if (material.normal_map_texture != NO_TEXTURE)
    {
        // Neither normalized nor made orthogonal, which is what mikktspace baking assumes.
        let n = surface.geometric_normal;
        let t = surface.tangent.xyz;
        let b = surface.tangent.w * cross(n, t);

        float3 mapped = sample_texture(material.normal_map_texture, uv).xyz * 2.0 - 1.0;
        // Rebuilt from x and y, so a map storing two channels reads the same as one storing three.
        mapped.z = sqrt(max(1.0 - dot(mapped.xy, mapped.xy), 0.0));
        surface.normal = normalize(mapped.x * t + mapped.y * b + mapped.z * n);
    }

    surface.base_color = material.base_color;
    if (material.base_color_texture != NO_TEXTURE)
    {
        surface.base_color *= sample_texture(material.base_color_texture, uv).rgb;
    }

    surface.emissive = material.emissive;
    if (material.emissive_texture != NO_TEXTURE)
    {
        surface.emissive *= sample_texture(material.emissive_texture, uv).rgb;
    }

    surface.reflectance = material.reflectance;
    surface.perceptual_roughness = material.perceptual_roughness;
    surface.metallic = material.metallic;

    if (material.metallic_roughness_texture != NO_TEXTURE)
    {
        let packed = sample_texture(material.metallic_roughness_texture, uv);
        surface.perceptual_roughness *= packed.g;
        surface.metallic *= packed.b;
    }

    surface.roughness = surface.perceptual_roughness * surface.perceptual_roughness;
    surface.triangle_area = length(cross(world_corners[0] - world_corners[1], world_corners[0] - world_corners[2])) * 0.5;

    return surface;
}

/// How many directional lights the scene has, for iterating `directional_lights`.
public uint directional_light_count()
{
    uint count;
    uint stride;
    directional_lights.GetDimensions(count, stride);
    return count;
}

}
