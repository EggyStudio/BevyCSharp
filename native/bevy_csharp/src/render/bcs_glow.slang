// The small emissive surfaces global illumination carries as lights of their own, each a box in
// the world, the light one sends a point in closed form, and the marches through the scene's
// distance field that say how much of it a point sees, which any Slang shader can import as
// `bcs_glow` (GlowLights.cs on the managed side writes them).
//
// Taken from 3DEngine's glow.slang and the glow lights' sum of its gi.slang, the field read
// through a texture and a sampler where 3DEngine reads it through a volume, and the lights in a
// buffer of their own, counted in the fourth of `GiLights.counts`, where 3DEngine keeps them in its
// lights' buffer. The rough light the screen's probes' texels are scaled by comes with the screen's
// probes. 3DEngine keeps its loops from unrolling, since unrolled, with each caller's copies
// inlined apart, they made its model pass compile four times as long on lavapipe. Here a loop over a
// small array is unrolled, so the array is indexed by constants, and a square is clipped an edge at
// a time with no array of corners, since with the arrays indexed as the loops ran the closed form
// alone took global illumination's pass from 0.19 ms to 2.9 at 1280 by 720 in a Cornell box with
// one light on the development machine's GPU, and unrolled 0.29. The marches' loops stay rolled.

module bcs_glow;

import bcs_field;

/// How many of the small emissive surfaces global illumination carries as lights at most, the
/// nearest the eye.
public static const int MostGlowLights = 64;

/// A small emissive surface, carried through global illumination as a light of its own rather than
/// by the rays that meet it, since few of a probe's rays meet a small one and those that do take it
/// for a whole texel's worth of the sky, and the probes' faces above a floor do not see a lamp lying
/// on it.
public struct GlowLight
{
    /// xyz: the middle of the box around it. w: how far its light reaches, past which it gives none.
    public float4 middleAndReach;
    /// xyz: half the box's side along each of the box's own axes, in the world. w: its length.
    public float4 halfX;
    public float4 halfY;
    public float4 halfZ;
    /// rgb: its surface's emission, linear, in the picture's units. w: its mesh's surface over that of
    /// the faces of its box that emit, below 2, and twice the faces that emit above it, a bit a face,
    /// as glowShare and glowFaces read them.
    public float4 glowAndShare;
};

/// A glow light's surface over that of its box's faces that emit.
public float glowShare(GlowLight light)
{
    float w = light.glowAndShare.w;
    return w - 2.0 * floor(w * 0.5);
}

/// Which faces of a glow light's box emit, a bit a face, two an axis of halfX, halfY and halfZ, the
/// side the axis points to the higher, every face where the light names none, as a block showing
/// all six does.
public uint glowFaces(GlowLight light)
{
    uint faces = uint(floor(light.glowAndShare.w * 0.5));
    return faces == 0u ? 63u : faces;
}

// The arc from a to b, both of length one, times the way the plane through them faces, over two
// pi, one edge's term of Lambert's sum for the light a polygon sends a point, by Heitz's fit of the
// arc over two pi times its sine, which keeps the short edges of a far square that acos loses near
// 1.
float3 edgeIntegral(float3 a, float3 b)
{
    float x = dot(a, b);
    float y = abs(x);
    float u = 0.8543985 + (0.4965155 + 0.0145206 * y) * y;
    float v = 3.4175940 + (4.1616724 + y) * y;
    float ratio = u / v;
    float arcOverSine = x > 0.0 ? ratio : 0.5 * rsqrt(max(1.0 - x * x, 1e-7)) - ratio;
    return cross(a, b) * arcOverSine;
}

// One edge's term of Lambert's sum from a to b, neither normalized, or none where either is the
// point itself.
float3 edgeTerm(float3 a, float3 b)
{
    return dot(a, a) > 1e-12 && dot(b, b) > 1e-12 ? edgeIntegral(normalize(a), normalize(b)) : float3(0.0);
}

// The light reaching a point x facing n from a square that emits one unit of light, its middle c
// and its half sides u and v, of the part of it above x's plane, by Lambert's sum over the edges of
// that part, the square clipped where it crosses the plane, each edge taken whole above the plane,
// from or to where it crosses it, and the edge the plane cuts along from where the square left
// the side above to where it came back.
float squareIrradiance(float3 x, float3 n, float3 c, float3 u, float3 v)
{
    float3 corners[4] = { c + u + v - x, c - u + v - x, c - u - v - x, c + u - v - x };
    float3 sum = float3(0.0);
    float3 leaving = float3(0.0), entering = float3(0.0);
    bool left = false, came = false;
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        float3 a = corners[i], b = corners[(i + 1) & 3];
        float above = dot(a, n), next = dot(b, n);
        if (above >= 0.0 && next >= 0.0)
            sum += edgeTerm(a, b);
        else if (above >= 0.0)
        {
            float3 cut = a + (b - a) * (above / (above - next));
            sum += edgeTerm(a, cut);
            leaving = cut;
            left = true;
        }
        else if (next >= 0.0)
        {
            float3 cut = a + (b - a) * (above / (above - next));
            sum += edgeTerm(cut, b);
            entering = cut;
            came = true;
        }
    }
    if (left && came)
        sum += edgeTerm(leaving, entering);
    return 3.14159265358979 * abs(dot(sum, n));
}

/// How far q lies from a glow light's box, 0 inside it.
public float glowLightDistance(GlowLight light, float3 q)
{
    float3 offset = q - light.middleAndReach.xyz;
    float3 beyond = float3(
        max(abs(dot(offset, light.halfX.xyz)) / max(light.halfX.w, 1e-6) - light.halfX.w, 0.0),
        max(abs(dot(offset, light.halfY.xyz)) / max(light.halfY.w, 1e-6) - light.halfY.w, 0.0),
        max(abs(dot(offset, light.halfZ.xyz)) / max(light.halfZ.w, 1e-6) - light.halfZ.w, 0.0));
    return length(beyond);
}

/// Whether p, facing n, lies on a glow light's own surface, within margin of its box with n leading
/// out of it, so that a step of margin along n moves p away from the box by half the step or more.
/// A ray's hit on the light lies anywhere within a fraction of a cell of the box, inside or out, and
/// the field's normal tilts near its edges, so part of a face rose above the hit's plane and lit
/// it. A floor beside the box, n along the box's side, is not on it.
public bool glowLightOn(GlowLight light, float3 p, float3 n, float margin)
{
    float at = glowLightDistance(light, p);
    return at < margin && glowLightDistance(light, p + n * margin) >= at + margin * 0.5;
}

/// The light reaching a point x facing n from a glow light, unhidden, the light from each face of
/// its box that x lies before, three at most, in closed form within four of the box's half
/// diagonals, which a path-traced floor beside an emissive block reads to within 3% in 3DEngine from
/// one block out to four, and past them each face's as points, its area by the two cosines over the
/// square of its distance, the face cut along its longer side into pieces about as long as its
/// shorter. None where x lies on the light's own surface, as glowLightOn finds it within margin.
public float3 glowLightIrradiance(GlowLight light, float3 x, float3 n, float margin)
{
    if (glowLightOn(light, x, n, margin))
        return float3(0.0);
    float3 middle = light.middleAndReach.xyz;
    float4 halves[3] = { light.halfX, light.halfY, light.halfZ };
    float3 offset = x - middle;
    bool near = dot(offset, offset) < 16.0 * (halves[0].w * halves[0].w + halves[1].w * halves[1].w + halves[2].w * halves[2].w);
    uint faces = glowFaces(light);
    float sum = 0.0;
    [unroll]
    for (int k = 0; k < 3; k++)
    {
        float3 h = halves[k].xyz;
        float side = dot(offset, h);
        float squared = halves[k].w * halves[k].w;
        if (squared < 1e-12 || abs(side) <= squared || (faces & (1u << uint(2 * k + (side > 0.0 ? 1 : 0)))) == 0u)
            continue;
        float3 face = middle + (side > 0.0 ? h : -h);
        float4 u = halves[(k + 1) % 3], v = halves[(k + 2) % 3];
        if (near)
            sum += squareIrradiance(x, n, face, u.xyz, v.xyz);
        else
        {
            bool longer = u.w >= v.w;
            float3 along = longer ? u.xyz : v.xyz, across = longer ? v.xyz : u.xyz;
            int pieces = clamp(int(max(u.w, v.w) / max(min(u.w, v.w), 1e-6) + 0.5), 1, 4);
            int points = pieces == 1 ? 4 : pieces;
            float area = 4.0 * u.w * v.w / float(points);
            [unroll]
            for (int piece = 0; piece < 4; piece++)
                if (piece < points)
                {
                    float3 at = pieces == 1
                        ? face + along * ((piece & 1) != 0 ? 0.5 : -0.5) + across * ((piece & 2) != 0 ? 0.5 : -0.5)
                        : face + along * ((2.0 * float(piece) + 1.0) / float(pieces) - 1.0);
                    float3 toward = at - x;
                    float distance2 = dot(toward, toward);
                    float3 way = toward * rsqrt(distance2);
                    sum += area * abs(dot(way, h)) / halves[k].w * max(dot(way, n), 0.0) / distance2;
                }
        }
    }
    return light.glowAndShare.rgb * (sum * glowShare(light));
}

/// How far along the way from `from` its line enters a glow light's box, 0 where it starts inside.
public float glowLightEntry(GlowLight light, float3 from, float3 way)
{
    float3 offset = from - light.middleAndReach.xyz;
    float3 halves[3] = { light.halfX.xyz, light.halfY.xyz, light.halfZ.xyz };
    float enter = 0.0, leave = 1e6;
    [unroll]
    for (int k = 0; k < 3; k++)
    {
        float squared = max(dot(halves[k], halves[k]), 1e-12);
        float at = dot(offset, halves[k]) / squared, speed = dot(way, halves[k]) / squared;
        if (abs(speed) < 1e-8)
            continue;
        float a = (-1.0 - at) / speed, b = (1.0 - at) / speed;
        enter = max(enter, min(a, b));
        leave = min(leave, max(a, b));
    }
    return enter <= leave ? enter : 0.0;
}

/// How much of a glow light's light reaches a surface at p facing n by its reach, fading to none at
/// it, or 0 where it lies past its reach or behind the surface.
public float glowLightFade(GlowLight light, float3 p, float3 n)
{
    float3 toward = light.middleAndReach.xyz - p;
    float reach = light.middleAndReach.w;
    float distance2 = dot(toward, toward);
    if (distance2 >= reach * reach || dot(toward, n) <= -light.halfX.w - light.halfY.w - light.halfZ.w)
        return 0.0;
    float edge = saturate(1.0 - pow(distance2 / (reach * reach), 2.0));
    return edge * edge;
}

/// Whether a ray along `way` that met the field at p passed through a glow light's box within
/// `within` before it, as a ray passes a light thinner than a cell, which the field holds about its
/// middle, and meets the wall behind it, where the light reaches it from no distance. Such a hit
/// takes none of that light, as a path-traced reference's path stops at the light and leaves its
/// emission to the light's own count at each surface.
public bool glowLightPassed(GlowLight light, float3 p, float3 way, float within)
{
    float3 offset = p - light.middleAndReach.xyz;
    float4 halves[3] = { light.halfX, light.halfY, light.halfZ };
    float enter = 0.0, leave = within;
    [unroll]
    for (int k = 0; k < 3; k++)
    {
        float side = halves[k].w;
        float3 axis = halves[k].xyz / max(side, 1e-6);
        float o = dot(offset, axis), d = -dot(way, axis);
        if (abs(d) < 1e-6)
        {
            if (abs(o) > side)
                return false;
            continue;
        }
        float t0 = (-side - o) / d, t1 = (side - o) / d;
        enter = max(enter, min(t0, t1));
        leave = min(leave, max(t0, t1));
    }
    return enter <= leave;
}

// How much of a glow light's point `target` a surface at p facing n, in cells of `cell`, sees past
// what the field holds between, from 0 to 1, by a march through the field from `lift` cells off the
// surface toward the target, stopping a cell short of it, at each step the way's clearance from the
// nearest surface against the cone of half-width `radius` at the target seen from p, or, where the
// radius is below zero, the cone across the box toward the surface the step passes, so a block that
// covers half the cone takes half the light and a shadow's edge is as soft as what is seen is wide
// and as near the block lies to the light. A surface counts only where it lies nearer than the
// plane p lies on and the light's own box. Between two steps the way's nearest pass is found as a
// sphere tracer's is, from the two distances. Inside a surface the share is half and less by how far
// under its edge the way passes, read once after the march by stepping out along p's normal and
// across the field's slope from the step that lay deepest inside, since read at each step it lost
// the device on 3DEngine's development GPU. Where nothing stands nearer, or the field does not hold
// p, all of it.
float glowMarch(Texture3D<float> field, SamplerState linear, FieldInfo info, GlowLight light, float3 p, float3 n, float cell, float3 target,
    float radius, float lift)
{
    float3 from = p + n * (cell * lift);
    float3 toward = target - from;
    float total = max(length(toward), 1e-6);
    float3 way = toward / total;
    float end = total - cell;
    float seen = 1.0;
    float before = 1e20;
    float t = cell * 0.5;
    float deepest = 0.0;
    float3 deepAt = float3(0.0);
    int deepIn = 0;
    float deepCone = 1.0;
    [loop]
    for (int i = 0; i < 64 && t < end; i++)
    {
        float3 q = from + way * t;
        int c = fieldCascade(info, q, 1.0);
        if (c < 0)
            break;
        float step = info.cascades[c].origin_and_cell.w;
        float d = fieldDistanceIn(field, linear, info, c, q);
        float height = dot(q - p, n);
        if (d < min(height, glowLightDistance(light, q)) - step * 0.5)
        {
            float y = d > 0.0 ? d * d / (2.0 * before) : 0.0;
            // A radius below zero takes the cone as wide as the box across the way toward the
            // surface the step passes, the box's support along the field's slope there, read by
            // forward differences.
            float wide = radius;
            if (wide < 0.0)
            {
                float e = step * 0.5;
                float3 slope = float3(fieldDistanceIn(field, linear, info, c, q + float3(e, 0, 0)), fieldDistanceIn(field, linear, info, c, q + float3(0, e, 0)),
                    fieldDistanceIn(field, linear, info, c, q + float3(0, 0, e))) - d;
                float3 across = slope - way * dot(slope, way);
                float acrossLength = length(across);
                across = acrossLength > 1e-4 ? across / acrossLength : slope;
                wide = abs(dot(across, light.halfX.xyz)) + abs(dot(across, light.halfY.xyz)) + abs(dot(across, light.halfZ.xyz));
            }
            float cone = max(min(wide * max(t - y, 0.0) / total, height), step * 0.25);
            if (d > 0.0)
                seen = min(seen, saturate(0.5 + 0.5 * sqrt(max(d * d - y * y, 0.0)) / cone));
            else
            {
                // It ends by its own condition, `t` set to its end, since left by a break there the
                // march lost the device on 3DEngine's development GPU.
                seen = min(seen, saturate(0.5 + 0.5 * d / cone));
                if (seen <= 0.0)
                    t = end;
                else if (d < deepest)
                {
                    deepest = d;
                    deepAt = q;
                    deepIn = c;
                    deepCone = cone;
                }
            }
        }
        before = max(d, 1e-4);
        t += max(d, step * 0.5);
    }
    if (deepest < 0.0 && seen > 0.0)
    {
        float step = info.cascades[deepIn].origin_and_cell.w;
        float e = step * 0.5;
        float3 slope = float3(
            fieldDistanceIn(field, linear, info, deepIn, deepAt + float3(e, 0, 0)) - fieldDistanceIn(field, linear, info, deepIn, deepAt - float3(e, 0, 0)),
            fieldDistanceIn(field, linear, info, deepIn, deepAt + float3(0, e, 0)) - fieldDistanceIn(field, linear, info, deepIn, deepAt - float3(0, e, 0)),
            fieldDistanceIn(field, linear, info, deepIn, deepAt + float3(0, 0, e)) - fieldDistanceIn(field, linear, info, deepIn, deepAt - float3(0, 0, e)));
        float3 across = slope - way * dot(slope, way);
        float acrossLength = length(across);
        float3 aside = acrossLength > 0.5 * length(slope) && acrossLength > 1e-6 ? across / acrossLength : n;
        float under = 0.0, beside = 0.0;
        bool outUnder = false, outBeside = false;
        [loop]
        for (int out = 0; out < 8 && !(outUnder && outBeside) && min(under, beside) < deepCone; out++)
        {
            if (!outUnder)
            {
                float inside = fieldDistanceIn(field, linear, info, deepIn, deepAt + n * under);
                if (inside >= 0.0)
                {
                    outUnder = true;
                    under = max(under - inside, 0.0);
                }
                else
                    under += max(-inside, step * 0.5);
            }
            if (!outBeside)
            {
                float inside = fieldDistanceIn(field, linear, info, deepIn, deepAt + aside * beside);
                if (inside >= 0.0)
                {
                    outBeside = true;
                    beside = max(beside - inside, 0.0);
                }
                else
                    beside += max(-inside, step * 0.5);
            }
        }
        seen = min(seen, saturate(0.5 - 0.5 * min(under, beside) / deepCone));
    }
    return seen;
}

/// How much of a glow light a surface at p facing n, in cells of `cell`, sees, from 0 to 1, by one
/// march from a cell and a half off the surface to where the way enters its box. Where the field
/// does not hold p, all of it.
public float glowLightSeen(Texture3D<float> field, SamplerState linear, FieldInfo info, GlowLight light, float3 p, float3 n, float cell)
{
    if (cell <= 0.0)
        return 1.0;
    float3 from = p + n * (cell * 1.5);
    float3 toward = light.middleAndReach.xyz - from;
    float total = max(length(toward), 1e-6);
    float entry = glowLightEntry(light, from, toward / total);
    if (entry <= cell * 2.0)
        return 1.0;
    float seen = glowMarch(field, linear, info, light, p, n, cell, from + toward / total * entry, -1.0, 1.5);
    return lerp(1.0, seen, fieldHold(info, p));
}

/// How much of a glow light's light a surface at p facing n, in cells of `cell`, sees, from 0 to 1,
/// by each face of its box p lies before, two or three, halved across the face's axis that rises
/// most from p's surface, and a march from a tenth of a cell off the surface to the middle of each
/// half, the cone the half's own rise, each half's share by its own light at p. Past a wall half as
/// high as a lamp beside it the floor read a path-traced reference to within a tenth of its light in
/// 3DEngine, where one march to the box's middle found the lamp hidden from the whole floor past the
/// wall. Where the field does not hold p, all of it.
public float glowLightShown(Texture3D<float> field, SamplerState linear, FieldInfo info, GlowLight light, float3 p, float3 n, float cell)
{
    if (cell <= 0.0)
        return 1.0;
    float3 middle = light.middleAndReach.xyz;
    float4 halves[3] = { light.halfX, light.halfY, light.halfZ };
    float3 offset = p - middle;
    float lit = 0.0, seen = 0.0;
    [unroll]
    for (int k = 0; k < 3; k++)
    {
        float3 h = halves[k].xyz;
        float side = dot(offset, h);
        float squared = halves[k].w * halves[k].w;
        if (squared < 1e-12 || abs(side) <= squared)
            continue;
        float3 face = middle + (side > 0.0 ? h : -h);
        float4 u = halves[(k + 1) % 3], v = halves[(k + 2) % 3];
        // The face's axis that rises most from p's surface is halved, the other kept whole, and the
        // longer where neither rises more, as from a floor under a level face.
        float riseU = abs(dot(u.xyz, n)) * v.w, riseV = abs(dot(v.xyz, n)) * u.w;
        bool alongU = abs(riseU - riseV) > 0.05 * u.w * v.w ? riseU > riseV : u.w >= v.w;
        float3 rise = alongU ? u.xyz : v.xyz;
        float radius = (alongU ? u.w : v.w) * 0.5;
        [loop]
        for (int half = 0; half < 2; half++)
        {
            float3 target = face + rise * (half == 0 ? 0.5 : -0.5);
            float3 toward = target - p;
            float distance2 = max(dot(toward, toward), 1e-8);
            float3 way = toward * rsqrt(distance2);
            // Each half by its own light as a point, its area by the two cosines over the square of
            // its distance.
            float given = abs(dot(way, h)) / halves[k].w * u.w * v.w * max(dot(way, n), 0.0) / distance2;
            if (given <= 0.0)
                continue;
            lit += given;
            seen += given * glowMarch(field, linear, info, light, p, n, cell, target, radius, 0.1);
        }
    }
    return lit > 0.0 ? lerp(1.0, seen / lit, fieldHold(info, p)) : 1.0;
}

/// The light reaching a surface at p facing n from the glow lights, irradiance, each light's in
/// closed form faded by its reach, and the `marches` brightest of them, four at most, by how much of
/// each the surface sees through the field, by the halves of its faces where `halves` asks
/// (glowLightShown) and by its middle otherwise (glowLightSeen), the others left out. At a probe's ray's
/// hit, the ray along `way` (zero elsewhere), a light the ray passed through right before it met the
/// field gives the hit none of its light (glowLightPassed), and a hit within a cell of a light's box
/// takes the light as its mean over a square a cell wide about the hit on its plane, since a light
/// thinner than a cell lights the wall it stands against within a few hundredths of its box, a patch
/// one probe's ray meets and the next probe's misses.
public float3 glowLightsAt(Texture3D<float> field, SamplerState linear, FieldInfo info, StructuredBuffer<GlowLight> glows, uint lights,
    float3 p, float3 n, int marches, bool halves, float3 way)
{
    int count = min(int(lights), MostGlowLights);
    float cell = fieldCell(info, p);
    float margin = max(cell, 0.0) * 0.5;
    float3 kept[4] = { float3(0.0), float3(0.0), float3(0.0), float3(0.0) };
    float bright[4] = { 0.0, 0.0, 0.0, 0.0 };
    int which[4] = { -1, -1, -1, -1 };
    int most = min(marches, 4);
    [loop]
    for (int i = 0; i < count; i++)
    {
        GlowLight light = glows[i];
        float faded = glowLightFade(light, p, n);
        if (faded <= 0.0 || (dot(way, way) > 0.0 && glowLightPassed(light, p, way, 2.0 * margin)))
            continue;
        float3 arrived;
        if (dot(way, way) > 0.0 && cell > 0.0 && glowLightDistance(light, p) < cell)
        {
            if (glowLightOn(light, p, n, margin))
                continue;
            float3 along = normalize(abs(n.y) < 0.9 ? cross(n, float3(0.0, 1.0, 0.0)) : cross(n, float3(1.0, 0.0, 0.0)));
            float3 across = cross(n, along);
            float q = cell * 0.25;
            arrived = (glowLightIrradiance(light, p + (along + across) * q, n, margin) + glowLightIrradiance(light, p + (along - across) * q, n, margin)
                + glowLightIrradiance(light, p - (along + across) * q, n, margin) + glowLightIrradiance(light, p - (along - across) * q, n, margin))
                * (0.25 * faded);
        }
        else
            arrived = glowLightIrradiance(light, p, n, margin) * faded;
        float b = dot(arrived, float3(0.2126, 0.7152, 0.0722));
        int one = i;
        [unroll]
        for (int k = 0; k < 4; k++)
            if (k < most && b > bright[k])
            {
                float3 movedLight = kept[k];
                float movedBright = bright[k];
                int movedOne = which[k];
                kept[k] = arrived;
                bright[k] = b;
                which[k] = one;
                arrived = movedLight;
                b = movedBright;
                one = movedOne;
            }
    }
    float3 sum = float3(0.0);
    [unroll]
    for (int k = 0; k < 4; k++)
        if (k < most && which[k] >= 0)
        {
            GlowLight light = glows[clamp(which[k], 0, MostGlowLights - 1)];
            sum += kept[k] * (halves ? glowLightShown(field, linear, info, light, p, n, cell) : glowLightSeen(field, linear, info, light, p, n, cell));
        }
    return sum;
}

/// The light the field's surfaces emit at `p` as surfaceGlow blends it, less that of the surfaces
/// global illumination carries as glow lights, whose cells say so in the glow's alpha, so a probe's
/// ray that meets one does not take its light a second time. Near such a cell the eight cells
/// around `p` are blended by hand, each carried one's light left out, since the blend of them all
/// mixes the two.
public float3 surfaceGlowUncarried(Texture3D<float4> glow, Texture3D<float4> albedo, SamplerState linear, FieldInfo info, float3 p)
{
    int c = fieldCascade(info, p, 1.0);
    if (c < 0)
        return float3(0.0);
    float4 oc = info.cascades[c].origin_and_cell;
    float size = info.counts.y;
    float3 cell = clamp((p - oc.xyz) / oc.w, 0.5, size - 0.5);
    float3 uvw = float3(cell.xy / size, (cell.z + float(c) * size) / (size * info.counts.x));
    float coverage = albedo.SampleLevel(linear, uvw, 0.0).a;
    if (coverage <= 0.05)
        return float3(0.0);
    float4 light = glow.SampleLevel(linear, uvw, 0.0);
    if (light.a <= 0.0)
        return light.rgb / coverage;
    float3 q = cell - 0.5;
    int3 base = int3(floor(q));
    float3 f = q - float3(base);
    float3 sum = float3(0.0);
    [loop]
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 at = min(base + offset, int3(int(size) - 1));
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float4 one = glow.Load(int4(at.xy, at.z + c * int(size), 0));
        sum += (one.a > 0.5 ? float3(0.0) : one.rgb) * (lerps.x * lerps.y * lerps.z);
    }
    return sum / coverage;
}
