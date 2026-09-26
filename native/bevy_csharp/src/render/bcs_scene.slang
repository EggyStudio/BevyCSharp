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
/// not in it; `base_color` is what the material multiplies its texture by.
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
    /// One for an unlit material, whose color is what it shows whatever lights it.
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

/// Where the same point was on the previous frame, which is what motion and reprojection need.
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
/// Each is the way a surface faces, and what is kept is the light that surface receives, so
/// `POSITIVE_Y` is what lights a floor.
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

}
