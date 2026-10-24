// Turns the codes field_splat.slang left in one cascade's cells into distances in world units,
// below zero behind a face and within half a cell of a double-sided one or a wall thinner than a
// cell, and writes them into the cascade's place in the field the meshes alone make and in the field
// the shaders read, with the color it painted and the way the cell's nearest face looks, a thread a
// cell, clearing each cell's words behind it for the next build.
//
// Taken from 3DEngine's field_resolve.slang, the emission and the light emitters thinner than a cell
// lend coming with the emitters. 3DEngine clears the words before each build with a fill of the
// buffer, which a dispatch here has no call for, so the resolve that reads a word clears it, and the
// buffers start cleared.

// The words of the cascade's cells, as field_splat.slang left them, and the nearest face each cell
// lies behind, in 1024ths of a cell.
RWStructuredBuffer<uint> cells;
RWStructuredBuffer<uint> behind;

[format("r16f")] RWTexture3D<float> still_field;
[format("r16f")] RWTexture3D<float> field;

// The color of each cell's nearest surface, as field_splat.slang painted it, and the images it goes
// into, the passes' and the still meshes' own for field_stamp.slang to start its bricks from, alpha
// 1 where a surface painted the cell and 0 where none did, so a read blending cells divides by it.
RWStructuredBuffer<uint> albedo;
[format("rgba8")] RWTexture3D<float4> albedo_field;
[format("rgba8")] RWTexture3D<float4> still_paint;

// The way each cell's nearest face looks, the eight bits field_splat.slang kept in its code, into the
// image the passes read and the still meshes' own, so a ray's hit takes its color from cells its own
// face painted (surfaceAlbedoFacing).
[format("r32ui")] RWTexture3D<uint> face_field;
[format("r32ui")] RWTexture3D<uint> still_faces;

// x: the cascade. y: the cells along each side. z: the width of a cell. w: the band in cells.
uniform float4 cascade;

// A face's way from its eight bits, as field_splat.slang folded it.
float3 way(uint bits)
{
    float2 o = (float2(float(bits & 15u), float((bits >> 4) & 15u)) + 0.5) / 16.0 * 2.0 - 1.0;
    float3 n = float3(o, 1.0 - abs(o.x) - abs(o.y));
    if (n.z < 0.0)
        n.xy = (1.0 - abs(n.yx)) * float2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
    return normalize(n);
}

[shader("compute")]
[numthreads(4, 4, 4)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint size = uint(cascade.y);
    if (any(id >= size))
        return;
    uint index = (id.z * size + id.y) * size + id.x;
    uint code = cells[index];
    float d = min(float(code >> 10) / 1024.0, cascade.w) * cascade.z;
    // A cell in front of one face and straight behind another that looks the other way, less than
    // a cell beyond it, lies against a wall thinner than a cell, which is held as a double-sided
    // sheet is, half a cell thick on either side of the wall's middle, so a ray between the cells on
    // either side still meets it, where the faces of a box's edge, which meet square, do not make
    // one. Held so only below half a cell, a wall of two thirds of a cell lay between two cells'
    // middles, each a sixth of a cell outside it, which a trace stepped over, as 3DEngine found.
    uint hidden = behind[index];
    float beyond = float(hidden >> 8) / 1024.0 * cascade.z;
    bool thin = (code & 0x300u) == 0x100u && hidden != 0xFFFFFFFFu && beyond - d < cascade.z
        && dot(way(code), way(hidden)) < -0.7;
    if ((code & 0x200u) != 0u)
        d -= cascade.z * 0.5;
    else if (thin)
        d = (d + beyond) * 0.5 - cascade.z * 0.5;
    else if ((code & 0x100u) == 0u)
        d = -d;
    uint3 at = uint3(id.xy, id.z + uint(cascade.x) * size);
    still_field[at] = d;
    field[at] = d;
    uint color = albedo[index];
    float4 paint = float4(float(color & 255u), float((color >> 8) & 255u), float((color >> 16) & 255u), float(color >> 24)) / 255.0;
    still_paint[at] = paint;
    albedo_field[at] = paint;
    still_faces[at] = code & 255u;
    face_field[at] = code & 255u;

    // Cleared for the next build, the band's distance in front, no face behind and no color.
    cells[index] = (uint(cascade.w) * 1024u << 10) | (1u << 8);
    behind[index] = 0xFFFFFFFFu;
    albedo[index] = 0u;
}
