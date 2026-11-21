// Writes the distances from each cell of one cascade of the scene's distance field to the triangles
// of the meshes it is built from, a workgroup a triangle. The triangle is put in the world by its
// instance's matrix, and every cell within the band of cells around it, a box the triangle's bounds
// grown by the band, takes its distance to the triangle, the least of those any triangle gives kept
// by an atomic minimum of a uint that holds the distance in 1024ths of a cell above a bit set where
// the triangle is double-sided, a bit set where the cell lies in front of the triangle's face, and
// the way the face looks in eight bits. Of two triangles as near to that 1024th, as where a crate
// stands on the ground, one behind whose face the cell lies wins, the cell being inside some mesh.
// A double-sided triangle has no inside, and the resolve takes half a cell off its distance, so a
// sheet thinner than a cell, as a wall or a floor of a mesh imported double-sided, crosses zero
// between the cells on either side wherever it lies between them and stops a ray traced through the
// field. A cell is behind a triangle only where the way from the triangle's nearest point to it
// lies within 60 degrees of straight back from the face, so where an edge or a corner is nearest,
// as above a pillar's rim or level with a wall's side above the wall, a face the way runs along
// does not put the cell inside, and a cell whose nearest point lies on an edge no other triangle
// shares, as a ground plane's rim, is in front, so an open mesh puts no wedge below its rim inside.
// The nearest face a cell lies straight behind is kept apart, for a wall thinner than a cell, which
// may have no cell's middle inside it. A cell no triangle comes within the band of keeps the code
// it was cleared to, the band's distance in front. A second dispatch over the same triangles paints
// each cell the color and the emission of a triangle whose code is the one the cell kept, its
// nearest surface, which global illumination is read from, its instance's color times its vertices'
// colors blended at the cell's nearest point, as a material blends them across a face. A third
// lends the light of an emitter thinner than a cell to the cells near it.
//
// The codes are kept through Slang's Atomic<uint>, which compiles to WGSL's atomics where
// InterlockedMin does not, and the emission is kept as four floats a cell, since the instruction
// that packs it in halves is SPIR-V's alone.
//
// A triangle's nearest point to a cell, its way, its blend at a point, whether it holds a surface,
// the cells its band covers and the paint's alpha are the RadianceCascades module's, written once
// for both engines. Its splatCodes and splatPaint are not, since the module lays a code out with
// the distance above bit 19 and a rim and an instance below, and gives a double-sided face's side
// of its plane where this engine's code holds its distance above bit 10 and a double-sided face in
// front, which field_resolve.slang reads. Nor is its cornerColor (cornerLight).
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

// A code a cell of the cascade, the distance's.
RWStructuredBuffer<Atomic<uint>> cells;

// The nearest face each cell lies behind, in 1024ths of a cell, which field_resolve.slang reads for
// a wall thinner than a cell.
RWStructuredBuffer<Atomic<uint>> behind;

// The color a cell's nearest surface paints it, sRGB bytes with 255 above them, which the painting
// dispatch writes and field_resolve.slang reads.
RWStructuredBuffer<uint> albedo;

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
    uint way = dot(face, face) > 1e-20 ? wayCode(face) : 0u;
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
    float reach = band * cell;
    int size = int(counts.x);
    SplatTriangle triangle = { a, b, c, edges, !sided, 0u };
    SplatCascade cascade = { origin_and_cell.xyz, cell, size, band, int3(0), int3(size - 1) };
    int3 first, span;
    if (!bandCells(triangle, cascade, first, span))
        return;
    uint count = uint(span.x * span.y * span.z);

    if (groups.z == 2u)
    {
        if (faceLength <= 1e-10)
            return;
        // The cells within a cell of the triangle, which lie within its bounds grown by one, every
        // cell a point on it is blended from where the field is read there.
        first = max(int3(floor((min(a, min(b, c)) - origin_and_cell.xyz) / cell - 1.0)), int3(0));
        int3 last = min(int3(ceil((max(a, max(b, c)) - origin_and_cell.xyz) / cell + 1.0)), int3(size - 1));
        if (any(last < first))
            return;
        span = last - first + 1;
        count = uint(span.x * span.y * span.z);
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

    for (uint k = lane; k < count; k += 64u)
    {
        int3 at = first + int3(int(k % uint(span.x)), int((k / uint(span.x)) % uint(span.y)), int(k / uint(span.x * span.y)));
        float3 p = origin_and_cell.xyz + (float3(at) + 0.5) * cell;
        bool open;
        float3 q = nearestOnTriangle(p, a, b, c, edges, open);
        float d = length(p - q);
        if (d >= reach)
            continue;
        uint front = sided && !open && dot(p - q, face) < -0.5 * d * faceLength ? 0u : 1u;
        uint code = (uint(d / cell * 1024.0) << 10) | (sided ? 0u : 512u) | (front << 8) | way;
        uint index = (at.z * size + at.y) * size + at.x;
        if (groups.z == 0u)
        {
            cells[index].min(code);
            // Straight behind, its nearest point inside the face and not on an edge, as a wall's
            // far face lies behind a cell by its near one, where a cell beside a box's edge lies 45
            // degrees behind the face round it. Taken within a wider angle, a wall thinner than a
            // cell standing on a floor would have its bottom face, buried against the floor's top,
            // nearer the cells along its foot than its far face, its edge a little off straight
            // back from them, so the resolve would find no face behind them looking against the one
            // before and hold none of them, and a probe's ray would pass under the wall, a lamp
            // outside a closed room lighting its faces inside where no light reaches them.
            if (front == 0u && dot(p - q, face) < -0.999 * d * faceLength)
                behind[index].min((uint(d / cell * 1024.0) << 8) | way);
        }
        else if (cells[index].load() == code)
        {
            float3 weights = barycentric(q, a, b, c);
            // The color painted times the paint's alpha, which tells a surface that lets light
            // through from a solid one.
            float coverage = lightThroughPaint(float((instance.triangles.z >> 8u) & 255u) / 255.0);
            float3 painted = instance.color.rgb * (weights.x * cornerLight(ca.w) + weights.y * cornerLight(cb.w) + weights.z * cornerLight(cc.w));
            uint4 bytes = uint4(uint3(saturate(painted * coverage) * 255.0 + 0.5), uint(coverage * 255.0 + 0.5));
            albedo[index] = bytes.r | (bytes.g << 8) | (bytes.b << 16) | (bytes.a << 24);
            gloss[index] = glossBits(instance.gloss.x, instance.gloss.y, instance.gloss.z) | uint(saturate(instance.gloss.w) * 255.0 + 0.5);
            // Alpha 1 where global illumination carries the emitter as a glow light, which a
            // probe's ray that meets it leaves out (surfaceGlowUncarried).
            float3 e = max(instance.emission.rgb, 0.0);
            glow[index] = float4(e, carried && any(e > 0.0) ? 1.0 : 0.0);
        }
    }
}
