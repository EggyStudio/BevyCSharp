// The field as the sun sees it, a square map laid across each of the field's cascades square to the
// sun's way (bcs_field's SunMap), which the hold reads whether the sun reaches a held surface by
// (gi_hold.slang) in place of a march toward the sun from each, a held cell reading its own
// cascade's map, whose texels are a few tenths of its cells. Each texel marches the field down the
// sun's light from the map's plane past the coarsest cascade, through the finest cascade that holds
// each point as a march toward the sun does (traceField), to the far side of the cascade the map
// lies across, and keeps how far it went before it came within a tenth of a cell of a surface, or
// far where it met none, a thread a texel, a cascade along z. In a kernel of its own the marches
// cost a fraction of what the same marches cost inside the hold, whose shading keeps fewer of its
// threads running.
import bcs_field;
import bcs_gi;
// The field's distance, written once in the RadianceCascades module for both engines.
import rc_field_read;

Texture3D<float4> field;
SamplerState field_sampler;
StructuredBuffer<FieldInfo> info;
StructuredBuffer<GiLights> lights;
[format("r32f")] WTexture3D<float> sun_map;

// x: the texels along each side of each cascade's map.
uniform uint4 sun;

// The most steps a texel's march takes, enough for one that sets out at the coarsest cascade's side
// and crosses every finer cascade to the far side of the one the map lies across.
static const int SunSteps = 256;

// Where a ray from origin along way enters and leaves the box from low to high, as distances along
// it, the entry past the exit where it misses the box.
float2 throughBox(float3 origin, float3 way, float3 low, float3 high)
{
    float3 inverse = select(abs(way) < 1e-6, float3(1e6), 1.0 / way);
    float3 t0 = (low - origin) * inverse, t1 = (high - origin) * inverse;
    float3 near = min(t0, t1), far = max(t0, t1);
    return float2(max(max(near.x, near.y), near.z), min(min(far.x, far.y), far.z));
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint texels = sun.x;
    if (any(id.xy >= texels) || id.z >= uint(info[0].counts.x))
        return;
    FieldMarch march = fieldMarch(field, field_sampler, info);
    float3 l = lights[0].sunDirection.xyz;
    float size = float(march.size());
    float4 oc = march.cornerAndCell(int(id.z));
    float4 outer = march.cornerAndCell(int(info[0].counts.x) - 1);
    SunMap map = sunMapAcross(oc, size, l, outer);
    float met = MapFar;
    if (lights[0].sunDirection.w > 0.5 && oc.w > 0.0 && outer.w > 0.0)
    {
        float2 xy = (float2(id.xy) + 0.5) / float(texels) * 2.0 - 1.0;
        float3 origin = map.middle + (map.u * xy.x + map.v * xy.y) * map.radius + l * map.plane;
        float3 way = -l;
        // From half a cell past where the ray enters the coarsest cascade a cell inside its sides,
        // which a march reads from, so its first point lies within that margin where its entry,
        // taken exactly, may fall outside it by a rounding, to where it leaves the cascade the map
        // lies across.
        float2 into = throughBox(origin, way, outer.xyz + outer.w, outer.xyz + (size - 1.0) * outer.w);
        float2 across = throughBox(origin, way, oc.xyz + oc.w, oc.xyz + (size - 1.0) * oc.w);
        float t = max(into.x, 0.0) + 0.5 * outer.w;
        for (int i = 0; i < SunSteps && t < across.y && into.x < into.y; i++)
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
    }
    sun_map.Store(id, met);
}
