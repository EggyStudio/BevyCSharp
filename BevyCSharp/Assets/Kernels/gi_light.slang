// Global illumination's light on the camera's picture, drawn over it once its opaque surfaces are
// lit and blended by adding, a surface's diffuse color times the indirect light reaching it in place
// of the share of the light from all around Bevy's lighting gave it.
//
// Bevy's lighting takes no indirect light from outside, so the light its ambient term gave a
// surface is worked out again from the camera's G-buffer as Bevy works it out and taken back, and
// the light global illumination's world probes hold is added in its place. Where a cascade of
// probes holds the surface, that is the light reaching it read between the probes around it
// (bouncedAt), and across the last cascade's outer band and past it, the light from beyond the
// probes, a sky over a ground, as 3DEngine's model pass takes it (distantAround), each directional
// light adding its share of the ground's light through its shadow map at the ground the surface
// looks at. The picture before tonemapping holds light as floats, so adding less than nothing takes
// it away. The probes' light is in the picture's units, the camera's exposure applied, and Bevy's
// ambient term and its diffuse light from an environment map are taken back times the exposure as
// Bevy applies it. Bevy's reflection of the light from all around is taken back and given again,
// traced below a roughness of a half and times the share of the sky the probes see above it. Bevy's
// screen-space ambient occlusion is Bevy's and left as it is, so where a camera has it the light
// taken back differs from the light Bevy gave by as much.
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

// The share of the sky each face of each probe saw, every cascade (gi_ambient.slang), which says how
// much of the sky a glossy surface reflects.
Texture3D<float> probe_skies;

// The field's colors, the way each cell's nearest face looks and its light, which a reflection that
// leaves the picture is shaded by (shadeHit).
Texture3D<float4> scene_albedo;
Texture3D<uint> scene_faces;
Texture3D<float4> scene_glow;

// The frame before's lit picture before tonemapping, which a reflection that meets a surface on the
// picture takes its light from, a camera image the engine keeps with its history.
Texture2D<float4> gi_picture_previous;

// x: the steps a reflection is traced through the depth in. y: how far, in world units. z: 1 where
// the frame before's picture is kept. w: unused.
uniform float4 reflection;

// The roughness from which a surface traces no reflection, the probes and the environment reflecting
// alone, its traced reflection fading out from half of it, as 3DEngine's GlossyRoughness.
static const float GlossyRoughness = 0.5;

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

// The share of the sky a surface at p facing n sees along r, the share its probes' rays reached the
// sky by, blended between the eight probes around it as bouncedAt blends their light, the three
// faces r looks toward weighed by the square of r along each, or the whole sky where no cascade of
// probes holds p, as 3DEngine's skyAt.
float skyAt(FieldInfo field, float3 p, float3 n, float3 r)
{
    FieldInfo info = probesOf(field, int(probes.z));
    int c = fieldCascade(info, p, 1.0);
    if (c < 0 || c >= int(probes.z))
        return 1.0;
    float4 oc = info.cascades[c].origin_and_cell;
    float3 q = clamp((p - oc.xyz) / (oc.w * probes.y) - 0.5, 0.0, probes.x - 1.0);
    int3 base = int3(floor(q));
    float3 f = q - float3(base);
    float3 r2 = r * r;
    float sum = 0.0, weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 at = min(base + offset, int3(int(probes.x) - 1));
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float sky = 0.0, valid = 1.0;
        float3 moved = float3(0.0);
        [unroll]
        for (int axis = 0; axis < 3; axis++)
        {
            int face = axis * 2 + (r[axis] < 0.0 ? 1 : 0);
            int4 texel = int4(face * int(probes.x) + at.x, at.y, c * int(probes.x) + at.z, 0);
            float alpha = probe_cubes.Load(texel).a;
            sky += probe_skies.Load(texel) * r2[axis];
            valid = min(valid, faceHolds(alpha));
            moved[axis] = faceMove(alpha);
        }
        float3 probe = oc.xyz + (float3(at) + 0.5 + moved) * (oc.w * probes.y);
        float weight = lerps.x * lerps.y * lerps.z * valid * probeFacing(p, n, probe);
        weight *= lerp(0.05, 1.0, probeSees(probe_reach, p, n, probe, at, c, probes.x, oc.w * probes.y));
        sum += sky * weight;
        weights += weight;
    }
    return weights > 1e-5 ? sum / weights : 1.0;
}

// Where q falls on the picture, its depth there and the depth the picture holds there, or false
// where it lies behind the eye or off the picture. Bevy's depth runs backwards, nearer greater.
bool onScreen(float3 q, out float2 uv, out float depth, out float held)
{
    uv = float2(0.0);
    depth = 0.0;
    held = 0.0;
    float4 clip = passTransform(passView.clip_from_world, float4(q, 1.0));
    if (clip.w <= 1e-4)
        return false;
    float3 ndc = clip.xyz / clip.w;
    uv = float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5);
    if (any(uv < 0.0) || any(uv > 1.0))
        return false;
    depth = ndc.z;
    held = passLoadDepth(min(int2(uv * float2(passPictureSize())), int2(passPictureSize()) - 1));
    return true;
}

// A ray from p along way traced through the picture's depth in steps that lengthen as they go,
// as 3DEngine's traceScreen, meeting a surface where it first passes behind the depth within two
// steps of it, or a quarter of a unit, the step halved five times to find where, the ray met only
// where the depth does not leap between the halving's ends and the surface there turns toward the
// ray. A ray that passed behind the depth so, or went off the picture, is left at the last point it
// stood in front of it in `left`, for the field to find what it passed behind.
bool traceScreen(float3 p, float3 way, float length, int steps, out float3 hit, out float2 hitUv, out float left)
{
    hit = p;
    hitUv = float2(0.0);
    left = length;
    float before = 0.0;
    float behindFrom = -1.0;
    float3 eye = passView.world_position;
    for (int i = 0; i < steps; i++)
    {
        float s = float(i + 1) / float(steps);
        float t = length * s * s;
        float3 q = p + way * t;
        float2 uv;
        float depth, held;
        if (!onScreen(q, uv, depth, held))
        {
            left = behindFrom >= 0.0 ? behindFrom : before;
            return false;
        }
        if (held > 0.0 && depth < held)
        {
            float3 surface = passWorldPosition(uv, held);
            float behind = distance(eye, q) - distance(eye, surface);
            if (behind < max(length / float(steps) * 2.0, 0.25))
            {
                float near = before, far = t;
                float2 atUv = uv;
                float atHeld = held;
                [loop]
                for (int k = 0; k < 5; k++)
                {
                    float middle = (near + far) * 0.5;
                    float2 m;
                    float md, mh;
                    if (onScreen(p + way * middle, m, md, mh) && mh > 0.0 && md < mh)
                    {
                        far = middle;
                        atUv = m;
                        atHeld = mh;
                    }
                    else
                        near = middle;
                }
                hit = passWorldPosition(atUv, atHeld);
                // Across a thing's edge the depth leaps between the halving's ends, and a ray that
                // crossed it passed behind the thing.
                float2 frontUv;
                float frontDepth, frontHeld;
                bool leaps = false;
                if (onScreen(p + way * near, frontUv, frontDepth, frontHeld))
                {
                    float frontBy = frontHeld > 0.0 ? distance(eye, p + way * near) - distance(eye, passWorldPosition(frontUv, frontHeld)) : -1e6;
                    float behindBy = distance(eye, p + way * far) - distance(eye, hit);
                    leaps = behindBy - frontBy > max(0.25, 8.0 * (far - near));
                }
                // A face the ray meets from behind, its way leaving the surface, is not where it
                // meets the thing.
                int2 pixel = min(int2(atUv * float2(passPictureSize())), int2(passPictureSize()) - 1);
                float3 facing = passSurfaceOf(gbuffer.Load(int3(pixel, 0))).normal;
                if (!leaps && dot(facing, way) < 0.0)
                {
                    hitUv = atUv;
                    return true;
                }
            }
            if (behindFrom < 0.0)
                behindFrom = before;
        }
        before = t;
    }
    left = behindFrom >= 0.0 ? behindFrom : length;
    return false;
}

// The light a glossy surface at p facing n reflects along r, traced through the picture's depth and
// past it through the field, with in a how much of it is known, 1 where the ray met a surface, and 0
// where it met nothing or the surface is too rough to trace for, where the probes and the
// environment reflect alone, as 3DEngine's tracedReflection. A surface met on the picture reflects
// the light the frame before's picture showed where it was, faded to its shading by the field at
// the picture's edge, and one past the picture or hidden from it its color times the light reaching
// it, as the probes shade a hit (shadeHit).
float4 tracedReflection(FieldInfo info, GiLights lights, float3 p, float3 n, float3 r, float roughness)
{
    if (roughness >= GlossyRoughness)
        return float4(0.0);
    float cell = max(fieldCell(info, p), 0.02);
    float3 start = p + n * cell;
    float3 hit;
    float2 hitUv;
    float left;
    bool onPicture = traceScreen(start, r, reflection.y, int(reflection.x), hit, hitUv, left);
    if (!onPicture)
    {
        float t = traceField(scene_field, scene_field_sampler, info, start, r, left, 1e5);
        if (t < 0.0)
            return float4(0.0);
        hit = start + r * t;
    }
    float3 shaded = shadeHit(scene_field, scene_field_sampler, info, scene_albedo, scene_faces, scene_glow, lights, glow_lights, probe_cubes,
        probe_reach, hit, probes.x, probes.y, probes.z);
    if (!onPicture || reflection.z < 0.5)
        return float4(shaded, 1.0);
    float2 uv = passPreviousUvOf(hit);
    if (any(uv < 0.0) || any(uv > 1.0))
        return float4(shaded, 1.0);
    float edge = saturate(min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y)) * 12.0);
    float3 seen = gi_picture_previous.SampleLevel(scene_field_sampler, uv, 0.0).rgb;
    return float4(lerp(shaded, seen, edge), 1.0);
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

    // The light from all around a glossy surface reflects, Bevy's from its ambient term and its
    // environment map along the mirror way, taken back and given again as 3DEngine's model pass
    // gives it, the reflection traced through the picture and the field as far as it met a surface,
    // fading out as the surface grows rough, and Bevy's in the rest, times the share of the sky the
    // probes around the surface see along the mirror way, so a room sealed from the sky reflects
    // none of it.
    float3 r = reflect(-V, n);
    float roughness = surface.perceptual_roughness;
    float3 f0 = lerp(float3(0.16 * surface.reflectance * surface.reflectance), surface.base_color, surface.metallic);
    float2 F_ab_r = F_AB(roughness, NdotV);
    float3 specularShare = f0 * F_ab_r.x + F_ab_r.y;
    float3 environment = passHasEnvironment() ? passEnvironmentSpecular(r, roughness) : float3(0.0);
    float3 ambientShare = specularShare * saturate(dot(f0, float3(50.0 * 0.33))) * passLights.ambient_color.rgb * occlusion;
    float3 bevySpecular = (specularShare * environment + ambientShare) * passView.exposure;
    float sky = held > 0.0 ? lerp(1.0, skyAt(info, world, n, r), held) : 1.0;
    float4 traced = tracedReflection(info, lights, world, n, r, roughness);
    float gloss = traced.a * (1.0 - smoothstep(GlossyRoughness * 0.5, GlossyRoughness, roughness));
    float3 specular = bevySpecular * sky * (1.0 - gloss) + occlusion * traced.rgb * gloss * specularShare;

    return float4(added - given + specular - bevySpecular, 0.0);
}
