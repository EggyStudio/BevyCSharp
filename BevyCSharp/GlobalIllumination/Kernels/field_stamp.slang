// Stamps the boxes of the meshes that moved into the field the shaders read, a workgroup a brick of
// 4 by 4 by 4 cells. Each cell takes the least of its distance in the field the still meshes make
// and its distance to each box that comes within its brick, a box around a mesh in its own space,
// so a brick stamped last frame and not this one goes back to what the still meshes make. A brick
// reads the boxes of its own run alone, so a frame's cost grows with the boxes of each brick and
// not with every box the frame stamps. Each cell is painted the color of the nearer surface, the
// still meshes' or a box's, with that surface's face and the share of its light it sends through,
// as the build paints a cell its nearest triangle's, so indirect light from a moving mesh takes its
// color, and a cell a box painted takes the still meshes' color again once the box has left.
//
// The still images are read as textures, since an image of half floats or of bytes is written here
// and not read and written at once.
import bcs_field;
// A cell as the boxes of the meshes that moved stamp it over the still meshes' field, written once
// in the RadianceCascades module for both engines.
import rc_field_stamp;

struct FieldShape
{
    // The rows of the matrix from the world into the box's own space, a 3x4.
    float4 row0;
    float4 row1;
    float4 row2;
    // xyz: the box's middle in its own space. w: the least scale from its own space to the world's,
    // which a distance there is turned into a world distance at most the true one by.
    float4 center_and_scale;
    // xyz: the box's half extents in its own space.
    float4 extents;
    // rgb: the mesh's color, linear. w: the share of light that passes a unit of its depth
    // (lightThroughAlpha in bcs_field.slang).
    float4 color;
    // xyz: the mesh's perceptual roughness, metallic share and reflectance. w: the share of its
    // diffuse light it sends through to its far side, its diffuse transmission.
    float4 gloss;
};

// x: the cascade. y: the brick's place in it, in bricks, a byte an axis. z: where its run of boxes
// begins. w: how many boxes are in it. Past the bricks, the runs one after another, four boxes'
// places to an element.
StructuredBuffer<uint4> bricks;

StructuredBuffer<FieldShape> shapes;

Texture3D<float4> still_field;

[format("rgba16f")] WTexture3D<float4> field;

// The colors, the faces and the shares sent through the still meshes paint, as field_resolve.slang
// wrote them, and those the passes read.
Texture3D<float4> still_paint;
Texture3D<uint> still_faces;
Texture3D<float> still_transmission;
[format("rgba8")] RWTexture3D<float4> albedo_field;
[format("r32ui")] RWTexture3D<uint> face_field;
[format("r16f")] RWTexture3D<float> transmission_field;

StructuredBuffer<FieldInfo> info;

// x: how many bricks. y: how many boxes. zw: unused.
uniform uint4 stamp_counts;

// The boxes of a brick's run as the module's stamp reads them (IStampShapes), each this engine's
// FieldShape laid out as the module's StampShape. The module paints a box's color times the alpha
// of its lightThroughPaint, where this engine keeps a share's alpha on an even byte below a solid's
// and a crown's leaf cards on an odd one (lightThroughAlpha), so a box is handed the share whose
// alpha is this engine's, which the module's paint takes back exactly. A box emits nothing here,
// the field's glow staying as the still meshes painted it, and the share of its diffuse light it
// sends through is its gloss's w.
struct BrickShapes : IStampShapes
{
    uint first;
    uint length;

    uint count() { return length; }

    StampShape shape(uint i)
    {
        uint run = first + i;
        FieldShape own = shapes[bricks[run / 4u][run % 4u]];
        StampShape shape;
        shape.row0 = own.row0;
        shape.row1 = own.row1;
        shape.row2 = own.row2;
        shape.centerAndScale = own.center_and_scale;
        shape.extents = own.extents;
        shape.color = float4(own.color.rgb, 2.0 * (1.0 - lightThroughAlpha(own.color.w)));
        shape.emission = float4(0.0, 0.0, 0.0, own.gloss.w);
        shape.gloss = float4(own.gloss.xyz, 0.0);
        return shape;
    }
}

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

    // The still cell as the module reads it, its distance and the plane it keeps, with no glow the
    // stamp writes.
    uint3 at = uint3(cell.xy, cell.z + cascade * size);
    float4 kept = still_field.Load(int4(int3(at), 0));
    FieldCell still;
    still.distance = kept.x;
    still.sheet = kept.yzw;
    still.paint = still_paint.Load(int4(int3(at), 0));
    still.glow = float4(0.0);
    still.face = still_faces.Load(int4(int3(at), 0));
    still.through = still_transmission.Load(int4(int3(at), 0));
    BrickShapes shapes = { stamp_counts.x * 4u + brick.z, brick.w };
    FieldCell stamped = stampCell(shapes, p, reach, still);
    field.Store(at, float4(stamped.distance, stamped.sheet));
    albedo_field[at] = stamped.paint;
    face_field[at] = stamped.face;
    transmission_field[at] = stamped.through;
}
