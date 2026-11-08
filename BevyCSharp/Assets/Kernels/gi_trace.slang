// Traces the rays of one cascade of light probes through the scene's distance field, a thread a
// ray. A cascade's probes lie every eight cells of the field's cascade of the same number, or of
// its far cascade for the last where it has one (probesOf), and each sends a ray along each texel of
// an octahedron of directions across its interval, from as far as the probes are apart to four times
// that, the last cascade's on to the field's edge, so the next cascade, twice as coarse, carries on
// from half way along it. A ray that meets a surface brings back the light the surface sends along
// it and stops there. One that meets nothing brings back nothing and lets what lies beyond through,
// but in the last cascade, where it brings back the light from all around. A probe inside a mesh
// sends nothing, which the merge reads as a probe to pass over. A probe that sees too little of the
// cascade above carries its rays on to the field's edge (carried, below).
//
// A ray is marched from its probe, since one marched from where its interval begins started under a
// floor its probe stood a little above and brought back the light under it. One that meets a
// surface before its interval begins brings back that surface's light, marked apart, alpha a half,
// which the probe's own faces take and the merge of the cascade below reads as dark.
//
// A probe nearer a surface than a tenth of its spacing, or inside a mesh, is moved off it along the
// field's normal before its rays leave, as DDGI moves its probes, so the probes in the plane of a
// wall or a ceiling stand in the room before it, and where it stands is left for the merge and the
// gather, which writes it into the probe's faces for every pass that reads them.
//
// A probe takes the indirect light reaching the surfaces its rays meet at the share the gather left
// it, other than the whole the frame after the light its rays bring straight from the sun and the
// lights fell by a fifth or rose by a quarter, so the indirect light from light that went out goes
// with it rather than over the frames each bounce takes to fade.
//
// Each probe's held light and the partial sums are buffers, which a dispatch here reads and writes
// at once where an image of half floats is only written, and the sum of the probes around one is
// taken by every workgroup and kept where its probe's share asks for it, since a barrier is reached
// by every invocation alike or by none.
import bcs_field;
import bcs_gi;
import bcs_glow;

Texture3D<float> field;
SamplerState field_sampler;
StructuredBuffer<FieldInfo> info;
Texture3D<float4> albedo;
Texture3D<uint> faces;
// The light each cell's surfaces emit, as field_resolve.slang writes it beside the colors.
Texture3D<float4> glow;
StructuredBuffer<GiLights> lights;
// The share of the sun's light the camera's air lets through toward it, as gi_sky.slang wrote it
// on the camera the frame before, since the probes are traced before the cameras draw and have no
// air of their own to read. One where no camera has written it.
StructuredBuffer<float4> air;
// The small emissive surfaces carried as lights of their own, as many as the lights' fourth count says.
StructuredBuffer<GlowLight> glow_lights;

// The camera's environment map's light along each way folded onto an octahedron, the frame before's,
// alpha 1 where the camera has one (gi_sky.slang).
Texture2D<float4> sky_map;

// The light from all around along `way` where a ray meets nothing, the ambient light's and the
// environment map's where the camera has one.
float3 skyAlong(GiLights lit, float3 way)
{
    float4 environment = sky_map.SampleLevel(field_sampler, octahedron(way), 0.0);
    return lit.sky.rgb + (environment.a > 0.5 ? environment.rgb : float3(0.0));
}

// What each texel's rays brought back, rgb the light of those that met a surface or the sky over how
// many it sends, and a what most of them met (texelMark). A texel a direction, each probe a tile of
// the octahedron's texels.
[format("rgba16f")] WTexture3D<float4> radiance;

// The indirect light reaching each probe the frame before, as gi_ambient.slang left it, for the
// light a surface sends on from it.
Texture3D<float4> bounced;
// The same faces' light at the surfaces they are read for, which a ray's hit takes the light it sends
// on from (bouncedSeenSum).
Texture3D<float4> bounced_at;
// Each probe's light along eight by eight directions the frame before, as gi_ambient.slang left it,
// which a ray's hit reflects by its gloss along the ray's mirror way (probeAlong).
Texture3D<float4> ways;

// How far each ray went before it met a surface, or its interval's end where it met none, below zero
// where it met a face from behind, a texel's rays taken together, which gi_ambient.slang counts to
// find a probe inside a solid.
[format("r16f")] RWTexture3D<float> distances;

// Every cascade's probes' reach, how far each of their rays went before a surface stopped it, or
// its interval's end where it met none, a texel a ray of sixteen by sixteen a probe (ReachTexels),
// the cascades one after another along z, which a surface is weighed by whether a probe sees it by.
// Each ray keeps its own, since four rays a texel taken together gave a texel of eight a side the
// reach of the one of them that passed beside a thin-walled room's corner, 5.8 where the wall stood
// at 2.6 along the texel's other ways, and a probe beyond the wall saw an eighth of the floor inside.
// A ray's reach stands for a cell of ways about 15 degrees wide, so an edge narrower than that seen
// from a probe can still let one cell read long.
[format("r16f")] RWTexture3D<float> reach;

// The share of light each ray kept across surfaces that let light through before its interval
// began, in w, which the probe's own faces take its light times (gi_ambient.slang), where the cascade
// below, whose interval that is, takes the light from the interval on whole, and in rgb the light
// those surfaces scattered along it, which the faces add, since the cascade below's ray gathers it
// for the merge and a probe beyond every cascade below, as one by a crown far from the eye, has
// nothing else to read it from.
[format("rgba16f")] WTexture3D<float4> kept;

// Each probe's own light as gi_ambient.slang left it, the frame before's and the one before that side
// by side, the share of the indirect light each probe takes, and where this trace moved each probe
// to, its move in its spacing with w 1 where it stands outside every mesh, every cascade, laid out as
// an image of four probes' widths by one by every cascade's probes.
RWStructuredBuffer<float4> held;

// What each workgroup of a probe's rays, 64 of them, brought back straight from the sun and the
// lights, summed, with in w the luminance of all they brought back, each ray weighed by the share of
// the sphere its texel spans, four workgroups a probe, laid out as held is.
RWStructuredBuffer<float4> partial;

// x: the cascade. y: the probes along each side. z: the octahedron's texels along each side. w: 1
// for the last cascade.
uniform uint4 cascade;
// x: where the interval starts. y: where it ends. z: the probes' spacing in cells. w: how many
// cascades.
uniform float4 interval;
// x: 1 where each probe's share holds for the probe, its cascade lying where it lay the frame
// before. y: the bits of the last cascade's interval's end, the field's edge, which a probe that
// sees too little of the cascade above carries its rays on to. z: the frame's parity. w: 1 where
// the field holds every mesh in reach, each of its cascades built and no triangle waiting.
uniform uint4 follow;

// The element of held and partial that holds column x, row y and layer z of their layout.
uint heldAt(int3 at)
{
    uint probes = cascade.y;
    return (uint(at.z) * probes + uint(at.y)) * 4u * probes + uint(at.x);
}

groupshared float4 place;
groupshared uint walledIn;
groupshared float4 probeShare;
groupshared float3 nearNow[32];
groupshared float3 nearBefore[32];
groupshared float4 brought[64];
// Each ray's light and mark, its own light with how far it went, below zero where it met a face from
// behind, the share of it that met nothing and the share it kept before its interval began, for the
// first ray of its texel to take together.
groupshared float4 rayLight[64];
groupshared float4 rayOwn[64];
groupshared float rayNothing[64];
groupshared float4 rayEarly[64];
// The weight each of the eight probes of the cascade above around this one carries, as the merge
// weighs it, where this probe sees it standing outside every mesh, and nothing elsewhere.
groupshared float aboveSeen[8];

// The field's layout with each cascade at the corner it lay at the frame before, where the faces of
// the indirect light, which the rays read, were gathered. Read at this frame's corners, a cascade
// that moved with the eye would read each probe's faces from the probe a step or more beside it, and
// the light past the first bounce would take many frames to settle again after a turn moved every
// cascade.
FieldInfo gatheredAt(FieldInfo fieldInfo, GiLights lit)
{
    int cascades = int(interval.w);
    FieldInfo then = probesOf(fieldInfo, cascades);
    for (int c = 0; c < min(cascades, MostProbeCascades); c++)
        then.cascades[c].origin_and_cell.xyz = lit.before[c].xyz;
    return then;
}

// Where the probe whose spot in the grid is at stands, moved off a surface nearer than Clearance of
// the spacing, or out of a mesh, along the field's normal, by MostMoved of the spacing at most, as
// its move in its spacing, with w 1 where it then stands outside every mesh.
float4 placed(FieldInfo fieldInfo, float3 at, float apart)
{
    float3 p = at;
    for (int step = 0; step < 4; step++)
    {
        float d = fieldDistance(field, field_sampler, fieldInfo, p);
        if (d >= Clearance * apart)
            break;
        p += fieldNormal(field, field_sampler, fieldInfo, p) * (Clearance * apart - d);
        float3 move = p - at;
        float length = length(move);
        if (length > MostMoved * apart)
            p = at + move * (MostMoved * apart / length);
    }
    // A crown's leaf cards run together where they lie closer than a cell and read below zero there,
    // as a solid's inside does, though light crosses them (lightThroughCrossed), so a probe in a cell
    // the cards painted stands outside unless it lies deeper than half a cell, as the crossing holds
    // a trunk's or a limb's inside.
    float d = fieldDistance(field, field_sampler, fieldInfo, p);
    bool cards = lightThroughCards(paintAlphaAt(albedo, fieldInfo, p)) && d >= -0.5 * fieldCell(fieldInfo, p);
    return float4((p - at) / apart, d >= 0.0 || cards ? 1.0 : 0.0);
}

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID, uint lane : SV_GroupIndex)
{
    uint probes = cascade.y;
    uint texels = cascade.z;
    // A thread a ray, a texel's rays (RaysAt) side by side in a workgroup, and a probe's 256 rays
    // four whole workgroups, so every workgroup is one probe's.
    uint each = RaysAt(texels);
    uint rays = texels * texels * each;
    uint probe = id.x / rays;
    bool live = probe < probes * probes * probes;
    uint index = id.x % rays;
    uint ray = index / each;
    uint quarter = index % each;
    uint3 at = uint3(probe % probes, (probe / probes) % probes, min(probe / (probes * probes), probes - 1u));
    uint2 texel = uint2(ray % texels, ray / texels);
    uint3 written = uint3(at.xy * texels + texel, at.z);
    int c = int(cascade.x);
    int z = c * int(probes) + int(at.z);

    FieldInfo fieldInfo = info[0];
    GiLights lit = lights[0];
    lit.sunColor.rgb *= air[0].rgb;
    float4 oc = probesOf(fieldInfo, int(interval.w)).cascades[c].origin_and_cell;
    float apart = oc.w * interval.z;
    float3 spot = oc.xyz + (float3(at) + 0.5) * apart;

    // The probe's share and its place, by the group's first ray, for every ray of it. One the gather
    // found inside a solid is left for the merge and the gather as one inside a mesh, its rays traced
    // all the same, so it is found again each frame.
    if (lane == 0u)
    {
        probeShare = follow.x != 0u ? held[heldAt(int3(2 * int(probes) + int(at.x), int(at.y), z))] : float4(1.0, 1.0, 1.0, 0.0);
        place = oc.w > 0.0 ? placed(fieldInfo, spot, apart) : float4(0.0);
        walledIn = place.w > 0.5 && follow.x != 0u && probeShare.w > 0.5 ? 1u : 0u;
        if (live)
            held[heldAt(int3(3 * int(probes) + int(at.x), int(at.y), z))] = walledIn != 0u ? float4(place.xyz, 0.0) : place;
    }

    // The share the own light of the 27 probes of the cascade within one of this one kept of the
    // frame before's, for a probe its own light lights too little of to judge by, so a room that goes
    // dark takes its walls' indirect light with it, and a lamp in a room far off, judged by every
    // probe's own light, does not dim or brighten the passages lit by indirect light alone with it.
    // Each of the group's first 27 rays reads one probe.
    uint parity = follow.z & 1u;
    if (lane < 32u)
    {
        float3 now = float3(0.0), before = float3(0.0);
        int3 near = int3(at) + int3(int(lane % 3u) - 1, int((lane / 3u) % 3u) - 1, int(lane / 9u) - 1);
        if (follow.x != 0u && lane < 27u && all(near >= int3(0)) && all(near < int3(int(probes))))
        {
            float4 last = held[heldAt(int3(int(parity ^ 1u) * int(probes) + near.x, near.y, c * int(probes) + near.z))];
            float4 earlier = held[heldAt(int3(int(parity) * int(probes) + near.x, near.y, c * int(probes) + near.z))];
            if (last.a >= 0.0 && earlier.a >= 0.0)
            {
                now = last.rgb;
                before = earlier.rgb;
            }
        }
        nearNow[lane] = now;
        nearBefore[lane] = before;
    }
    GroupMemoryBarrierWithGroupSync();
    for (uint half = 16u; half > 0u; half >>= 1u)
    {
        if (lane < half)
        {
            nearNow[lane] += nearNow[lane + half];
            nearBefore[lane] += nearBefore[lane + half];
        }
        GroupMemoryBarrierWithGroupSync();
    }

    float3 share = float3(1.0);
    if (follow.x != 0u)
    {
        if (probeShare.r >= 0.0)
            share = probeShare.rgb;
        else
        {
            float was = luminance(nearBefore[0]);
            share = was > 1e-4 ? select(nearBefore[0] > was * 1e-3, followed(nearNow[0], nearBefore[0]), float3(1.0)) : float3(1.0);
        }
    }

    float3 origin = spot + place.xyz * apart;

    // A probe below the last cascade that sees none of the eight probes of the cascade above around
    // it standing outside every mesh, or sees them for under a quarter of their weight, has no
    // probe to give it what lies past its interval, and its rays go on to the field's edge and
    // bring the light of what they meet there, the merge finding them met and giving them nothing
    // more. In the feature test's alley, 5.5 wide, cascade 2's probes stand 8 apart outside its
    // walls, each hidden from the alley's cascade 1 probes, so no light came from past 16 units,
    // neither the sky nor the alley's lit ends. Carried, a corridor 5.6 wide read its walls 1.045
    // and 1.010 of a path-traced reference's light past the direct where 0.700 and 0.651, and the
    // light hall 3 to 7% under its reference where 9 to 14% (GlobalIlluminationTests.Corridor.cs).
    // A carried ray that meets nothing brings the sky once the field holds every mesh in reach, and
    // nothing before, since the sky reached through cascades not yet built would light a closed
    // room in its first frames, and its reach is its interval's end where it meets nothing. The
    // share is a quarter, since a probe that sees more of the cascade above takes the light past
    // its interval better from those probes, and carried, its rays bring light that lands in the
    // wrong places.
    if (lane < 8u)
    {
        float weight = 0.0;
        if (cascade.w == 0u && oc.w > 0.0 && place.w > 0.5 && walledIn == 0u)
        {
            int upperCascade = c + 1;
            float4 ac = probesOf(fieldInfo, int(interval.w)).cascades[upperCascade].origin_and_cell;
            float upperApart = ac.w * interval.z;
            float3 q = (spot - ac.xyz) / upperApart - 0.5;
            int3 base = int3(floor(q));
            int3 offset = int3(int(lane & 1u), int((lane >> 1u) & 1u), int(lane >> 2u));
            int3 neighbor = clamp(base + offset, int3(0), int3(int(probes) - 1));
            float4 upperPlace = held[heldAt(int3(3 * int(probes) + neighbor.x, neighbor.y, upperCascade * int(probes) + neighbor.z))];
            float3 upperProbe = ac.xyz + (float3(neighbor) + 0.5 + upperPlace.xyz) * upperApart;
            float3 f = q - float3(base);
            float3 lerps = lerp(1.0 - f, f, float3(offset));
            if (ac.w > 0.0 && upperPlace.w > 0.5 && seesPastLeaves(field, field_sampler, albedo, fieldInfo, origin, upperProbe, oc.w * 0.5))
                weight = lerps.x * lerps.y * lerps.z;
        }
        aboveSeen[lane] = weight;
    }
    GroupMemoryBarrierWithGroupSync();
    float seenAbove = aboveSeen[0] + aboveSeen[1] + aboveSeen[2] + aboveSeen[3] + aboveSeen[4] + aboveSeen[5] + aboveSeen[6] + aboveSeen[7];
    bool carried = cascade.w == 0u && oc.w > 0.0 && place.w > 0.5 && walledIn == 0u && seenAbove < 0.25;
    float end = carried ? asfloat(follow.y) : interval.y;
    bool skyPast = cascade.w != 0u || carried && follow.w != 0u;

    float4 light = float4(0.0, 0.0, 0.0, 1.0);
    float3 own = float3(0.0);
    float reached = 0.0;
    bool behind = false;
    // The share of the ray's light still to come past the surfaces that let light through it
    // crossed, which a ray that met nothing hands on to the cascade above, and the share it kept
    // across those before its interval began, which its probe's own faces alone take.
    float through = 1.0;
    float keptBefore = 1.0;
    float3 earlyLight = float3(0.0);
    if (oc.w <= 0.0)
    {
    }
    else if (place.w < 0.5)
        light = float4(0.0);
    else
    {
        // A cascade of 8 texels a side sends a ray from the middle of each quarter of its texel, and
        // the texel takes their light together (below), so a lamp's bright spot under a ceiling
        // stands for a quarter of the texel it falls in rather than the whole of it.
        float2 within = each == 1u ? float2(0.5) : float2(float(quarter & 1u), float(quarter >> 1u)) * 0.5 + 0.25;
        float3 way = fromOctahedron((float2(texel) + within) / float(texels));
        float t = traceField(field, field_sampler, fieldInfo, origin, way, 0.0, end);
        reached = t >= 0.0 ? t : interval.y;
        // A ray that came on a face from behind it stops half a cell or more behind the face, having
        // stepped from a cell no face comes within the band of, which the field holds as the open,
        // straight into the cells behind it, where a ray from in front, slowing as it nears the
        // face, stops within a tenth of a cell of it.
        behind = t >= 0.0 && fieldDistance(field, field_sampler, fieldInfo, origin + way * t) < -0.5 * fieldCell(fieldInfo, origin + way * t);
#if defined(NO_LIGHT_THROUGH)
        // A frame where no surface lets light through traces with this build, whose ray stops at the
        // first surface it meets and takes its light, as it did before any surface let light
        // through, so such a frame's light is that light to the bit.
        if (t >= 0.0)
        {
            light = float4(shadeProbeHit(field, field_sampler, fieldInfo, gatheredAt(fieldInfo, lit), albedo, faces, glow, lit, glow_lights,
                bounced, bounced_at, ways, origin + way * t, way, float(probes), interval.z, interval.w, share, own), t < interval.x ? 0.5 : 0.0);
            through = 0.0;
        }
        else if (skyPast)
        {
            float3 sky = skyAlong(lit, way);
            light = float4(sky, 0.25);
            own = sky;
            through = 0.0;
        }
#else
        // A surface that lets a share of light through, as a canopy's leaves, is crossed, the ray
        // keeping that share for each unit of its depth and the rest taking the surface's light as
        // a solid's would, up to four of them before the ray stops at the next as a solid. One
        // crossed before the interval began is the cascade below's, whose ray takes this one's
        // light from the interval on whole where it met nothing, so its light and its share go to
        // the probe's own faces alone (kept), since a probe past every cascade below, as one by a
        // crown far from the eye, has nothing else to read them from, and taking only its share, a
        // probe in an oak's front half read its ways into the crown 63% under its reference. A
        // solid surface is shaded in the same place as a crossed one, keeping none, so the build
        // holds one copy of the shading.
        float3 crossed = float3(0.0), crossedOwn = float3(0.0);
        float firstAt = -1.0;
        [loop]
        for (int layer = 0; layer < 5 && t >= 0.0; layer++)
        {
            float3 at = origin + way * t;
            float passing = layer < 4 ? lightThroughAt(albedo, fieldInfo, at) : 0.0;
            bool inside = false;
            float passes = 0.0;
            float stopped = 0.0;
            float depth = passing > 0.0 ? lightThroughCrossedAt(field, field_sampler, albedo, fieldInfo, origin, way, t, end - t, inside, passes, stopped) : -1.0;
            // The light the surface sends along the ray of all it stops, lit where it stops it on
            // average, so a crown's cards are lit where their leaves stop the ray and not where the
            // ray met the crown, whose sunlit edge taken for all a crown stops read a probe's ways
            // into a front-lit oak 118 to 160% over its reference, and any other surface where the
            // ray met it. Lit at four places, where it lost each quarter of what it stops, the trace
            // took 9.0 to 12.2 ms in the meadow at High where 3.5 to 3.6, for no nearer a reading.
            float3 itsOwn;
            float lost = 1.0 - passes;
            float3 itsLight = shadeProbeHit(field, field_sampler, fieldInfo, gatheredAt(fieldInfo, lit), albedo, faces, glow, lit, glow_lights,
                bounced, bounced_at, ways, origin + way * (t + stopped), way, float(probes), interval.z, interval.w, share, itsOwn) * lost;
            itsOwn *= lost;
            if (depth >= 0.0 && t < interval.x)
            {
                earlyLight += itsLight * keptBefore;
                keptBefore *= passes;
            }
            else
            {
                crossed += itsLight * through;
                crossedOwn += itsOwn * through;
                through *= passes;
                firstAt = firstAt < 0.0 ? t : firstAt;
            }
            if (depth < 0.0)
                break;
            // A ray still within at its interval's end met nothing past it, its share handed on.
            t = inside ? -1.0 : traceField(field, field_sampler, fieldInfo, origin, way, t + depth, end);
        }
        // How far it went is where it stopped, past what it crossed, which the screen's probes read
        // the cascades' light beyond their reach by.
        reached = t >= 0.0 ? t : interval.y;
        // Marked by what the ray met first that counts, a surface crossed in its interval or one it
        // stopped at, before its interval began or in it.
        float mark = firstAt >= 0.0 ? (firstAt < interval.x ? 0.5 : 0.0) : 1.0;
        if (t < 0.0 && skyPast)
        {
            float3 sky = skyAlong(lit, way);
            light = float4(crossed + through * sky, firstAt >= 0.0 ? mark : 0.25);
            own = crossedOwn + through * sky;
            through = 0.0;
        }
        else if (firstAt >= 0.0)
        {
            // A surface it stopped at, or those it crossed with nothing past them, the share of the
            // light it kept left for the cascade above to give.
            light = float4(crossed, mark);
            own = crossedOwn;
        }
#endif
    }
    if (walledIn != 0u)
    {
        light = float4(0.0);
        own = float3(0.0);
        earlyLight = float3(0.0);
    }

    // A ray that met nothing at all, and one that crossed surfaces letting light through and met
    // nothing past them, hand on the share they kept.
    float nothingShare = light.a >= 0.75 ? 1.0 : place.w < 0.5 || walledIn != 0u ? 0.0 : through;

    // A texel's rays taken together by its first, the light of those that met a surface or the sky
    // over how many there are, marked with what most met and how many met nothing (texelMark), their
    // own light and their reach their means, and a face met from behind where most met one so.
    rayLight[lane] = light;
    rayOwn[lane] = float4(own, behind ? -reached : reached);
    rayNothing[lane] = nothingShare;
    uint2 itsTexel = texel * (uint(ReachTexels) / texels) + (each == 4u ? uint2(quarter & 1u, quarter >> 1u) : uint2(0u));
    if (live)
        reach[uint3(at.xy * uint(ReachTexels) + itsTexel, uint(c) * probes + at.z)] = reached;
    rayEarly[lane] = float4(earlyLight, keptBefore);
    GroupMemoryBarrierWithGroupSync();
    float4 texelLight = float4(0.0);
    float3 texelOwn = float3(0.0);
    if (quarter == 0u)
    {
        float3 met = float3(0.0);
        float texelReach = 0.0;
        // How many met a surface, one before their interval began and the sky, and the share that
        // met nothing, fractions of a ray where one crossed surfaces letting light through.
        uint surfaces = 0u, early = 0u, skies = 0u, behinds = 0u;
        float nothing = 0.0;
        float4 before = float4(0.0);
        for (uint q = 0u; q < each; q++)
        {
            float4 one = rayLight[lane + q];
            float4 itsOwn = rayOwn[lane + q];
            surfaces += one.a < 0.125 ? 1u : 0u;
            skies += one.a >= 0.125 && one.a < 0.375 ? 1u : 0u;
            early += one.a >= 0.375 && one.a < 0.75 ? 1u : 0u;
            nothing += rayNothing[lane + q];
            before += rayEarly[lane + q];
            met += one.a < 0.75 ? one.rgb : float3(0.0);
            texelOwn += itsOwn.rgb;
            texelReach += abs(itsOwn.w);
            behinds += itsOwn.w < 0.0 ? 1u : 0u;
        }
        float most = nothing * 2.0 > float(each) ? 1.0 : early > surfaces && early >= skies ? 0.5 : skies > surfaces ? 0.25 : 0.0;
        texelLight = float4(met / float(each), texelMark(most, nothing, each));
        texelOwn /= float(each);
        texelReach /= float(each);
        if (live)
        {
            radiance.Store(written, texelLight);
            distances[written] = behinds * 2u > each ? -texelReach : texelReach;
            kept.Store(written, before / float(each));
        }
    }

    // The group's own light and all it brought back summed, each ray weighed by the share of the
    // sphere its texel spans, as the gather weighs them, halving the rays that add each step.
    brought[lane] = quarter == 0u ? float4(texelOwn, luminance(texelLight.rgb)) * octahedronShare((float2(texel) + 0.5) / float(texels)) : float4(0.0);
    GroupMemoryBarrierWithGroupSync();
    for (uint half = 32u; half > 0u; half >>= 1u)
    {
        if (lane < half)
            brought[lane] += brought[lane + half];
        GroupMemoryBarrierWithGroupSync();
    }
    if (lane == 0u && live)
        partial[heldAt(int3(int(at.x) * 4 + int(index / 64u), int(at.y), z))] = brought[0];
}
