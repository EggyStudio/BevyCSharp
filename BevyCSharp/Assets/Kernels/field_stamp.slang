// Stamps the boxes of the meshes that moved into the field the shaders read, a workgroup a brick of
// 4 by 4 by 4 cells. Each cell takes the least of its distance in the field the still meshes make
// and its distance to each box that comes within its brick, a box around a mesh in its own space, so
// a brick stamped last frame and not this one goes back to what the still meshes make. A brick reads
// the boxes of its own run alone, so a frame's cost grows with the boxes of each brick and not with
// every box the frame stamps.
//
// Taken from 3DEngine's field_stamp.slang, its distances alone, a box's color and face coming with
// global illumination. The still field is read as a texture, since an image of half floats is
// written here and not read and written at once.
import bcs_field;

struct FieldShape
{
    // The rows of the matrix from the world into the box's own space, a 3x4.
    float4 row0;
    float4 row1;
    float4 row2;
    // xyz: the box's middle in its own space. w: the least scale from its own space to the world's,
    // which a distance there is turned into a world distance no longer than the true one by.
    float4 center_and_scale;
    // xyz: the box's half extents in its own space.
    float4 extents;
    // rgb: the mesh's color, linear.
    float4 color;
};

// x: the cascade. y: the brick's place in it, in bricks, a byte an axis. z: where its run of boxes
// begins. w: how many boxes are in it. Past the bricks, the runs one after another, four boxes'
// places to an element.
StructuredBuffer<uint4> bricks;

StructuredBuffer<FieldShape> shapes;

Texture3D<float> still_field;

[format("r16f")] RWTexture3D<float> field;

StructuredBuffer<bcs_field::Info> info;

// x: how many bricks. y: how many boxes. zw: unused.
uniform uint4 stamp_counts;

[shader("compute")]
[numthreads(4, 4, 4)]
void main(uint3 group : SV_GroupID, uint3 local : SV_GroupThreadID)
{
    uint index = group.x;
    if (index >= stamp_counts.x)
        return;
    uint4 brick = bricks[index];
    uint cascade = brick.x;
    bcs_field::Info field_info = info[0];
    uint size = uint(field_info.counts.y);
    uint3 place = uint3(brick.y & 0xFFu, (brick.y >> 8) & 0xFFu, (brick.y >> 16) & 0xFFu);
    uint3 cell = place * 4u + local;
    if (any(cell >= size))
        return;
    float4 oc = field_info.cascades[cascade].origin_and_cell;
    float3 p = oc.xyz + (float3(cell) + 0.5) * oc.w;
    float reach = field_info.counts.z * oc.w;

    uint3 at = uint3(cell.xy, cell.z + cascade * size);
    float d = still_field.Load(int4(int3(at), 0));
    for (uint i = 0; i < brick.w; i++)
    {
        uint run = stamp_counts.x * 4u + brick.z + i;
        FieldShape shape = shapes[bricks[run / 4u][run % 4u]];
        float4 h = float4(p, 1.0);
        float3 own = float3(dot(shape.row0, h), dot(shape.row1, h), dot(shape.row2, h)) - shape.center_and_scale.xyz;
        float3 q = abs(own) - shape.extents.xyz;
        float box = (length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0)) * shape.center_and_scale.w;
        d = min(d, max(box, -reach));
    }
    field[at] = d;
}
