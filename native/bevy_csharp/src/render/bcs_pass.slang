// What a shader running on a camera sees of it, the picture drawn so far, time, the view and the
// previous frame's, and the depth, normals and motion the camera's prepass drew.
//
// A full-screen pass, a compute shader dispatched on a camera and geometry a camera draws all reach
// it with `import bcs_pass;` and use its names plainly, each helper named `pass` and then its work,
// as 3DEngine's modules name theirs, so none meets a name of the shader's own. The shader itself
// carries whatever it declares. Every global without a binding of its own belongs to it and is set
// from C# by its name, and an image the camera owns is bound by the name the camera gave it. The
// inputs below are in space 101, which the bridge moves to group one after the compile, so the
// shader's own globals keep group zero to themselves whatever they are.
//
// A pass's entry point is a fragment shader taking `FullscreenOutput`, which Bevy's full-screen
// triangle writes, and returning the pixel. A compute shader works in pixels, and the helpers
// taking an `int2` are for it. A draw over the whole view places its triangle with
// `fullscreenTriangle`.

module bcs_pass;

// The deferred buffers as a pass reads them and a draw writes them, and the depth a draw's shadow
// stage returns, a part of this module kept in a file of its own.
__include bcs_pass_deferred;


/// Time, in the layout Bevy's `Globals` has.
public struct PassGlobals
{
    /// Seconds since startup, wrapping to zero after an hour.
    public float time;
    /// Seconds since the previous frame.
    public float delta_time;
    public uint frame_count;
};

/// The part of Bevy's view uniform a shader usually reads, the same prefix `MeshView` is.
///
/// Matrices are arrays of four columns, which `passTransform` multiplies out.
public struct PassView
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

/// The view as it was on the previous frame, for a shader reprojecting last frame's results.
public struct PassPreviousView
{
    public float4 view_from_world[4];
    public float4 clip_from_world[4];
    public float4 clip_from_view[4];
    public float4 world_from_clip[4];
    public float4 view_from_clip[4];
};

/// The picture so far, and the sampler it is read with, which is linear and clamped at the edges.
[[vk::binding(0, 101)]] public Texture2D passPicture;
[[vk::binding(1, 101)]] public SamplerState passPictureSampler;

/// Time.
///
/// Laid out as C lays it out (`ScalarDataLayout`), which is twelve bytes and what Bevy binds. The
/// usual constant buffer rules round the struct up to sixteen, and a pipeline refuses a shader
/// that reads more of a buffer than its layout promises.
[[vk::binding(2, 101)]] public ConstantBuffer<PassGlobals, ScalarDataLayout> passGlobals;
[[vk::binding(3, 101)]] public ConstantBuffer<PassView> passView;

/// The camera's depth, from its prepass, or zero, which is the far plane, where it draws none.
///
/// Bevy's depth runs backwards, from one at the near plane to zero infinitely far away, so a
/// larger number is nearer. `Shaders.SetPrepass` asks a camera for it.
[[vk::binding(4, 101)]] public DepthTexture2D passDepth;

/// The camera's normals, from its prepass, each packed from minus one to one into zero to one, or
/// white where it draws none.
[[vk::binding(5, 101)]] public Texture2D passNormals;

/// The camera's motion vectors, from its prepass, or zero where it draws none.
///
/// How far each pixel moved since the previous frame, in UV units, so the same surface was at
/// `uv - motion` on the previous frame. Both the camera's movement and the object's are in it.
[[vk::binding(6, 101)]] public Texture2D<float2> passMotion;

/// The previous frame's view, or zeros on a camera that keeps none, which is every 2D camera.
[[vk::binding(7, 101)]] public ConstantBuffer<PassPreviousView> passPreviousView;


/// One cascade of a directional light's shadow, what projects the world onto its layer of the
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
    /// Its color times its brightness, which it lights a surface with before exposure.
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

/// What the bridge adds about the scene to Bevy's own light data, how many point and spot lights
/// `passPointLights` holds, and the camera's environment map.
public struct SceneInfo
{
    public uint point_lights;
    /// How many mip levels the environment's specular cube has, and zero for a camera lit by no
    /// environment map, which is how `passHasEnvironment` tells.
    public uint environment_mips;
    /// What the environment's cubes are multiplied by, in candelas per square meter.
    public float environment_intensity;
    /// Which of the prepass's textures are the camera's own rather than stand-ins, depth one,
    /// normals two and motion four, which is how `passHasDepth` and the two beside it tell.
    public uint own_inputs;
    /// The environment's rotation undone, as a quaternion, which turns a world direction into the
    /// one to sample the cubes by.
    public float4 environment_rotation;
    /// The planet's radius, the atmosphere's top and 1 where the camera is under one of Bevy's
    /// atmospheres, in meters, which is how `passHasAtmosphere` tells.
    public float4 atmosphere;
    /// The turn from the world into the atmosphere's space, meters about the planet's middle with
    /// up along y, a column at a time.
    public float4 world_to_atmosphere[4];
};

/// A point light casts shadows.
public static const uint PointLightShadows = 1u << 0u;
/// A spot light points down negative y, since only its direction's x and z are kept.
public static const uint PointLightSpotYNegative = 1u << 1u;
/// A point light is a spot light.
public static const uint PointLightSpot = 1u << 5u;
/// A directional light casts shadows.
public static const uint DirectionalLightShadows = 1u << 0u;

[[vk::binding(8, 101)]] public ConstantBuffer<Lights> passLights;
[[vk::binding(9, 101)]] public StructuredBuffer<PointLight> passPointLights;
[[vk::binding(10, 101)]] public DepthTexture2DArray passDirectionalShadowMaps;
[[vk::binding(11, 101)]] public DepthTextureCubeArray passPointShadowMaps;
[[vk::binding(12, 101)]] public SamplerComparisonState passShadowSampler;
[[vk::binding(13, 101)]] public ConstantBuffer<SceneInfo> passSceneInfo;
/// The camera's environment map, blurred for what a rough surface or a diffuse bounce sees.
[[vk::binding(14, 101)]] public TextureCube passEnvironmentDiffuseMap;
/// The same, sharp in its first level and rougher in each after it.
[[vk::binding(15, 101)]] public TextureCube passEnvironmentSpecularMap;
/// Bevy's spatio-temporal blue noise, layers of noise whose values are spread evenly both across a
/// layer and from one layer to the next. Gray until it arrives.
[[vk::binding(16, 101)]] public Texture2DArray passBlueNoiseLayers;

/// How much of the sun's light the camera's atmosphere lets through to a height along a way, Bevy's
/// own lookup texture, which `passAtmosphereTransmittance` reads.
[[vk::binding(17, 101)]] public Texture2D passTransmittanceLut;

/// How many directional lights the camera sees.
public uint passDirectionalLightCount()
{
    return passLights.n_directional_lights;
}

/// How many point and spot lights there are.
public uint passPointLightCount()
{
    return passSceneInfo.point_lights;
}

/// How much of a directional light reaches a point in the world, from nothing in full shadow to one
/// in full light, the way Bevy's own materials work it out with its hardware filtering.
///
/// `view_depth` is how far in front of the camera the point is, which picks the cascade.
public float passDirectionalShadow(uint light_index, float3 world, float3 normal, float view_depth)
{
    let light = passLights.directional_lights[light_index];

    if ((light.flags & DirectionalLightShadows) == 0u)
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

    let clip = passTransform(light.cascades[cascade].clip_from_world, offset);
    if (clip.w <= 0.0)
        return 1.0;

    let ndc = clip.xyz / clip.w;
    if (any(ndc.xy < float2(-1.0)) || ndc.z < 0.0 || any(ndc > float3(1.0)))
        return 1.0;

    let uv = ndc.xy * float2(0.5, -0.5) + 0.5;
    let layer = float(light.depth_texture_base_index + cascade);
    return passDirectionalShadowMaps.SampleCmpLevelZero(passShadowSampler, float3(uv, layer), ndc.z);
}

/// Which way a spot light points, rebuilt from the x and z Bevy keeps and the sign of y it flags.
public float3 passSpotDirection(PointLight light)
{
    var direction = float3(light.light_custom_data.x, 0.0, light.light_custom_data.y);
    direction.y = sqrt(max(0.0, 1.0 - direction.x * direction.x - direction.z * direction.z));

    if ((light.flags & PointLightSpotYNegative) != 0u)
        direction.y = -direction.y;

    return direction;
}

/// How much of a spot light reaches a point in the world, from its layer of the directional
/// shadow maps, which is where Bevy keeps a spot light's shadow.
float spot_shadow(uint light_index, PointLight light, float3 world, float3 normal)
{
    let surface_to_light = light.position_radius.xyz - world;
    let forward = -passSpotDirection(light);
    let distance_to_light = dot(forward, surface_to_light);

    let offset = -surface_to_light
        + light.shadow_depth_bias * normalize(surface_to_light)
        + normal * light.shadow_normal_bias * distance_to_light;

    // The light's rotation built from its direction alone, as Bevy builds it on both sides, and
    // applied transposed, since it is a pure rotation.
    let sign = forward.z >= 0.0 ? 1.0 : -1.0;
    let a = -1.0 / (sign + forward.z);
    let b = forward.x * forward.y * a;
    let x_basis = float3(1.0 + sign * forward.x * forward.x * a, sign * b, -sign * forward.x);
    let y_basis = float3(b, sign + forward.y * forward.y * a, -forward.y);
    let projected = float3(dot(offset, x_basis), dot(offset, y_basis), dot(offset, forward));

    if (projected.z >= 0.0)
        return 1.0;

    let ndc = projected.xy / (light.spot_light_tan_angle * -projected.z);
    let uv = ndc * float2(0.5, -0.5) + 0.5;

    if (any(uv < 0.0) || any(uv > 1.0))
        return 1.0;

    let depth_value = light.shadow_map_near_z / -projected.z;
    let layer = float(int(light_index) + passLights.spot_light_shadowmap_offset);
    return passDirectionalShadowMaps.SampleCmpLevelZero(passShadowSampler, float3(uv, layer), depth_value);
}

/// How much of a point or spot light reaches a point in the world, from nothing in full shadow to
/// one in full light, the way Bevy's own materials work it out with its hardware filtering.
public float passPointShadow(uint light_index, float3 world, float3 normal)
{
    let light = passPointLights[light_index];

    if ((light.flags & PointLightShadows) == 0u)
        return 1.0;

    if ((light.flags & PointLightSpot) != 0u)
        return spot_shadow(light_index, light, world, normal);

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

    return passPointShadowMaps.SampleCmpLevelZero(
        passShadowSampler,
        float4(local * float3(1.0, 1.0, -1.0), float(light_index)),
        depth_value);
}

/// How much light a point or spot light sends to a point in the world before shadow, which is its
/// color falling off with distance the way Bevy's does, inside its range, and for a spot light
/// inside its cone.
public float3 passPointLightRadiance(uint light_index, float3 world)
{
    let light = passPointLights[light_index];
    let to_light = light.position_radius.xyz - world;
    let distance_squared = dot(to_light, to_light);
    let factor = distance_squared * light.color_inverse_square_range.w;
    let window = saturate(1.0 - factor * factor);
    var attenuation = window * window / max(distance_squared, 0.0001);

    if ((light.flags & PointLightSpot) != 0u)
    {
        let along = dot(-passSpotDirection(light), normalize(to_light));
        let cone = saturate(along * light.light_custom_data.z + light.light_custom_data.w);
        attenuation *= cone * cone;
    }

    return light.color_inverse_square_range.rgb * attenuation;
}

/// What Bevy's full-screen triangle hands the fragment shader.
public struct FullscreenOutput
{
    public float4 position : SV_Position;
    /// Where the pixel is on the picture, from zero at the top left to one at the bottom right.
    public float2 uv : TEXCOORD0;
};

/// Multiplies a matrix held as four columns by a vector.
public float4 passTransform(float4 columns[4], float4 v)
{
    return columns[0] * v.x + columns[1] * v.y + columns[2] * v.z + columns[3] * v.w;
}

/// The picture so far at a point, from zero to one across it.
public float4 passSample(float2 uv)
{
    return passPicture.Sample(passPictureSampler, uv);
}

/// How large the picture is, in pixels.
///
/// A draw on a camera draws into the picture, so it cannot read it, and a texel stands in for it;
/// there the size is the view's, so `passUv` and `passPixelSize` mean the same in a draw as in a
/// pass.
public uint2 passPictureSize()
{
    uint width;
    uint height;
    passPicture.GetDimensions(width, height);

    if (width <= 1 && height <= 1)
        return uint2(passView.viewport.zw);

    return uint2(width, height);
}

/// How far apart two neighboring pixels are in `uv`, which a blur steps by.
public float2 passPixelSize()
{
    return 1.0 / float2(passPictureSize());
}

/// The middle of a pixel in `uv`, which is where a compute shader working a pixel at a time
/// samples.
public float2 passUv(int2 pixel)
{
    return (float2(pixel) + 0.5) * passPixelSize();
}

/// The depth at a pixel.
public float passLoadDepth(int2 pixel)
{
    return passDepth.Load(int3(pixel, 0));
}

/// The world-space normal at a pixel.
public float3 passLoadNormal(int2 pixel)
{
    return passNormals.Load(int3(pixel, 0)).xyz * 2.0 - 1.0;
}

/// How far a pixel moved since the previous frame, in UV units.
public float2 passLoadMotion(int2 pixel)
{
    return passMotion.Load(int3(pixel, 0));
}

/// The depth at a pixel, given the pixel's `SV_Position`.
public float passDepthAt(float4 position)
{
    return passLoadDepth(int2(position.xy));
}

/// The world-space normal of the surface at a pixel, given its `SV_Position`.
public float3 passNormalAt(float4 position)
{
    return passLoadNormal(int2(position.xy));
}

/// How far the surface at a pixel moved since the previous frame, in UV units, given its
/// `SV_Position`.
public float2 passMotionAt(float4 position)
{
    return passLoadMotion(int2(position.xy));
}

/// Where the surface at `uv` was on the previous frame's picture.
public float2 passPreviousUv(float2 uv, float2 moved)
{
    return uv - moved;
}

/// How far in front of the camera a surface at `depth_value` is, in world units.
///
/// Worked out the way Bevy's own `depth_ndc_to_view_z` does for a perspective camera. Infinitely
/// far where nothing was drawn.
public float passDistanceFromDepth(float depth_value)
{
    let near = passView.clip_from_view[3][2];
    return near / max(depth_value, 1e-7);
}

/// How far in front of the camera the surface at a pixel is, in world units, given its
/// `SV_Position`.
public float passDistanceAt(float4 position)
{
    return passDistanceFromDepth(passDepthAt(position));
}

/// Where in the world the surface at `uv` with `depth_value` is.
public float3 passWorldPosition(float2 uv, float depth_value)
{
    let ndc = float4(uv.x * 2.0 - 1.0, 1.0 - uv.y * 2.0, depth_value, 1.0);
    let world = passTransform(passView.world_from_clip, ndc);
    return world.xyz / world.w;
}

/// How far in front of the camera a point in the world is, along the way it looks, which picks a
/// directional light's shadow cascade.
public float passViewDepth(float3 world)
{
    return -passTransform(passView.view_from_world, float4(world, 1.0)).z;
}

/// Where a point in the world was on the previous frame's picture, in `uv`.
public float2 passPreviousUvOf(float3 world)
{
    let clip = passTransform(passPreviousView.clip_from_world, float4(world, 1.0));
    let ndc = clip.xy / clip.w;
    return float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5);
}

/// The depth Bevy keeps for a point in the world as the view sees it, which a draw that finds its
/// own surface returns through `SV_Depth`, and which is the shadow map's where the view is a
/// light's.
public float passDepthOf(float3 world)
{
    let clip = passTransform(passView.clip_from_world, float4(world, 1.0));
    return clip.z / clip.w;
}

/// Whether the view is orthographic, as a directional light's shadow cascades are.
public bool passOrthographic()
{
    return passView.clip_from_view[3].w == 1.0;
}

/// Where the ray through the pixel at `uv` starts, at the camera for a perspective view, and on
/// the near plane for an orthographic one.
public float3 passRayOrigin(float2 uv)
{
    if (passOrthographic())
        return passWorldPosition(uv, 1.0);

    return passView.world_position;
}

/// Which way the ray through the pixel at `uv` goes from `passRayOrigin`, for a shader that finds a
/// surface by marching along it.
public float3 passRayDirection(float2 uv)
{
    if (passOrthographic())
        return normalize(-passView.world_from_view[2].xyz);

    return normalize(passWorldPosition(uv, 1.0) - passView.world_position);
}

/// A corner of one triangle covering the whole view, for a draw over every pixel of it with three
/// vertices, the triangle Bevy draws a pass with.
public FullscreenOutput fullscreenTriangle(uint vertex)
{
    FullscreenOutput output;
    output.uv = float2(float(vertex >> 1), float(vertex & 1)) * 2.0;
    output.position = float4(output.uv * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
    return output;
}

/// Whether the camera is lit by an environment map, which `SetSkyLighting`, `SetImageLighting` and
/// `SetEnvironmentMap` all give it.
public bool passHasEnvironment()
{
    return passSceneInfo.environment_mips > 0;
}

/// Whether the depth a shader reads is the camera's own. A camera asked for no depth, or drawing
/// several samples a pixel (`PostSettings.Msaa` above one), is read zeros instead, which is the
/// depth of nothing in front of it, so a shader that treats an empty pixel differently tells the
/// two apart here.
public bool passHasDepth()
{
    return (passSceneInfo.own_inputs & 1u) != 0u;
}

/// Whether the normals a shader reads are the camera's own rather than a stand-in.
public bool passHasNormals()
{
    return (passSceneInfo.own_inputs & 2u) != 0u;
}

/// Whether the motion a shader reads is the camera's own rather than none at all.
public bool passHasMotion()
{
    return (passSceneInfo.own_inputs & 4u) != 0u;
}

/// Whether the camera is under one of Bevy's atmospheres, which `Render.SetAtmosphere` gives it.
public bool passHasAtmosphere()
{
    return passSceneInfo.atmosphere.z > 0.5;
}

/// The share of light the camera's atmosphere lets through from a point in the world to the top of
/// the air along a direction, as Bevy's lighting dims a directional light by it, red at a low sun,
/// and none where the planet stands in the way. One everywhere without an atmosphere.
public float3 passAtmosphereTransmittance(float3 world, float3 direction)
{
    if (!passHasAtmosphere())
        return 1.0;
    float inner = passSceneInfo.atmosphere.x;
    float outer = passSceneInfo.atmosphere.y;
    float3 at = passTransform(passSceneInfo.world_to_atmosphere, float4(world, 1.0)).xyz;
    float3 toward = normalize(passTransform(passSceneInfo.world_to_atmosphere, float4(direction, 0.0)).xyz);

    // Held above the ground as Bevy holds the camera, so a point under the planet's surface reads
    // the light at the surface.
    float r = clamp(length(at), inner + 1.0, outer);
    float mu = dot(at / max(length(at), 1e-3), toward);
    if (mu < 0.0 && r * r * (mu * mu - 1.0) + inner * inner >= 0.0)
        return 0.0;

    // Bevy's mapping of a height and a way into its lookup texture, Bruneton's.
    float horizon = sqrt(outer * outer - inner * inner);
    float rho = sqrt(max(r * r - inner * inner, 0.0));
    float top = -r * mu + sqrt(max(r * r * (mu * mu - 1.0) + outer * outer, 0.0));
    float nearest = outer - r;
    float farthest = rho + horizon;
    float2 uv = float2((top - nearest) / max(farthest - nearest, 1e-3), rho / horizon);
    return passTransmittanceLut.SampleLevel(passPictureSampler, uv, 0.0).rgb;
}

/// A world direction turned the way the environment's cubes are sampled, rotated as the
/// environment was, and with z negated, since cube maps are left-handed and Bevy's world is not.
float3 environment_direction(float3 direction)
{
    let q = passSceneInfo.environment_rotation;
    let t = 2.0 * cross(q.xyz, direction);
    var turned = direction + q.w * t + cross(q.xyz, t);
    turned.z = -turned.z;
    return turned;
}

/// The light the environment sends a surface facing `normal`, from all of the half of the sky in
/// front of it, as a diffuse bounce off the sky gathers. Black without an environment.
public float3 passEnvironmentDiffuse(float3 normal)
{
    if (!passHasEnvironment())
        return float3(0.0);

    let sample = passEnvironmentDiffuseMap.SampleLevel(passPictureSampler, environment_direction(normal), 0.0);
    return sample.rgb * passSceneInfo.environment_intensity;
}

/// The light the environment sends along `direction`, blurred as much as a surface of
/// `perceptual_roughness` blurs it, as a ray that left the scene sees. Zero roughness is the sky as
/// it is. Black without an environment.
public float3 passEnvironmentSpecular(float3 direction, float perceptual_roughness)
{
    if (!passHasEnvironment())
        return float3(0.0);

    let level = perceptual_roughness * float(passSceneInfo.environment_mips - 1);
    let sample = passEnvironmentSpecularMap.SampleLevel(passPictureSampler, environment_direction(direction), level);
    return sample.rgb * passSceneInfo.environment_intensity;
}

/// Blue noise at a pixel for this frame, four numbers from zero to one.
///
/// What a stochastic technique picks its ray directions or sample offsets with instead of white
/// noise. Neighboring pixels get values far apart, so the error a few samples leave looks like fine
/// grain a blur removes rather than blotches, and each frame takes the next layer, so an
/// accumulation over frames sees values spread evenly in time as well. The pattern repeats every
/// hundred and twenty eight pixels and every thirty two frames.
public float4 passBlueNoise(int2 pixel)
{
    uint width, height, layers;
    passBlueNoiseLayers.GetDimensions(width, height, layers);

    let at = int2(uint2(pixel) % uint2(width, height));
    let layer = int(passGlobals.frame_count % layers);
    return passBlueNoiseLayers.Load(int4(at, layer, 0));
}
