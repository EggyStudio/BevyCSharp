// Global illumination's light on the camera's picture, drawn over it once its opaque surfaces are
// lit and blended by adding, a surface's diffuse color times the indirect light reaching it in place
// of the share of the light from all around Bevy's lighting gave it.
//
// Bevy's lighting takes no indirect light from outside, so the light its ambient term gave a surface
// is worked out again from the camera's G-buffer as Bevy works it out and taken back, and the light
// global illumination's world probes hold is added in its place. Where a cascade of probes holds the
// surface, that is the light reaching it read between the probes around it (bouncedAt), and across
// the last cascade's outer band and past it, the light from beyond the probes, a sky over a ground,
// as 3DEngine's model pass takes it (distantAround), each directional light adding its share of the
// ground's light through its shadow map at the ground the surface looks at. The picture before
// tonemapping holds light as floats, so adding less than nothing takes it away. The probes' light is
// in the picture's units, the camera's exposure applied, and Bevy's ambient term and its diffuse
// light from an environment map are taken back times the exposure as Bevy applies it. Bevy's
// screen-space ambient occlusion and an environment map's specular light are Bevy's and left as they
// are, so where a camera has occlusion the light taken back differs from the light Bevy gave by as
// much.
import bcs_pass;
import bcs_field;
import bcs_gi;
import bcs_glow;

Texture2D<uint4> gbuffer;
Texture3D<float> scene_field;
SamplerState scene_field_sampler;
StructuredBuffer<FieldInfo> scene_field_info;

// Each probe's six faces and its reach, every cascade, as gi_ambient.slang left them this frame.
Texture3D<float4> probe_cubes;
Texture3D<float> probe_reach;

// The lights the probes are lit by, for the light from all around, and the small emissive surfaces
// carried as lights of their own, as many as the lights' fourth count says.
StructuredBuffer<GiLights> gi_lights;
StructuredBuffer<GlowLight> glow_lights;

// x: the probes along each side of a cascade. y: their spacing in cells. z: how many cascades of
// probes. w: unused.
uniform float4 probes;

[shader("vertex")]
FullscreenOutput vertex(uint index : SV_VertexID)
{
    // At the near plane, which every surface's depth passes against, where Bevy's own triangle over
    // the view lies at the far plane and would be drawn over the sky alone.
    var corner = fullscreenTriangle(index);
    corner.position.z = 1.0;
    return corner;
}

// The light from all around a surface facing n that the probes leave to what lies past them, as a
// sky over a ground, the sky's light from above on the share of the sphere above the surface's
// horizon, (1 + n.y) / 2 of it, and below its horizon the light the ground sends back, the ground's
// color taken as the surface's own times the sky's light on it, as 3DEngine's model pass has it.
float3 distantAround(float3 sky, float3 n, float3 ground)
{
    float above = saturate(n.y * 0.5 + 0.5);
    return sky * (above + ground * (1.0 - above));
}

// The polynomial Bevy takes F_AB from where it reads no table, which its ambient term is worked out
// with, at a perceptual roughness of one for the diffuse share.
float2 F_AB(float perceptual_roughness, float NdotV)
{
    float4 c0 = float4(-1.0, -0.0275, -0.572, 0.022);
    float4 c1 = float4(1.0, 0.0425, 1.04, -0.04);
    float4 r = perceptual_roughness * c0 + c1;
    float a004 = min(r.x * r.x, exp2(-9.28 * NdotV)) * r.x + r.y;
    return float2(-1.04, 1.04) * a004 + r.zw;
}

// The diffuse share of the light an environment map gave a surface, as Bevy's environment_map_light
// works it out, the dielectric's and the metal's multiple scattering mixed by the metalness and the
// diffuse color times what the dielectric's single and multiple scattering leave, times the map's
// irradiance facing n, or none where the camera has no environment map.
float3 environmentGiven(PassSurface surface, float3 n, float NdotV, float3 diffuse)
{
    if (!passHasEnvironment())
        return float3(0.0);
    float2 F_ab = F_AB(surface.perceptual_roughness, NdotV);
    float Ems = 1.0 - (F_ab.x + F_ab.y);
    float3 F0[2] = { float3(0.16 * surface.reflectance * surface.reflectance), surface.base_color };
    float3 scattered[2];
    float3 left = float3(0.0);
    [unroll]
    for (int i = 0; i < 2; i++)
    {
        float3 FssEss = F0[i] * F_ab.x + F_ab.y;
        float3 Favg = F0[i] + (1.0 - F0[i]) / 21.0;
        scattered[i] = FssEss * Favg / (1.0 - Ems * Favg) * Ems;
        if (i == 0)
            left = 1.0 - (FssEss + scattered[i]);
    }
    return (lerp(scattered[0], scattered[1], surface.metallic) + diffuse * left) * passEnvironmentDiffuse(n);
}

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    FieldInfo info = scene_field_info[0];
    int2 pixel = int2(input.position.xy);
    float depth = passLoadDepth(pixel);
    uint4 gpixel = gbuffer.Load(int3(pixel, 0));
    if (depth <= 0.0 || !fieldOn(info) || all(gpixel == uint4(0u)))
        discard;

    PassSurface surface = passSurfaceOf(gpixel);
    if (surface.unlit)
        discard;

    float3 world = passWorldPosition(input.uv, depth);
    float3 n = surface.normal;
    float3 V = normalize(passView.world_position - world);
    float NdotV = max(dot(n, V), 0.0001);
    float3 diffuse = surface.base_color * (1.0 - surface.metallic);
    float occlusion = surface.diffuse_occlusion;
    GiLights lights = gi_lights[0];

    // How much of the light from all around the probes give, all of it inside the last cascade and
    // none past its outer band, and the light they hold here, irradiance over pi.
    float held = bouncedHold(info, world, n, probes.y, probes.z);
    float3 bounced = held > 0.0 ? bouncedAt(probe_cubes, probe_reach, info, world, n, probes.x, probes.y, probes.z) / Pi : float3(0.0);

    // Past the probes, the sky over a ground and each directional light's share of the ground's
    // light, read through its shadow map where the ground the surface looks at lies, a cell of the
    // field's coarsest cascade along its normal, so a face turned from the sun, which lies behind
    // its own mesh as the sun sees it, takes the sunlit ground before it.
    float3 distant = float3(0.0);
    if (held < 1.0)
    {
        // The environment map's light from above with the ambient light, as 3DEngine's sky over a
        // ground reads its environment's irradiance facing up.
        distant = distantAround(lights.sky.rgb + passEnvironmentDiffuse(float3(0.0, 1.0, 0.0)) * passView.exposure, n, diffuse);
        float below = saturate(0.5 - n.y * 0.5);
        if (below > 0.0)
        {
            int c = fieldCoarsestBuilt(info, int(info.counts.x) - 1);
            float3 ground = world + n * (c < 0 ? 0.0 : info.cascades[c].origin_and_cell.w);
            for (uint i = 0u; i < min(passLights.n_directional_lights, 10u); i++)
            {
                DirectionalLight light = passLights.directional_lights[i];
                float floor = saturate(light.direction_to_light.y);
                if (floor <= 0.0)
                    continue;
                float reached = passDirectionalShadow(i, ground, float3(0.0, 1.0, 0.0), passViewDepth(ground));
                distant += diffuse * light.color.rgb / Pi * passView.exposure * floor * reached * below;
            }
        }
    }

    float3 added = diffuse * (bounced * held + distant * (1.0 - held)) * occlusion;

    // The glow lights' light straight to the surface, the four brightest within reach shadowed by a
    // march through the field to the middle of each, the probes' rays leaving a carried emitter's
    // light to them. 3DEngine's pixels where no screen probe stands near march to the halves of each
    // face a light shows, which in the bounce rooms read as the middle does, the Cornell box's floor
    // 13.4% over its reference against 13.2%, and took the pass from 1.3 ms to 2.0 to 3.5 at 1280 by
    // 720 with one light over the whole view of a Cornell box on the development machine's GPU,
    // where without the glow lights it takes 0.19.
    if (lights.counts.w > 0.5)
        added += diffuse * glowLightsAt(scene_field, scene_field_sampler, info, glow_lights, uint(lights.counts.w), world, n, 4, false, float3(0.0)) / Pi;

    // The diffuse share of the light from all around Bevy's lighting gave the surface, its ambient
    // term's and its environment map's, taken back.
    float2 F_ab = F_AB(1.0, NdotV);
    float3 given = ((diffuse * F_ab.x + F_ab.y) * passLights.ambient_color.rgb + environmentGiven(surface, n, NdotV, diffuse))
        * occlusion * passView.exposure;

    return float4(added - given, 0.0);
}
