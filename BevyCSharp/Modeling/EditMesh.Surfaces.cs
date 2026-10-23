namespace Bevy;

// The two modifiers that make a new surface from the whole mesh rather than running an operation
// on it, Catmull and Clark's subdivision surface and the thickness solidify gives a surface.
public sealed partial class EditMesh
{
    /// <summary>
    /// Subdivides every face once by Catmull and Clark's rules, a four sided face for each corner of
    /// each face, the corners and the edges' points moved toward the smooth surface the mesh bounds.
    /// </summary>
    /// <remarks>
    /// A face's point is its corners' mean, an edge's point the mean of its ends and the two faces'
    /// points beside it, and a vertex moves to the mean of its faces' points, twice the mean of its
    /// edges' middles and as many less three times itself as faces meet at it, over that count. An
    /// edge on a hole's rim takes its middle, and a vertex on the rim three quarters of itself and
    /// an eighth of each neighbor along the rim, so an open edge stays where it ends. Texture
    /// coordinates are taken at the corners', the edges' and the faces' means, without smoothing.
    /// </remarks>
    internal void CatmullClark()
    {
        var count = _faces.Count;
        if (count == 0) return;

        var directed = Directed();
        var uses = EdgeUses();
        var old = _positions.ToArray();

        var centers = new int[count];
        var around = new Dictionary<int, List<int>>();
        for (var face = 0; face < count; face++)
        {
            centers[face] = AddVertex(Center(face));
            foreach (var corner in _faces[face].Corners) Listed(around, corner).Add(face);
        }

        var points = new Dictionary<(int, int), int>();
        var edgesAt = new Dictionary<int, List<(int Other, bool Rim)>>();
        foreach (var ((low, high), use) in uses)
        {
            var rim = !(use.Forward == 1 && use.Backward == 1);
            var point = rim
                ? (old[low] + old[high]) * 0.5f
                : (old[low] + old[high] + _positions[centers[directed[(low, high)]]] + _positions[centers[directed[(high, low)]]]) * 0.25f;
            points[(low, high)] = AddVertex(point);

            Listed(edgesAt, low).Add((high, rim));
            Listed(edgesAt, high).Add((low, rim));
        }

        foreach (var (vertex, edges) in edgesAt)
        {
            var at = old[vertex];
            var rims = edges.Where(edge => edge.Rim).ToArray();
            if (rims.Length == 0)
            {
                var faces = around[vertex];
                var n = (float)faces.Count;
                var meanFace = faces.Aggregate(Vec3.Zero, (sum, face) => sum + _positions[centers[face]]) * (1f / n);
                var meanEdge = edges.Aggregate(Vec3.Zero, (sum, edge) => sum + (at + old[edge.Other]) * 0.5f) * (1f / edges.Count);
                _positions[vertex] = (meanFace + meanEdge * 2f + at * (n - 3f)) * (1f / n);
            }
            else if (rims.Length == 2)
            {
                _positions[vertex] = at * 0.75f + (old[rims[0].Other] + old[rims[1].Other]) * 0.125f;
            }
        }

        for (var face = 0; face < count; face++)
        {
            var known = _faces[face];
            var corners = known.Corners;
            var n = corners.Length;
            var uvs = known.Uvs;
            var middleUv = uvs is null ? default : uvs.Aggregate(Vec2.Zero, (sum, uv) => sum + uv) * (1f / n);

            for (var i = 0; i < n; i++)
            {
                var (before, here, after) = ((i + n - 1) % n, i, (i + 1) % n);
                int[] quad = [corners[here], points[Key(corners[here], corners[after])], centers[face], points[Key(corners[before], corners[here])]];
                Vec2[]? quadUvs = uvs is null ? null : [uvs[here], (uvs[here] + uvs[after]) * 0.5f, middleUv, (uvs[before] + uvs[here]) * 0.5f];

                if (i == 0) (known.Corners, known.Uvs) = (quad, quadUvs);
                else AddFace(quad, known.Slot, known.Smooth, quadUvs);
            }
        }

        Changed();
    }

    /// <summary>
    /// Gives every face a thickness, a copy of the surface moved along its normals by the thickness,
    /// facing the other way from the first, and a band of faces along each open edge joining the two.
    /// </summary>
    /// <remarks>
    /// Above zero the copy is outside and the faces as they were turn to face in, and below zero
    /// the copy is inside, facing in. Each vertex moves so each face round it is the thickness from
    /// where it was, as an extrude moves a region's corners, so a box's walls are as thick at their
    /// corners as in their middles. A plane given a thickness is a closed slab, and a closed mesh
    /// becomes two shells, one inside the other.
    /// </remarks>
    internal void Solidify(float thickness)
    {
        var count = _faces.Count;
        if (count == 0) return;

        var normals = new Dictionary<int, List<Vec3>>();
        for (var face = 0; face < count; face++)
        {
            var normal = Normal(face);
            foreach (var corner in _faces[face].Corners)
            {
                var list = Listed(normals, corner);
                if (!list.Any(seen => (seen - normal).LengthSquared < 1e-8f)) list.Add(normal);
            }
        }

        var copies = new Dictionary<int, int>();
        foreach (var (vertex, around) in normals) copies[vertex] = AddVertex(_positions[vertex] + Offset(around, thickness));

        var uses = EdgeUses();
        var outward = thickness > 0f;
        var rims = new List<(int From, int To, int Slot, float Smooth)>();
        for (var face = 0; face < count; face++)
        {
            var known = _faces[face];
            var corners = known.Corners;
            for (var i = 0; i < corners.Length; i++)
            {
                var (from, to) = (corners[i], corners[(i + 1) % corners.Length]);
                var use = uses[Key(from, to)];
                if (use.Forward + use.Backward == 1) rims.Add((from, to, known.Slot, known.Smooth));
            }

            var copy = AddFace([.. corners.Select(corner => copies[corner])], known.Slot, known.Smooth, known.Uvs is { } uvs ? [.. uvs] : null);
            (outward ? known : _faces[copy]).Reverse();
        }

        foreach (var (from, to, slot, smooth) in rims)
        {
            AddFace(outward ? [from, to, copies[to], copies[from]] : [to, from, copies[from], copies[to]], slot, smooth);
        }

        Changed();
    }

    /// <summary>The list a map holds for a vertex, made empty where it holds none yet.</summary>
    private static List<T> Listed<T>(Dictionary<int, List<T>> map, int vertex)
    {
        if (!map.TryGetValue(vertex, out var list)) map[vertex] = list = [];
        return list;
    }
}
