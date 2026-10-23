// The sun's contact shadows through the scene's distance field, drawn over the camera's lit
// picture once its opaque surfaces are lit and blended by adding, as the light each directional
// light sent a pixel's surface taken away where the field says it is closed off from that light.
//
// Bevy's lighting takes no share of a directional light from outside, where 3DEngine's model pass
// multiplies the sun's light by the share its occlusion pass traced, so the light Bevy added is
// worked out again here from the camera's G-buffer, as Bevy's own `directional_light` works it out
// (Burley's diffuse, the GGX specular with its multiple scattering, Schlick's Fresnel), and taken
// back times what the field closes off. The picture before tonemapping holds light as floats, so
// adding less than nothing takes it away. Bevy's atmosphere, its clearcoat and anisotropy, a light
// texture and the light's own contact shadows are not worked out again, so where a surface has
// them the light taken away differs from the light Bevy added by as much.
//
// Past the shadow map's reach alone by default, so a shadow the map draws keeps its hard edge and
// the ground beyond the map is still shadowed, or everywhere the field reaches, the map's shadow
// and the field's both darkening a surface, as `Render.SetSceneFieldShadows` chooses.
import bcs_pass;
import bcs_field;

Texture2D<uint4> gbuffer;
Texture3D<float> scene_field;
SamplerState scene_field_sampler;
StructuredBuffer<bcs_field::Info> scene_field_info;

// x: 1 where the field darkens a directional light everywhere it reaches, 0 where past its shadow
// map's reach alone. y: how far toward the light a surface's ray is traced, in world units.
uniform float4 shadows;

static const float PI = 3.141592653589793;

[shader("vertex")]
bcs_pass::Input vertex(uint index : SV_VertexID)
{
    // At the near plane, which every surface's depth passes against, where Bevy's own triangle
    // over the view lies at the far plane and would be drawn over the sky alone.
    var corner = bcs_pass::full_screen(index);
    corner.position.z = 1.0;
    return corner;
}

// The share of a directional light reaching p on a surface facing n, traced toward it through the
// field from a cell and a half out, darkened by how near a surface the ray passes as a share of how
// far along it is, as Quilez's soft shadows have it, so a shadow is soft where its caster is far and
// hard where it meets what it falls on. Taken from 3DEngine's ao.slang.
float reaching(bcs_field::Info info, float3 p, float3 n, float3 l, float reach)
{
    float cell = bcs_field::cell(info, p);
    if (reach <= 0.0 || cell <= 0.0)
        return 1.0;
    float3 start = p + n * (cell * 1.5);
    float hold = bcs_field::hold(info, p);
    float shown = 1.0;
    float t = cell;
    for (int i = 0; i < 40 && t < reach; i++)
    {
        float3 q = start + l * t;
        float d = bcs_field::distance(scene_field, scene_field_sampler, info, q);
        if (d > 1e5)
            break;
        shown = min(shown, 8.0 * d / t);
        if (shown < 0.01)
            return 1.0 - hold;
        t += max(d, 0.5 * bcs_field::cell(info, q));
    }
    // Faded across the field's last band (bcs_field::hold).
    return lerp(1.0, saturate(shown), hold);
}

// How much of a point a directional light's shadow map holds, one within its cascades and falling
// to none across the last one's overlap, so the field takes over where the map's shadows fade out.
float mapped(bcs_pass::DirectionalLight light, float view_depth)
{
    if ((light.flags & bcs_pass::DIRECTIONAL_LIGHT_SHADOWS) == 0u || light.num_cascades == 0u)
        return 0.0;
    float far = light.cascades[light.num_cascades - 1u].far_bound;
    float overlap = max(far * light.cascades_overlap_proportion, 1e-3);
    return saturate((far - view_depth) / overlap);
}

// Bevy's own pieces of its directional light, as its pbr_lighting.wesl writes them.
float D_GGX(float roughness, float NdotH)
{
    float oneMinusNdotHSquared = 1.0 - NdotH * NdotH;
    float a = NdotH * roughness;
    float k = roughness / (oneMinusNdotHSquared + a * a);
    return k * k * (1.0 / PI);
}

float V_SmithGGXCorrelated(float roughness, float NdotV, float NdotL)
{
    float a2 = roughness * roughness;
    float lambdaV = NdotL * sqrt((NdotV - a2 * NdotV) * NdotV + a2);
    float lambdaL = NdotV * sqrt((NdotL - a2 * NdotL) * NdotL + a2);
    return 0.5 / (lambdaV + lambdaL);
}

float F_Schlick(float f0, float f90, float VdotH)
{
    return f0 + (f90 - f0) * pow(1.0 - VdotH, 5.0);
}

float3 fresnel(float3 f0, float LdotH)
{
    float f90 = saturate(dot(f0, float3(50.0 * 0.33)));
    return f0 + (f90 - f0) * pow(1.0 - LdotH, 5.0);
}

// The polynomial Bevy uses where it reads no table of these, which differs from the table only in
// the energy a rough metal's multiple scattering adds back.
float2 F_AB(float perceptual_roughness, float NdotV)
{
    float4 c0 = float4(-1.0, -0.0275, -0.572, 0.022);
    float4 c1 = float4(1.0, 0.0425, 1.04, -0.04);
    float4 r = perceptual_roughness * c0 + c1;
    float a004 = min(r.x * r.x, exp2(-9.28 * NdotV)) * r.x + r.y;
    return max(float2(-1.04, 1.04) * a004 + r.zw, float2(0.00005));
}

// The light a directional light sends toward the eye from a surface, before its shadow and the
// camera's exposure, as Bevy's `directional_light` works it out for a standard material.
float3 sent(bcs_pass::Surface surface, float3 V, float3 L, float3 color)
{
    float3 N = surface.normal;
    float perceptual = clamp(surface.perceptual_roughness, 0.089, 1.0);
    float roughness = perceptual * perceptual;
    float NdotV = max(dot(N, V), 0.0001);
    float3 H = normalize(L + V);
    float NdotL = saturate(dot(N, L));
    float NdotH = saturate(dot(N, H));
    float LdotH = saturate(dot(L, H));

    float3 diffuse_color = surface.base_color * (1.0 - surface.metallic);
    float f90 = 0.5 + 2.0 * roughness * LdotH * LdotH;
    float3 diffuse = diffuse_color * F_Schlick(1.0, f90, NdotL) * F_Schlick(1.0, f90, NdotV) * (1.0 / PI);

    float3 F0 = lerp(float3(0.16 * surface.reflectance * surface.reflectance), surface.base_color, surface.metallic);
    float2 F_ab = F_AB(perceptual, NdotV);
    float3 specular = D_GGX(roughness, NdotH) * V_SmithGGXCorrelated(roughness, NdotV, NdotL) * fresnel(F0, LdotH);
    specular *= 1.0 + F0 * (1.0 / (F_ab.x + F_ab.y) - 1.0);

    return (diffuse + specular) * NdotL * color;
}

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    bcs_field::Info info = scene_field_info[0];
    int2 pixel = int2(input.position.xy);
    float depth = bcs_pass::load_depth(pixel);
    if (depth <= 0.0 || !bcs_field::on(info))
        discard;

    bcs_pass::Surface surface = bcs_pass::surface_of(gbuffer.Load(int3(pixel, 0)));
    if (surface.unlit || !surface.shadow_receiver)
        discard;

    float3 world = bcs_pass::world_position(input.uv, depth);
    float3 V = normalize(bcs_pass::view.world_position - world);
    float view_depth = bcs_pass::view_depth(world);
    float3 taken = float3(0.0);

    for (uint i = 0u; i < min(bcs_pass::lights.n_directional_lights, 10u); i++)
    {
        bcs_pass::DirectionalLight light = bcs_pass::lights.directional_lights[i];
        float3 L = light.direction_to_light;
        if (dot(surface.normal, L) <= 0.0)
            continue;

        // The share the field darkens, all of it everywhere, or as little as the map holds the point.
        float share = shadows.x > 0.5 ? 1.0 : 1.0 - mapped(light, view_depth);
        if (share <= 0.0)
            continue;

        float shown = reaching(info, world, surface.normal, L, shadows.y);
        if (shown >= 1.0)
            continue;

        // The light Bevy added, its shadow map's share of it included, the light there is to take back.
        float map = bcs_pass::directional_shadow(i, world, surface.normal, view_depth);
        taken += sent(surface, V, L, light.color.rgb) * map * share * (1.0 - shown);
    }

    if (all(taken <= 0.0))
        discard;
    return float4(-taken * bcs_pass::view.exposure, 0.0);
}
