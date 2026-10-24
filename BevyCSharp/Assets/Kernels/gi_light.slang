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
// in the picture's units, the camera's exposure applied, and Bevy's ambient term is taken back times
// the exposure as Bevy applies it. Bevy's screen-space ambient occlusion and an environment map's
// light are Bevy's and left as they are, so where a camera has them the light taken back differs
// from the light Bevy gave by as much.
import bcs_pass;
import bcs_field;
import bcs_gi;

Texture2D<uint4> gbuffer;
Texture3D<float> scene_field;
SamplerState scene_field_sampler;
StructuredBuffer<FieldInfo> scene_field_info;

// Each probe's six faces and its reach, every cascade, as gi_ambient.slang left them this frame.
Texture3D<float4> probe_cubes;
Texture3D<float> probe_reach;

// The lights the probes are lit by, for the light from all around.
StructuredBuffer<GiLights> gi_lights;

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
        distant = distantAround(lights.sky.rgb, n, diffuse);
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

    // The diffuse share of the light from all around Bevy's lighting gave the surface, taken back.
    float2 F_ab = F_AB(1.0, NdotV);
    float3 given = (diffuse * F_ab.x + F_ab.y) * passLights.ambient_color.rgb * occlusion * passView.exposure;

    return float4(added - given, 0.0);
}
