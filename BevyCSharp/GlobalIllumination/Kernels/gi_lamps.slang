// The field as each lamp that casts shadows sees it, a cube of six square faces about the lamp
// (bcs_field's lampMapWay), which the hold reads whether a lamp reaches a held surface by
// (gi_hold.slang) in place of a march toward the lamp from each. Each texel marches the field out
// from the lamp along its way, from two of the lamp's cells off it, as a march toward a lamp stops
// two cells short of it so the lamp's own fitting does not hide it (hiddenLampLight), out to the
// lamp's range, and keeps how far from the lamp it came within a tenth of a cell of a surface, far
// where it met none, or the distance it reached as a negative where its steps ran out first, past
// which the hold marches. The first thread also lists the lamps whose range meets the field, so the
// hold passes over a lamp that reaches none of its cells before any cell asks. A thread a texel, a
// lamp along z.
import bcs_field;
import bcs_gi;
// The field's distance, written once in the RadianceCascades module for both engines.
import rc_field_read;

Texture3D<float4> field;
SamplerState field_sampler;
StructuredBuffer<FieldInfo> info;
StructuredBuffer<GiLights> lights;
[format("r32f")] WTexture3D<float> lamp_maps;
// How many lamps reach the field, and after it each one's index among the lights.
RWStructuredBuffer<uint> lamps_reaching;

// x: the texels along each side of a face of a lamp's map.
uniform uint4 lamp;

// The most steps a texel's march takes.
static const int LampSteps = 128;

// The most lamps the lights carry (GiLights).
static const int MostLamps = 16;

// Whether the light reaches the box of the field's coarsest cascade, whose corner and cell are
// outer, every lamp without a range reaching it.
bool reachesField(GiLight light, float4 outer, float size)
{
    float range = light.directionAndRange.w;
    if (range <= 0.0)
        return true;
    float3 nearest = clamp(light.positionAndKind.xyz, outer.xyz, outer.xyz + size * outer.w);
    return distance(nearest, light.positionAndKind.xyz) <= range;
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    FieldMarch march = fieldMarch(field, field_sampler, info);
    int count = min(int(lights[0].counts.x), MostLamps);
    float size = float(march.size());
    float4 outer = march.cornerAndCell(int(info[0].counts.x) - 1);
    if (all(id == uint3(0u)))
    {
        uint listed = 0u;
        for (int i = 0; i < count; i++)
            if (reachesField(lights[0].lights[i], outer, size))
            {
                lamps_reaching[1u + listed] = uint(i);
                listed++;
            }
        lamps_reaching[0] = listed;
    }
    uint texels = lamp.x;
    uint face = id.x / texels;
    if (face >= 6u || id.y >= texels || id.z >= uint(MostLamps))
        return;
    GiLight light = lights[0].lights[id.z];
    float met = MapFar;
    if (int(id.z) < count && light.colorAndShadow.w >= 0.5 && reachesField(light, outer, size))
    {
        float3 origin = light.positionAndKind.xyz;
        int at = march.cascadeOf(origin, 1.0);
        // A lamp outside every cascade has no map, and the hold marches toward it.
        met = -1e-3;
        if (at >= 0)
        {
            float2 uv = (float2(float(id.x % texels), float(id.y)) + 0.5) / float(texels) * 2.0 - 1.0;
            float3 way = lampMapWay(face, uv);
            float range = light.directionAndRange.w > 0.0 ? light.directionAndRange.w : 1e5;
            float t = 2.0 * march.cornerAndCell(at).w;
            met = MapFar;
            int i = 0;
            for (; i < LampSteps && t < range; i++)
            {
                float3 p = origin + way * t;
                int c = march.cascadeOf(p, 1.0);
                if (c < 0)
                    break;
                float cell = march.cornerAndCell(c).w;
                float d = fieldDistanceIn(march, c, p);
                if (d < cell * MarchHit)
                {
                    met = t;
                    break;
                }
                t += max(d, cell * MarchStep);
            }
            if (i == LampSteps)
                met = -t;
        }
    }
    lamp_maps.Store(id, met);
}
