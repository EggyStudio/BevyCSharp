// What the passes of global illumination share, which any shader can reach with `import bcs_gi;`:
// directions laid out on an octahedron, rays traced through the scene's distance field, the light a
// surface the field holds sends back along a ray, and the light the world's probes hold, read at a
// surface between the eight probes around it.
//
// The world's probes stand in cascades over the field's cascades, eight cells of their cascade
// apart, and each gathers the light arriving at it into the six faces of a cube, irradiance from
// each axis's two ways, laid out in one image of half floats, every probe's six faces a block of
// the probes along x and the cascades one after another along z, its alpha 0 where the probe stands
// inside a mesh and two more than its move along that axis in its spacing where it does not (see
// `Render.SetGlobalIllumination`). The light is in the units of the camera's picture, its exposure
// applied.
//
// Taken from 3DEngine's `gi.slang` and `cascades.slang`, its names kept where it has them, each
// reader taking the field and its sampler apart as this bridge binds them, and its glow lights and
// emissive surfaces coming with the emitters.

module bcs_gi;

import bcs_field;

public static const float Pi = 3.14159265358979;

/// A unit direction as a point of the square from 0 to 1, the octahedron folded flat, which lays
/// every direction on a square with texels of nearly even solid angle.
public float2 octahedron(float3 d)
{
    d /= abs(d.x) + abs(d.y) + abs(d.z);
    float2 p = d.xy;
    if (d.z < 0.0)
        p = (1.0 - abs(p.yx)) * float2(p.x >= 0.0 ? 1.0 : -1.0, p.y >= 0.0 ? 1.0 : -1.0);
    return p * 0.5 + 0.5;
}

/// The point of the octahedron a point of the square stands for, not yet a direction.
public float3 octahedronPoint(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;
    float3 d = float3(p, 1.0 - abs(p.x) - abs(p.y));
    if (d.z < 0.0)
        d.xy = (1.0 - abs(d.yx)) * float2(d.x >= 0.0 ? 1.0 : -1.0, d.y >= 0.0 ? 1.0 : -1.0);
    return d;
}

/// The direction a point of the square stands for.
public float3 fromOctahedron(float2 uv)
{
    return normalize(octahedronPoint(uv));
}

/// How much of the sphere a texel of the square spans at `uv`, against the others, one over the
/// cube of its octahedron point's distance from the middle, as a patch of the octahedron there
/// covers its area over that cube. The texels near the octahedron's corners span the least, those
/// at the middles of its faces the most, near three times as much.
public float octahedronShare(float2 uv)
{
    float l = length(octahedronPoint(uv));
    return 1.0 / (l * l * l);
}

/// The rays a texel of a cascade's octahedron of `texels` along each side sends, four where it has
/// 8, one from the middle of each quarter of the texel, and one from its middle where it has 16, so
/// every probe sends 256. A texel of 8 a side spans a sixty-fourth of the sphere, which one ray takes
/// a lamp's bright spot under a ceiling or misses it for, and in 3DEngine a Cornell box's probe read
/// 93% over a path-traced reference with one ray and 18% with four.
public uint RaysAt(uint texels)
{
    return texels <= 8u ? 4u : 1u;
}

/// A texel's mark as the trace writes it for `rays` rays, `nothing` of which met nothing, says
/// what most of them met, 0 a surface, a half a surface before their interval began, a quarter the sky
/// and 1 nothing, moved a sixteenth on for each that met nothing, up from 0 or a half and down from
/// 1 for each that met something, so the mark stays within the band each reader tells what a
/// texel's rays met by; `texelNothing` reads back how many met nothing.
public float texelMark(float most, uint nothing, uint rays)
{
    return most >= 0.75 ? 1.0 - 0.0625 * float(rays - nothing) : most + 0.0625 * float(nothing);
}

public uint texelNothing(float mark, uint rays)
{
    if (mark >= 0.75)
        return rays - uint((1.0 - mark) / 0.0625 + 0.5);
    if (mark >= 0.375)
        return uint((mark - 0.5) / 0.0625 + 0.5);
    return mark >= 0.1875 ? 0u : uint(mark / 0.0625 + 0.5);
}

/// A point or spot light the indirect light is lit by, as the bridge gathers it.
public struct GiLight
{
    /// xyz: where it is. w: 1 for a point light, 2 for a spot light.
    public float4 positionAndKind;
    /// xyz: the way a spot light points. w: how far it reaches.
    public float4 directionAndRange;
    /// rgb: the light a white surface facing it a unit away returns. w: 1 where it casts shadows.
    public float4 colorAndShadow;
    /// x: a spot light's scale and y its offset, Bevy's falloff from its inner cone to its outer.
    public float4 cone;
};

/// The most cascades of probes, the four `High` traces and the far cascade's.
public static const int MostProbeCascades = 5;

/// The lights the indirect light is lit by, as the managed side writes them each frame.
public struct GiLights
{
    /// xyz: the way toward the sun. w: 1 where there is a sun.
    public float4 sunDirection;
    /// rgb: the light a white surface facing the sun returns. w: 1 where it casts shadows.
    public float4 sunColor;
    /// rgb: the light from all around where a ray reaches nothing.
    public float4 sky;
    /// x: how many lights. y: the frame's number. z: how strongly the indirect light of the frame
    /// before is added. w: unused.
    public float4 counts;
    public GiLight lights[16];
    /// xyz: the corner each cascade of probes lay at the frame before, where the faces the probes'
    /// rays read were gathered, its own corner where it lay nowhere. w: unused.
    public float4 before[MostProbeCascades];
};

/// Where a ray from `origin` along `way` first comes within a tenth of a cell of a surface between
/// `tMin` and `tMax`, through the finest cascade that holds each point along it, or -1 where it
/// reaches `tMax` first or leaves every cascade.
public float traceField(Texture3D<float> field, SamplerState linear, FieldInfo info, float3 origin, float3 way, float tMin, float tMax)
{
    float t = tMin;
    for (int i = 0; i < 64 && t < tMax; i++)
    {
        float3 p = origin + way * t;
        int c = fieldCascade(info, p, 1.0);
        if (c < 0)
            return -1.0;
        float cell = info.cascades[c].origin_and_cell.w;
        float d = fieldDistanceIn(field, linear, info, c, p);
        if (d < cell * 0.1)
            return t;
        t += max(d, cell * 0.25);
    }
    return -1.0;
}

/// How much of a spot light's light reaches a point the way `l` from it points, as Bevy's lighting
/// falls off from the inner cone to the outer, and all of a point light's.
public float spotShare(GiLight light, float3 l)
{
    if (int(light.positionAndKind.w) != 2)
        return 1.0;
    float share = saturate(dot(-l, light.directionAndRange.xyz) * light.cone.x + light.cone.y);
    return share * share;
}

/// The light a point or spot light sends a surface at `p` facing `n`, falling off with distance and
/// toward its range as Bevy's lighting has it, unshadowed.
public float lampLight(GiLight light, float3 p, float3 n)
{
    float3 toLight = light.positionAndKind.xyz - p;
    float distance2 = max(dot(toLight, toLight), 1e-4);
    float3 l = toLight * rsqrt(distance2);
    float result = max(dot(n, l), 0.0) / distance2;
    float range = light.directionAndRange.w;
    if (range > 0.0)
    {
        float edge = saturate(1.0 - pow(distance2 / (range * range), 2.0));
        result *= edge * edge;
    }
    return result * spotShare(light, l);
}

/// The light that reaches a surface at `p` facing `n` from the sun, where the field lets it
/// through, and from each point and spot light, unshadowed.
public float3 directLight(Texture3D<float> field, SamplerState linear, FieldInfo info, GiLights lights, float3 p, float3 n)
{
    float3 arrived = float3(0.0);
    if (lights.sunDirection.w > 0.5)
    {
        float3 l = lights.sunDirection.xyz;
        float facing = dot(n, l);
        if (facing > 0.0)
        {
            float cell = fieldCell(info, p);
            if (traceField(field, linear, info, p + n * (cell * 1.5), l, cell, 1e5) < 0.0)
                arrived += lights.sunColor.rgb * facing;
        }
    }
    for (int i = 0; i < min(int(lights.counts.x), 16); i++)
        arrived += lights.lights[i].colorAndShadow.rgb * lampLight(lights.lights[i], p, n);
    return arrived;
}

/// How much a probe at `probe` counts toward the light at a surface at `p` facing `n`, besides how
/// near it is, more the more squarely it stands in front of the surface, as DDGI weighs its probes.
/// A probe behind the surface's plane stands on its other side, under a floor or beyond a wall
/// thinner than the probes are apart, and sees light that never reaches this side, so it lends next
/// to nothing, only enough to light a surface no probe in front of it reaches.
public float probeFacing(float3 p, float3 n, float3 probe)
{
    float3 toward = probe - p;
    float length2 = dot(toward, toward);
    if (length2 <= 1e-8)
        return 1.0;
    float cosine = dot(toward * rsqrt(length2), n);
    float facing = (cosine + 1.0) * 0.5;
    return lerp(0.02 * facing * facing, facing * facing + 0.2, smoothstep(-0.15, 0.15, cosine));
}

/// Whether nothing the field holds stands between `a` and `b`, `clearance` kept clear at either end
/// for the surfaces `a` and `b` lie near.
public bool seesThrough(Texture3D<float> field, SamplerState linear, FieldInfo info, float3 a, float3 b, float clearance)
{
    float3 toward = b - a;
    float length = length(toward);
    return length <= clearance * 2.0 || traceField(field, linear, info, a, toward / length, clearance, length - clearance) < 0.0;
}

/// The directions each probe's reach is kept along, eight by eight of its octahedron.
public static const int ReachTexels = 8;

/// How near a surface a probe may stand, and the most it is moved from its spot in the grid to stand
/// so far off one or out of a mesh, in its spacing, so a probe in the plane of a wall or a ceiling
/// stands in the room before it rather than inside it, as DDGI moves its probes. A probe moved stays
/// within half the spacing of its spot, so the probes keep their order.
public static const float Clearance = 0.1;
public static const float MostMoved = 0.45;

/// What a probe's faces carry in alpha, read as a face of each axis, whether the probe holds light,
/// 0 where it stands inside a mesh, and where it does, two more than its move along the axis in its
/// spacing.
public float faceHolds(float alpha)
{
    return alpha > 0.5 ? 1.0 : 0.0;
}

public float faceMove(float alpha)
{
    return alpha > 0.5 ? alpha - 2.0 : 0.0;
}

/// How much of the frame before's own light a probe's may keep before the indirect light is taken at
/// the share it kept, a fall by a fifth or more, which a light switched off or carried away gives
/// and the field's cells settling after a camera moves do not, and a rise by a quarter or more, its
/// inverse, so a light that flickers moves its indirect light down and up alike.
public static const float Falls = 0.8;

/// The most a probe's indirect light is raised by in a frame its own light rose by much, so a probe
/// its own light barely lit before gains no more than four times its bounce at once.
public static const float Rises = 4.0;

/// The light a color gives the eye, by Rec. 709's weights.
public float luminance(float3 c)
{
    return dot(c, float3(0.2126, 0.7152, 0.0722));
}

/// The share of the indirect light a probe takes the frame after its own light went from `before`
/// to `now`, the same share as its own light kept where that fell or rose by much, and the whole of
/// it otherwise.
public float3 followed(float3 now, float3 before)
{
    float3 fell = now / max(before, float3(1e-6));
    return select(fell < Falls || fell > 1.0 / Falls, clamp(fell, 0.0, Rises), float3(1.0));
}

/// How much of the light a probe's rays bring back must come straight from the sun and the lights for
/// the probe to be judged by its own light, a quarter, where a probe lit mostly by indirect light, as
/// one down a corridor from a lit room, is judged by the own light of the probes around it summed,
/// since its own light is too little of its light to say what went out.
public static const float OwnShare = 0.25;

/// How much the probe of cascade `c` at `at`, standing at `probe`, sees of a surface at `p` facing
/// `n`, 1 where `p`, taken three tenths of the probes' spacing off the surface along `n`, lies nearer
/// the probe than its rays reached that way the frame before, falling to 0 over an eighth of the
/// spacing past it, as DDGI weighs its probes by the distances they traced. Taken off the surface,
/// the point comes nearer a probe on its own side and goes further from one past it, so a probe
/// under a floor or beyond a wall, whose rays stop at the floor's or the wall's far face, lends a
/// surface on this side none of the light it sees there.
public float probeSees(Texture3D<float> reach, float3 p, float3 n, float3 probe, int3 at, int c, float probes, float apart)
{
    float3 toward = p + n * (0.3 * apart) - probe;
    float d = length(toward);
    if (d < 1e-4)
        return 1.0;
    int2 texel = min(int2(octahedron(toward / d) * float(ReachTexels)), int2(ReachTexels - 1));
    float reached = reach.Load(int4(int3(at.xy * ReachTexels + texel, c * int(probes) + at.z), 0));
    float slack = 0.125 * apart;
    return saturate((reached + slack - d) / slack);
}

/// How far off a surface along its normal the blend toward the light past the probes is weighed
/// from (`probesHold`), in the probes' spacing. With none, in 3DEngine a wall in the outer band of
/// `Low`'s last cascade took a closed room's absent ambient light, 46% under.
public static const float Lean = 0.5;

/// The cells of a cascade's outer band, a probes' spacing, across which the indirect light fades
/// from its probes to the next cascade's, and from the last cascade's to the light from beyond it
/// (`bouncedHold`). Read from one cascade alone, the light stepped where each ended.
public static const float CascadeBand = 8.0;

/// How much of the indirect light a point `p` takes from cascade `c` rather than the next, all of it
/// inside, falling to none across `c`'s outer band (`CascadeBand`), kept clear of the eye
/// (`bandHold`), past the cell `fieldCascade` keeps.
public float cascadeHold(FieldInfo info, int c, float3 p)
{
    float4 oc = info.cascades[c].origin_and_cell;
    return bandHold(info, c, (p - oc.xyz) / oc.w, CascadeBand, 1.0);
}

/// How much of the light from all around a surface at `p` facing `n` the probes of the cascades up
/// to `upTo` give rather than what lies past them, all of it inside the coarsest of them built,
/// falling to none across its outer band (`CascadeBand`), kept clear of the eye (`bandHold`), and
/// none past it. The point is taken leaned off the surface by half a spacing (`Lean`), or as the
/// surface itself where that holds more.
public float probesHold(FieldInfo info, float3 p, float3 n, float spacing, int upTo)
{
    int c = fieldCoarsestBuilt(info, upTo);
    if (c < 0)
        return 0.0;
    float4 oc = info.cascades[c].origin_and_cell;
    float3 cell = (p - oc.xyz) / oc.w;
    return max(bandHold(info, c, cell + n * (Lean * spacing), CascadeBand, 0.0), bandHold(info, c, cell, CascadeBand, 0.0));
}

/// How much of the light from all around a surface at `p` facing `n` the probes give rather than
/// what lies past them, through as many cascades as `cascades` (`probesHold`), the probes found
/// where the field's cascades they stand in lie (`probesOf`).
public float bouncedHold(FieldInfo info, float3 p, float3 n, float spacing, float cascades)
{
    return probesHold(probesOf(info, int(cascades)), p, n, spacing, int(cascades) - 1);
}

/// The indirect light reaching a surface at `p` facing `n`, irradiance, from the faces of the eight
/// probes of cascade `c` around it, each weighed by how near it is, how squarely it stands in front
/// of the surface and how much of it it sees (`probeSees`), a probe inside a mesh passed over, the
/// three faces `n` looks toward weighed by the square of `n` along each. A probe it does not see
/// keeps a twentieth of its weight, so a surface wedged between two meshes is not left dark.
float3 bouncedIn(Texture3D<float4> cubes, Texture3D<float> reach, FieldInfo info, float3 p, float3 n, float probes, float spacing, int c)
{
    float4 oc = info.cascades[c].origin_and_cell;
    float3 q = clamp((p - oc.xyz) / (oc.w * spacing) - 0.5, 0.0, probes - 1.0);
    int3 base = int3(floor(q));
    float3 f = q - float3(base);
    float3 n2 = n * n;
    float3 sum = float3(0.0);
    float weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 at = min(base + offset, int3(int(probes) - 1));
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float3 light = float3(0.0);
        float valid = 1.0;
        float3 moved = float3(0.0);
        for (int axis = 0; axis < 3; axis++)
        {
            int face = axis * 2 + (n[axis] < 0.0 ? 1 : 0);
            float4 texel = cubes.Load(int4(face * int(probes) + at.x, at.y, c * int(probes) + at.z, 0));
            light += texel.rgb * n2[axis];
            valid = min(valid, faceHolds(texel.a));
            moved[axis] = faceMove(texel.a);
        }
        float3 probe = oc.xyz + (float3(at) + 0.5 + moved) * (oc.w * spacing);
        float weight = lerps.x * lerps.y * lerps.z * valid * probeFacing(p, n, probe);
        weight *= lerp(0.05, 1.0, probeSees(reach, p, n, probe, at, c, probes, oc.w * spacing));
        sum += light * weight;
        weights += weight;
    }
    return weights > 1e-5 ? sum / weights : float3(0.0);
}

/// The indirect light reaching a surface at `p` facing `n`, irradiance, from the finest cascade of
/// probes that holds it, fading to the next cascade's across its outer band (`cascadeHold`) where
/// that one is built, or none where no cascade of probes holds `p`. The probes are found where the
/// field's cascades they stand in lie (`probesOf`).
public float3 bouncedAt(Texture3D<float4> cubes, Texture3D<float> reach, FieldInfo field, float3 p, float3 n, float probes, float spacing, float cascades)
{
    FieldInfo info = probesOf(field, int(cascades));
    int c = fieldCascade(info, p, 1.0);
    if (c < 0 || c >= int(cascades))
        return float3(0.0);
    float own = c + 1 < int(cascades) && info.cascades[c + 1].origin_and_cell.w > 0.0 ? cascadeHold(info, c, p) : 1.0;
    float3 light = float3(0.0);
    [loop]
    for (int k = 0; k < 2; k++)
    {
        float share = k == 0 ? own : 1.0 - own;
        if (share > 0.0)
            light += bouncedIn(cubes, reach, info, p, n, probes, spacing, c + k) * share;
    }
    return light;
}

/// The sum `bouncedSeenAt` divides for cascade `c`, the light of the probes seen times their
/// weights, and the weights, which a reader that takes another light where no probe is seen tells
/// apart from dark. `gathered` places the probes as `probesOf` does.
public float4 bouncedSeenSum(Texture3D<float> field, SamplerState linear, FieldInfo info, FieldInfo gathered, Texture3D<float4> cubes, float3 p, float3 n,
    float probes, float spacing, int c)
{
    float4 oc = gathered.cascades[c].origin_and_cell;
    float3 q = clamp((p - oc.xyz) / (oc.w * spacing) - 0.5, 0.0, probes - 1.0);
    int3 base = int3(floor(q));
    float3 f = q - float3(base);
    float3 n2 = n * n;
    float3 from = p + n * oc.w;
    float3 sum = float3(0.0);
    float weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 at = min(base + offset, int3(int(probes) - 1));
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float3 light = float3(0.0);
        float valid = 1.0;
        float3 moved = float3(0.0);
        for (int axis = 0; axis < 3; axis++)
        {
            int face = axis * 2 + (n[axis] < 0.0 ? 1 : 0);
            float4 texel = cubes.Load(int4(face * int(probes) + at.x, at.y, c * int(probes) + at.z, 0));
            light += texel.rgb * n2[axis];
            valid = min(valid, faceHolds(texel.a));
            moved[axis] = faceMove(texel.a);
        }
        float3 probe = oc.xyz + (float3(at) + 0.5 + moved) * (oc.w * spacing);
        if (valid < 0.5 || dot(probe - p, n) <= 0.0 || !seesThrough(field, linear, info, from, probe, oc.w * 0.5))
            continue;
        float weight = lerps.x * lerps.y * lerps.z * probeFacing(p, n, probe);
        sum += light * weight;
        weights += weight;
    }
    return float4(sum, weights);
}

/// The indirect light reaching a surface at `p` facing `n`, as `bouncedAt` gives it, from those of
/// the eight probes around it the surface sees, in front of its plane with nothing the field holds
/// between, so a probe under a floor or beyond a wall lends the light it sees there to no surface on
/// this side. The faces are read where the probes stood as `gatheredField` says, the frame before's
/// corners for the world's probes' rays, and the field marched as `info` has it.
public float3 bouncedSeenAt(Texture3D<float> field, SamplerState linear, FieldInfo info, FieldInfo gatheredField, Texture3D<float4> cubes, float3 p, float3 n,
    float probes, float spacing, float cascades)
{
    FieldInfo gathered = probesOf(gatheredField, int(cascades));
    int c = fieldCascade(gathered, p, 1.0);
    if (c < 0 || c >= int(cascades))
        return float3(0.0);
    float4 seen = bouncedSeenSum(field, linear, info, gathered, cubes, p, n, probes, spacing, c);
    return seen.w > 1e-5 ? seen.rgb / seen.w : float3(0.0);
}

/// The light the lamps that cast shadows would send a surface at `p` facing `n` but for what the
/// field holds between them, each found by a march toward the lamp from a cell and a half off the
/// surface, stopping two cells short of the lamp so its own fitting does not hide it.
public float3 hiddenLampLight(Texture3D<float> field, SamplerState linear, FieldInfo info, GiLights lights, float3 p, float3 n)
{
    float3 hidden = float3(0.0);
    float cell = fieldCell(info, p);
    for (int i = 0; i < min(int(lights.counts.x), 16); i++)
    {
        GiLight light = lights.lights[i];
        if (light.colorAndShadow.w < 0.5)
            continue;
        float result = lampLight(light, p, n);
        if (result > 0.0 && !seesThrough(field, linear, info, p + n * (cell * 1.5), light.positionAndKind.xyz, cell * 2.0))
            hidden += light.colorAndShadow.rgb * result;
    }
    return hidden;
}

/// The light a surface the field holds at `p` sends back along a probe's ray that met it, its color
/// times the light that reaches it from the sun and the lights, the lamps that cast shadows hidden
/// where the field stands between, so a lamp's light is not reflected off the far side of a wall,
/// and its color over pi times the indirect light reaching it from the probes it sees
/// (`bouncedSeenAt`), at `kept`'s share of it, from the faces where `gathered` says the probes
/// stood. `own` holds the light the surface sends on of what reaches it straight from the sun and
/// the lights, which a probe weighs the frame before's light by. The sun and the lights are given as
/// the light a white surface facing one returns, and the probes hold the light reaching a face, so
/// the indirect light is taken over pi.
public float3 shadeProbeHit(Texture3D<float> field, SamplerState linear, FieldInfo info, FieldInfo gathered, Texture3D<float4> albedo, Texture3D<uint> faces,
    GiLights lights, Texture3D<float4> cubes, float3 p, float probes, float spacing, float cascades, float3 kept, out float3 own)
{
    float3 n = fieldNormal(field, linear, info, p);
    float3 color = surfaceAlbedoFacing(faces, albedo, linear, info, p, n);
    float3 bounced = bouncedSeenAt(field, linear, info, gathered, cubes, p, n, probes, spacing, cascades);
    own = color * (directLight(field, linear, info, lights, p, n) - hiddenLampLight(field, linear, info, lights, p, n));
    return own + color / Pi * bounced * (lights.counts.z * kept);
}

/// The light a surface the field holds at `p` sends back along a ray that met it, its color over pi
/// times the light reaching it, the sun's and the lights' times pi, the lamps that cast shadows
/// hidden where the field stands between, and the indirect light that reached it the frame before,
/// read between the probes around it (`bouncedAt`).
public float3 shadeHit(Texture3D<float> field, SamplerState linear, FieldInfo info, Texture3D<float4> albedo, Texture3D<uint> faces, GiLights lights,
    Texture3D<float4> cubes, Texture3D<float> reach, float3 p, float probes, float spacing, float cascades)
{
    float3 n = fieldNormal(field, linear, info, p);
    float3 color = surfaceAlbedoFacing(faces, albedo, linear, info, p, n);
    float3 arrived = (directLight(field, linear, info, lights, p, n) - hiddenLampLight(field, linear, info, lights, p, n)) * Pi
        + bouncedAt(cubes, reach, info, p, n, probes, spacing, cascades) * lights.counts.z;
    return color / Pi * arrived;
}
