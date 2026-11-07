// Gathers the light each probe of a cascade receives into the six faces of a cube, a thread a
// probe's face. Each face holds the light arriving at a surface facing along its axis, every merged
// direction weighed by how squarely it falls on that surface and by the share of the sphere its
// texel spans, which a pass and the next frame's rays read, blended between probes. The weights are
// scaled to sum to pi, the light reaching a face under a sky of 1, so a uniform sky reads alike on
// every face at any number of texels. Each probe's reach is laid out beside, how far its rays went
// along each of eight by eight directions before a surface stopped them, from this cascade's rays,
// each face's thread a sixth of the directions, which a probe is weighed by.
//
// Beside each face it writes the light the surface the face is read for gathers, the one the probe's
// own ray along the face's opposite way met, which a ray's hit on that surface and a pixel on it take
// their light from. Read where the probe stands, a face a unit over a floor gathered the walls and
// the ceiling nearer than the floor sees them, and in a closed room of the level's white each bounce
// came back about 5% over its reference, the light past the direct 16 to 27% over through them all.
// Each patch the rays met is turned toward the surface by the field's normal there, since moved by
// its distance alone a patch the probe saw squarely and the surface sees at a slant, as a wall's
// band under a ceiling, counted for as much as it did at the probe, and a closed box with dark side
// walls took its first bounce 14% over at its floor, where turned it reads 2% over.
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
import bcs_field;
import bcs_gi;

Texture3D<float4> merged;

// Every cascade's probes, each face a block of the probes along x, the cascades one after another
// along z.
[format("rgba16f")] WTexture3D<float4> cubes;
// Each face's light as the surface it is read for gathers it, laid out as the faces are, with how far
// the probe's ray along the face's opposite way went to that surface, or the face's own light and 0
// where that ray found none (bouncedSeenSum).
[format("rgba16f")] WTexture3D<float4> cubes_at;

// How far this cascade's rays went, as gi_trace.slang wrote them, a texel a ray, below zero where a
// ray met a face from behind.
Texture3D<float> distances;

// The field, for the normal of the patch each ray met, which turns it toward the surface a face is
// read for.
Texture3D<float> field;
SamplerState field_sampler;
StructuredBuffer<FieldInfo> info;

// The share of light this cascade's rays kept across surfaces that let light through before their
// interval began, as gi_trace.slang wrote it, which the probe's faces take their light times.
Texture3D<float> kept;

// Every cascade's probes' reach, eight by eight directions a probe, the cascades one after another
// along z.
[format("r16f")] RWTexture3D<float> reach;

// Every cascade's probes' light along eight by eight directions, each the merged directions about
// it weighed by the lobe a surface of roughness 0.5 spreads its reflection over, a block of ten by
// ten texels a probe, its border each texel across the octahedron's edge, the cascades one after
// another along z, which the light pass reads a rough surface's reflection from (reflectedAt in
// gi_light.slang), as 3DEngine's gi_ambient.slang gathers its probes' ways.
[format("rgba16f")] WTexture3D<float4> ways;

// What each workgroup of a probe's rays brought back straight from the sun and the lights, and the
// luminance of all they brought back, summed, as gi_trace.slang wrote it.
StructuredBuffer<float4> partial;

// Each probe's own light and the luminance of all its rays brought, the frames of each parity side
// by side, after them the share of the indirect light each probe's next rays take, a negative one
// where the probe is judged by the probes around it, with in w a 1 where its rays met more faces
// from behind than from in front, and after that where the trace moved each probe to, every cascade.
RWStructuredBuffer<float4> held;

// x: the cascade. y: the probes along each side. z: the octahedron's texels along each side. w: the
// frame's parity, plus 2 where the cascade lies where it lay the frame before, so the frame before's
// own light is the same probes'.
uniform uint4 cascade;
// x: where the cascade's rays' interval ends, which a ray that met nothing reached. y: the probes'
// spacing in their field cascade's cells. z: how many cascades of probes. w: unused.
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
            float before = kept.Load(from);
            shares += share;
            sum += light.rgb * before * weight;
            weights += weight;
            // A probe inside a mesh holds alpha 0 in every texel, where every other's are a half or 1.
            valid = max(valid, light.a > 0.25 ? 1.0 : 0.0);
        }
    // Its alpha 0 where the probe holds no light, and two more than its move along the face's axis
    // where it does (faceHolds and faceMove).
    float4 place = held[heldAt(int3(3 * int(probes) + int(at.x), int(at.y), int(cascade.x * probes + at.z)))];
    float holds = valid > 0.5 && place.w > 0.5 ? 1.0 : 0.0;
    uint3 into = uint3(face * probes + at.x, at.y, cascade.x * probes + at.z);
    float3 layer = sum * (Pi / max(weights, 1e-6));
    cubes.Store(into, float4(layer, holds * (2.0 + place[face / 2u])));

    // The surface the face is read for, where the probe's own ray along the face's opposite way met
    // one. Each merged direction is moved to start from that point. A ray that met a surface keeps
    // its point, seen from there along a new way, its texel's share of the sphere scaled by the
    // square of how much nearer or farther that point lies from there, and one that met nothing keeps
    // its way, since light from past the interval arrives alike at both. Each is weighed by how
    // squarely it falls there and the whole scaled to pi, as the face's own light is. Where that ray
    // met nothing in its interval, or met a face from behind, as over a drop or beside a doorway's
    // edge, the face has no surface to be read for, and its light is its own with a distance of 0.
    int2 below = min(int2(octahedron(-axis) * float(texels)), int2(int(texels) - 1));
    float h = distances.Load(int4(int2(at.xy * texels) + below, int(at.z), 0));
    h = h > 0.0 && h < interval.x * 0.999 ? h : 0.0;
    float3 atSurface = layer;
    if (h > 0.0)
    {
        FieldInfo fi = info[0];
        float4 oc = probesOf(fi, int(interval.z)).cascades[cascade.x].origin_and_cell;
        float apart = oc.w * interval.y;
        float3 origin = oc.xyz + (float3(at) + 0.5 + place.xyz) * apart;
        float3 y = -axis * h;
        float3 sumAt = float3(0.0);
        float weightsAt = 0.0;
        for (uint v = 0u; v < texels; v++)
            for (uint u = 0u; u < texels; u++)
            {
                int4 from = int4(int2(at.xy * texels + uint2(u, v)), int(at.z), 0);
                float4 light = merged.Load(from);
                float2 uv = (float2(float(u), float(v)) + 0.5) / float(texels);
                float share = octahedronShare(uv);
                float3 d = fromOctahedron(uv);
                float t = abs(distances.Load(from));
                bool far = t >= interval.x * 0.999;
                float3 toward = far ? d : d * t - y;
                float length = far ? 1.0 : max(sqrt(dot(toward, toward)), 1e-4);
                float3 w = toward / length;
                float cosine = dot(w, axis);
                if (cosine <= 0.05)
                    continue;
                float solid = far ? share : share * t * t / (length * length);
                // How squarely the patch faces the surface against how squarely it faced the probe, and
                // none of one the probe met from behind or along its face.
                if (!far)
                {
                    float3 normal = fieldNormal(field, field_sampler, fi, origin + d * t);
                    float seenHere = -dot(d, normal);
                    float seenThere = max(-dot(w, normal), 0.0);
                    solid *= seenHere > 0.05 ? min(seenThere / seenHere, 8.0) : 0.0;
                }
                float before = kept.Load(from);
                sumAt += light.rgb * before * cosine * solid;
                weightsAt += cosine * solid;
            }
        if (weightsAt > 1e-6)
            atSurface = sumAt * (Pi / weightsAt);
    }
    cubes_at.Store(into, float4(atSurface, h));
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

    // The probe's light by way, eight by eight directions in a block with a texel's border.
    uint along = uint(WaysTexels) - 2u;
    uint stride = max(texels / along, 1u);
    for (uint k = face; k < along * along; k += 6u)
    {
        uint2 texel = uint2(k % along, k / along);
        // Its light along the direction, every merged direction weighed by its share of the sphere
        // and by its cosine to the direction to the power of 8, the lobe a surface of roughness 0.5
        // spreads its reflection over, so the eight by eight directions hold the light blurred as
        // that surface reflects it and read smoothly between them. A cascade of more directions is
        // read at one in each stride by stride of them.
        float3 toward = fromOctahedron((float2(texel) + 0.5) / float(along));
        float3 light = float3(0.0);
        float weights = 0.0;
        for (uint v = stride / 2u; v < texels; v += stride)
            for (uint u = stride / 2u; u < texels; u += stride)
            {
                float2 uv = (float2(u, v) + 0.5) / float(texels);
                float lobe = saturate(dot(fromOctahedron(uv), toward));
                lobe *= lobe;
                lobe *= lobe;
                float weight = lobe * lobe * octahedronShare(uv);
                if (weight <= 0.0)
                    continue;
                int4 from = int4(int2(at.xy * texels + uint2(u, v)), int(at.z), 0);
                light += merged.Load(from).rgb * kept.Load(from) * weight;
                weights += weight;
            }
        float4 way = float4(light / max(weights, 1e-6), 1.0);
        uint3 block = uint3(at.xy * uint(WaysTexels), cascade.x * probes + at.z);
        ways.Store(block + uint3(texel + 1u, 0u), way);
        // The border texels across the octahedron's edges that stand for this direction, an edge's
        // texels mirrored along it and each corner's the opposite corner's.
        uint last = along - 1u;
        if (texel.y == 0u)
            ways.Store(block + uint3(last + 1u - texel.x, 0u, 0u), way);
        if (texel.y == last)
            ways.Store(block + uint3(last + 1u - texel.x, last + 2u, 0u), way);
        if (texel.x == 0u)
            ways.Store(block + uint3(0u, last + 1u - texel.y, 0u), way);
        if (texel.x == last)
            ways.Store(block + uint3(last + 2u, last + 1u - texel.y, 0u), way);
        if (all(texel == uint2(last, last)))
            ways.Store(block, way);
        if (all(texel == uint2(0u, last)))
            ways.Store(block + uint3(last + 2u, 0u, 0u), way);
        if (all(texel == uint2(last, 0u)))
            ways.Store(block + uint3(0u, last + 2u, 0u), way);
        if (all(texel == uint2(0u, 0u)))
            ways.Store(block + uint3(last + 2u, last + 2u, 0u), way);
    }
}
