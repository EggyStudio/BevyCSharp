// Traces the rays of one cascade of light probes through the scene's distance field, a thread a
// ray. A cascade's probes lie every eight cells of the field's cascade of the same number, or of
// its far cascade for the last where it has one (probesOf), and each sends a ray along each texel of
// an octahedron of directions across its interval, from as far as the probes are apart to four times
// that, the last cascade's on to the field's edge, so the next cascade, twice as coarse, carries on
// from half way along it. A ray that meets a surface brings back the light the surface sends along
// it and stops there. One that meets nothing brings back nothing and lets what lies beyond through,
// but in the last cascade, where it brings back the light from all around. A probe inside a mesh
// sends nothing, which the merge reads as a probe to pass over.
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
// Taken from 3DEngine's gi_trace.slang, its glow lights and emissive surfaces coming with the
// emitters. Each probe's held light and the partial sums are buffers, which a dispatch here reads
// and writes at once where an image of half floats is only written, and the sum of the probes
// around one is taken by every workgroup and kept where its probe's share asks for it, since a
// barrier is reached by every invocation alike or by none.
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
// The small emissive surfaces carried as lights of their own, as many as the lights' fourth count says.
StructuredBuffer<GlowLight> glow_lights;

// What each texel's rays brought back, rgb the light of those that met a surface or the sky over how
// many it sends, and a what most of them met (texelMark). A texel a direction, each probe a tile of
// the octahedron's texels.
[format("rgba16f")] WTexture3D<float4> radiance;

// The indirect light reaching each probe the frame before, as gi_ambient.slang left it, for the
// light a surface sends on from it.
Texture3D<float4> bounced;

// How far each ray went before it met a surface, or its interval's end where it met none, below zero
// where it met a face from behind, which gi_ambient.slang lays out as each probe's reach and counts
// to find a probe inside a solid.
[format("r16f")] RWTexture3D<float> distances;

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
// before. y: unused. z: the frame's parity. w: unused.
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
// Each ray's light and mark, and its own light with how far it went, below zero where it met a face
// from behind, for the first ray of its texel to take together.
groupshared float4 rayLight[64];
groupshared float4 rayOwn[64];

// The field's layout with each cascade at the corner it lay at the frame before, where the faces of
// the indirect light, which the rays read, were gathered. Read at this frame's corners, a cascade
// that moved with the eye read each probe's faces from the probe a step or more beside it, and in
// 3DEngine the light past the first bounce took ten frames and more to find itself again after a
// turn moved every cascade.
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
    return float4((p - at) / apart, fieldDistance(field, field_sampler, fieldInfo, p) >= 0.0 ? 1.0 : 0.0);
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
    float4 light = float4(0.0, 0.0, 0.0, 1.0);
    float3 own = float3(0.0);
    float reached = 0.0;
    bool behind = false;
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
        float t = traceField(field, field_sampler, fieldInfo, origin, way, 0.0, interval.y);
        reached = t >= 0.0 ? t : interval.y;
        if (t >= 0.0)
        {
            float3 hit = origin + way * t;
            light = float4(shadeProbeHit(field, field_sampler, fieldInfo, gatheredAt(fieldInfo, lit), albedo, faces, glow, lit, glow_lights,
                bounced, hit, way, float(probes), interval.z, interval.w, share, own), t < interval.x ? 0.5 : 0.0);
            // A ray that came on a face from behind it stops half a cell or more behind the face,
            // having stepped from a cell no face comes within the band of, which the field holds as
            // the open, straight into the cells behind it, where a ray from in front, slowing as it
            // nears the face, stops within a tenth of a cell of it.
            behind = fieldDistance(field, field_sampler, fieldInfo, hit) < -0.5 * fieldCell(fieldInfo, hit);
        }
        else if (cascade.w != 0u)
        {
            light = float4(lit.sky.rgb, 0.25);
            own = lit.sky.rgb;
        }
    }
    if (walledIn != 0u)
    {
        light = float4(0.0);
        own = float3(0.0);
    }

    // A texel's rays taken together by its first, the light of those that met a surface or the sky
    // over how many there are, marked with what most met and how many met nothing (texelMark), their
    // own light and their reach their means, and a face met from behind where most met one so.
    rayLight[lane] = light;
    rayOwn[lane] = float4(own, behind ? -reached : reached);
    GroupMemoryBarrierWithGroupSync();
    float4 texelLight = float4(0.0);
    float3 texelOwn = float3(0.0);
    if (quarter == 0u)
    {
        float3 met = float3(0.0);
        float reach = 0.0;
        uint surfaces = 0u, early = 0u, skies = 0u, nothing = 0u, behinds = 0u;
        for (uint q = 0u; q < each; q++)
        {
            float4 one = rayLight[lane + q];
            float4 itsOwn = rayOwn[lane + q];
            surfaces += one.a < 0.125 ? 1u : 0u;
            skies += one.a >= 0.125 && one.a < 0.375 ? 1u : 0u;
            early += one.a >= 0.375 && one.a < 0.75 ? 1u : 0u;
            nothing += one.a >= 0.75 ? 1u : 0u;
            met += one.a < 0.75 ? one.rgb : float3(0.0);
            texelOwn += itsOwn.rgb;
            reach += abs(itsOwn.w);
            behinds += itsOwn.w < 0.0 ? 1u : 0u;
        }
        float most = nothing * 2u > each ? 1.0 : early > surfaces && early >= skies ? 0.5 : skies > surfaces ? 0.25 : 0.0;
        texelLight = float4(met / float(each), texelMark(most, nothing, each));
        texelOwn /= float(each);
        reach /= float(each);
        if (live)
        {
            radiance.Store(written, texelLight);
            distances[written] = behinds * 2u > each ? -reach : reach;
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
