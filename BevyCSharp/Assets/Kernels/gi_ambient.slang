// Gathers the light each probe of a cascade receives into the six faces of a cube, a thread a
// probe's face. Each face holds the light arriving at a surface facing along its axis, every merged
// direction weighed by how squarely it falls on that surface and by the share of the sphere its
// texel spans, which a pass and the next frame's rays read, blended between probes. The weights are
// scaled to sum to pi, the light reaching a face under a sky of 1, so a uniform sky reads alike on
// every face at any number of texels. Each probe's reach is laid out beside, how far its rays went
// along each of eight by eight directions before a surface stopped them, from this cascade's rays,
// each face's thread a sixth of the directions, which a probe is weighed by.
//
// The first face's thread also adds up the light the probe's rays brought straight from the sun and
// the lights, its own light, and all they brought, as the trace summed them a workgroup at a time,
// and sets the share of the indirect light the probe's next rays take (gi_trace.slang). Where its
// own light is a quarter of all it brings or more and fell by a fifth or more against the frame
// before's, that is the share it kept, channel by channel, so the indirect light from light that
// went out goes with it rather than fading over the frames each bounce takes; where its own light
// is less, it is a mark that the probe is judged by the probes around it.
//
// Taken from 3DEngine's gi_ambient.slang, the held light a buffer as gi_trace.slang lays it out.
import bcs_gi;

Texture3D<float4> merged;

// Every cascade's probes, each face a block of the probes along x, the cascades one after another
// along z.
[format("rgba16f")] WTexture3D<float4> cubes;

// How far this cascade's rays went, as gi_trace.slang wrote them, a texel a ray, below zero where a
// ray met a face from behind.
Texture3D<float> distances;

// Every cascade's probes' reach, eight by eight directions a probe, the cascades one after another
// along z.
[format("r16f")] RWTexture3D<float> reach;

// What each workgroup of a probe's rays brought back straight from the sun and the lights, and the
// luminance of all they brought back, summed, as gi_trace.slang wrote it.
StructuredBuffer<float4> partial;

// Each probe's own light and the luminance of all its rays brought, the frames of each parity side
// by side, after them the share of the indirect light each probe's next rays take, a negative one
// where the probe is judged by the probes around it, with in w a 1 where its rays met more faces
// from behind than from in front, and after that where the trace moved each probe to, every cascade.
RWStructuredBuffer<float4> held;

// The share of each merged direction that reaches the sky, as gi_merge.slang wrote it, and every
// cascade's probes' faces' share of sky, laid out as the faces are, each direction weighed as the
// light is.
Texture3D<float> merged_sky;
[format("r16f")] RWTexture3D<float> skies;

// x: the cascade. y: the probes along each side. z: the octahedron's texels along each side. w: the
// frame's parity, plus 2 where the cascade lies where it lay the frame before, so the frame before's
// own light is the same probes'.
uniform uint4 cascade;
// x: where the cascade's rays' interval ends, which a ray that met nothing reached. yzw: unused.
uniform float4 interval;

static const float3 Axes[6] = { float3(1, 0, 0), float3(-1, 0, 0), float3(0, 1, 0), float3(0, -1, 0), float3(0, 0, 1), float3(0, 0, -1) };

// The element of held and partial that holds column x, row y and layer z of their layout.
uint heldAt(int3 at)
{
    uint probes = cascade.y;
    return (uint(at.z) * probes + uint(at.y)) * 4u * probes + uint(at.x);
}

// Adds up the probe's own light and all its rays brought, over the shares of the sphere its texels
// span, and sets the share of the indirect light its next rays take, and whether the probe stands
// inside a solid.
void follow(uint3 at, uint probes, uint texels, float shares, bool walledIn)
{
    uint parity = cascade.w & 1u;
    int p = int(probes);
    int z = int(cascade.x) * p + int(at.z);
    float4 sum = float4(0.0);
    uint rays = texels * texels;
    for (uint g = 0u; g < max(rays / 64u, 1u); g++)
        sum += partial[heldAt(int3(int(at.x) * 4 + int(g), int(at.y), z))];
    float3 own = sum.rgb / max(shares, 1e-6);
    float all = sum.a / max(shares, 1e-6);

    float4 before = held[heldAt(int3(int(parity ^ 1u) * p + int(at.x), int(at.y), z))];
    float3 kept = float3(1.0);
    float was = luminance(before.rgb);
    if ((cascade.w & 2u) != 0u && before.a >= 0.0)
    {
        if (was > 1e-4 && was >= OwnShare * before.a)
        {
            // Each channel's, a channel the frame before's light held next to none of kept whole.
            kept = select(before.rgb > was * 1e-3, followed(own, before.rgb), float3(1.0));
        }
        else
            kept = float3(-1.0);
    }
    held[heldAt(int3(int(parity) * p + int(at.x), int(at.y), z))] = float4(own, all);
    held[heldAt(int3(2 * p + int(at.x), int(at.y), z))] = float4(kept, walledIn ? 1.0 : 0.0);
}

// Whether more of a probe's rays met a face from behind than from in front, as one inside a solid
// does. The field holds a solid as the faces around it, so its cells deeper inside than the faces'
// band read as the open, and a probe standing there traces through the rock to the backs of the
// faces around it. As DDGI finds its probes inside geometry by the share of rays that met back faces,
// a probe whose rays met more faces from behind, or more than a quarter of them so, is passed over
// until it meets fewer.
bool walledIn(uint3 at, uint texels)
{
    uint backs = 0u, fronts = 0u;
    for (uint v = 0u; v < texels; v++)
        for (uint u = 0u; u < texels; u++)
        {
            float d = distances.Load(int4(int2(at.xy * texels + uint2(u, v)), int(at.z), 0));
            if (d < 0.0)
                backs++;
            else if (d > 0.0 && d < interval.x * 0.999)
                fronts++;
        }
    return backs > fronts || 4u * backs > texels * texels;
}

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint probes = cascade.y;
    uint texels = cascade.z;
    uint face = id.x % 6u;
    uint probe = id.x / 6u;
    if (probe >= probes * probes * probes)
        return;
    uint3 at = uint3(probe % probes, (probe / probes) % probes, probe / (probes * probes));
    float3 axis = Axes[face];
    float3 sum = float3(0.0);
    float sky = 0.0;
    float weights = 0.0;
    float shares = 0.0;
    float valid = 0.0;
    for (uint v = 0u; v < texels; v++)
        for (uint u = 0u; u < texels; u++)
        {
            int4 from = int4(int2(at.xy * texels + uint2(u, v)), int(at.z), 0);
            float4 light = merged.Load(from);
            float2 uv = (float2(float(u), float(v)) + 0.5) / float(texels);
            float share = octahedronShare(uv);
            float weight = max(dot(fromOctahedron(uv), axis), 0.0) * share;
            shares += share;
            sum += light.rgb * weight;
            sky += merged_sky.Load(from) * weight;
            weights += weight;
            // A probe inside a mesh holds alpha 0 in every texel, where every other's are a half or 1.
            valid = max(valid, light.a > 0.25 ? 1.0 : 0.0);
        }
    // Its alpha 0 where the probe holds no light, and two more than its move along the face's axis
    // where it does (faceHolds and faceMove).
    float4 place = held[heldAt(int3(3 * int(probes) + int(at.x), int(at.y), int(cascade.x * probes + at.z)))];
    float holds = valid > 0.5 && place.w > 0.5 ? 1.0 : 0.0;
    uint3 into = uint3(face * probes + at.x, at.y, cascade.x * probes + at.z);
    cubes.Store(into, float4(sum * (Pi / max(weights, 1e-6)), holds * (2.0 + place[face / 2u])));
    skies[into] = sky / max(weights, 1e-6);
    if (face == 0u)
        follow(at, probes, texels, shares, walledIn(at, texels));

    // This probe's reach, the nearest ray's distance along each direction where the cascade has as
    // many directions or fewer, and the mean of the rays inside it where it has more.
    uint step = max(texels / uint(ReachTexels), 1u);
    for (uint k = face; k < uint(ReachTexels * ReachTexels); k += 6u)
    {
        uint2 texel = uint2(k % uint(ReachTexels), k / uint(ReachTexels));
        uint2 first = texels >= uint(ReachTexels) ? texel * step : uint2((float2(texel) + 0.5) / float(ReachTexels) * float(texels));
        float reached = 0.0;
        for (uint v = 0u; v < step; v++)
            for (uint u = 0u; u < step; u++)
                reached += abs(distances.Load(int4(int2(at.xy * texels + first + uint2(u, v)), int(at.z), 0)));
        reach[uint3(at.xy * uint(ReachTexels) + texel, cascade.x * probes + at.z)] = reached / float(step * step);
    }
}
