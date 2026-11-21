// Turns the codes field_splat.slang left in one cascade's cells into distances in world units,
// below zero behind a face and within half a cell of a double-sided one or a wall thinner than a
// cell, and writes them into the cascade's place in the field the meshes alone make and in the
// field the shaders read, with the color and the emission it painted, the way the cell's nearest
// face looks and the share of its light that face sends through, a thread a cell, or clears the
// codes for the next build.
//
// The codes are cleared for the next build by a dispatch of this that clears alone, since a
// dispatch here has no call that fills a buffer. It follows each resolve, whose cells read their
// neighbors' codes, and the buffers start cleared.

import bcs_field;
// A face's way from its eight bits and the pinch of a cell between two blocks, written once in the
// RadianceCascades module for both engines.
import rc_field_code;

// The codes of the cascade's cells, as field_splat.slang left them, and the nearest face each cell
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

// The area of the leaf cards whose middles lie in each cell, as field_splat.slang summed it, in
// 65536ths of a square meter.
RWStructuredBuffer<uint> leaf_area;
[format("rgba8")] RWTexture3D<float4> still_paint;

// The way each cell's nearest face looks, the eight bits field_splat.slang kept in its code, and
// its gloss as field_splat.slang painted it, in the bits faceBits and glossBits in bcs_field.slang
// lay out, into the image the passes read and the still meshes' own, so a ray's hit takes its color
// and its gloss from cells its own face painted (surfaceFacing).
RWStructuredBuffer<uint> gloss;
[format("r32ui")] RWTexture3D<uint> face_field;
[format("r32ui")] RWTexture3D<uint> still_faces;

// The share of its diffuse light each cell's nearest surface sends through to its far side, which
// field_splat.slang painted in the lowest byte of its gloss, into the image the passes read and the
// still meshes' own (transmissionAt in bcs_field.slang).
[format("r16f")] RWTexture3D<float> transmission_field;
[format("r16f")] RWTexture3D<float> still_transmission;

// The light each cell's nearest surface emits, as field_splat.slang painted it, what the emitters
// thinner than a cell lend it, the most light any emits in 256ths and the area of their faces near
// the cell seen along x, y and z in 1024ths of a cell's face, and the image both go into.
RWStructuredBuffer<float4> glow;
RWStructuredBuffer<uint> emitted;
[format("rgba16f")] WTexture3D<float4> glow_field;

// x: the cascade. y: the cells along each side. z: the width of a cell. w: the band in cells.
uniform float4 cascade;

// x: 1 where the dispatch clears the cells alone, writing nothing into the images, as it does after
// each resolve and before the first part of a build where a build left unfinished splatted into
// them, 0 where it resolves them and leaves them as they are.
uniform uint4 clearing;

[shader("compute")]
[numthreads(4, 4, 4)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint size = uint(cascade.y);
    if (any(id >= size))
        return;
    uint index = (id.z * size + id.y) * size + id.x;
    uint e = index * 6u;
    if (clearing.x == 0u)
    {
        resolve(id, index, e);
        return;
    }

    // Cleared for the next build, the band's distance in front, no face behind, no color and no
    // light.
    cells[index] = (uint(cascade.w) * 1024u << 10) | (1u << 8);
    behind[index] = 0xFFFFFFFFu;
    albedo[index] = 0u;
    leaf_area[index] = 0u;
    gloss[index] = 0u;
    glow[index] = float4(0.0);
    for (uint i = 0u; i < 6u; i++)
        emitted[e + i] = 0u;
}

// The distance a cell holds, from the code field_splat.slang left it and the face it lies behind.
// The module's resolvedDistance reads a code laid out as its own splat lays it, its distance above
// bit 19 with a rim and an instance below, and holds a double-sided face at its distance with its
// plane kept beside it, where this engine's code keeps its distance above bit 10 and the resolve
// takes half a cell off a double-sided face's, so the distance is this file's own.
float cellDistance(uint3 c, uint size)
{
    uint index = (c.z * size + c.y) * size + c.x;
    uint code = cells[index];
    float d = min(float(code >> 10) / 1024.0, cascade.w) * cascade.z;
    // A cell in front of one face and straight behind another that looks the other way, less than a
    // cell beyond it, lies against a wall thinner than a cell, which is held as a double-sided
    // sheet is, half a cell thick on either side of the wall's middle, so a ray between the cells
    // on either side still meets it, where the faces of a box's edge, which meet square, do not
    // make one. Held so only below half a cell, a wall of two thirds of a cell would lie between
    // two cells' middles, each a sixth of a cell outside it, which a trace steps over.
    uint hidden = behind[index];
    float beyond = float(hidden >> 8) / 1024.0 * cascade.z;
    bool thin = (code & 0x300u) == 0x100u && hidden != 0xFFFFFFFFu && beyond - d < cascade.z
        && dot(wayOf(code), wayOf(hidden)) < -0.7;
    if ((code & 0x200u) != 0u)
        d -= cascade.z * 0.5;
    else if (thin)
        d = (d + beyond) * 0.5 - cascade.z * 0.5;
    else if ((code & 0x100u) == 0u)
        d = -d;
    return d;
}

// The cascade's cells as the module's pinch reads them (pinched), each the distance this file
// resolves its code to.
struct ResolvedCells : IResolvedCells
{
    int size() { return int(cascade.y); }
    float resolvedAt(int3 at) { return cellDistance(uint3(at), uint(cascade.y)); }
}

// The cell's codes turned into its distance, color, face and light in the cascade's place in the
// images, given its place in the buffers and in the lent light.
void resolve(uint3 id, uint index, uint e)
{
    uint size = uint(cascade.y);
    uint code = cells[index];
    // A cell pinched between two blocks that meet along an edge is held inside at its distance, so
    // a march through the edge meets the field there. Between the four cells about the edge the
    // distance would otherwise stay above zero, and a march from the floor beside a wall whose
    // halves meet along one edge would pass through the edge and see the light beyond the wall.
    float d = cellDistance(id, size);
    ResolvedCells resolved;
    if (d > 0.0 && pinched(resolved, int3(id)))
        d = -d;
    uint3 at = uint3(id.xy, id.z + uint(cascade.x) * size);
    still_field[at] = d;
    field[at] = d;
    uint color = albedo[index];
    float4 paint = float4(float(color & 255u), float((color >> 8) & 255u), float((color >> 16) & 255u), float(color >> 24)) / 255.0;
    // A cell a surface letting light through painted takes the share a meter of it lets through
    // again, a crown's leaf cards, whose nearest triangle is double-sided, the share their material
    // lets through a meter at a preset crown's density raised to the cell's own card area a cubic
    // meter over it, so a crown's sparse edge lets more through than its core, and any other
    // surface its material's share, the color staying the color over the alpha.
    if (paint.a > 0.25 && paint.a < 1.0)
    {
        float through = lightThroughOf(paint.a);
        float alpha = (code & 0x200u) != 0u
            ? lightThroughCardsPaint(through, float(leaf_area[index]) / 65536.0 / (cascade.z * cascade.z * cascade.z))
            : lightThroughAlpha(through);
        paint = float4(paint.rgb * (alpha / paint.a), alpha);
    }
    still_paint[at] = paint;
    albedo_field[at] = paint;
    uint word = gloss[index];
    uint face = (code & 255u) | (word & ~255u);
    still_faces[at] = face;
    face_field[at] = face;
    float sends = float(word & 255u) / 255.0;
    still_transmission[at] = sends;
    transmission_field[at] = sends;

    // The lent light times the share of the cell's face the emitters cover along the axis they
    // cover most of, a whole face at most, as the faces of a strip along a wall lend a cell beside
    // it what it shows from the front and from above alike, where summed they would lend it twice
    // that. The larger of that and the cell's own, since an emitter that painted the cell its own
    // gives it its whole light, as an emissive panel under a ceiling does, times the paint's alpha
    // where a surface painted the cell, as its color is (lightThroughAlpha in bcs_field.slang),
    // since a reader divides both by it.
    float3 light = float3(float(emitted[e]), float(emitted[e + 1u]), float(emitted[e + 2u])) / 256.0;
    float3 seen = float3(float(emitted[e + 3u]), float(emitted[e + 4u]), float(emitted[e + 5u])) / 1024.0;
    float3 lent = light * min(max(seen.x, max(seen.y, seen.z)), 1.0);
    // Alpha 1 where the light is a carried emitter's, which a probe's ray leaves out.
    float4 painted = glow[index];
    glow_field.Store(at, float4(max(painted.rgb, lent) * (paint.a > 0.0 ? paint.a : 1.0), any(lent > painted.rgb) ? 0.0 : painted.a));
}
