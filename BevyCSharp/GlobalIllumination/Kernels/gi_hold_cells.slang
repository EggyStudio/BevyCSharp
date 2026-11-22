// The cells of the field the hold shades this frame (gi_hold.slang), listed one after another, so
// each of the hold's threads takes a listed cell and every thread of a workgroup shades, where a
// thread for each cell of the whole field leaves most threads of a workgroup idle beside the few
// within a cell of a surface, and the workgroup takes as long as one that shades whole. A thread a
// cell, the cells of the field's cascades counted one after another as holdCellLight counts them,
// each cell the module's own test holds (holdCellLight, through a shading that lists the cell and
// shades nothing) listed by its index.
import bcs_field;
// The probes' passes, written once in the RadianceCascades module for both engines.
import rc_probe_passes;

Texture3D<float4> field;
SamplerState field_sampler;
StructuredBuffer<FieldInfo> info;

// The cells listed, by their index, and how many.
RWStructuredBuffer<uint> held_list;
RWStructuredBuffer<Atomic<uint>> held_listed;

// The index of the thread's cell, and whether it is listed yet, since a cell holding two sides is
// shaded at each.
static uint cellIndex;
static bool cellListed;

// A shading that lists the cell it is asked to shade and gives no light (IHeldShading).
struct ListedCells : IHeldShading
{
    float3 ownLight(float3 place, float3 way, int c, int3 cell)
    {
        if (!cellListed)
        {
            cellListed = true;
            held_list[held_listed[0].add(1u)] = cellIndex;
        }
        return float3(0.0);
    }

    float3 probeLight(float3 place, float3 way, int c, int3 cell) { return float3(0.0); }
}

// Held light that holds nothing (IHeldLight), the listing writing no light.
struct NoHeldLight : IHeldLight
{
    float3 heldAt(int c, int3 cell, uint side) { return float3(0.0); }
    float3 heldProbeLightAt(int c, int3 cell, uint side) { return float3(0.0); }
    float probeShare(float3 place, int c, int3 cell) { return 1.0; }
    void hold(int c, int3 cell, uint side, float3 own, float3 probes) {}
}

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    cellIndex = id.x;
    cellListed = false;
    FieldMarch march = fieldMarch(field, field_sampler, info);
    ListedCells listing;
    NoHeldLight none;
    holdCellLight(march, listing, none, uint(info[0].counts.x), 1u, 0u, id.x);
}
