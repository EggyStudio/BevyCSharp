// Rays for a compute shader of the game's own, which it reaches with `import bcs_ray;`, against the
// scene Bevy's Solari traces, or against a ray scene the game built over a geometry pool.
//
// Solari keeps an acceleration structure over every mesh given to `Render.SetRayTraced`, and
// beside it the vertices, indices, transforms and materials a hit is shaded from. This declares
// the same group Solari binds for its own shaders, in the same order, and the bridge binds Solari's
// bind group to it, so a package's shader traces the scene Solari keeps current rather than
// building a second copy of it.
//
// Ray queries exist only in SPIR-V, so a program importing this compiles its compute stage to
// SPIR-V (`ShaderProgramSettings.ComputeTarget = ShaderTarget.SpirV`), which the bridge hands to
// the driver untouched. It runs where Solari runs, in a bridge built with `--solari` and an app
// that set `Config.RayTracedLighting`, on an adapter with ray queries. The group is space 2, which
// is group two of a dispatch, after the shader's own and the bridge's inputs.
//
// The layout below follows Bevy's `bevy_solari::scene_bindings` for the Bevy version this bridge
// builds against, since a declaration that differs from what Solari binds reads the wrong bytes.
//
// A ray scene of the game's own (`Shaders.CreateRayScene`) needs none of that. A shader declares a
// `RaytracingAccelerationStructure` of its own, is handed the scene by name, and traces it with
// `rayTraceIn` and `rayVisibleIn`, which run anywhere the device has ray queries, Solari or not.
// Only the functions a shader calls bind anything, so importing this for them binds nothing of
// Solari's.

module bcs_ray;


/// One vertex as Solari packs it, the position with the normal's x, the normal's y and z with the
/// texture coordinates, and the tangent.
public struct RayVertex
{
    public float4 a;
    public float4 b;
    public float4 tangent;
};

/// Where an instance's triangles are, among the vertex and index buffers.
public struct RayGeometryIds
{
    public uint vertex_buffer;
    public uint vertex_offset;
    public uint index_buffer;
    public uint index_offset;
    public uint triangle_count;
};

/// A standard material as Solari keeps it. A texture number of `RayNoTexture` means the material
/// has none of that kind.
public struct RayMaterial
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

/// An instance's transform as Solari keeps it, the top three rows of the matrix, its translation
/// in each row's last place, the bottom row being always the same.
public struct RayAffine
{
    public float4 rows[3];
};

/// A light Solari samples. The low bit of `kind` says which kind, and the rest is extra data.
public struct RayLightSource
{
    public uint kind;
    public uint id;
};

/// A directional light, as Solari keeps it for sampling.
public struct RayDirectionalLight
{
    public float3 direction_to_light;
    public float cos_theta_max;
    public float3 luminance;
    public float inverse_pdf;
};

public static const uint RayNoTexture = 0xFFFFFFFF;
public static const uint RayLightEmissiveMesh = 0;
public static const uint RayLightDirectional = 1;

// The counts are Solari's, and an array declared shorter or longer than the group it is bound to
// is a different layout.
[[vk::binding(0, 2)]] public StructuredBuffer<RayVertex> rayVertexBuffers[500];
[[vk::binding(1, 2)]] public StructuredBuffer<uint> rayIndexBuffers[500];
[[vk::binding(2, 2)]] public Texture2D<float4> rayTextures[5000];
[[vk::binding(3, 2)]] public SamplerState raySamplers[5000];
[[vk::binding(4, 2)]] public StructuredBuffer<RayMaterial> rayMaterials;
[[vk::binding(5, 2)]] public RaytracingAccelerationStructure rayTlas;
[[vk::binding(6, 2)]] public RaytracingAccelerationStructure rayPreviousTlas;
[[vk::binding(7, 2)]] public StructuredBuffer<RayAffine> rayTransforms;
[[vk::binding(8, 2)]] public StructuredBuffer<RayAffine> rayPreviousTransforms;
[[vk::binding(9, 2)]] public StructuredBuffer<RayGeometryIds> rayGeometryIds;
[[vk::binding(10, 2)]] public StructuredBuffer<uint> rayMaterialIds;
[[vk::binding(11, 2)]] public StructuredBuffer<RayLightSource> rayLightSources;
[[vk::binding(12, 2)]] public StructuredBuffer<RayDirectionalLight> rayDirectionalLights;
[[vk::binding(13, 2)]] public StructuredBuffer<uint> rayPreviousLightIds;
[[vk::binding(14, 2)]] public Texture2D<float4> rayBrdfLut;
[[vk::binding(15, 2)]] public SamplerState rayBrdfLutSampler;

/// What a ray met, or `hit` false where it met nothing between its two distances.
public struct RayHit
{
    public bool hit;
    /// How far along the ray, in units of its direction's length.
    public float t;
    /// Which instance, which is the slot every per-instance buffer above is read at, and the slot
    /// an entity was put in for a ray scene of the game's own.
    public uint instance;
    /// The instance's own number, the pool mesh it is made of, for a ray scene of the game's own.
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
public RayHit rayTrace(float3 origin, float3 direction, float t_min, float t_max)
{
    return rayTraceIn(rayTlas, origin, direction, t_min, t_max);
}

/// Traces a ray against `scene`, and answers the nearest triangle it meets.
public RayHit rayTraceIn(RaytracingAccelerationStructure scene, float3 origin, float3 direction, float t_min, float t_max)
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

    RayHit found;
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
public bool rayVisible(float3 from, float3 to)
{
    return rayVisibleIn(rayTlas, from, to);
}

/// Whether nothing in `scene` is in the way between two points, for a shadow ray.
///
/// Stops at the first triangle met rather than looking for the nearest, since any one of them
/// answers the question.
public bool rayVisibleIn(RaytracingAccelerationStructure scene, float3 from, float3 to)
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

/// Multiplies an instance's transform by a vector.
public float4 rayTransform(RayAffine matrix, float4 v)
{
    return float4(dot(matrix.rows[0], v), dot(matrix.rows[1], v), dot(matrix.rows[2], v), v.w);
}

/// Multiplies a direction by an instance's transform, which leaves out its translation.
public float3 rayTransformDirection(RayAffine matrix, float3 v)
{
    return float3(dot(matrix.rows[0].xyz, v), dot(matrix.rows[1].xyz, v), dot(matrix.rows[2].xyz, v));
}

/// A texture of the scene's, read at `uv` from its largest level.
public float4 raySampleTexture(uint id, float2 uv)
{
    return rayTextures[NonUniformResourceIndex(id)].SampleLevel(
        raySamplers[NonUniformResourceIndex(id)], uv, 0.0);
}

/// What is at a hit, with the material's textures applied.
public struct RaySurface
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

/// Unpacks what `hit` met, where it is, which way it faces, and what it is made of there.
///
/// The vertices are read from the instance's slice of the vertex and index buffers, interpolated
/// by the hit's barycentrics and carried into the world by its transform, and the material's
/// textures are read at the interpolated coordinates. This matches how Solari resolves its own
/// hits, including taking the normal through the transform's upper three columns.
public RaySurface raySurfaceAt(RayHit hit)
{
    let weights = float3(1.0 - hit.barycentrics.x - hit.barycentrics.y, hit.barycentrics);

    let ids = rayGeometryIds[hit.instance];
    let first = hit.triangle * 3 + ids.index_offset;
    let index_buffer = NonUniformResourceIndex(ids.index_buffer);
    let vertex_buffer = NonUniformResourceIndex(ids.vertex_buffer);

    RayVertex corners[3];

    for (uint corner = 0; corner < 3; corner++)
    {
        let index = rayIndexBuffers[index_buffer][first + corner] + ids.vertex_offset;
        corners[corner] = rayVertexBuffers[vertex_buffer][index];
    }

    let world = rayTransforms[hit.instance];
    let previous = rayPreviousTransforms[hit.instance];

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
        world_corners[corner] = rayTransform(world, float4(packed.a.xyz, 1.0)).xyz;
    }

    RaySurface surface;
    surface.position = rayTransform(world, float4(local, 1.0)).xyz;
    surface.previous_position = rayTransform(previous, float4(local, 1.0)).xyz;
    surface.uv = uv;
    surface.tangent = float4(normalize(rayTransformDirection(world, local_tangent)), corners[0].tangent.w);
    surface.geometric_normal = normalize(rayTransformDirection(world, local_normal));
    surface.normal = surface.geometric_normal;

    let material = rayMaterials[rayMaterialIds[hit.instance]];

    if (material.normal_map_texture != RayNoTexture)
    {
        // Neither normalized nor made orthogonal, as mikktspace baking assumes.
        let n = surface.geometric_normal;
        let t = surface.tangent.xyz;
        let b = surface.tangent.w * cross(n, t);

        float3 mapped = raySampleTexture(material.normal_map_texture, uv).xyz * 2.0 - 1.0;
        // Rebuilt from x and y, so a map storing two channels reads the same as one storing three.
        mapped.z = sqrt(max(1.0 - dot(mapped.xy, mapped.xy), 0.0));
        surface.normal = normalize(mapped.x * t + mapped.y * b + mapped.z * n);
    }

    surface.base_color = material.base_color;
    if (material.base_color_texture != RayNoTexture)
    {
        surface.base_color *= raySampleTexture(material.base_color_texture, uv).rgb;
    }

    surface.emissive = material.emissive;
    if (material.emissive_texture != RayNoTexture)
    {
        surface.emissive *= raySampleTexture(material.emissive_texture, uv).rgb;
    }

    surface.reflectance = material.reflectance;
    surface.perceptual_roughness = material.perceptual_roughness;
    surface.metallic = material.metallic;

    if (material.metallic_roughness_texture != RayNoTexture)
    {
        let packed = raySampleTexture(material.metallic_roughness_texture, uv);
        surface.perceptual_roughness *= packed.g;
        surface.metallic *= packed.b;
    }

    surface.roughness = surface.perceptual_roughness * surface.perceptual_roughness;
    surface.triangle_area = length(cross(world_corners[0] - world_corners[1], world_corners[0] - world_corners[2])) * 0.5;

    return surface;
}

/// How many directional lights the scene has, for iterating `rayDirectionalLights`.
public uint rayDirectionalLightCount()
{
    uint count;
    uint stride;
    rayDirectionalLights.GetDimensions(count, stride);
    return count;
}
