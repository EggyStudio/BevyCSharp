// Bevy's light probes the camera sees, as its lighting reads them, part of the `bcs_pass` module,
// which includes this file. A shader taking back the light Bevy's lighting gave a surface reads
// them as Bevy does, in the order of bevy_pbr 0.20's pbr_functions with the bridge's mend of its
// fallback (mended.rs), through three things bevy_pbr keeps public, `LightProbesBuffer` at the
// view's `ViewLightProbesUniformOffset` and `RenderViewLightProbes`' `binding_index_to_textures`,
// which the bridge binds among a pass's inputs (views/light_probes.rs). A Bevy past 0.20 is read
// against this again, since it mirrors Bevy's code.

implementing bcs_pass;

/// One of Bevy's light probes as its lighting reads it, a reflection probe or an irradiance volume,
/// in the layout of bevy_pbr 0.20's `LightProbe`.
public struct PassLightProbe
{
    /// The turn from the world into the probe's space, where its box is the unit cube about the
    /// origin, its three rows, which Bevy keeps as the columns of the matrix transposed.
    public float4 light_from_world[3];
    /// The share of the box's size across which the probe's light fades out toward each side.
    public float3 falloff;
    public float bounding_sphere_radius;
    /// Half the box a reflection is taken as meeting, in the probe's space, where it is corrected
    /// for parallax.
    public float3 parallax_correction_bounds;
    public float intensity;
    public float3 world_position;
    /// Which of the images bound beside it the probe's is, Bevy's binding index.
    public int cubemap_index;
    /// 2 where a reflection probe corrects its reflection for parallax.
    public uint flags;
};

/// Bevy's light probes the camera sees, the nearest of each kind first, in the layout of bevy_pbr
/// 0.20's `LightProbesUniform`, which its lighting reads them by. The counts are zero for a camera
/// with none.
public struct PassLightProbes
{
    public PassLightProbe reflection_probes[8];
    public PassLightProbe irradiance_volumes[8];
    public int reflection_probe_count;
    public int irradiance_volume_count;
    public int view_cubemap_index;
    public uint smallest_specular_mip_level_for_view;
    public float4 view_rotation;
    public float intensity_for_view;
    public uint view_environment_map_affects_lightmapped_mesh_diffuse;
};

[[vk::binding(18, 101)]] public ConstantBuffer<PassLightProbes> passLightProbes;

// Bevy's reflection probes' diffuse cubes and its irradiance volumes' images, the first four of
// each by the probes' `cubemap_index`, as Bevy binds them for its own lighting, each a texture of
// its own, since a group binding an array of images may hold no uniform buffer. Black where the
// camera has none.
[[vk::binding(19, 101)]] public TextureCube passLightProbeDiffuseMap0;
[[vk::binding(20, 101)]] public TextureCube passLightProbeDiffuseMap1;
[[vk::binding(21, 101)]] public TextureCube passLightProbeDiffuseMap2;
[[vk::binding(22, 101)]] public TextureCube passLightProbeDiffuseMap3;
[[vk::binding(23, 101)]] public Texture3D passIrradianceVolume0;
[[vk::binding(24, 101)]] public Texture3D passIrradianceVolume1;
[[vk::binding(25, 101)]] public Texture3D passIrradianceVolume2;
[[vk::binding(26, 101)]] public Texture3D passIrradianceVolume3;

/// Whether Bevy's lighting reads its reflection probes and more than one irradiance volume, which
/// it does where the device binds arrays of images. Where it does not, it reads the camera's
/// environment map and the first volume alone, for every volume holding a point.
public bool passLightProbeArrays()
{
    return passSceneInfo.light_probes.x != 0u;
}

/// A point or a way, w 1 or 0, in a light probe's space, where its box is the unit cube about the
/// origin, as Bevy's light_probe_iterator_next turns it.
public float3 passLightProbeSpace(PassLightProbe probe, float4 p)
{
    return float3(dot(probe.light_from_world[0], p), dot(probe.light_from_world[1], p), dot(probe.light_from_world[2], p));
}

/// How much of its light a light probe gives a point, all of it inside, falling to none across the
/// share of its box its falloff sets at each side, and none outside, as Bevy weighs it.
public float passLightProbeWeight(PassLightProbe probe, float3 world)
{
    float3 at = passLightProbeSpace(probe, float4(world, 1.0));
    float3 each = saturate((1.0 - 2.0 * abs(at)) / (2.0 * max(probe.falloff, float3(0.0001))));
    return min(min(each.x, each.y), each.z);
}

/// The way a reflection probe's cube is read for the light arriving along `way` at `world`, as
/// Bevy's compute_cubemap_sample_dir turns it, through the probe's space, where it meets the box
/// parallax is corrected by where the probe asks for that, and with z negated for the cube.
public float3 passReflectionProbeWay(PassLightProbe probe, float3 world, float3 way)
{
    float3 along = passLightProbeSpace(probe, float4(way, 0.0));
    float3 read = along;
    if ((probe.flags & 2u) != 0u)
    {
        float3 from = passLightProbeSpace(probe, float4(world, 1.0));
        float3 near = (-probe.parallax_correction_bounds - from) / along;
        float3 far = (probe.parallax_correction_bounds - from) / along;
        near = select(near >= 0.0, near, float3(3.40282347e+38));
        far = select(far >= 0.0, far, float3(3.40282347e+38));
        float3 first = min(near, far);
        read = from + along * min(min(first.x, first.y), first.z);
    }
    read.z = -read.z;
    return read;
}

// A reflection probe's diffuse cube, by its binding index, read along `way`, and nothing past the
// four the pass's inputs hold.
float3 reflectionProbeDiffuse(int index, float3 way)
{
    switch (index)
    {
    case 0: return passLightProbeDiffuseMap0.SampleLevel(passPictureSampler, way, 0.0).rgb;
    case 1: return passLightProbeDiffuseMap1.SampleLevel(passPictureSampler, way, 0.0).rgb;
    case 2: return passLightProbeDiffuseMap2.SampleLevel(passPictureSampler, way, 0.0).rgb;
    case 3: return passLightProbeDiffuseMap3.SampleLevel(passPictureSampler, way, 0.0).rgb;
    default: return float3(0.0);
    }
}

/// The diffuse light Bevy's environment maps give a surface at `world` facing `n`, its reflection
/// probes' diffuse cubes weighed as each holds the point and the camera's map in what share they
/// leave, all over the weight they come to, as environment_map_light blends them, and in
/// `viewShare` the share of the blend the reflection probes leave, the camera's map's where it has
/// one, and all of it where nothing is blended, which a pass giving its own reflection gives it in.
/// Where the device binds no arrays Bevy reads the camera's map alone. A probe whose cube lies past
/// the four the inputs hold keeps its share and lends no light, which is left as Bevy gave it.
public float3 passEnvironmentAround(float3 world, float3 n, out float viewShare)
{
    float3 sum = float3(0.0);
    float total = 0.0;
    for (int i = 0; passLightProbeArrays() && i < min(passLightProbes.reflection_probe_count, 8); i++)
    {
        PassLightProbe probe = passLightProbes.reflection_probes[i];
        float weight = passLightProbeWeight(probe, world);
        if (weight <= 0.0)
            continue;
        sum += reflectionProbeDiffuse(probe.cubemap_index, passReflectionProbeWay(probe, world, n)) * probe.intensity * weight;
        total += weight;
    }
    float fill = passHasEnvironment() && total < 0.9999 ? 1.0 - total : 0.0;
    sum += passEnvironmentDiffuse(n) * fill;
    total += fill;
    viewShare = total > 0.0 ? fill / total : 1.0;
    return total > 0.0 ? sum / total : float3(0.0);
}

// One of Bevy's irradiance volumes' images by its binding index, its size and a point of it, none
// past the four the pass's inputs hold.
uint3 volumeSize(int index)
{
    uint3 size = uint3(0u);
    switch (index)
    {
    case 0: passIrradianceVolume0.GetDimensions(size.x, size.y, size.z); break;
    case 1: passIrradianceVolume1.GetDimensions(size.x, size.y, size.z); break;
    case 2: passIrradianceVolume2.GetDimensions(size.x, size.y, size.z); break;
    case 3: passIrradianceVolume3.GetDimensions(size.x, size.y, size.z); break;
    default: break;
    }
    return size;
}

float3 volumeAt(int index, float3 uvw)
{
    switch (index)
    {
    case 0: return passIrradianceVolume0.SampleLevel(passPictureSampler, uvw, 0.0).rgb;
    case 1: return passIrradianceVolume1.SampleLevel(passPictureSampler, uvw, 0.0).rgb;
    case 2: return passIrradianceVolume2.SampleLevel(passPictureSampler, uvw, 0.0).rgb;
    case 3: return passIrradianceVolume3.SampleLevel(passPictureSampler, uvw, 0.0).rgb;
    default: return float3(0.0);
    }
}

/// The diffuse light Bevy's irradiance volumes give a surface at `world` facing `n`, Valve's
/// ambient cube of each volume holding the point read as irradiance_volume_light reads it, its
/// three slices one an axis and each slice's lower half the negative side, times the volume's
/// intensity, all of them weighed as each holds the point over the weight they come to, and in
/// `held` whether any holds it, which is where the bridge's mend has Bevy take this light in place
/// of the environment maps'. Where the device binds no arrays Bevy reads its first volume's image
/// for each.
public float3 passIrradianceVolumesAround(float3 world, float3 n, out bool held)
{
    float3 sum = float3(0.0);
    float total = 0.0;
    float3 nn = n * n;
    float3 lower = select(n < 0.0, float3(0.5), float3(0.0));
    for (int i = 0; i < min(passLightProbes.irradiance_volume_count, 8); i++)
    {
        PassLightProbe volume = passLightProbes.irradiance_volumes[i];
        float weight = passLightProbeWeight(volume, world);
        if (weight <= 0.0)
            continue;
        int image = passLightProbeArrays() ? volume.cubemap_index : 0;
        float3 atlas = max(float3(volumeSize(image)), float3(1.0));
        float3 cells = atlas / float3(1.0, 2.0, 3.0);
        float3 at = passLightProbeSpace(volume, float4(world, 1.0));
        float3 uvw = clamp((at + 0.5) * cells, float3(0.5), cells - 0.5) / atlas;
        float3 x = volumeAt(image, uvw + float3(0.0, lower.x, 0.0));
        float3 y = volumeAt(image, uvw + float3(0.0, lower.y, 1.0 / 3.0));
        float3 z = volumeAt(image, uvw + float3(0.0, lower.z, 2.0 / 3.0));
        sum += (x * nn.x + y * nn.y + z * nn.z) * volume.intensity * weight;
        total += weight;
    }
    held = total > 0.0;
    return held ? sum / total : float3(0.0);
}
