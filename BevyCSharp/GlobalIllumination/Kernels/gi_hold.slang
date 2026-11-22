// The light held for each cell of the field that lies within a cell of a surface, held each frame
// before the merges, which read it where their marches meet a surface (gi_merge.slang, the
// RadianceCascades module's HeldHitLight) in place of shading the surface there. Each of a cell's
// two sides (the module's heldSide) holds the light that side sends back along a ray that met it,
// as a probe's ray's hit is lit (shadeProbeHit), its own light, its emission and the light of the
// sun, the lamps and the glow lights, apart from the light of the probes it reflects, which is held
// taken whole and weighed at a hit by the share of the indirect light the merged probe takes. What
// reaches a side is found with few marches. The sun reaches it as the sun's map says (gi_sun.slang)
// and each lamp that casts shadows as its map says (gi_lamps.slang), a march left where no map
// holds the point or the sun's texels about it differ, only the lamps whose range meets the field
// are asked, and the probes about it are seen through the reach their rays had (probeSees) rather
// than a march to each. The table of the field's cascades is read from its buffer by index, never
// copied into a thread's memory. A thread holds one of the cells the listing found to hold this
// frame (gi_hold_cells.slang, holdListedCell), so every thread of a workgroup shades.
import bcs_field;
import bcs_gi;
import bcs_glow;
// The probes' passes, written once in the RadianceCascades module for both engines.
import rc_probe_passes;

Texture3D<float4> field;
SamplerState field_sampler;
StructuredBuffer<FieldInfo> info;
Texture3D<float4> albedo;
Texture3D<uint> faces;
Texture3D<float> transmission;
Texture3D<float4> glow;
StructuredBuffer<GiLights> lights;
StructuredBuffer<float4> air;
StructuredBuffer<GlowLight> glow_lights;
Texture3D<float4> bounced;
Texture3D<float4> bounced_at;
Texture3D<float4> ways;
// How far each probe's rays reached each way the frame before, as the probes' faces were gathered.
Texture3D<float> reach;

// The field as the sun sees it (gi_sun.slang) and as each lamp does (gi_lamps.slang), and the lamps
// whose range meets the field, their count first.
Texture3D<float> sun_map;
Texture3D<float> lamp_maps;
StructuredBuffer<uint> lamps_reaching;

// The glow lights whose reach meets each block of cells (gi_glow_blocks.slang), each block's count
// and then their indices.
StructuredBuffer<uint> glow_blocks;

// The light each side of each cell's surface sends of its own and of the probes it reflects, the
// field's cascades one after another along z, all their side 0s and then all their side 1s.
// The cells the listing found to hold this frame, by their index, and how many.
StructuredBuffer<uint> held_list;
StructuredBuffer<uint> held_listed;

[format("rgba16f")] WTexture3D<float4> held_own;
[format("rgba16f")] WTexture3D<float4> held_probes;

// y: the probes along each side of a cascade.
uniform uint4 cascade;
// z: the probes' spacing in cells. w: how many cascades of probes.
uniform float4 interval;
// x: the texels along each side of each cascade's map of the field as the sun sees it.
uniform uint4 sun;
// x: the texels along each side of a face of a lamp's map.
uniform uint4 lamp;
// x: the cells along each side of a block the glow lights are listed for.
uniform uint4 glow_block;

// The sun's light reaching a face at p facing n in the field's cascade c, whose cells are cell
// wide, as directLight takes it, none where the face looks away or there is no sun. A march toward
// the sun from a cell and a half off the face, past its first cell, finds whether it reaches, and
// the cascade's map answers the same, the sun reaching the point a cell and a half off the face
// where the first surface the map's texel met lies no more than a cell short of it, or past it, as
// much again allowed as half a texel leans across a face the sun meets slanting. Each of the four
// texels whose middles lie about the point answers so, and where they differ, a shadow's edge
// passes between their middles and the point's own march answers, as it does where the map does
// not lie across the point. A texel alone answers for points up to a texel from its middle, and
// moves a sunlit patch's edges by as much.
float3 sunReaching(GiLights lit, FieldMarch march, int c, float3 p, float3 n, float cell)
{
    float3 l = lit.sunDirection.xyz;
    float facing = dot(n, l);
    if (lit.sunDirection.w <= 0.5 || facing <= 0.0)
        return float3(0.0);
    float3 from = p + n * (cell * 1.5);
    SunMap map = sunMapAcross(march.cornerAndCell(c), float(march.size()), l, march.cornerAndCell(int(info[0].counts.x) - 1));
    float2 xy;
    float depth = sunMapDepth(map, from, xy);
    bool reaches = false;
    bool marched = true;
    if (all(abs(xy) < 1.0))
    {
        int2 base = int2(floor((xy * 0.5 + 0.5) * float(sun.x) - 0.5));
        float slant = sqrt(max(1.0 - facing * facing, 0.0)) / max(facing, 0.05);
        float allowed = map.radius / float(sun.x) * min(slant, 4.0);
        uint lit = 0u;
        for (int k = 0; k < 4; k++)
        {
            int2 texel = clamp(base + int2(k & 1, k >> 1), int2(0), int2(int(sun.x) - 1));
            lit += depth - cell <= sun_map.Load(int4(texel, c, 0)) + allowed ? 1u : 0u;
        }
        reaches = lit == 4u;
        marched = lit != 0u && lit != 4u;
    }
    if (marched)
        reaches = traceField(march, from, l, cell, 1e5) < 0.0;
    return reaches ? lit.sunColor.rgb * facing : float3(0.0);
}

// Whether the field stands between the lamp i, whose light is light, and the point from a cell and
// a half off a face at p facing n, as hiddenLampLight finds it, by the lamp's map, its depth kept
// as the sun's is, or by a march toward the lamp stopping two cells short of it where the map's
// steps ran out short of the point or the lamp stands outside every cascade.
bool lampHidden(GiLight light, uint i, FieldMarch march, float3 from, float3 n, float cell)
{
    float3 toward = light.positionAndKind.xyz - from;
    float length = length(toward);
    if (length <= cell * 2.0)
        return false;
    float3 l = toward / length;
    float2 uv;
    uint face = lampMapFace(-l, uv);
    uint texels = lamp.x;
    uint2 texel = min(uint2((uv * 0.5 + 0.5) * float(texels)), uint2(texels - 1u));
    float met = lamp_maps.Load(int4(int(face * texels + texel.x), int(texel.y), int(i), 0));
    if (met < 0.0)
        return length > -met && traceField(march, from, l, 0.0, length - cell * 2.0) >= 0.0;
    float facing = saturate(dot(n, l));
    float slant = sqrt(max(1.0 - facing * facing, 0.0)) / max(facing, 0.05);
    return length > met + 1.5 * cell + 2.0 * length / float(texels) * min(slant, 4.0);
}

// The glow lights listed for the block of cells holding a held cell (IGlowLightList).
struct BlockGlowLights : IGlowLightList
{
    uint first;

    uint count() { return glow_blocks[first]; }

    uint at(uint i) { return glow_blocks[first + 1u + i]; }
}

// The glow lights listed for the block holding the cell of the field's cascade c.
BlockGlowLights glowLightsNear(int c, int3 cell)
{
    uint side = uint(info[0].counts.y) / glow_block.x;
    uint3 block = uint3(cell) / glow_block.x;
    BlockGlowLights near = { (((uint(c) * side + block.z) * side + block.y) * side + block.x) * (1u + uint(MostGlowLights)) };
    return near;
}

// The light reaching a face at p facing n from the sun, from each lamp whose range meets the field,
// those that cast shadows hidden where the field stands between, and from the two brightest glow
// lights, as shadeProbeHit takes it, the ray along way.
float3 lightsReachingHeld(GiLights lit, FieldInfo fieldInfo, FieldMarch march, int c, int3 at, float3 p, float3 n, float3 way, float cell)
{
    float3 arrived = sunReaching(lit, march, c, p, n, cell);
    float3 from = p + n * (cell * 1.5);
    uint count = lamps_reaching[0];
    for (uint k = 0u; k < count; k++)
    {
        uint i = lamps_reaching[1u + k];
        GiLight light = lit.lights[i];
        float result = lampLight(light, p, n);
        if (result > 0.0 && !(light.colorAndShadow.w >= 0.5 && lampHidden(light, i, march, from, n, cell)))
            arrived += light.colorAndShadow.rgb * result;
    }
    return arrived + glowLightsIn(field, field_sampler, fieldInfo, glow_lights, glowLightsNear(c, at), p, n, 2, false, way) / Pi;
}

// The cascade of probes the gather left holding p, at least a cell inside its sides, and in oc its
// corner where the probes stood as their faces were gathered (GiLights.before) and its cell, the
// last standing in the field's far cascade where the field has one, as gatheredAt and probesOf
// place them, or -1 where none holds p.
int probesHolding(float3 p, out float4 oc)
{
    int probes = int(interval.w);
    int far = int(info[0].counts.w);
    int count = min(probes, int(info[0].counts.x));
    float size = info[0].counts.y;
    oc = float4(0.0);
    for (int k = 0; k < count; k++)
    {
        float4 at = info[0].cascades[far > 0 && k == probes - 1 ? far : k].origin_and_cell;
        if (k < MostProbeCascades)
            at.xyz = lights[0].before[k].xyz;
        if (at.w <= 0.0)
            continue;
        float3 cell = (p - at.xyz) / at.w;
        if (all(cell >= 1.0) && all(cell <= size - 1.0))
        {
            oc = at;
            return k;
        }
    }
    return -1;
}

// The indirect light reaching a face at p facing n, irradiance, from the faces of the eight probes
// about it of the cascade holding it, as the trace's hits read it (bouncedSeenAt), each probe
// weighed by how near it is and how squarely it stands in front of the face, passed over where it
// stands inside a mesh or behind the face's plane, its faces read toward the light at the surfaces
// they were read for, but seen through the reach its rays had that way (probeSees) rather than by a
// march to it. In along, the light the same probes hold along r for a face of roughness.
float3 bounceReaching(float3 p, float3 n, float3 r, float roughness, out float3 along)
{
    along = float3(0.0);
    float4 oc;
    int c = probesHolding(p, oc);
    if (c < 0)
        return float3(0.0);
    float probes = float(cascade.y);
    float apart = oc.w * interval.z;
    float3 q = clamp((p - oc.xyz) / apart - 0.5, 0.0, probes - 1.0);
    int3 base = int3(floor(q));
    float3 f = q - float3(base);
    float3 n2 = n * n;
    float3 sum = float3(0.0);
    float3 alongSum = float3(0.0);
    float weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 at = min(base + offset, int3(int(probes) - 1));
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float3 light = float3(0.0);
        float3 lightAt = float3(0.0);
        float3 reachAt = float3(0.0);
        float valid = 1.0;
        float3 moved = float3(0.0);
        for (int axis = 0; axis < 3; axis++)
        {
            int4 where = int4((axis * 2 + (n[axis] < 0.0 ? 1 : 0)) * int(probes) + at.x, at.y, c * int(probes) + at.z, 0);
            float4 texel = bounced.Load(where);
            float4 texelAt = bounced_at.Load(where);
            light += texel.rgb * n2[axis];
            lightAt += texelAt.rgb * n2[axis];
            reachAt[axis] = texelAt.a;
            valid = min(valid, faceHolds(texel.a));
            moved[axis] = faceMove(texel.a);
        }
        float3 probe = oc.xyz + (float3(at) + 0.5 + moved) * apart;
        if (valid < 0.5 || dot(probe - p, n) <= 0.0)
            continue;
        float weight = lerps.x * lerps.y * lerps.z * probeFacing(p, n, probe) * probeSees(reach, p, n, probe, at, c, probes, apart);
        if (weight <= 0.0)
            continue;
        float h = dot(reachAt, n2);
        light = lerp(light, lightAt, h > 1e-3 ? saturate(dot(probe - p, n) / h) : 0.0);
        sum += light * weight;
        alongSum += probeAlong(ways, bounced, field_sampler, at, c, r, roughness, probes, interval.w) * weight;
        weights += weight;
    }
    if (weights <= 1e-5)
        return float3(0.0);
    along = alongSum / weights;
    return sum / weights;
}

// The light of the probes the surface last shaded reflects, each thread's own, shaded with its own
// light in one shading (ownLight) and handed on after it (probeLight).
static float3 shadedProbes;

// A held surface shaded as a probe's hit is (IHeldShading), the probes' light taken whole, its
// gloss toward the ray by the split sum and its diffuse share past its metal of what the gloss
// leaves, and a surface that sends some of its diffuse light through, as a leaf does, lit on both
// its sides as shadeSendingThrough lights it.
struct HoldShading : IHeldShading
{
    float3 ownLight(float3 place, float3 way, int c, int3 cell)
    {
        FieldInfo fieldInfo = info[0];
        FieldMarch march = fieldMarch(field, field_sampler, info);
        GiLights lit = lights[0];
        lit.sunColor.rgb *= air[0].rgb;
        float wide = march.cornerAndCell(c).w;
        float3 n = fieldNormal(field, field_sampler, fieldInfo, place);
        float3 gloss;
        float3 paint = surfaceFacing(faces, albedo, field_sampler, fieldInfo, place, n, gloss);
        float through = transmissionAt(transmission, fieldInfo, place);
        // The ray meets the side away from n where it runs out of a surface letting light through.
        bool farSide = through > 0.0 && dot(n, way) > 0.0 && lightThroughAt(albedo, fieldInfo, place) > 0.0;
        float3 seen = farSide ? -n : n;
        float2 ab = splitSum(gloss.x, saturate(dot(seen, -way)));
        float3 f0 = lerp(float3(0.16 * gloss.z * gloss.z), paint, gloss.y);
        float3 glossy = f0 * ab.x + ab.y;
        float3 diffuse = paint * (1.0 - gloss.y) * saturate(1.0 - glossy);
        float3 along;
        float3 bounce = bounceReaching(place, seen, reflect(way, seen), gloss.x, along);
        float3 own = surfaceGlowUncarried(glow, albedo, field_sampler, fieldInfo, place);
        float3 litOn = lightsReachingHeld(lit, fieldInfo, march, c, cell, place, n, way, wide);
        if (through > 0.0)
        {
            float3 litOff = lightsReachingHeld(lit, fieldInfo, march, c, cell, place, -n, way, wide);
            float3 litAway = farSide ? litOn : litOff, litSeen = farSide ? litOff : litOn;
            own += diffuse * (1.0 - through) * litOn + diffuse * through * litAway + glossy * litSeen;
        }
        else
            own += (diffuse + glossy) * litOn;
        shadedProbes = diffuse / Pi * bounce + glossy * along;
        return own;
    }

    float3 probeLight(float3 place, float3 way, int c, int3 cell) { return shadedProbes; }
}

// The held light as the hold writes it (IHeldLight), laid out as gi_merge.slang reads it.
struct HeldCells : IHeldLight
{
    int size;
    int cascades;

    float3 heldAt(int c, int3 cell, uint side) { return float3(0.0); }
    float3 heldProbeLightAt(int c, int3 cell, uint side) { return float3(0.0); }
    float probeShare(float3 place, int c, int3 cell) { return 1.0; }

    void hold(int c, int3 cell, uint side, float3 own, float3 probes)
    {
        uint3 at = uint3(cell.xy, uint((int(side) * cascades + c) * size + cell.z));
        held_own.Store(at, float4(own, 1.0));
        held_probes.Store(at, float4(probes, 1.0));
    }
}

// The cells the listing found (IHeldList), which the hold reads and adds none to.
struct Listed : IHeldList
{
    void list(uint at) {}
    uint count() { return held_listed[0]; }
    uint listedAt(uint index) { return held_list[index]; }
}

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    FieldMarch march = fieldMarch(field, field_sampler, info);
    HoldShading shading;
    HeldCells cells = { march.size(), int(info[0].counts.x) };
    Listed listed;
    holdListedCell(march, shading, cells, listed, id.x);
}
