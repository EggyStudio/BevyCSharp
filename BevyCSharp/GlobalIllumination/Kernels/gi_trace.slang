// Traces the rays of one cascade of light probes through the scene's distance field, a thread a
// ray, each ray as the RadianceCascades module traces it (traceProbeRay), and keeps it for the
// merge (gi_merge.slang). A cascade's probes lie every eight cells of the field's cascade of the
// same number, or of its far cascade for the last where it has one (probesOf), and each sends a ray
// along each texel of a 16 by 16 octahedron of ways from where it stands to its cascade's end,
// twice the probes' spacing, the last cascade's the field's edge, the module's ends. A cascade that
// keeps eight texels a side sends four of them a texel, so a lamp's bright spot under a ceiling
// stands for a quarter of the texel it falls in rather than the whole of it. A ray that meets a
// surface takes the light the surface sends along it (shadeProbeHit) and stops there, one that
// meets a surface letting light through crosses it, and one that reaches its end or leaves the
// field hands on the share it carries, which the merge gives the light past it. A probe inside a
// mesh sends nothing, which the merge reads as a probe to pass over.
//
// A probe nearer a surface than a tenth of its spacing, or inside a mesh, is moved off it along the
// field's normal before its rays leave, as DDGI moves its probes, so the probes in the plane of a
// wall or a ceiling stand in the room before it, and where it stands is left for the reach maps,
// the merge and the gather, which writes it into the probe's faces for every pass that reads them.
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
// The probes' passes, written once in the RadianceCascades module for both engines, whose trace of
// a ray this is.
import rc_probe_passes;

Texture3D<float4> field;
SamplerState field_sampler;
StructuredBuffer<FieldInfo> info;
Texture3D<float4> albedo;
Texture3D<uint> faces;
// The share of its diffuse light each cell's surface sends through to its far side
// (transmissionAt).
Texture3D<float> transmission;
// The light each cell's surfaces emit, as field_resolve.slang writes it beside the colors.
Texture3D<float4> glow;
StructuredBuffer<GiLights> lights;
// The share of the sun's light the camera's air lets through toward it, as gi_sky.slang wrote it
// on the camera the frame before, since the probes are traced before the cameras draw and have no
// air of their own to read. One where no camera has written it.
StructuredBuffer<float4> air;
// The small emissive surfaces carried as lights of their own, as many as the lights' fourth count
// says.
StructuredBuffer<GlowLight> glow_lights;

// The camera's environment map's light along each way folded onto an octahedron, the frame
// before's, alpha 1 where the camera has one (gi_sky.slang).
Texture2D<float4> sky_map;

// The indirect light reaching each probe the frame before, as gi_ambient.slang left it, for the
// light a surface sends on from it.
Texture3D<float4> bounced;
// The same faces' light at the surfaces they are read for, which a ray's hit takes the light it
// sends on from (bouncedSeenSum).
Texture3D<float4> bounced_at;
// Each probe's light along eight by eight directions the frame before, as gi_ambient.slang left it,
// which a ray's hit reflects by its gloss along the ray's mirror way (probeAlong).
Texture3D<float4> ways;

// Each ray as it ended, a ray of 16 by 16 a probe, the probes' rays one after another
// (cascadeTexelIndex), its light in half floats in x and y with the share it carries, the distance
// where it ended in z and how it ended in w (ProbeRays).
RWStructuredBuffer<uint4> rays;

// How far each ray went before it met a surface, or its end where it met none, below zero where it
// met a face from behind, a texel's rays taken together, which gi_ambient.slang counts to find a
// probe inside a solid.
[format("r16f")] RWTexture3D<float> distances;

// Every cascade's probes' reach, how far each of their rays went before a surface stopped it, past
// the surfaces letting light through it crossed, or its end where it met none, a texel a ray of
// sixteen by sixteen a probe, the cascades one after another along z, which a surface is weighed
// by whether a probe sees it by. Each ray keeps its own, since four rays a texel taken together
// gave a texel of eight a side the reach of the one of them that passed beside a thin-walled room's
// corner, 5.8 where the wall stood at 2.6 along the texel's other ways, and a probe beyond the wall
// saw an eighth of the floor inside. A ray's reach stands for a cell of ways about 15 degrees wide,
// so an edge narrower than that seen from a probe can still let one cell read long.
[format("r16f")] RWTexture3D<float> reach;

// Each probe's own light as gi_ambient.slang left it, the frame before's and the one before that
// side by side, the share of the indirect light each probe takes, and where this trace moved each
// probe to, its move in its spacing with w 1 where it stands outside every mesh, every cascade,
// laid out as an image of four probes' widths by one by every cascade's probes.
RWStructuredBuffer<float4> held;

// What each workgroup of a probe's rays, 64 of them, brought back straight from the sun and the
// lights, summed, with in w the luminance of all they brought back, each ray weighed by the share
// of the sphere its texel spans, four workgroups a probe, laid out as held is.
RWStructuredBuffer<float4> partial;

// The share of the indirect light each probe's rays took this frame, its own or the one the probes
// around it judge it by, a probe a value, every cascade, which the merge lights the surfaces its
// marches past a ray's end meet by.
RWStructuredBuffer<float4> shares;

// x: the cascade. y: the probes along each side. z: the octahedron's texels along each side. w: 1
// for the last cascade.
uniform uint4 cascade;
// x: unused. y: where the cascade's rays end. z: the probes' spacing in cells. w: how many
// cascades.
uniform float4 interval;
// x: 1 where each probe's share holds for the probe, its cascade lying where it lay the frame
// before. y: unused. z: the frame's parity. w: 1 where the field holds every mesh in reach, each
// of its cascades built and no triangle waiting.
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
// Each ray's light, its own light with how far it went, below zero where it met a face from behind,
// for the first ray of its texel to take together.
groupshared float4 rayLight[64];
groupshared float4 rayOwn[64];

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
    // A crown's leaf cards run together where they lie closer than a cell and read below zero
    // there, as a solid's inside does, though light crosses them (lightThroughCrossed), so a probe
    // in a cell the cards painted stands outside unless it lies deeper than half a cell, as the
    // crossing holds a trunk's or a limb's inside. A probe the moves leave at nothing stands
    // inside, since a wall thinner than a cell whose middle lies on the boundary between two cells,
    // as a probe's place does, reads nothing in both (field_resolve.slang), so a probe on it has no
    // slope across the wall to leave by and is moved up the wall, the normal's way where it has
    // none, and taken as outside it would hold the light of both sides and lend it to the wall's
    // shaded foot.
    float d = fieldDistance(field, field_sampler, fieldInfo, p);
    bool cards = lightThroughCards(paintAlphaAt(albedo, fieldInfo, p)) && d >= -0.5 * fieldCell(fieldInfo, p);
    return float4((p - at) / apart, d > 0.0 || cards ? 1.0 : 0.0);
}

// The share of the indirect light the probe whose ray is traced takes, and the light straight from
// the sun and the lights of every surface the ray met and all the light those sent along it, each
// thread's own, which ProbeScene's hits add to.
static float3 hitShare;
static float3 ownSum;
static float3 lightSum;

// The field as the module's trace of a probe's ray reads it (IProbeField), its distances through
// the whole field and a hit's light as this engine shades it.
struct ProbeScene : IProbeField
{
    FieldMarch march;

    int cascadeOf(float3 p, float margin) { return march.cascadeOf(p, margin); }
    float4 cornerAndCell(int c) { return march.cornerAndCell(c); }
    int size() { return march.size(); }
    float4 between(int c, float3 cells) { return march.between(c, cells); }
    float4 cellAt(int c, int3 cell) { return march.cellAt(c, cell); }
    float3 secondAt(int c, int3 cell) { return march.secondAt(c, cell); }

    // The light the surface met at place sends back along the ray, as the probes shade it
    // (shadeProbeHit), its light straight from the sun and the lights and all it sends added up. A
    // crown's leaves are lit where the module's march takes their light, within the crossing.
    float3 hitLight(float3 at, float3 way, int c, int3 cell)
    {
        FieldInfo fieldInfo = info[0];
        GiLights lit = lights[0];
        lit.sunColor.rgb *= air[0].rgb;
        float3 own;
        float3 light = shadeProbeHit(field, field_sampler, fieldInfo, gatheredAt(fieldInfo, lit, int(interval.w)), albedo, faces, transmission, glow, lit,
            glow_lights, bounced, bounced_at, ways, at, way, float(cascade.y), interval.z, interval.w, hitShare, own);
        ownSum += own;
        lightSum += light;
        return light;
    }

    // The share of light a unit of the depth of the surface nearest the cell lets pass, as its
    // paint's alpha holds it (lightThroughOf), none in the build that crosses no surface.
    float throughAt(int c, int3 cell)
    {
#if defined(NO_LIGHT_THROUGH)
        return 0.0;
#else
        return lightThroughOf(albedo.Load(int4(cell.xy, cell.z + c * march.size(), 0)).a);
#endif
    }
}

// The rays along each side of a probe's octahedron, every cascade's.
static const uint RayTexels = 16u;

// A ray not traced, met at once with nothing carried, a probe's inside a mesh.
ProbeRay untraced()
{
    ProbeRay ray = { RayMet, 0.0, 0.0, float3(0.0) };
    return ray;
}

// The most steps a march of a probe's ray takes, as traceField's.
static const uint StepCap = 64u;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID, uint lane : SV_GroupIndex)
{
    uint probes = cascade.y;
    uint texels = cascade.z;
    // A thread a ray, a texel's rays (RaysAt) side by side in a workgroup, and a probe's 256 rays
    // four whole workgroups, so every workgroup is one probe's.
    uint each = RaysAt(texels);
    uint count = texels * texels * each;
    uint probe = id.x / count;
    bool live = probe < probes * probes * probes;
    uint index = id.x % count;
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

    // The probe's share and its place, by the group's first ray, for every ray of it. One the
    // gather found inside a solid is left for the merge and the gather as one inside a mesh, its
    // rays traced all the same, so it is found again each frame.
    if (lane == 0u)
    {
        probeShare = follow.x != 0u ? held[heldAt(int3(2 * int(probes) + int(at.x), int(at.y), z))] : float4(1.0, 1.0, 1.0, 0.0);
        place = oc.w > 0.0 ? placed(fieldInfo, spot, apart) : float4(0.0);
        walledIn = place.w > 0.5 && follow.x != 0u && probeShare.w > 0.5 ? 1u : 0u;
        if (live)
            held[heldAt(int3(3 * int(probes) + int(at.x), int(at.y), z))] = walledIn != 0u ? float4(place.xyz, 0.0) : place;
    }

    // The share the own light of the 27 probes of the cascade within one of this one kept of the
    // frame before's, for a probe its own light lights too little of to judge by, so a room that
    // goes dark takes its walls' indirect light with it, and a lamp in a room far off, judged by
    // every probe's own light, does not dim or brighten the passages lit by indirect light alone
    // with it. Each of the group's first 27 rays reads one probe.
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

    if (lane == 0u && live)
        shares[(uint(z) * probes + at.y) * probes + at.x] = float4(share, 0.0);

    float3 origin = spot + place.xyz * apart;
    float end = interval.y;

    // The ray of the probe's ray texel, the texel's own where it sends one and a quarter of it
    // where it sends four, along that ray texel's middle way, as the merge reads it (texelWay). A
    // probe inside a mesh, or in a cascade not yet built, sends none, its ray met at once with
    // nothing carried. A ray that leaves the field takes the sky along its way past it, and in the
    // last cascade one that reaches its end, once the field holds every mesh in reach or in the
    // last cascade at once, since the sky reached through cascades not yet built would light a
    // closed room in its first frames.
    uint2 rayTexel = texel * (RayTexels / texels) + (each == 4u ? uint2(quarter & 1u, quarter >> 1u) : uint2(0u));
    float3 way = texelWay(RayTexels, rayTexel);
    ProbeRay traced = untraced();
    float3 own = float3(0.0);
    float3 brings = float3(0.0);
    float reached = 0.0;
    bool behind = false;
    if (oc.w > 0.0 && place.w >= 0.5)
    {
        hitShare = share;
        ownSum = float3(0.0);
        lightSum = float3(0.0);
        ProbeScene scene = { fieldMarch(field, field_sampler, info) };
        NoSteps none;
        traced = traceProbeRay(scene, origin, way, 0.0, end, StepCap, none);
        // The light straight from the sun and the lights the ray brings, its surfaces' own light
        // times the share of their light the ray took, the whole of it from one surface met.
        float taken = luminance(lightSum);
        own = taken > 1e-6 ? ownSum * (luminance(traced.light) / taken) : float3(0.0);
        ProbeSky sky = { sky_map, field_sampler, lit.sky.rgb, cascade.w != 0u || follow.w != 0u };
        brings = traced.ended == RayLeft || traced.ended == RayPast && cascade.w != 0u ? lastCascadeLight(traced, way, sky) : traced.light;
        own += brings - traced.light;
        reached = traced.ended == RayMet ? traced.t : end;
        // A ray that came on a face from behind it stops half a cell or more behind the face,
        // having stepped from a cell no face comes within the band of, which the field holds as the
        // open, straight into the cells behind it, where a ray from in front, slowing as it nears
        // the face, stops within a tenth of a cell of it.
        float3 met = origin + way * traced.t;
        behind = traced.ended == RayMet && fieldDistance(field, field_sampler, fieldInfo, met) < -0.5 * fieldCell(fieldInfo, met);
    }
    if (walledIn != 0u)
    {
        traced = untraced();
        own = float3(0.0);
        brings = float3(0.0);
    }
    if (live)
    {
        reach[uint3(at.xy * ReachTexels + rayTexel, uint(c) * probes + at.z)] = reached;
        ProbeCascade grid = { float3(0.0), apart, int3(int(probes)), RayTexels, 0.0, end, StepCap };
        rays[cascadeTexelIndex(grid, int3(at), rayTexel)] = uint4(f32tof16(traced.light.r) | f32tof16(traced.light.g) << 16u,
            f32tof16(traced.light.b) | f32tof16(traced.carried) << 16u, asuint(traced.t), traced.ended);
    }

    // A texel's rays taken together by its first, their own light, all they brought and how far
    // they went their means, and a face met from behind where most met one so.
    rayLight[lane] = float4(brings, 0.0);
    rayOwn[lane] = float4(own, behind ? -reached : reached);
    GroupMemoryBarrierWithGroupSync();
    float3 texelLight = float3(0.0);
    float3 texelOwn = float3(0.0);
    if (quarter == 0u)
    {
        float texelReach = 0.0;
        uint behinds = 0u;
        for (uint q = 0u; q < each; q++)
        {
            float4 itsOwn = rayOwn[lane + q];
            texelLight += rayLight[lane + q].rgb;
            texelOwn += itsOwn.rgb;
            texelReach += abs(itsOwn.w);
            behinds += itsOwn.w < 0.0 ? 1u : 0u;
        }
        texelLight /= float(each);
        texelOwn /= float(each);
        texelReach /= float(each);
        if (live)
            distances[written] = behinds * 2u > each ? -texelReach : texelReach;
    }

    // The group's own light and all it brought back summed, each ray weighed by the share of the
    // sphere its texel spans, as the gather weighs them, halving the rays that add each step.
    brought[lane] = quarter == 0u ? float4(texelOwn, luminance(texelLight)) * octahedronShare((float2(texel) + 0.5) / float(texels)) : float4(0.0);
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
