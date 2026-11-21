// Writes the distances from each cell of one cascade of the scene's distance field to the triangles
// of the meshes it is built from, a workgroup a triangle. The triangle is put in the world by its
// instance's matrix, and every cell within the band of cells around it, a box the triangle's bounds
// grown by the band, takes its distance to the triangle, the least of those any triangle gives kept
// by an atomic minimum of a uint the module's cellCode lays out, the distance in 1024ths of a cell
// above a bit set where a double-sided triangle is nearest at its rim, a bit set where the triangle
// is double-sided, a bit set where the cell lies in front of the triangle's face, the way the face
// looks in eight bits and the instance in seven. Of two triangles as near to that 1024th, as where
// a crate stands on the ground, one behind whose face the cell lies wins, the cell being inside
// some mesh. A double-sided triangle has no inside, and the resolve keeps its plane in the cell
// beside its distance, so a ray traced through the field meets a sheet thinner than a cell, as a
// wall or a floor of a mesh imported double-sided, wherever it lies between the cells on either
// side. A cell is behind a triangle only where the way from the triangle's nearest point to it lies
// within 60 degrees of straight back from the face, so where an edge or a corner is nearest, as
// above a pillar's rim or level with a wall's side above the wall, a face the way runs along does
// not put the cell inside, and a cell whose nearest point lies on an edge no other triangle shares,
// as a ground plane's rim, is in front, so an open mesh puts no wedge below its rim inside. The
// nearest face a cell lies straight behind is kept apart, for a wall thinner than a cell, which may
// have no cell's middle inside it. A cell no triangle comes within the band of keeps the code it
// was cleared to (EmptyCode), which resolves to the band's distance. A second dispatch over the
// same triangles paints each cell the color and the emission of a triangle whose code is the one
// the cell kept, its nearest surface, which global illumination is read from, its instance's color
// times its vertices' colors blended at the cell's nearest point, as a material blends them across
// a face. A third lends the light of an emitter thinner than a cell to the cells near it.
//
// The codes are kept through Slang's Atomic<uint>, which compiles to WGSL's atomics where
// InterlockedMin does not, and the emission is kept as four floats a cell, since the instruction
// that packs it in halves is SPIR-V's alone.
//
// A triangle's codes and the paint of the cells that kept them (splatCodes and splatPaint), with
// its nearest point to a cell, its way, its blend at a point, whether it holds a surface, the cells
// its band covers and the paint's alpha, are the RadianceCascades module's, written once for both
// engines. Its cornerColor is not (cornerLight).
import rc_field_splat;

struct FieldInstance
{
    // The rows of the instance's world matrix, a 3x4.
    float4 row0;
    float4 row1;
    float4 row2;
    // x: its mesh's first triangle in the pool. y: how many it has. z: 1 in its lowest bit when the
    // mesh has no inside, as a double-sided one, whose cells are all in front, and in its second
    // byte the share of light that passes a unit of its depth in 255ths (lightThroughPaint). w: the
    // first of its triangles counted over every instance of the build before it.
    uint4 triangles;
    // rgb: the surface's color, linear. w: 2 where global illumination carries its emission as a
    // glow light, 1 elsewhere.
    float4 color;
    // rgb: the surface's emission, linear, in the picture's units. w: where it is emissive, its
    // thinnest extent in world units, its mesh's bounds along each of its own axes scaled into the
    // world.
    float4 emission;
    // xyz: the surface's perceptual roughness, metallic share and reflectance. w: the share of its
    // diffuse light it sends through to its far side, its diffuse transmission.
    float4 gloss;
};

// Each triangle of every mesh the field is built from, three corners in the mesh's own space, each
// its place's bits and in w its vertex's color, three sRGB bytes, with the bit above them set where
// the edge from it to the triangle's next corner is open.
StructuredBuffer<uint4> corners;

// Every build's instances this frame, one build after another.
StructuredBuffer<FieldInstance> instances;

// A code a cell of the cascade (cellCode).
RWStructuredBuffer<Atomic<uint>> cells;

// The nearest face each cell lies behind, in 1024ths of a cell, which field_resolve.slang reads for
// a wall thinner than a cell.
RWStructuredBuffer<Atomic<uint>> behind;

// The color a cell's nearest surface paints it, sRGB bytes with 255 above them, which the painting
// dispatch keeps by an atomic maximum (PaintCells), a nearer code clears (CodeCells) and
// field_resolve.slang reads.
RWStructuredBuffer<Atomic<uint>> albedo;

// The area of a crown's leaf cards, double-sided triangles letting light through, whose middles lie
// in each cell, in 65536ths of a square meter, which field_resolve.slang turns into the share of
// light a meter of the cell lets through (lightThroughCardsPaint in bcs_field.slang).
RWStructuredBuffer<Atomic<uint>> leaf_area;

// The light a cell's nearest surface emits, in the picture's units, its alpha 1 where global
// illumination carries the emitter as a glow light, which the painting dispatch writes and
// field_resolve.slang reads.
RWStructuredBuffer<float4> glow;

// The gloss of a cell's nearest surface in the bits a cell of the faces image keeps it in
// (glossBits), which the painting dispatch writes and field_resolve.slang puts beside the face, and
// in bits 0 to 7, which the faces image keeps the face's way in and this leaves free, the share of
// its diffuse light the surface sends through to its far side in 255ths, which field_resolve.slang
// puts in the image of that share.
RWStructuredBuffer<uint> gloss;

// What the emitters thinner than a cell lend each cell they lie near, six codes a cell, the most
// light any of them emits, red, green and blue in 256ths, and the area of their faces near the cell
// seen along x, y and z, in 1024ths of a cell's face, which field_resolve.slang reads.
RWStructuredBuffer<Atomic<uint>> emitted;

// xyz: the cascade's corner. w: the width of a cell.
uniform float4 origin_and_cell;
// x: the cells along each side. y: how many instances the build has. z: how many triangles in all.
// w: the band in cells.
uniform uint4 counts;
// x: how many workgroups the dispatch has along x, which the triangle's number is read across. y:
// the build's first instance in the buffer. z: 0 for the dispatch that finds the distances, 1 for
// the one that paints, 2 for the one that lends an emitter thinner than a cell's light to the cells
// near it.
uniform uint4 groups;

float3 place(FieldInstance instance, float3 p)
{
    float4 h = float4(p, 1.0);
    return float3(dot(instance.row0, h), dot(instance.row1, h), dot(instance.row2, h));
}

// The area of the triangle a b c, given in coordinates of its own plane, within the square of half
// side h about the plane's origin, the triangle cut by each of the square's sides in turn
// (Sutherland and Hodgman), seven corners at most.
float areaWithin(float2 a, float2 b, float2 c, float h)
{
    float2 lo = min(a, min(b, c)), hi = max(a, max(b, c));
    if (all(lo >= -h) && all(hi <= h))
        return abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) * 0.5;
    if (any(lo > h) || any(hi < -h))
        return 0.0;
    float2 corners[8];
    float2 cut[8];
    corners[0] = a;
    corners[1] = b;
    corners[2] = c;
    uint count = 3u;
    for (uint side = 0u; side < 4u; side++)
    {
        float sign = (side & 1u) == 0u ? 1.0 : -1.0;
        uint axis = side >> 1;
        uint kept = 0u;
        for (uint i = 0u; i < count; i++)
        {
            float2 p = corners[i], q = corners[(i + 1u) % count];
            float dp = sign * p[axis] - h, dq = sign * q[axis] - h;
            if (dp <= 0.0 && kept < 8u)
                cut[kept++] = p;
            if ((dp <= 0.0) != (dq <= 0.0) && kept < 8u)
                cut[kept++] = lerp(p, q, dp / (dp - dq));
        }
        count = kept;
        for (uint i = 0u; i < count; i++)
            corners[i] = cut[i];
        if (count < 3u)
            return 0.0;
    }
    float twice = 0.0;
    for (uint i = 0u; i < count; i++)
    {
        float2 p = corners[i], q = corners[(i + 1u) % count];
        twice += p.x * q.y - q.x * p.y;
    }
    return abs(twice) * 0.5;
}

// A corner's color, the three sRGB bytes the pool keeps in its fourth uint, in linear light by
// sRGB's own transfer function. The module's cornerColor is the same with no max, which a GPU's
// shader compiler may build to other last bits, as NVIDIA's does, so a gray painted on a 255th's
// boundary, as 0.7 is at 178.5, paints the byte beside.
float3 cornerLight(uint packed)
{
    float3 c = float3(float(packed & 255u), float((packed >> 8) & 255u), float((packed >> 16) & 255u)) / 255.0;
    return select(c <= 0.04045, c / 12.92, pow((max(c, 0.0) + 0.055) / 1.055, 2.4));
}

// The cascade's cells as the module's splat keeps their codes (splatCodes), by atomic minima. A
// cascade is splatted in several builds, each painting the cells after its codes, and a code
// nearer than the one a cell kept clears the cell's paint, which an earlier build gave it from
// another face. The paint is kept as the larger color (PaintCells), and a leaf card's, whose alpha
// lies below 1, would otherwise stay under the floor's or a trunk's, the crown held solid.
struct CodeCells : ISplatCells
{
    void keepLeast(uint index, uint code)
    {
        if (cells[index].min(code) > code)
            albedo[index].store(0u);
    }
    void keepBehind(uint index, uint code) { behind[index].min(code); }
}

// The cascade's cells as the module's paint reads their codes and writes their paint (splatPaint),
// the color times the alpha and the alpha in bytes, the gloss as given, and the emission with alpha
// 1 where global illumination carries the emitter as a glow light, which a probe's ray that meets
// it leaves out (surfaceGlowUncarried).
//
// Two triangles of one mesh tie on a cell whose middle lies as near both along the same way, in
// the code's steps, since the code holds the distance, the way and the mesh's instance and nothing
// of the triangle, and both paint the cell, each the color of its own nearest point, which differs
// where its corners do. The color is kept as the larger of the two in its bytes, an atomic maximum,
// so a build paints the cell the same whichever triangle's thread comes first, where stored as they
// came the cell kept the last, and 226 of a ball's 17,416 cells took another color in another
// build. Their gloss and their emission are their mesh's, the same from both.
struct PaintCells : IPaintCells
{
    bool carried;

    uint kept(uint index) { return cells[index].load(); }

    void paint(uint index, float4 paint, float3 emission, uint bits)
    {
        uint4 bytes = uint4(saturate(paint) * 255.0 + 0.5);
        albedo[index].max(bytes.r | (bytes.g << 8) | (bytes.b << 16) | (bytes.a << 24));
        gloss[index] = bits;
        glow[index] = float4(emission, carried && any(emission > 0.0) ? 1.0 : 0.0);
    }
}

groupshared uint triangleInstance;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 group : SV_GroupID, uint lane : SV_GroupIndex)
{
    uint t = group.y * groups.x + group.x;
    if (t >= counts.z)
        return;

    // The instance the triangle is of, the last whose first triangle is at or before it.
    if (lane == 0)
    {
        uint lo = 0, hi = counts.y - 1;
        while (lo < hi)
        {
            uint mid = (lo + hi + 1) / 2;
            if (instances[groups.y + mid].triangles.w <= t)
                lo = mid;
            else
                hi = mid - 1;
        }
        triangleInstance = lo;
    }
    GroupMemoryBarrierWithGroupSync();
    FieldInstance instance = instances[groups.y + triangleInstance];
    // An emitter thinner than a cell paints no cell its own where a surface behind it is nearer the
    // cells' middles, as a bright strip on a wall, so its light was in the field nowhere. It lends
    // the cells within a cell of it its light, with the area of its faces near each, which the
    // resolve turns into the share of a cell's face it covers, so a strip 0.06 wide lends cells of
    // 0.15 two fifths of its light and a sheet as thin lends its whole light. An emitter global
    // illumination carries as a glow light lends nothing, its light reaching the cells' surfaces as
    // a light's does.
    float thickness = instance.emission.w;
    bool carried = instance.color.w > 1.5;
    if (groups.z == 2u && (carried || !(thickness > 0.0 && thickness < origin_and_cell.w && any(instance.emission.rgb > 0.0))))
        return;
    uint corner = (instance.triangles.x + t - instance.triangles.w) * 3;
    uint4 ca = corners[corner], cb = corners[corner + 1], cc = corners[corner + 2];
    float3 a = place(instance, asfloat(ca.xyz));
    float3 b = place(instance, asfloat(cb.xyz));
    float3 c = place(instance, asfloat(cc.xyz));
    bool3 edges = bool3(((ca.w >> 24) & 1u) != 0u, ((cb.w >> 24) & 1u) != 0u, ((cc.w >> 24) & 1u) != 0u);
    float3 face = cross(b - a, c - a);
    // A triangle of next to no area beside its longest side, as a sphere's last row has at a pole
    // whose corners the rounding of sin(pi) leaves apart, has a face turned whichever way that
    // rounding took it, which, at a distance the triangles around it share, said cells outside lay
    // behind it and turned them inside. It holds no surface its neighbors do not, and is passed
    // over.
    if (!holdsSurface(a, b, c))
        return;
    bool sided = (instance.triangles.z & 1u) == 0u && dot(face, face) > 1e-20;
    float faceLength = length(face);

    // A leaf card adds its area to the cell its middle lies in, once, in the dispatch that writes
    // the distances.
    if (groups.z == 0u && lane == 0u && !sided && ((instance.triangles.z >> 8u) & 255u) != 0u)
    {
        int3 middle = int3(floor(((a + b + c) / 3.0 - origin_and_cell.xyz) / origin_and_cell.w));
        if (all(middle >= int3(0)) && all(middle < int3(int(counts.x))))
            leaf_area[(uint(middle.z) * counts.x + uint(middle.y)) * counts.x + uint(middle.x)].add(uint(faceLength * 0.5 * 65536.0 + 0.5));
    }

    float cell = origin_and_cell.w;
    float band = float(counts.w);
    int size = int(counts.x);
    SplatTriangle triangle = { a, b, c, edges, (instance.triangles.z & 1u) != 0u, triangleInstance };
    SplatCascade cascade = { origin_and_cell.xyz, cell, size, band, int3(0), int3(size - 1) };

    if (groups.z == 2u)
    {
        if (faceLength <= 1e-10)
            return;
        // The cells within a cell of the triangle, which lie within its bounds grown by one, every
        // cell a point on it is blended from where the field is read there.
        int3 first = max(int3(floor((min(a, min(b, c)) - origin_and_cell.xyz) / cell - 1.0)), int3(0));
        int3 last = min(int3(ceil((max(a, max(b, c)) - origin_and_cell.xyz) / cell + 1.0)), int3(size - 1));
        if (any(last < first))
            return;
        int3 span = last - first + 1;
        uint count = uint(span.x * span.y * span.z);
        uint3 light = uint3(max(instance.emission.rgb, 0.0) * 256.0);
        // The face's way and two ways along it, square to the axes where the face is, so a cell's
        // square lies along an upright wall's rows of cells.
        float3 n = face / faceLength;
        float3 across = abs(n.x) < abs(n.y) ? (abs(n.x) < abs(n.z) ? float3(1, 0, 0) : float3(0, 0, 1)) : (abs(n.y) < abs(n.z) ? float3(0, 1, 0) : float3(0, 0, 1));
        float3 u = normalize(cross(n, across));
        float3 v = cross(n, u);
        // The share of a cell's face it covers seen along each axis, 1024 a whole face.
        float3 seen = abs(n) * (1024.0 / (cell * cell));
        for (uint k = lane; k < count; k += 64u)
        {
            int3 at = first + int3(int(k % uint(span.x)), int((k / uint(span.x)) % uint(span.y)), int(k / uint(span.x * span.y)));
            float3 p = origin_and_cell.xyz + (float3(at) + 0.5) * cell;
            bool open;
            if (length(p - nearestOnTriangle(p, a, b, c, edges, open)) >= cell)
                continue;
            // A face of a closed mesh counts where the cell lies before it, as a sheet's top face
            // for a cell above it and its bottom face for one below, so the two faces of a sheet
            // are not counted twice; a mesh with no inside counts every face.
            float off = dot(p - a, n);
            if (sided && off <= 0.0)
                continue;
            // The face's area within a cell's square about the cell's middle laid on its plane.
            float3 q = p - n * off;
            float area = areaWithin(float2(dot(a - q, u), dot(a - q, v)), float2(dot(b - q, u), dot(b - q, v)),
                float2(dot(c - q, u), dot(c - q, v)), cell * 0.5);
            if (area <= 0.0)
                continue;
            uint3 share = uint3(area * seen + 0.5);
            uint index = (at.z * size + at.y) * size + at.x;
            emitted[index * 6u].max(light.r);
            emitted[index * 6u + 1u].max(light.g);
            emitted[index * 6u + 2u].max(light.b);
            emitted[index * 6u + 3u].add(share.x);
            emitted[index * 6u + 4u].add(share.y);
            emitted[index * 6u + 5u].add(share.z);
        }
        return;
    }

    if (groups.z == 0u)
    {
        CodeCells codes;
        splatCodes(codes, triangle, cascade, lane, 64u);
        return;
    }
    // The color painted times the paint's alpha, which tells a surface that lets light through from
    // a solid one, and in the gloss's lowest byte the share of its diffuse light the surface sends
    // through.
    PaintCells painted = { carried };
    SplatSurface surface = { instance.color.rgb, float3x3(cornerLight(ca.w), cornerLight(cb.w), cornerLight(cc.w)),
        float((instance.triangles.z >> 8u) & 255u) / 255.0, max(instance.emission.rgb, 0.0),
        glossBits(instance.gloss.x, instance.gloss.y, instance.gloss.z) | uint(saturate(instance.gloss.w) * 255.0 + 0.5) };
    splatPaint(painted, triangle, cascade, surface, lane, 64u);
}
