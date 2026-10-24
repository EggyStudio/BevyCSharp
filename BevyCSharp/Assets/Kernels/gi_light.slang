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

// The light arriving at each screen probe's surface blended with the probes around and the frame
// before's, its alpha below a half where the probe holds none, and each probe's surface's normal and
// distance from the eye, -1 for one on no surface, as gi_screen_filter.slang and gi_screen.slang
// left them, the camera's images where its screen probes are traced.
Texture2D<float4> gi_screen_blended;
Texture2D<float4> gi_screen_surfaces;

// The glow lights' light on the picture's surfaces at half its size, the share of the near light's
// rough light, the far lights' light and each texel's normal and what it says, as
// gi_screen_glow.slang left them.
Texture2D<float4> gi_glow_light;
Texture2D<float4> gi_glow_far;
Texture2D<float4> gi_glow_normal;

// x: a screen probe's tile's pixels along each side. y: 1 where the screen probes' light is read. z: 1
// where the glow lights' light is read from gi_glow_light, as it is where the screen probes are
// traced or stand for the glow lights alone. w: unused.
uniform float4 screen;

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

// The step toward a pixel's neighbor a pixel on or back along an axis, the one whose point lies
// nearer p, so a pixel at a thing's edge takes its neighbor on its own surface, which says how the
// surface's point moves from pixel to pixel.
float3 nearerStep(int2 pixel, int2 step, float3 p)
{
    int2 size = int2(passPictureSize());
    int2 ahead = min(pixel + step, size - 1), behind = max(pixel - step, int2(0));
    float aheadDepth = passLoadDepth(ahead), behindDepth = passLoadDepth(behind);
    float3 forward = aheadDepth > 0.0 ? passWorldPosition(passUv(ahead), aheadDepth) - p : float3(1e10);
    float3 back = behindDepth > 0.0 ? p - passWorldPosition(passUv(behind), behindDepth) : float3(1e10);
    float3 nearer = dot(forward, forward) < dot(back, back) ? forward : back;
    return dot(nearer, nearer) < 1e18 ? nearer : float3(0.0);
}

// The four screen probes around a pixel, where each is, how much each counts for the light that
// bounces, 0 for one that holds no light or stands on no surface like the pixel's, and how much by
// how near it is alone, 0 for one on no surface.
struct ScreenProbes
{
    int2 at[4];
    float bounced[4];
    float near[4];
};

// Each probe weighed by how near it is, how alike its normal is to the surface's, and how near its
// distance from the eye is to the distance the surface's own plane lies at under the probe, found from
// how the surface's point moves from pixel to pixel, so a floor seen at a slant takes the probes up
// and down its slope as well as those beside it, as 3DEngine's model pass weighs them.
ScreenProbes screenProbesHere(float2 position, float3 toEye, float3 facing, float3 acrossOne, float3 downOne)
{
    ScreenProbes probes;
    [unroll]
    for (int none = 0; none < 4; none++)
    {
        probes.at[none] = int2(0);
        probes.bounced[none] = 0.0;
        probes.near[none] = 0.0;
    }
    if (screen.y < 0.5)
        return probes;
    uint across, down;
    gi_screen_blended.GetDimensions(across, down);
    float2 g = position / screen.x - 0.5;
    int2 base = int2(floor(g));
    float2 f = g - float2(base);
    float distance = length(toEye);
    [unroll]
    for (int corner = 0; corner < 4; corner++)
    {
        int2 offset = int2(corner & 1, corner >> 1);
        probes.at[corner] = clamp(base + offset, int2(0), int2(int(across) - 1, int(down) - 1));
        float4 surface = gi_screen_surfaces.Load(int3(probes.at[corner], 0));
        if (surface.w < 0.0)
            continue;
        float2 lerps = lerp(1.0 - f, f, float2(offset));
        probes.near[corner] = lerps.x * lerps.y;
        float2 toProbe = (float2(probes.at[corner]) + 0.5) * screen.x - position;
        float expected = length(toEye - acrossOne * toProbe.x - downOne * toProbe.y);
        float like = probes.near[corner] * pow(saturate(dot(facing, surface.xyz)), 8.0) * exp(-abs(surface.w - expected) / (0.02 * distance + 0.05));
        probes.bounced[corner] = gi_screen_blended.Load(int3(probes.at[corner], 0)).a < 0.5 ? 0.0 : like;
    }
    return probes;
}

// The indirect light reaching a pixel, irradiance over pi, blended between the four screen probes
// around it that stand on a surface like its own, or -1 where none does.
float3 screenBounced(ScreenProbes probes)
{
    float4 sum = float4(0.0);
    [unroll]
    for (int corner = 0; corner < 4; corner++)
        if (probes.bounced[corner] > 0.0)
            sum += float4(gi_screen_blended.Load(int3(probes.at[corner], 0)).rgb, 1.0) * probes.bounced[corner];
    return sum.w > 1e-3 ? sum.rgb / sum.w / Pi : float3(-1.0);
}

// The indirect light reaching a surface at p facing n, irradiance over pi, from those of the world's
// probes around it it sees with nothing the field holds between, faded to the next cascade across
// this one's outer band, or -1 where it sees none, which the caller reads from them all as bouncedAt
// reads them, since read here as well bouncedAt's code was in the pass twice. A pixel no
// screen probe on a surface like its own stands near, as where two walls meet, reads it, since a
// probe beyond a wall thinner than the probes' spacing is seen by bouncedAt's reach where the point
// it judges from lies in the other wall's cells, and in 3DEngine lent a thin room's corner the
// lamp's light outside, 3.25 times the wall beside it where its reference reads 1.10, and 1.02 so.
float3 bouncedSeen(FieldInfo field, float3 p, float3 n)
{
    int cascades = int(probes.z);
    FieldInfo info = probesOf(field, cascades);
    int c = fieldCascade(info, p, 1.0);
    if (c < 0 || c >= cascades)
        return float3(0.0);
    float own = c + 1 < cascades && info.cascades[c + 1].origin_and_cell.w > 0.0 ? cascadeHold(info, c, p) : 1.0;
    float3 light = float3(0.0);
    [loop]
    for (int k = 0; k < 2; k++)
    {
        float share = k == 0 ? own : 1.0 - own;
        if (share <= 0.0)
            continue;
        float4 seen = bouncedSeenSum(scene_field, scene_field_sampler, field, info, probe_cubes, p, n, probes.x, probes.y, c + k);
        if (seen.w <= 1e-5)
            return float3(-1.0);
        light += seen.rgb / seen.w * share;
    }
    return light / Pi;
}

// The light the glow lights send a pixel straight, irradiance over pi, as gi_screen_glow.slang left it
// on the two by two texels of its half-sized images around the pixel, the share each texel takes of
// the rough light of the glow light that gives it the most of it near it (glowLightRough,
// glowLightNear), worked out here at the pixel with the pixel's own normal, and the other lights'
// light as it is. Each texel weighed by how near it is, how alike its normal is to the surface's, and
// how near its point's distance from the eye is to the distance the surface's own plane lies at
// there, as the screen probes are weighed. A pixel whose like texels no probe around holds marches to
// the lights' middles itself, which this asks for with -1, the march's code in the pass once, and one
// no texel around holds a point like its own, as along a thin
// mesh, takes the texels around on any surface by how near each is, since marching from those few
// pixels cost 3DEngine's model pass 1.1 ms in a room of 64 lamps at 1280 by 720, each group of pixels
// the GPU runs together waiting on its slowest.
float3 glowLightsHere(FieldInfo info, float2 position, float3 p, float3 n, float3 toEye, float3 facing, float3 acrossOne, float3 downOne)
{
    int count = min(int(gi_lights[0].counts.w), MostGlowLights);
    if (count == 0)
        return float3(0.0);
    uint width, height;
    gi_glow_light.GetDimensions(width, height);
    float2 g = position * 0.5 - 0.5;
    int2 base = int2(floor(g));
    float2 f = g - float2(base);
    float distance = length(toEye);
    float margin = max(fieldCell(info, p), 0.0) * 0.5;
    float3 alike = float3(0.0), anywhere = float3(0.0);
    float alikeWeight = 0.0, anywhereWeight = 0.0, askWeight = 0.0;
    bool onLight = false;
    // The rough light of the last light a texel named, so texels that name the same light work it out
    // once.
    int named = -1;
    float3 rough = float3(0.0);
    [unroll]
    for (int corner = 0; corner < 4; corner++)
    {
        int2 offset = int2(corner & 1, corner >> 1);
        int3 texel = int3(clamp(base + offset, int2(0), int2(int(width) - 1, int(height) - 1)), 0);
        float4 held = gi_glow_light.Load(texel);
        float4 normal = gi_glow_normal.Load(texel);
        int kind = int(normal.w * 255.0 + 0.5);
        if (kind == 0)
            continue;
        float2 lerps = lerp(1.0 - f, f, float2(offset));
        float near = lerps.x * lerps.y;
        float2 toTexel = (float2(base + offset) + 0.5) * 2.0 - position;
        float expected = length(toEye - acrossOne * toTexel.x - downOne * toTexel.y);
        float weight = near * pow(saturate(dot(facing, normal.xyz * 2.0 - 1.0)), 8.0) * exp(-abs(held.a - expected) / (0.02 * distance + 0.05));
        onLight = onLight || kind == GlowOnLight;
        if (kind == GlowAsk)
            askWeight += weight;
        if (kind >= GlowOnLight)
            continue;
        int dominant = kind - 2;
        if (dominant >= 0 && dominant != named)
        {
            GlowLight light = glow_lights[min(dominant, count - 1)];
            float faded = glowLightFade(light, p, n);
            rough = faded > 0.0 ? glowLightRough(light, p, n, margin) * (faded * glowLightNear(light, p)) : float3(0.0);
            named = dominant;
        }
        float3 lit = (dominant >= 0 ? held.rgb * rough : float3(0.0)) + gi_glow_far.Load(texel).rgb;
        alike += lit * weight;
        alikeWeight += weight;
        anywhere += lit * near;
        anywhereWeight += near;
    }
    // A pixel whose texels around lie on a glow light's own surface alone takes no other's.
    if (alikeWeight > 1e-3)
        return alike / (alikeWeight * Pi);
    if (anywhereWeight > 1e-3 && askWeight <= 1e-3)
        return anywhere / (anywhereWeight * Pi);
    if (onLight && askWeight <= 1e-3)
        return float3(0.0);
    // A pixel on a lamp's own side, whose half-sized texels lie across its edge, takes no other light's
    // either, where marched the sides of a room of sixteen lamps cost 3DEngine's model pass 0.9 ms at
    // 1280 by 720.
    [loop]
    for (int i = 0; i < count; i++)
        if (glowLightNear(glow_lights[i], p) > 0.0 && glowLightOn(glow_lights[i], p, n, margin))
            return float3(0.0);
    return float3(-1.0);
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

    // How the surface's point moves from pixel to pixel, and the way it faces as the depth gives it,
    // which weigh the screen probes and the glow lights' texels around the pixel.
    float3 toEye = passView.world_position - world;
    float3 acrossOne = nearerStep(pixel, int2(1, 0), world), downOne = nearerStep(pixel, int2(0, 1), world);
    float3 facing = surfaceNormal(cross(acrossOne, downOne), toEye);
    if (dot(facing, toEye) < 0.0)
        facing = -facing;

    // How much of the light from all around the probes give, all of it inside the last cascade and
    // none past its outer band, and the light they hold here, irradiance over pi, the screen's probes'
    // within the world's second cascade, fading to the world's across its outer band, since switched
    // there a canyon's floor stepped by 14% in 3DEngine. A pixel the screen's probes stand around on
    // no surface like its own, as at a corner, reads the world's probes it sees through the field.
    float held = bouncedHold(info, world, n, probes.y, probes.z);
    float3 bounced = float3(0.0);
    if (held > 0.0)
    {
        ScreenProbes around = screenProbesHere(input.position.xy, toEye, facing, acrossOne, downOne);
        float3 near = screenBounced(around);
        float screened = near.x >= 0.0 ? probesHold(probesOf(info, int(probes.z)), world, n, probes.y, min(1, int(probes.z) - 1)) : 0.0;
        bounced = screened > 0.0 ? near : float3(0.0);
        if (screened < 1.0)
        {
            bool edge = screen.y > 0.5 && near.x < 0.0 && around.near[0] + around.near[1] + around.near[2] + around.near[3] > 0.0;
            float3 far = edge ? bouncedSeen(info, world, n) : float3(-1.0);
            if (far.x < 0.0)
                far = bouncedAt(probe_cubes, probe_reach, info, world, n, probes.x, probes.y, probes.z) / Pi;
            bounced = lerp(far, bounced, screened);
        }
    }

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
    // Where the screen's probes are traced, or stand for the glow lights alone, a pixel reads their
    // texels instead, at a cost the lights do not change (glowLightsHere).
    if (lights.counts.w > 0.5)
    {
        float3 glowing = screen.z > 0.5 ? glowLightsHere(info, input.position.xy, world, n, toEye, facing, acrossOne, downOne) : float3(-1.0);
        if (glowing.x < 0.0)
            glowing = glowLightsAt(scene_field, scene_field_sampler, info, glow_lights, uint(lights.counts.w), world, n, 4, false, float3(0.0)) / Pi;
        added += diffuse * glowing;
    }

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
