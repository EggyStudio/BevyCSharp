namespace Bevy;

// The cuts that add vertices along edges and divide faces through them without changing the shape,
// a loop cut round a ring of four sided faces and the selected faces subdivided into a grid, as
// Blender's loop cut and subdivide are.
public sealed partial class EditMesh
{
    /// <summary>The most cuts <see cref="Subdivide"/> makes along each edge, sixteen.</summary>
    public const int MostCuts = 16;

    /// <summary>Cuts the ring of four sided faces across the selected edge in two, along a loop through each of them.</summary>
    /// <param name="fraction">Where along each edge the ring crosses the loop runs, above zero and below one.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// <para>
    /// The ring starts at the selected edge and goes on through each four sided face to the edge
    /// across it, both ways, until it comes back to where it started, meets a face of another count
    /// or reaches a hole's rim, as Blender's loop cut runs, so a vertical edge of a box cut at half
    /// makes a loop round its four sides at half its height. Each face of the ring is cut in two
    /// through the points the loop puts on its two edges, and a face beyond the ring's end that
    /// shares a cut edge takes the point as a corner, so the mesh stays closed.
    /// </para>
    /// <para>
    /// The fraction runs from the selected edge's end of the lower index, and from the end the ring
    /// carries that one to on every edge after, so the loop lies as straight as the ring does. The
    /// loop's vertices are selected after, and with them its edges. A face's own texture
    /// coordinates are cut with it.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">No edge is selected, or the edges selected lie on more than one ring.</exception>
    public EditMesh LoopCut(float fraction)
    {
        if (!(fraction > 0f && fraction < 1f)) throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "A loop cut's fraction lies above zero and below one, between the ends of the edges it crosses.");

        var selected = EdgeUses().Keys.Where(edge => _selectedVertices.Contains(edge.Low) && _selectedVertices.Contains(edge.High)).Order().ToArray();
        if (selected.Length == 0) throw new InvalidOperationException("A loop cut crosses the ring of faces through a selected edge, and no edge is selected.");

        var directed = Directed();
        var start = selected[0];
        var (rungs, faces) = Ring(directed, start);

        var crossed = rungs.Select(rung => rung.From < rung.To ? (rung.From, rung.To) : (rung.To, rung.From)).ToHashSet();
        if (selected.Any(edge => !crossed.Contains(edge)))
            throw new InvalidOperationException("A loop cut crosses one ring of faces, and the selected edges lie on more than one.");

        // A point on each edge the ring crosses, the fraction from the end the ring carries the
        // selected edge's lower end to.
        var points = new Dictionary<(int, int), int>();
        foreach (var (from, to) in rungs)
        {
            var key = from < to ? (from, to) : (to, from);
            if (!points.ContainsKey(key)) points[key] = AddVertex(_positions[from] + (_positions[to] - _positions[from]) * fraction);
        }

        var ring = faces.ToHashSet();
        var cut = points.ToDictionary(entry => entry.Key, entry => new[] { entry.Value });
        var count = _faces.Count;
        for (var index = 0; index < count; index++)
        {
            var face = _faces[index];
            var corners = face.Corners;
            if (!ring.Contains(index))
            {
                PutOnEdges(face, cut);
                continue;
            }

            // The face's first edge the ring crosses, and the one across it.
            var i = Enumerable.Range(0, 4).First(k => points.ContainsKey(Key(corners[k], corners[(k + 1) % 4])) && points.ContainsKey(Key(corners[(k + 2) % 4], corners[(k + 3) % 4])));
            var (p0, p1, p2, p3) = (corners[i], corners[(i + 1) % 4], corners[(i + 2) % 4], corners[(i + 3) % 4]);
            var (m01, m23) = (points[Key(p0, p1)], points[Key(p2, p3)]);

            Vec2[]? first = null, second = null;
            if (face.Uvs is { } uvs)
            {
                var (u0, u1, u2, u3) = (uvs[i], uvs[(i + 1) % 4], uvs[(i + 2) % 4], uvs[(i + 3) % 4]);
                var (a, b) = (Along(p0, p1, m01), Along(p2, p3, m23));
                var (n01, n23) = (u0 + (u1 - u0) * a, u2 + (u3 - u2) * b);
                (first, second) = ([u0, n01, n23, u3], [n01, u1, u2, n23]);
            }

            (face.Corners, face.Uvs) = ([p0, m01, m23, p3], first);
            AddFace([m01, p1, p2, m23], face.Slot, face.Smooth, second);
        }

        _selectedFaces.Clear();
        _selectedVertices.Clear();
        _selectedVertices.UnionWith(points.Values);

        Changed();
        return Did($"loopcut {F(fraction)}");
    }

    /// <summary>Cuts each edge of the selected faces into equal parts and each face into smaller ones through the cuts.</summary>
    /// <param name="cuts">How many points each edge takes, from one to <see cref="MostCuts"/>.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// <para>
    /// A four sided face becomes a grid of one more face than cuts a side, a triangle a triangle of
    /// triangles as many a side, and a face of more corners a face for each corner, from its middle
    /// to the points nearest the middles of its two edges, as Blender's subdivision of a face of
    /// many corners makes a fan of them. A box's six faces subdivided once are 24 faces on 26
    /// vertices, the corners, a point on each edge and one in each face.
    /// </para>
    /// <para>
    /// The shape is the same, every new point lying on a face it was. A face not selected that shares
    /// a cut edge takes the edge's points as corners, so the mesh stays closed. The pieces are
    /// selected after, the first in the face's place and the rest after the last face, and a face's
    /// own texture coordinates are cut with it.
    /// </para>
    /// </remarks>
    public EditMesh Subdivide(int cuts = 1)
    {
        if (cuts < 1 || cuts > MostCuts) throw new ArgumentOutOfRangeException(nameof(cuts), cuts, $"A subdivision makes from one to {MostCuts} cuts along each edge.");

        var picked = _selectedFaces.Order().ToArray();
        var parts = cuts + 1;

        // The points along each edge of the selected faces, from its lower end to its higher.
        var along = new Dictionary<(int, int), int[]>();
        foreach (var index in picked)
        {
            var corners = _faces[index].Corners;
            for (var i = 0; i < corners.Length; i++)
            {
                var key = Key(corners[i], corners[(i + 1) % corners.Length]);
                if (along.ContainsKey(key)) continue;

                var (low, high) = (_positions[key.Item1], _positions[key.Item2]);
                along[key] = Enumerable.Range(1, cuts).Select(k => AddVertex(low + (high - low) * ((float)k / parts))).ToArray();
            }
        }

        var count = _faces.Count;
        var chosen = picked.ToHashSet();
        for (var index = 0; index < count; index++)
        {
            if (!chosen.Contains(index)) PutOnEdges(_faces[index], along);
        }

        _selectedFaces.Clear();
        _selectedVertices.Clear();
        foreach (var index in picked)
        {
            var face = _faces[index];
            var pieces = face.Corners.Length switch
            {
                4 => GridOf(face, along, parts),
                3 => TrianglesOf(face, along, parts),
                _ => FanOf(face, along, parts),
            };

            (face.Corners, face.Uvs) = pieces[0];
            Pick(index);
            foreach (var (corners, uvs) in pieces.Skip(1)) Pick(AddFace(corners, face.Slot, face.Smooth, uvs));
        }

        Changed();
        return Did(cuts == 1 ? "subdivide" : $"subdivide {cuts}");

        void Pick(int face)
        {
            _selectedFaces.Add(face);
            _selectedVertices.UnionWith(_faces[face].Corners);
        }
    }

    /// <summary>A four sided face as a grid of parts a side, its corners and coordinates.</summary>
    private List<(int[] Corners, Vec2[]? Uvs)> GridOf(Face face, Dictionary<(int, int), int[]> along, int parts)
    {
        var corners = face.Corners;
        var points = new int[parts + 1, parts + 1];
        var coordinates = face.Uvs is { } own ? new Vec2[parts + 1, parts + 1] : null;
        var (p0, p1, p2, p3) = (_positions[corners[0]], _positions[corners[1]], _positions[corners[2]], _positions[corners[3]]);

        // The first index runs along the first edge, from the first corner to the second, and the
        // second from the first edge to the third, from the fourth corner to the third.
        for (var u = 0; u <= parts; u++)
        {
            for (var v = 0; v <= parts; v++)
            {
                points[u, v] = (u, v) switch
                {
                    (_, 0) => OnEdge(along, corners[0], corners[1], u, parts),
                    (_, var top) when top == parts => OnEdge(along, corners[3], corners[2], u, parts),
                    (0, _) => OnEdge(along, corners[0], corners[3], v, parts),
                    (var right, _) when right == parts => OnEdge(along, corners[1], corners[2], v, parts),
                    _ => AddVertex(Bilinear(p0, p1, p2, p3, (float)u / parts, (float)v / parts)),
                };

                if (coordinates is not null) coordinates[u, v] = Bilinear(face.Uvs![0], face.Uvs[1], face.Uvs[2], face.Uvs[3], (float)u / parts, (float)v / parts);
            }
        }

        var pieces = new List<(int[], Vec2[]?)>();
        for (var v = 0; v < parts; v++)
        {
            for (var u = 0; u < parts; u++)
            {
                pieces.Add((
                    [points[u, v], points[u + 1, v], points[u + 1, v + 1], points[u, v + 1]],
                    coordinates is null ? null : [coordinates[u, v], coordinates[u + 1, v], coordinates[u + 1, v + 1], coordinates[u, v + 1]]));
            }
        }

        return pieces;
    }

    /// <summary>A triangle as triangles of parts a side, its corners and coordinates.</summary>
    private List<(int[] Corners, Vec2[]? Uvs)> TrianglesOf(Face face, Dictionary<(int, int), int[]> along, int parts)
    {
        var corners = face.Corners;
        var (a, b, c) = (_positions[corners[0]], _positions[corners[1]], _positions[corners[2]]);
        var points = new Dictionary<(int, int), int>();
        var coordinates = new Dictionary<(int, int), Vec2>();

        // A point i parts of the way from the first corner toward the second and j toward the third.
        for (var i = 0; i <= parts; i++)
        {
            for (var j = 0; i + j <= parts; j++)
            {
                points[(i, j)] = (i, j) switch
                {
                    (_, 0) => OnEdge(along, corners[0], corners[1], i, parts),
                    (0, _) => OnEdge(along, corners[0], corners[2], j, parts),
                    _ when i + j == parts => OnEdge(along, corners[1], corners[2], j, parts),
                    _ => AddVertex(a + (b - a) * ((float)i / parts) + (c - a) * ((float)j / parts)),
                };

                if (face.Uvs is { } own) coordinates[(i, j)] = own[0] + (own[1] - own[0]) * ((float)i / parts) + (own[2] - own[0]) * ((float)j / parts);
            }
        }

        var pieces = new List<(int[], Vec2[]?)>();
        void Add((int, int) first, (int, int) second, (int, int) third) => pieces.Add((
            [points[first], points[second], points[third]],
            face.Uvs is null ? null : [coordinates[first], coordinates[second], coordinates[third]]));

        for (var j = 0; j < parts; j++)
        {
            for (var i = 0; i + j < parts; i++)
            {
                Add((i, j), (i + 1, j), (i, j + 1));
                if (i + j < parts - 1) Add((i + 1, j), (i + 1, j + 1), (i, j + 1));
            }
        }

        return pieces;
    }

    /// <summary>A face of more than four corners as a face for each corner round its middle.</summary>
    private List<(int[] Corners, Vec2[]? Uvs)> FanOf(Face face, Dictionary<(int, int), int[]> along, int parts)
    {
        var corners = face.Corners;
        var n = corners.Length;
        var sum = Vec3.Zero;
        foreach (var corner in corners) sum += _positions[corner];
        var middle = AddVertex(sum * (1f / n));
        var middleUv = face.Uvs is { } own ? own.Aggregate(Vec2.Zero, (total, uv) => total + uv) * (1f / n) : default;

        // An edge's points from its start to its end, split where the face for its start ends and
        // the one for its end begins, the middle where the parts are even.
        var split = (parts + 1) / 2;
        (int Point, Vec2 Uv) At(int edge, int k)
        {
            var (from, to) = (corners[edge], corners[(edge + 1) % n]);
            var uv = face.Uvs is { } uvs ? uvs[edge] + (uvs[(edge + 1) % n] - uvs[edge]) * ((float)k / parts) : default;
            return (OnEdge(along, from, to, k, parts), uv);
        }

        var pieces = new List<(int[], Vec2[]?)>();
        for (var corner = 0; corner < n; corner++)
        {
            var before = (corner + n - 1) % n;
            var loop = new List<(int Point, Vec2 Uv)> { (middle, middleUv) };
            for (var k = split; k <= parts; k++) loop.Add(At(before, k));
            for (var k = 1; k <= split; k++) loop.Add(At(corner, k));
            pieces.Add(([.. loop.Select(point => point.Point)], face.Uvs is null ? null : [.. loop.Select(point => point.Uv)]));
        }

        return pieces;
    }

    /// <summary>The point k parts of the way along an edge from one end to the other, an end itself at nothing and at all the parts.</summary>
    private static int OnEdge(Dictionary<(int, int), int[]> along, int from, int to, int k, int parts)
    {
        if (k == 0) return from;
        if (k == parts) return to;
        var points = along[Key(from, to)];
        return from < to ? points[k - 1] : points[parts - 1 - k];
    }

    /// <summary>Puts the points along a face's edges into it as corners, so it stays joined to the faces those edges were cut in.</summary>
    private void PutOnEdges(Face face, Dictionary<(int, int), int[]> along)
    {
        var corners = face.Corners;
        if (!Enumerable.Range(0, corners.Length).Any(i => along.ContainsKey(Key(corners[i], corners[(i + 1) % corners.Length])))) return;

        var fit = face.Uvs is null ? null : FitOf(face);
        var put = new List<int>();
        var uvs = new List<Vec2>();
        for (var i = 0; i < corners.Length; i++)
        {
            var (from, to) = (corners[i], corners[(i + 1) % corners.Length]);
            put.Add(from);
            if (face.Uvs is { } own) uvs.Add(own[i]);
            if (!along.TryGetValue(Key(from, to), out var points)) continue;

            foreach (var point in from < to ? points : Enumerable.Reverse(points))
            {
                put.Add(point);
                if (fit is { } map) uvs.Add(map.At(_positions[point]));
            }
        }

        (face.Corners, face.Uvs) = ([.. put], face.Uvs is null ? null : [.. uvs]);
    }

    /// <summary>
    /// The edges a ring of four sided faces crosses from an edge, each as the ring runs it, from the
    /// end the start's lower end is carried to, and the faces between, in the order they were met.
    /// </summary>
    private (List<(int From, int To)> Rungs, List<int> Faces) Ring(Dictionary<(int From, int To), int> directed, (int Low, int High) start)
    {
        var rungs = new List<(int From, int To)> { (start.Low, start.High) };
        var faces = new List<int>();

        // One way through the face running along the edge as it is, and back the other way through
        // the face running along it backwards, where the first did not come round.
        foreach (var forward in (ReadOnlySpan<bool>)[true, false])
        {
            var (from, to) = (start.Low, start.High);
            while (true)
            {
                if (!directed.TryGetValue(forward ? (from, to) : (to, from), out var face) || faces.Contains(face) || _faces[face].Corners.Length != 4) break;

                var corners = _faces[face].Corners;
                var i = Array.IndexOf(corners, forward ? from : to);
                faces.Add(face);

                // Across the face, the corner after the edge's far end is where its far end goes,
                // and the one after that where its near end goes, run the face's way or back.
                (from, to) = forward ? (corners[(i + 3) % 4], corners[(i + 2) % 4]) : (corners[(i + 2) % 4], corners[(i + 3) % 4]);
                if ((Math.Min(from, to), Math.Max(from, to)) == start) return (rungs, faces);
                rungs.Add((from, to));
            }
        }

        return (rungs, faces);
    }

    /// <summary>Which face runs along each edge which way.</summary>
    private Dictionary<(int From, int To), int> Directed()
    {
        var directed = new Dictionary<(int From, int To), int>();
        for (var face = 0; face < _faces.Count; face++)
        {
            var corners = _faces[face].Corners;
            for (var i = 0; i < corners.Length; i++) directed.TryAdd((corners[i], corners[(i + 1) % corners.Length]), face);
        }

        return directed;
    }

    /// <summary>How far along an edge a point on it is, from nothing at its start to one at its end.</summary>
    private float Along(int from, int to, int point)
    {
        var run = _positions[to] - _positions[from];
        return run.LengthSquared < 1e-20f ? 0f : Vec3.Dot(_positions[point] - _positions[from], run) / run.LengthSquared;
    }

    private static (int, int) Key(int a, int b) => a < b ? (a, b) : (b, a);

    private static Vec3 Bilinear(Vec3 p0, Vec3 p1, Vec3 p2, Vec3 p3, float u, float v)
    {
        var near = p0 + (p1 - p0) * u;
        var far = p3 + (p2 - p3) * u;
        return near + (far - near) * v;
    }

    private static Vec2 Bilinear(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, float u, float v)
    {
        var near = p0 + (p1 - p0) * u;
        var far = p3 + (p2 - p3) * u;
        return near + (far - near) * v;
    }
}
