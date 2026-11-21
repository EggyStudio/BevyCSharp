// The light held for each cell of the field that lies within a cell of a surface, held each frame
// before the merges, which read it where their marches meet a surface (gi_merge.slang, the
// RadianceCascades module's HeldHitLight) in place of shading the surface there. Each of a cell's
// two sides (the module's heldSide) holds the light that side sends as a probe's ray that met it
// shades it (shadeProbeHit), its own light, its emission and the light of the sun and the lamps,
// apart from the light of the probes it reflects, which is held taken whole and weighed at a hit by
// the share of the indirect light the merged probe takes. A thread holds a cell (holdCellLight).
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

// The light each side of each cell's surface sends of its own and of the probes it reflects, the
// field's cascades one after another along z, all their side 0s and then all their side 1s.
[format("rgba16f")] WTexture3D<float4> held_own;
[format("rgba16f")] WTexture3D<float4> held_probes;

// y: the probes along each side of a cascade.
uniform uint4 cascade;
// z: the probes' spacing in cells. w: how many cascades of probes.
uniform float4 interval;

// The light of the probes the surface last shaded reflects, each thread's own, shaded with its own
// light in one shading (ownLight) and handed on after it (probeLight).
static float3 shadedProbes;

// A held surface shaded as a probe's hit is (IHeldShading), the probes' light taken whole.
struct HoldShading : IHeldShading
{
    float3 ownLight(float3 place, float3 way, int c, int3 cell)
    {
        FieldInfo fieldInfo = info[0];
        GiLights lit = lights[0];
        lit.sunColor.rgb *= air[0].rgb;
        lit.counts.z = 1.0;
        float3 own;
        float3 light = shadeProbeHit(field, field_sampler, fieldInfo, gatheredAt(fieldInfo, lit, int(interval.w)), albedo, faces, transmission, glow, lit,
            glow_lights, bounced, bounced_at, ways, place, way, float(cascade.y), interval.z, interval.w, float3(1.0), own);
        shadedProbes = light - own;
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

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    FieldMarch march = fieldMarch(field, field_sampler, info);
    HoldShading shading;
    HeldCells cells = { march.size(), int(info[0].counts.x) };
    holdCellLight(march, shading, cells, uint(info[0].counts.x), 1u, 0u, id.x);
}
