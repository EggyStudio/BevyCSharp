// What a shader running on a camera sees of it: the picture drawn so far, time, the view and the
// previous frame's, and the depth, normals and motion the camera's prepass drew.
//
// A full-screen pass and a compute shader dispatched on a camera both reach it with
// `import bcs_pass;`, and everything in it is under the `bcs_pass` namespace. What the shader
// itself carries is whatever it declares: every global without a binding of its own belongs to it
// and is set from C# by its name, and an image the camera owns is bound by the name the camera
// gave it. The inputs below are in space 101, which the bridge moves to group one after the
// compile, so the shader's own globals keep group zero to themselves whatever they are.
//
// A pass's entry point is a fragment shader taking `bcs_pass::Input`, which is what Bevy's
// full-screen triangle writes, and returning the pixel. A compute shader works in pixels, and the
// helpers taking an `int2` are for it.

module bcs_pass;

public namespace bcs_pass
{

/// Time, in the layout Bevy's `Globals` has.
public struct Globals
{
    /// Seconds since startup, wrapping to zero after an hour.
    public float time;
    /// Seconds since the previous frame.
    public float delta_time;
    public uint frame_count;
};

/// The part of Bevy's view uniform a shader usually reads, the same prefix `bcs::View` is.
///
/// Matrices are arrays of four columns, which `transform` multiplies out.
public struct View
{
    public float4 clip_from_world[4];
    public float4 unjittered_clip_from_world[4];
    public float4 world_from_clip[4];
    public float4 world_from_view[4];
    public float4 view_from_world[4];
    public float4 clip_from_view[4];
    public float4 view_from_clip[4];
    public float3 world_position;
    public float exposure;
    public float4 viewport;
    public float4 main_pass_viewport;
};

/// The view as it was on the previous frame, which is what a shader reprojecting last frame's
/// results reads.
public struct PreviousView
{
    public float4 view_from_world[4];
    public float4 clip_from_world[4];
    public float4 clip_from_view[4];
    public float4 world_from_clip[4];
    public float4 view_from_clip[4];
};

/// The picture so far, and the sampler it is read with, which is linear and clamped at the edges.
[[vk::binding(0, 101)]] public Texture2D picture;
[[vk::binding(1, 101)]] public SamplerState picture_sampler;

/// Time.
///
/// Laid out as C lays it out (`ScalarDataLayout`), which is twelve bytes and what Bevy binds. The
/// usual constant buffer rules round the struct up to sixteen, and a pipeline refuses a shader
/// that reads more of a buffer than its layout promises.
[[vk::binding(2, 101)]] public ConstantBuffer<Globals, ScalarDataLayout> globals;
[[vk::binding(3, 101)]] public ConstantBuffer<View> view;

/// The camera's depth, from its prepass, or zero, which is the far plane, where it draws none.
///
/// Bevy's depth runs backwards, from one at the near plane to zero infinitely far away, so a
/// larger number is nearer. `Shaders.SetPrepass` asks a camera for it.
[[vk::binding(4, 101)]] public DepthTexture2D depth;

/// The camera's normals, from its prepass, each packed from minus one to one into zero to one, or
/// white where it draws none.
[[vk::binding(5, 101)]] public Texture2D normals;

/// The camera's motion vectors, from its prepass, or zero where it draws none.
///
/// How far each pixel moved since the previous frame, in UV units, so the same surface was at
/// `uv - motion` on the previous frame. Both the camera's movement and the object's are in it.
[[vk::binding(6, 101)]] public Texture2D<float2> motion;

/// The previous frame's view, or zeros on a camera that keeps none, which is every 2D camera.
[[vk::binding(7, 101)]] public ConstantBuffer<PreviousView> previous_view;

// -- The scene's lights, laid out as Bevy lays them out

/// One cascade of a directional light's shadow: what projects the world onto its layer of the
/// shadow map, and how far from the camera it reaches.
public struct DirectionalCascade
{
    public float4 clip_from_world[4];
    public float texel_size;
    public float far_bound;
};

/// A directional light, the sun most often.
public struct DirectionalLight
{
    public DirectionalCascade cascades[4];
    /// Its color times its brightness, which is what it lights a surface with before exposure.
    public float4 color;
    /// The way to the light, from any surface, as a unit vector.
    public float3 direction_to_light;
    public uint flags;
    public float soft_shadow_size;
    public float shadow_depth_bias;
    public float shadow_normal_bias;
    public uint num_cascades;
    public float cascades_overlap_proportion;
    /// The first layer of the directional shadow maps its cascades take.
    public uint depth_texture_base_index;
    public uint decal_index;
    public float sun_disk_angular_size;
    public float sun_disk_intensity;
};

/// The part of Bevy's per-view light data a shader usually reads.
public struct Lights
{
    public DirectionalLight directional_lights[10];
    /// The ambient light's color times its brightness.
    public float4 ambient_color;
    public uint4 cluster_dimensions;
    public float4 cluster_factors;
    public uint n_directional_lights;
    /// What a spot light's index is moved by to find its layer of the directional shadow maps,
    /// which is where spot lights keep theirs.
    public int spot_light_shadowmap_offset;
};

/// A point or a spot light.
public struct PointLight
{
    /// For a point light, what turns a distance along the cube's major axis into a shadow depth.
    /// For a spot light, its direction's x and z, and its angular falloff.
    public float4 light_custom_data;
    /// Its color times its brightness, and one over its range squared.
    public float4 color_inverse_square_range;
    /// Where it is, and how large.
    public float4 position_radius;
    public uint flags;
    public float shadow_depth_bias;
    public float shadow_normal_bias;
    public float spot_light_tan_angle;
    public float soft_shadow_size;
    public float shadow_map_near_z;
    public uint decal_index;
    public float range;
};

/// How many point and spot lights `point_lights` holds.
public struct LightCounts
{
    public uint point_lights;
    public uint unused_a;
    public uint unused_b;
    public uint unused_c;
};

/// A point light casts shadows.
public static const uint POINT_LIGHT_SHADOWS = 1u << 0u;
/// A point light is a spot light.
public static const uint POINT_LIGHT_SPOT = 1u << 5u;
/// A directional light casts shadows.
public static const uint DIRECTIONAL_LIGHT_SHADOWS = 1u << 0u;

[[vk::binding(8, 101)]] public ConstantBuffer<Lights> lights;
[[vk::binding(9, 101)]] public StructuredBuffer<PointLight> point_lights;
[[vk::binding(10, 101)]] public DepthTexture2DArray directional_shadow_maps;
[[vk::binding(11, 101)]] public DepthTextureCubeArray point_shadow_maps;
[[vk::binding(12, 101)]] public SamplerComparisonState shadow_sampler;
[[vk::binding(13, 101)]] public ConstantBuffer<LightCounts> light_counts;

/// How many directional lights the camera sees.
public uint directional_light_count()
{
    return lights.n_directional_lights;
}

/// How many point and spot lights there are.
public uint point_light_count()
{
    return light_counts.point_lights;
}

/// How much of a directional light reaches a point in the world, from nothing in full shadow to one
/// in full light, the way Bevy's own materials work it out with its hardware filtering.
///
/// `view_depth` is how far in front of the camera the point is, which picks the cascade.
public float directional_shadow(uint light_index, float3 world, float3 normal, float view_depth)
{
    let light = lights.directional_lights[light_index];

    if ((light.flags & DIRECTIONAL_LIGHT_SHADOWS) == 0u)
        return 1.0;

    var cascade = light.num_cascades;
    for (uint i = 0u; i < light.num_cascades; i++)
    {
        if (view_depth < light.cascades[i].far_bound)
        {
            cascade = i;
            break;
        }
    }

    if (cascade >= light.num_cascades)
        return 1.0;

    let normal_offset = light.shadow_normal_bias * light.cascades[cascade].texel_size * normal;
    let depth_offset = light.shadow_depth_bias * light.direction_to_light;
    let offset = float4(world + normal_offset + depth_offset, 1.0);

    let clip = transform(light.cascades[cascade].clip_from_world, offset);
    if (clip.w <= 0.0)
        return 1.0;

    let ndc = clip.xyz / clip.w;
    if (any(ndc.xy < float2(-1.0)) || ndc.z < 0.0 || any(ndc > float3(1.0)))
        return 1.0;

    let uv = ndc.xy * float2(0.5, -0.5) + 0.5;
    let layer = float(light.depth_texture_base_index + cascade);
    return directional_shadow_maps.SampleCmpLevelZero(shadow_sampler, float3(uv, layer), ndc.z);
}

/// How much of a point light reaches a point in the world, from nothing in full shadow to one in
/// full light, the way Bevy's own materials work it out with its hardware filtering.
///
/// Spot lights keep their shadows elsewhere, and are answered as unshadowed here.
public float point_shadow(uint light_index, float3 world, float3 normal)
{
    let light = point_lights[light_index];

    if ((light.flags & POINT_LIGHT_SHADOWS) == 0u || (light.flags & POINT_LIGHT_SPOT) != 0u)
        return 1.0;

    let surface_to_light = light.position_radius.xyz - world;
    let away = abs(surface_to_light);
    let distance_to_light = max(away.x, max(away.y, away.z));

    let normal_offset = light.shadow_normal_bias * distance_to_light * normal;
    let depth_offset = light.shadow_depth_bias * normalize(surface_to_light);
    let offset = world + normal_offset + depth_offset;

    let local = offset - light.position_radius.xyz;
    let local_abs = abs(local);
    let major = max(local_abs.x, max(local_abs.y, local_abs.z));

    let zw = -major * light.light_custom_data.xy + light.light_custom_data.zw;
    let depth_value = zw.x / zw.y;

    return point_shadow_maps.SampleCmpLevelZero(
        shadow_sampler,
        float4(local * float3(1.0, 1.0, -1.0), float(light_index)),
        depth_value);
}

/// How much light a point light sends to a point in the world before shadow, which is its color
/// falling off with distance the way Bevy's does, inside its range.
public float3 point_light_radiance(uint light_index, float3 world)
{
    let light = point_lights[light_index];
    let to_light = light.position_radius.xyz - world;
    let distance_squared = dot(to_light, to_light);
    let factor = distance_squared * light.color_inverse_square_range.w;
    let window = saturate(1.0 - factor * factor);
    let attenuation = window * window / max(distance_squared, 0.0001);
    return light.color_inverse_square_range.rgb * attenuation;
}

/// What Bevy's full-screen triangle hands the fragment shader.
public struct Input
{
    public float4 position : SV_Position;
    /// Where the pixel is on the picture, from zero at the top left to one at the bottom right.
    public float2 uv : TEXCOORD0;
};

/// Multiplies a matrix held as four columns by a vector.
public float4 transform(float4 columns[4], float4 v)
{
    return columns[0] * v.x + columns[1] * v.y + columns[2] * v.z + columns[3] * v.w;
}

/// The picture so far at a point, from zero to one across it.
public float4 sample(float2 uv)
{
    return picture.Sample(picture_sampler, uv);
}

/// How large the picture is, in pixels.
public uint2 picture_size()
{
    uint width;
    uint height;
    picture.GetDimensions(width, height);
    return uint2(width, height);
}

/// How far apart two neighboring pixels are in `uv`, which is what a blur steps by.
public float2 pixel_size()
{
    return 1.0 / float2(picture_size());
}

/// The middle of a pixel in `uv`, which is where a compute shader working a pixel at a time samples.
public float2 uv_of(int2 pixel)
{
    return (float2(pixel) + 0.5) * pixel_size();
}

/// The depth at a pixel.
public float load_depth(int2 pixel)
{
    return depth.Load(int3(pixel, 0));
}

/// The world-space normal at a pixel.
public float3 load_normal(int2 pixel)
{
    return normals.Load(int3(pixel, 0)).xyz * 2.0 - 1.0;
}

/// How far a pixel moved since the previous frame, in UV units.
public float2 load_motion(int2 pixel)
{
    return motion.Load(int3(pixel, 0));
}

/// The depth at a pixel, given the pixel's `SV_Position`.
public float depth_at(float4 position)
{
    return load_depth(int2(position.xy));
}

/// The world-space normal of the surface at a pixel, given its `SV_Position`.
public float3 normal_at(float4 position)
{
    return load_normal(int2(position.xy));
}

/// How far the surface at a pixel moved since the previous frame, in UV units, given its
/// `SV_Position`.
public float2 motion_at(float4 position)
{
    return load_motion(int2(position.xy));
}

/// Where the surface at `uv` was on the previous frame's picture.
public float2 previous_uv(float2 uv, float2 moved)
{
    return uv - moved;
}

/// How far in front of the camera a surface at `depth_value` is, in world units.
///
/// Worked out the way Bevy's own `depth_ndc_to_view_z` does for a perspective camera. Infinitely
/// far where nothing was drawn.
public float distance_from_depth(float depth_value)
{
    let near = view.clip_from_view[3][2];
    return near / max(depth_value, 1e-7);
}

/// How far in front of the camera the surface at a pixel is, in world units, given its
/// `SV_Position`.
public float distance_at(float4 position)
{
    return distance_from_depth(depth_at(position));
}

/// Where in the world the surface at `uv` with `depth_value` is.
public float3 world_position(float2 uv, float depth_value)
{
    let ndc = float4(uv.x * 2.0 - 1.0, 1.0 - uv.y * 2.0, depth_value, 1.0);
    let world = transform(view.world_from_clip, ndc);
    return world.xyz / world.w;
}

/// How far in front of the camera a point in the world is, along the way it looks, which is what
/// picks a directional light's shadow cascade.
public float view_depth(float3 world)
{
    return -transform(view.view_from_world, float4(world, 1.0)).z;
}

/// Where a point in the world was on the previous frame's picture, in `uv`.
public float2 previous_uv_of(float3 world)
{
    let clip = transform(previous_view.clip_from_world, float4(world, 1.0));
    let ndc = clip.xy / clip.w;
    return float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5);
}

}
