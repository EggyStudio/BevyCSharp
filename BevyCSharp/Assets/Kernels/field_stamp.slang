// Stamps the boxes of the meshes that moved into the field the shaders read, a workgroup a brick of
// 4 by 4 by 4 cells. Each cell takes the least of its distance in the field the still meshes make
// and its distance to each box that comes within its brick, a box around a mesh in its own space, so
// a brick stamped last frame and not this one goes back to what the still meshes make. A brick reads
// the boxes of its own run alone, so a frame's cost grows with the boxes of each brick and not with
// every box the frame stamps. Each cell is painted the color of the nearer surface, the still
// meshes' or a box's, with that surface's face, as the build paints a cell its nearest triangle's,
// so indirect light from a moving mesh takes its color, and a cell a box painted takes the still
// meshes' color again once the box has left.
//
// Taken from 3DEngine's field_stamp.slang. The still images are read as textures, since an image of
// half floats or of bytes is written here and not read and written at once.
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
    // rgb: the mesh's color, linear. w: the share of light that passes a unit of its depth
    // (lightThroughPaint in bcs_field.slang).
    float4 color;
};

// x: the cascade. y: the brick's place in it, in bricks, a byte an axis. z: where its run of boxes
// begins. w: how many boxes are in it. Past the bricks, the runs one after another, four boxes'
// places to an element.
StructuredBuffer<uint4> bricks;

StructuredBuffer<FieldShape> shapes;

Texture3D<float> still_field;

[format("r16f")] RWTexture3D<float> field;

// The colors and the faces the still meshes paint, as field_resolve.slang wrote them, and those the
// passes read.
Texture3D<float4> still_paint;
Texture3D<uint> still_faces;
[format("rgba8")] RWTexture3D<float4> albedo_field;
[format("r32ui")] RWTexture3D<uint> face_field;

StructuredBuffer<FieldInfo> info;

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
    FieldInfo field_info = info[0];
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
    float nearest = abs(d);
    float4 paint = still_paint.Load(int4(int3(at), 0));
    uint face = still_faces.Load(int4(int3(at), 0));
    for (uint i = 0; i < brick.w; i++)
    {
        uint run = stamp_counts.x * 4u + brick.z + i;
        FieldShape shape = shapes[bricks[run / 4u][run % 4u]];
        float4 h = float4(p, 1.0);
        float3 own = float3(dot(shape.row0, h), dot(shape.row1, h), dot(shape.row2, h)) - shape.center_and_scale.xyz;
        float3 q = abs(own) - shape.extents.xyz;
        float box = (length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0)) * shape.center_and_scale.w;
        d = min(d, max(box, -reach));
        if (abs(box) < min(nearest, reach))
        {
            nearest = abs(box);
            float coverage = lightThroughPaint(shape.color.w);
            paint = float4(shape.color.rgb * coverage, coverage);
            // The box's face nearest the cell, the axis it lies furthest out along, in the world.
            int axis = q.x >= q.y && q.x >= q.z ? 0 : q.y >= q.z ? 1 : 2;
            float3 outward = float3(axis == 0 ? sign(own.x) : 0.0, axis == 1 ? sign(own.y) : 0.0, axis == 2 ? sign(own.z) : 0.0);
            face = faceBits(normalize(shape.row0.xyz * outward.x + shape.row1.xyz * outward.y + shape.row2.xyz * outward.z));
        }
    }
    field[at] = d;
    albedo_field[at] = paint;
    face_field[at] = face;
}
