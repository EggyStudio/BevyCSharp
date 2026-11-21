// Traces one cascade's reach maps, how far each probe's ray along each of sixteen by sixteen ways
// goes from where the probe stands before it meets a surface, whether or not the surface lets light
// through, up to the cascade's end, or ReachOpen where it meets none first or leaves the field, a
// thread a reach texel (reachTexel), which the merge reads whether a probe above sees a ray's end
// by and where it meets a surface along the ray (gi_merge.slang). The faces' gather, the screen's
// probes and the leaves weigh a probe by the reach the trace keeps (gi_trace.slang), which passes
// the surfaces that let light through, since a floor under a crown takes the light of the probes
// above it, where reaching to the crown alone it read under half of its reference.
//
// It follows the cascade's trace, which moved each probe off the surfaces near it this frame, and
// reads where the trace left each probe standing.
import bcs_field;
// The probes' passes, written once in the RadianceCascades module for both engines, whose reach
// map's trace this is.
import rc_probe_passes;

Texture3D<float4> field;
SamplerState field_sampler;
StructuredBuffer<FieldInfo> info;

// Where the trace moved each probe to this frame, after each probe's own light, its share and the
// frame before's, laid out as gi_trace.slang lays it.
StructuredBuffer<float4> held;

// Every cascade's probes' reach maps, a texel a ray of sixteen by sixteen a probe, the cascades one
// after another along z, in 32-bit floats, which keep ReachOpen, where a 16-bit float written by a
// device that rounds toward zero would hold it short at its largest and read a surface met where
// none is.
[format("r32f")] RWTexture3D<float> reach_maps;

// x: the cascade. y: the probes along each side. z: how many cascades. w: unused.
uniform uint4 cascade;
// x: where the cascade's rays end. y: the probes' spacing in cells. zw: unused.
uniform float4 interval;

// The cascade's reach maps in the image, each probe a block of sixteen by sixteen texels.
struct ReachImage : IReachTexels
{
    void keepReach(uint index, float reached)
    {
        uint probes = cascade.y;
        uint texels = ReachTexels * ReachTexels;
        uint probe = index / texels;
        uint2 texel = uint2((index % texels) % ReachTexels, (index % texels) / ReachTexels);
        uint3 at = uint3(probe % probes, (probe / probes) % probes, probe / (probes * probes));
        reach_maps[uint3(at.xy * ReachTexels + texel, cascade.x * probes + at.z)] = reached;
    }
}

// Where the trace left each probe of the cascade standing, its grid's place moved by its move in
// its spacing.
struct HeldPlaces : IProbePlaces
{
    float3 corner;
    float apart;

    float3 placeOf(int3 probe)
    {
        int probes = int(cascade.y);
        uint at = (uint(int(cascade.x) * probes + probe.z) * uint(probes) + uint(probe.y)) * 4u * uint(probes) + uint(3 * probes + probe.x);
        return corner + (float3(probe) + held[at].xyz) * apart;
    }
}

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint probes = cascade.y;
    // The field's cascade the probes stand in, the far one for the last where the field has one
    // (probesOf), read by its index alone.
    int far = int(info[0].counts.w);
    int c = far > 0 && cascade.x + 1u == cascade.z ? far : int(cascade.x);
    float4 oc = info[0].cascades[c].origin_and_cell;
    float apart = oc.w * interval.y;
    ProbeCascade cascaded = { oc.xyz + 0.5 * apart, apart, int3(int(probes)), ReachTexels, 0.0, interval.x, 0u };
    // A cascade not yet built holds no probes, which reach nothing.
    if (oc.w <= 0.0)
    {
        if (id.x < cascadeReachTexels(cascaded))
        {
            ReachImage none;
            none.keepReach(id.x, 0.0);
        }
        return;
    }
    ReachImage image;
    HeldPlaces places = { cascaded.corner, apart };
    reachTexel(fieldMarch(field, field_sampler, info), image, places, cascaded, id.x);
}
