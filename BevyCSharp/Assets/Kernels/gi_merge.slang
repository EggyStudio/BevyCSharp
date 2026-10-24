// Merges one cascade of light probes with the merged cascade above it, a thread a probe's
// direction, from the last cascade down to the first. What a probe's ray brought back is kept, and
// where the ray met nothing the light from beyond is added, the cascade above's probes around this
// one blended by how near each is, each one's texels of the directions inside this texel's
// averaged, so the first cascade holds the light from every distance. A probe inside a mesh, or one
// a surface hides from this probe, as a probe above a ceiling is hidden from one under it, is passed
// over in the blend, and a probe inside a mesh holds nothing itself, its alpha 0 where every other's
// is 1. A ray of the cascade above that met a surface before its interval began keeps that surface's
// light for its own probe's faces, its alpha a half, and the blend reads it as dark, since the
// surface lies nearer that probe than this one's ray reaches. Each probe above is read toward the
// point of this probe's ray nearest where its own ray along the way met a surface the frame before
// (its reach), and not along the same way, so a probe above standing elsewhere lends the light of
// what this ray would meet and not of what it sees from where it stands, the merge's parallax.
//
// Taken from 3DEngine's gi_merge.slang.
import bcs_field;
import bcs_gi;

Texture3D<float4> radiance;

// The cascade above, merged, or this cascade's own radiance for the last.
Texture3D<float4> above;

[format("rgba16f")] WTexture3D<float4> merged;

StructuredBuffer<FieldInfo> info;
Texture3D<float> field;
SamplerState field_sampler;

// Where the trace moved each probe to this frame, after each probe's own light, its share and the
// frame before's, laid out as gi_trace.slang lays it.
StructuredBuffer<float4> held;

// The share of each merged direction that reaches the sky, 1 where a ray of the last cascade met
// nothing, carried down as the light from beyond is, and the cascade above's, read where this
// cascade's ray met nothing; the last cascade reads none.
[format("r16f")] RWTexture3D<float> merged_sky;
Texture3D<float> above_sky;

// The cascade above's probes' reach, the nearest ray's distance along each of their eight by eight
// directions, as the gather wrote it the frame before.
Texture3D<float> reach_above;

// x: the cascade. y: the probes along each side. z: this cascade's octahedron's texels along each
// side. w: the cascade above's, or 0 for the last cascade.
uniform uint4 cascade;
// x: the probes' spacing in cells. y: unused. z: unused. w: where the cascade above's interval
// ends, which its ray that met nothing reached.
uniform float4 spacing;
// x: how many cascades. yzw: unused.
uniform uint4 counts;

// The texel of an octahedron of `texels` along each side at t, folded across its edges.
int2 foldedAt(int2 t, int texels)
{
    if (t.x < 0)
        t = int2(0, texels - 1 - t.y);
    else if (t.x >= texels)
        t = int2(texels - 1, texels - 1 - t.y);
    if (t.y < 0)
        t = int2(texels - 1 - t.x, 0);
    else if (t.y >= texels)
        t = int2(texels - 1 - t.x, texels - 1);
    return t;
}

float4 placeOf(int3 at, uint c, uint probes)
{
    int3 slot = int3(3 * int(probes) + at.x, at.y, int(c * probes) + at.z);
    return held[(uint(slot.z) * probes + uint(slot.y)) * 4u * probes + uint(slot.x)];
}

// Whether each probe of the group sees each of the eight probes of the cascade above around it, a
// probe's eight together, for every thread of the probe to read. A group holds four probes at most,
// as a probe has sixteen threads at fewest.
groupshared uint seen[32];

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID, uint3 local : SV_GroupThreadID)
{
    uint probes = cascade.y;
    uint texels = cascade.z;
    uint rays = texels * texels;
    uint probe = id.x / rays;
    uint ray = id.x % rays;
    uint3 at = uint3(probe % probes, (probe / probes) % probes, min(probe / (probes * probes), probes - 1u));
    uint2 texel = uint2(ray % texels, ray / texels);
    uint3 written = uint3(at.xy * texels + texel, at.z);
    bool live = probe < probes * probes * probes;

    uint c = cascade.x;
    uint upper = cascade.w;
    FieldInfo fieldInfo = info[0];
    // The probes where the field's cascades they stand in lie, the far one's for the last.
    FieldInfo probed = probesOf(fieldInfo, int(counts.x));
    float4 oc = probed.cascades[c].origin_and_cell;
    float3 spot = oc.xyz + (float3(at) + 0.5) * (oc.w * spacing.x);
    float4 here = live ? placeOf(int3(at), c, probes) : float4(0.0);
    float3 origin = spot + here.xyz * (oc.w * spacing.x);
    uint upperCascade = min(c + 1u, counts.x - 1u);
    float4 ac = probed.cascades[upperCascade].origin_and_cell;
    float3 q = (spot - ac.xyz) / (ac.w * spacing.x) - 0.5;
    int3 base = int3(floor(q));

    // The first eight threads of each probe in the group trace whether it sees each of the probes
    // above around it, every thread of the group reaching the barrier before any returns.
    uint inGroup = min(rays, 64u);
    uint slot = local.x / inGroup;
    uint traced = local.x % inGroup;
    if (traced < 8u)
    {
        int3 offset = int3(int(traced & 1u), int((traced >> 1) & 1u), int(traced >> 2));
        int3 neighbor = clamp(base + offset, int3(0), int3(int(probes) - 1));
        float3 upperProbe = ac.xyz + (float3(neighbor) + 0.5 + placeOf(neighbor, upperCascade, probes).xyz) * (ac.w * spacing.x);
        seen[slot * 8u + traced] = upper > 0u && live && oc.w > 0.0
            && seesThrough(field, field_sampler, fieldInfo, origin, upperProbe, oc.w * 0.5) ? 1u : 0u;
    }
    GroupMemoryBarrierWithGroupSync();

    if (!live)
        return;
    if (oc.w <= 0.0 || here.w < 0.5)
    {
        merged.Store(written, float4(0.0));
        merged_sky[written] = 0.0;
        return;
    }
    float4 own = radiance.Load(int4(int3(written), 0));
    // What most of the texel's rays met, and the share that met nothing, which takes the light from
    // beyond, the light of the rest in own's color (gi_trace.slang).
    float nothing = float(texelNothing(own.a, RaysAt(texels))) / float(RaysAt(texels));
    // Rays of the last cascade that met nothing, the sky's.
    float ownSky = abs(own.a - 0.25) < 0.1 ? 1.0 : 0.0;
    // A ray that met a surface keeps its light, marked a half where most met it before its interval.
    float kept = own.a > 0.25 && own.a < 0.75 ? 0.5 : 1.0;
    if (upper == 0u || nothing <= 0.0)
    {
        merged.Store(written, float4(own.rgb, own.a < 0.75 ? kept : 1.0));
        merged_sky[written] = ownSky;
        return;
    }

    // The cascade above's probes around this one, eight of them weighed by how near each is, and
    // passed over where it is inside a mesh or a surface hides it from this one.
    float3 f = q - float3(base);
    uint factor = upper / texels;
    float3 d = fromOctahedron((float2(texel) + 0.5) / float(texels));
    float3 sum = float3(0.0);
    float sky = 0.0;
    float weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 neighbor = clamp(base + offset, int3(0), int3(int(probes) - 1));
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z * float(seen[slot * 8u + uint(corner)]) / float(factor * factor);
        // The probe above read toward the point of this ray nearest where its own ray along the way
        // met a surface, and along the way itself where it met nothing.
        float3 upperAt = ac.xyz + (float3(neighbor) + 0.5 + placeOf(neighbor, upperCascade, probes).xyz) * (ac.w * spacing.x);
        int2 rt = min(int2(octahedron(d) * float(ReachTexels)), int2(ReachTexels - 1));
        float r = reach_above.Load(int4(neighbor.xy * ReachTexels + rt, int(c + 1u) * int(probes) + neighbor.z, 0));
        float3 way = d;
        if (r > 0.0 && r < spacing.w * 0.999)
        {
            float3 met = upperAt + d * r;
            float3 toward = origin + d * max(dot(met - origin, d), 0.0) - upperAt;
            if (dot(toward, toward) > 1e-8)
                way = normalize(toward);
        }
        int2 first = int2(floor(octahedron(way) * float(upper) - float(factor) * 0.5 + 0.5));
        // The texels of the cascade above that lie inside this one's, one or four of them, those that
        // met a surface before their interval dark, and those inside a mesh passed over.
        for (uint v = 0u; v < factor; v++)
            for (uint u = 0u; u < factor; u++)
            {
                int2 tt = foldedAt(first + int2(int(u), int(v)), int(upper));
                int4 from = int4(int2(neighbor.xy) * int(upper) + tt, neighbor.z, 0);
                float4 child = above.Load(from);
                if (child.a > 0.75)
                {
                    sum += child.rgb * weight;
                    sky += above_sky.Load(from) * weight;
                    weights += weight;
                }
                else if (child.a > 0.25)
                    weights += weight;
            }
    }
    float3 beyond = weights > 1e-4 ? sum / weights : float3(0.0);
    merged.Store(written, float4(own.rgb + beyond * nothing, own.a < 0.75 ? kept : 1.0));
    merged_sky[written] = weights > 1e-4 ? sky / weights * nothing : 0.0;
}
