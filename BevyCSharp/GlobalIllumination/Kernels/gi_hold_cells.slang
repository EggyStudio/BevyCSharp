// The cells of the field the hold shades this frame (gi_hold.slang), listed one after another, so
// each of the hold's threads takes a listed cell and every thread of a workgroup shades, where a
// thread for each cell of the whole field leaves most threads of a workgroup idle beside the few
// within a cell of a surface, and the workgroup takes as long as one that shades whole. A thread a
// cell, the cells of the field's cascades counted one after another, each cell the module's own
// test holds listed by its index (listHeldCell).
import bcs_field;
// The probes' passes, written once in the RadianceCascades module for both engines.
import rc_probe_passes;

Texture3D<float4> field;
SamplerState field_sampler;
StructuredBuffer<FieldInfo> info;

// The cells listed, by their index, and how many, which the frame sets to none before the listing.
RWStructuredBuffer<uint> held_list;
RWStructuredBuffer<Atomic<uint>> held_listed;

// The list the module fills (IHeldList), over the hold's buffers.
struct ListedCells : IHeldList
{
    void list(uint at) { held_list[held_listed[0].add(1u)] = at; }
    uint count() { return held_listed[0].load(); }
    uint listedAt(uint index) { return held_list[index]; }
}

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    FieldMarch march = fieldMarch(field, field_sampler, info);
    ListedCells listing;
    listHeldCell(march, listing, uint(info[0].counts.x), 1u, 0u, id.x);
}
