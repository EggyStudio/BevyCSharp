// Merges one cascade of light probes with the merged cascade above it, a thread a ray, from the
// last cascade down to the first, by the RadianceCascades module's rule (mergeTexel), and the last
// by its own rays alone (settleTexel). A ray that met a surface keeps its light, and one that
// reached its end with nothing met takes the light of the cascade above's probes about where it
// ended, marching on itself first to open space, across the cascade above's interval where those
// probes disagree on where a surface lies along it, and on to the last cascade's end where none of
// them sees its end (mergedLight). A probe above that stands inside a mesh reaches nothing, so the
// merge passes it over as one that does not see the ray's end, and a probe inside a mesh holds
// nothing itself, its alpha 0 where every other's is 1. A cascade that keeps eight texels a side
// merges each of its four rays a texel along its own way and keeps their mean.
//
// A march past a ray's end takes the light of a surface it meets from the light held for the cell
// and the side it met it in (gi_hold.slang, the module's HeldHitLight), held this frame before the
// merges, in place of shading the surface there as the trace does, since the shading was most of
// what a merge cost.
import bcs_field;
import bcs_gi;
// The probes' passes, written once in the RadianceCascades module for both engines, whose merge
// this is.
import rc_probe_passes;

Texture3D<float4> field;
SamplerState field_sampler;
StructuredBuffer<FieldInfo> info;
Texture3D<float4> albedo;
StructuredBuffer<GiLights> lights;
Texture2D<float4> sky_map;

// The light each side of each cell's surface sends of its own and of the probes it reflects, as
// gi_hold.slang held it this frame.
Texture3D<float4> held_own;
Texture3D<float4> held_probes;

// This cascade's rays as gi_trace.slang kept them.
StructuredBuffer<uint4> rays;

// The cascade above, merged, which the last cascade reads nowhere.
Texture3D<float4> above;

// Every cascade's probes' reach maps, as gi_reach.slang traced them this frame.
Texture3D<float> reach_maps;

// Where the trace moved each probe to this frame, after each probe's own light, its share and the
// frame before's, laid out as gi_trace.slang lays it.
StructuredBuffer<float4> held;

// The share of the indirect light each probe's rays took this frame, as gi_trace.slang kept it.
StructuredBuffer<float4> shares;

[format("rgba16f")] WTexture3D<float4> merged;

// x: the cascade. y: the probes along each side. z: this cascade's octahedron's texels along each
// side. w: the cascade above's, or 0 for the last cascade.
uniform uint4 cascade;
// x: where this cascade's rays end. y: where the cascade above's end. z: the probes' spacing in
// cells. w: how many cascades.
uniform float4 interval;
// x: unused. y: the bits of the last cascade's end, the field's edge, to which a march past every
// cascade above runs. z: unused. w: 1 where the field holds every mesh in reach, each of its
// cascades built and no triangle waiting.
uniform uint4 follow;

// The element of held that holds column x, row y and layer z of its layout.
uint heldAt(int3 at)
{
    uint probes = cascade.y;
    return (uint(at.z) * probes + uint(at.y)) * 4u * probes + uint(at.x);
}

// The corner and cell of the field cascade a cascade of probes lies around, as probesOf lays them,
// the far cascade's for the last where one is built, read from the one cascade in place.
float4 probesCorner(int c)
{
    int far = int(info[0].counts.w);
    return info[0].cascades[far > 0 && c == int(interval.w) - 1 ? far : c].origin_and_cell;
}

// A cascade of the probes as the module reads it, its probes' grid and its rays' end, each probe's
// octahedron `texels` along each side.
ProbeCascade probeCascade(int c, uint texels, float end)
{
    float4 oc = probesCorner(c);
    float apart = oc.w * interval.z;
    ProbeCascade grid = { oc.xyz + 0.5 * apart, apart, int3(int(cascade.y)), texels, 0.0, end, StepCap };
    return grid;
}

// The rays along each side of a probe's octahedron, every cascade's, as gi_trace.slang sends them.
static const uint RayTexels = 16u;

// The most steps a march of a probe's ray takes, as gi_trace.slang's.
static const uint StepCap = 64u;

// The share of the indirect light the probe whose ray is merged takes, each thread's own, by which
// a surface a march past the ray's end meets weighs the light of the probes it reflects.
static float3 hitShare;

// The field as the module's marches past a ray's end read it (IProbeField), as gi_trace.slang's,
// whose surfaces the held light (HeldCells) lights in its place.
struct ProbeScene : IProbeField
{
    FieldMarch march;

    int cascadeOf(float3 p, float margin) { return march.cascadeOf(p, margin); }
    float4 cornerAndCell(int c) { return march.cornerAndCell(c); }
    int size() { return march.size(); }
    float4 between(int c, float3 cells) { return march.between(c, cells); }
    float4 cellAt(int c, int3 cell) { return march.cellAt(c, cell); }
    float3 secondAt(int c, int3 cell) { return march.secondAt(c, cell); }

    // Never read, since the held light (HeldHitLight) answers each hit.
    float3 hitLight(float3 at, float3 way, int c, int3 cell) { return float3(0.0); }

    float throughAt(int c, int3 cell)
    {
        return lightThroughOf(albedo.Load(int4(cell.xy, cell.z + c * march.size(), 0)).a);
    }
}

// The light gi_hold.slang held for the field's cells (IHeldLight), each side's own and its probes'
// apart, the probes' weighed by the share of the indirect light the merged probe takes, as the
// trace weighs a hit's, the mean of its three channels since the module's share is one number.
struct HeldCells : IHeldLight
{
    int size;
    int cascades;

    int4 heldCell(int c, int3 cell, uint side) { return int4(cell.xy, (int(side) * cascades + c) * size + cell.z, 0); }
    float3 heldAt(int c, int3 cell, uint side) { return held_own.Load(heldCell(c, cell, side)).rgb; }
    float3 heldProbeLightAt(int c, int3 cell, uint side) { return held_probes.Load(heldCell(c, cell, side)).rgb; }
    float probeShare(float3 place, int c, int3 cell) { return lights[0].counts.z * dot(hitShare, float3(1.0 / 3.0)); }
    void hold(int c, int3 cell, uint side, float3 own, float3 probes) {}
}

// This cascade's rays as the trace kept them (IProbeRays).
struct KeptRays : IProbeRays
{
    ProbeRay rayAt(uint index)
    {
        uint4 kept = rays[index];
        ProbeRay ray = { kept.w, asfloat(kept.z), f16tof32(kept.y >> 16u), float3(f16tof32(kept.x & 0xFFFFu), f16tof32(kept.x >> 16u), f16tof32(kept.y & 0xFFFFu)) };
        return ray;
    }

    void keepRay(uint index, ProbeRay ray) {}
}

// The merged light of the ray, each thread's own (keepMerged).
static float3 mergedHere;

// The ray's merged light, for its texel's first ray to take with the rest (IMergedLight).
struct MergedRay : IMergedLight
{
    void keepMerged(uint index, float3 light) { mergedHere = light; }
}

// The cascade above's probes as the merge reads them (IFarProbes), their merged light and their
// reach, a probe that stands inside a mesh reaching nothing.
struct AboveProbes : IFarProbes
{
    float3 lightAt(int3 probe, uint2 texel)
    {
        return above.Load(int4(probe.xy * int(cascade.w) + int2(texel), probe.z, 0)).rgb;
    }

    float reachAt(int3 probe, uint2 texel)
    {
        int probes = int(cascade.y);
        int z = (int(cascade.x) + 1) * probes + probe.z;
        if (held[heldAt(int3(3 * probes + probe.x, probe.y, z))].w < 0.5)
            return ReachInside;
        return reach_maps.Load(int4(probe.xy * int(ReachTexels) + int2(texel), z, 0));
    }
}

// Where the trace left each probe of a cascade standing, its grid's place moved by its move in its
// spacing (IProbePlaces).
struct HeldPlaces : IProbePlaces
{
    ProbeCascade grid;
    int c;

    float3 placeOf(int3 probe)
    {
        int probes = int(cascade.y);
        return grid.corner + (float3(probe) + held[heldAt(int3(3 * probes + probe.x, probe.y, c * probes + probe.z))].xyz) * grid.spacing;
    }
}

groupshared float3 rayMerged[64];

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID, uint lane : SV_GroupIndex)
{
    uint probes = cascade.y;
    uint texels = cascade.z;
    uint each = RaysAt(texels);
    uint count = texels * texels * each;
    uint probe = id.x / count;
    bool live = probe < probes * probes * probes;
    uint index = id.x % count;
    uint quarter = index % each;
    uint ray = index / each;
    uint3 at = uint3(probe % probes, (probe / probes) % probes, min(probe / (probes * probes), probes - 1u));
    uint2 texel = uint2(ray % texels, ray / texels);
    uint2 rayTexel = texel * (RayTexels / texels) + (each == 4u ? uint2(quarter & 1u, quarter >> 1u) : uint2(0u));
    int c = int(cascade.x);

    // A probe inside a mesh, or in a cascade not yet built, holds nothing.
    ProbeCascade near = probeCascade(c, RayTexels, interval.x);
    float4 placed = held[heldAt(int3(3 * int(probes) + int(at.x), int(at.y), c * int(probes) + int(at.z)))];
    bool holds = live && near.spacing > 0.0 && placed.w > 0.5;
    mergedHere = float3(0.0);
    if (holds)
    {
        uint rayIndex = cascadeTexelIndex(near, int3(at), rayTexel);
        GiLights lit = lights[0];
        ProbeSky sky = { sky_map, field_sampler, lit.sky.rgb, cascade.w == 0u || follow.w != 0u };
        KeptRays kept;
        MergedRay merging;
        if (cascade.w == 0u)
            settleTexel(kept, merging, near, sky, rayIndex);
        else
        {
            HeldPlaces places = { near, c };
            hitShare = shares[(uint(c) * probes + at.z) * probes * probes + at.y * probes + at.x].rgb;
            ProbeCascade far = probeCascade(c + 1, cascade.w, interval.y);
            HeldPlaces abovePlaces = { far, c + 1 };
            ProbeScene scene = { fieldMarch(field, field_sampler, info) };
            HeldCells cells = { scene.size(), int(info[0].counts.x) };
            HeldHitLight<ProbeScene, HeldCells> heldScene = { scene, cells };
            AboveProbes probesAbove;
            NoSteps none;
            mergeTexel(heldScene, kept, places, probesAbove, abovePlaces, merging, near, far, asfloat(follow.y), sky, none, rayIndex);
        }
    }

    // A texel's rays taken together by its first, their merged light's mean.
    rayMerged[lane] = mergedHere;
    GroupMemoryBarrierWithGroupSync();
    if (!live || quarter != 0u)
        return;
    float3 sum = float3(0.0);
    for (uint q = 0u; q < each; q++)
        sum += rayMerged[lane + q];
    merged.Store(uint3(at.xy * texels + texel, at.z), holds ? float4(sum / float(each), 1.0) : float4(0.0));
}
