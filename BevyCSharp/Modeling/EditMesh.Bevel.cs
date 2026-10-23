namespace Bevy;

// The bevel, which cuts the selected edges off by a width, each edge becoming a strip of faces
// across its profile, and closes the corners where beveled edges meet. Blender's edge bevel is the
// model, its offset width, its count of segments and its round profile.
public sealed partial class EditMesh
{
    /// <summary>The most segments a bevel's profile is cut into, sixty-four.</summary>
    public const int MostSegments = 64;

    /// <summary>Cuts the selected edges off by a width, each a strip of faces across a round profile, the corners where they meet closed.</summary>
    /// <param name="width">How far each face's edge moves in along the face, in meters, above zero.</param>
    /// <param name="segments">How many faces the strip has across, one for a flat chamfer, up to <see cref="MostSegments"/>.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// <para>
    /// An edge is selected where both its ends are, and one shared by two faces running along it
    /// in opposite directions is beveled, with both its ends where the faces close round. An edge
    /// on a hole's rim, or with an end on one, is left, as nothing closes the corner there. Each
    /// face beside a beveled edge has that edge moved in along it by the width, so a box's twelve
    /// edges beveled leave each side a smaller square in its middle.
    /// </para>
    /// <para>
    /// The profile across an edge runs from where one face's edge went to where the other's went,
    /// bulging toward the edge it replaced, a quarter circle where the faces meet square and an
    /// ellipse's quarter where they meet at another angle, as Blender's round profile is. A corner
    /// where three or more beveled edges meet is closed by a patch, one face at one segment, four
    /// sided faces round its middle at two, and triangles round its middle at more. Where two
    /// beveled edges meet and every face round the corner is beside one, the two strips run into
    /// each other, as the top's edges of a box beveled turn its corners. A corner of a face no
    /// beveled edge reaches stays where it is, and an edge that is not beveled at a corner keeps its
    /// faces joined along it, their moved corners put on it at one point.
    /// </para>
    /// <para>
    /// The strips and the patches are selected after, each in the slot of the first face beside
    /// it. A width past half an edge's length makes faces that cross, which the operation leaves as
    /// asked. A face's own texture coordinates follow it to where its corners moved.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">No selected edge can be beveled.</exception>
    public EditMesh Bevel(float width, int segments = 1)
    {
        if (!(width > 0f) || !float.IsFinite(width)) throw new ArgumentOutOfRangeException(nameof(width), width, "A bevel's width is a finite number above zero.");
        if (segments < 1 || segments > MostSegments) throw new ArgumentOutOfRangeException(nameof(segments), segments, $"A bevel has from one to {MostSegments} segments.");

        var edges = EdgeUses().Keys.Where(edge => _selectedVertices.Contains(edge.Low) && _selectedVertices.Contains(edge.High)).ToHashSet();
        if (!BevelEdges(edges, width, segments, select: true))
            throw new InvalidOperationException("No selected edge is shared by two faces closing round both its ends, which a bevel needs.");

        return Did(segments == 1 ? $"bevel {F(width)}" : $"bevel {F(width)} {segments}");
    }

    /// <summary>
    /// Bevels the edges given that two faces share with both ends closed round, selecting what it
    /// made where asked, and says whether there was one.
    /// </summary>
    internal bool BevelEdges(HashSet<(int Low, int High)> candidates, float width, int segments, bool select)
    {
        // Which face runs along each edge which way, an edge run the same way twice marking the
        // mesh as not closed there.
        var directed = new Dictionary<(int From, int To), int>();
        var twice = new HashSet<(int, int)>();
        var around = new Dictionary<int, List<int>>();
        for (var face = 0; face < _faces.Count; face++)
        {
            var corners = _faces[face].Corners;
            for (var i = 0; i < corners.Length; i++)
            {
                if (!directed.TryAdd((corners[i], corners[(i + 1) % corners.Length]), face)) twice.Add((corners[i], corners[(i + 1) % corners.Length]));
                if (!around.TryGetValue(corners[i], out var list)) around[corners[i]] = list = [];
                list.Add(face);
            }
        }

        var beveled = candidates.Where(edge =>
                directed.TryGetValue((edge.Low, edge.High), out var one) && directed.TryGetValue((edge.High, edge.Low), out var other) && one != other
                && !twice.Contains((edge.Low, edge.High)) && !twice.Contains((edge.High, edge.Low)))
            .ToHashSet();

        // The faces round each end in order, each face's next the one across its edge leaving the
        // corner, and an end they do not close round, or that a face meets twice, takes its edges
        // out of the bevel.
        var rings = new Dictionary<int, List<int>>();
        foreach (var vertex in beveled.SelectMany(edge => (int[])[edge.Low, edge.High]).Distinct().Order())
        {
            var faces = around[vertex];
            var ring = new List<int> { faces[0] };
            var closes = faces.Distinct().Count() == faces.Count;
            while (closes)
            {
                var next = directed.GetValueOrDefault((After(ring[^1], vertex), vertex), -1);
                if (next == ring[0]) break;
                if (next < 0 || ring.Contains(next)) closes = false;
                else ring.Add(next);
            }

            if (closes && ring.Count == faces.Count) rings[vertex] = ring;
        }

        beveled.RemoveWhere(edge => !rings.ContainsKey(edge.Low) || !rings.ContainsKey(edge.High));
        if (beveled.Count == 0) return false;

        bool IsBeveled(int a, int b) => beveled.Contains(a < b ? (a, b) : (b, a));

        var fits = new Dictionary<int, UvMap>();
        var normals = new Dictionary<int, Vec3>();
        foreach (var face in rings.Values.SelectMany(ring => ring).Distinct())
        {
            normals[face] = Newell(_faces[face].Corners).Normalized;
            if (FitOf(_faces[face]) is { } fit) fits[face] = fit;
        }

        // Each face's corner at an end, where it moved, the corners of two moved faces either side
        // of an edge not beveled put on it at one point, their mean.
        var moved = new Dictionary<(int Face, int Vertex), int>();
        var degenerate = new HashSet<int>();
        foreach (var (vertex, ring) in rings)
        {
            var at = _positions[vertex];
            var places = new Vec3?[ring.Count];
            for (var j = 0; j < ring.Count; j++)
            {
                var (before, after) = (Before(ring[j], vertex), After(ring[j], vertex));
                var (inward, outward) = (IsBeveled(before, vertex), IsBeveled(vertex, after));
                if (!inward && !outward) continue;
                places[j] = OffsetCorner(normals[ring[j]], _positions[before], at, _positions[after], inward ? width : 0f, outward ? width : 0f);
            }

            // A moved face meets a face not beveled on one side at most, so each corner is in one
            // pair or none.
            var made = new int[ring.Count];
            Array.Fill(made, -1);
            for (var j = 0; j < ring.Count; j++)
            {
                var next = (j + 1) % ring.Count;
                if (places[j] is not { } place || places[next] is not { } beside || next == j || IsBeveled(vertex, After(ring[j], vertex))) continue;
                made[j] = made[next] = AddVertex((place + beside) * 0.5f);
            }

            for (var j = 0; j < ring.Count; j++)
            {
                if (places[j] is { } place && made[j] < 0) made[j] = AddVertex(place);
                if (made[j] >= 0) moved[(ring[j], vertex)] = made[j];
            }

            var count = ring.Count(face => IsBeveled(vertex, After(face, vertex)));
            if (count == 2 && places.All(place => place is not null)) degenerate.Add(vertex);
        }

        // The profile across each beveled edge at each end, from the face running along it from
        // its lower end to the face running back, its corners new but its two ends.
        var profiles = new Dictionary<((int Low, int High) Edge, int End), int[]>();
        var identified = new HashSet<int>();
        foreach (var edge in beveled.Order())
        {
            var (first, second) = (directed[(edge.Low, edge.High)], directed[(edge.High, edge.Low)]);
            foreach (var end in (ReadOnlySpan<int>)[edge.Low, edge.High])
            {
                var (from, to) = (moved[(first, end)], moved[(second, end)]);
                var profile = new int[segments + 1];
                (profile[0], profile[segments]) = (from, to);

                // At a corner where two strips run into each other, the second takes the first's
                // corners, which lie between the same two points, whichever way it runs.
                var known = degenerate.Contains(end)
                    ? profiles.FirstOrDefault(entry => entry.Key.End == end && (entry.Value[0], entry.Value[^1]) is var ends && (ends == (from, to) || ends == (to, from))).Value
                    : null;
                if (known is not null) identified.Add(end);
                var same = known is not null && known[0] == from;

                // The profile bulges toward the edge's point level with its two ends, which is the
                // corner itself where two strips run into each other along the line between them.
                var (a, b) = (_positions[edge.Low], _positions[edge.High]);
                var run = b - a;
                var level = a + run * (Vec3.Dot((_positions[from] + _positions[to]) * 0.5f - a, run) / run.LengthSquared);
                var corner = degenerate.Contains(end) ? _positions[end] : level;

                for (var k = 1; k < segments; k++)
                {
                    profile[k] = known is not null
                        ? known[same ? k : segments - k]
                        : AddVertex(Profile(_positions[from], _positions[to], corner, (float)k / segments));
                }

                profiles[(edge, end)] = profile;
            }
        }

        var original = _faces.Count;
        var added = new List<int>();

        // The strips, a face for each segment, running back along the moved edges beside them.
        foreach (var edge in beveled.Order())
        {
            var (low, high) = (profiles[(edge, edge.Low)], profiles[(edge, edge.High)]);
            var source = _faces[Math.Min(directed[(edge.Low, edge.High)], directed[(edge.High, edge.Low)])];
            for (var k = 0; k < segments; k++) added.Add(AddFace([high[k], low[k], low[k + 1], high[k + 1]], source.Slot, source.Smooth));
        }

        // The corners, each walked round against the faces' order, so it runs along every strip's
        // end and every moved face's new corner the other way.
        var spliced = new Dictionary<(int Face, int Vertex), int[]>();
        foreach (var (vertex, ring) in rings)
        {
            if (identified.Contains(vertex)) continue;

            var walk = new List<int>();
            void Visit(int point)
            {
                if (walk.Count == 0 || walk[^1] != point) walk.Add(point);
            }

            for (var j = ring.Count - 1; j >= 0; j--)
            {
                var (face, next) = (ring[j], ring[(j + 1) % ring.Count]);
                var after = After(face, vertex);
                if (IsBeveled(vertex, after))
                {
                    var edge = vertex < after ? (vertex, after) : (after, vertex);
                    var profile = profiles[(edge, vertex)];

                    // The profile runs from the face running the edge up from its lower end, and
                    // the walk goes from the face behind round to this one.
                    var fromNext = moved[(next, vertex)] == profile[0];
                    for (var k = 0; k <= segments; k++) Visit(fromNext ? profile[k] : profile[segments - k]);
                }

                Visit(moved.TryGetValue((face, vertex), out var corner) ? corner : vertex);
            }

            while (walk.Count > 1 && walk[0] == walk[^1]) walk.RemoveAt(walk.Count - 1);

            var kept = ring.Where(face => !moved.ContainsKey((face, vertex))).ToArray();
            var at = walk.IndexOf(vertex);
            var source = _faces[ring.Min()];

            if (at >= 0 && kept.Length == 1 && walk.Count(point => point == vertex) == 1)
            {
                // The one face that kept the corner takes the patch's points in its place.
                spliced[(kept[0], vertex)] = [.. walk.Skip(at + 1), .. walk.Take(at)];
                continue;
            }

            if (at >= 0)
            {
                // The faces that kept the corner keep it, and each stretch of the walk between two
                // visits to it is a face of its own closing on it.
                var turned = walk.Skip(at).Concat(walk.Take(at)).ToList();
                var piece = new List<int> { vertex };
                for (var i = 1; i <= turned.Count; i++)
                {
                    if (i < turned.Count && turned[i] != vertex)
                    {
                        piece.Add(turned[i]);
                        continue;
                    }

                    if (piece.Count >= 3) added.Add(AddFace([.. piece], source.Slot, source.Smooth));
                    piece = [vertex];
                }

                continue;
            }

            if (segments == 1 || walk.Count == 3)
            {
                if (walk.Count >= 3) added.Add(AddFace([.. walk], source.Slot, source.Smooth));
                continue;
            }

            // A point in the patch's middle, as far from the point inside every face's offset as
            // the profiles are, so a box's corner is an eighth of a sphere's facets.
            var inside = _positions[vertex] + Offset(ring.Select(face => normals[face]).Distinct().ToList(), -width);
            var reach = walk.Average(point => (_positions[point] - inside).Length);
            var toward = _positions[vertex] - inside;
            var middle = AddVertex(toward.LengthSquared > 1e-12f ? inside + toward.Normalized * reach : _positions[vertex]);

            if (segments == 2 && walk.Count % 2 == 0)
            {
                // A four sided face from the middle across each corner of a face, as Blender's
                // patch of two segments is, the walk alternating corners and profiles' middles.
                var offset = ring.Any(face => moved[(face, vertex)] == walk[0]) ? 1 : 0;
                for (var i = offset; i < walk.Count + offset; i += 2)
                {
                    added.Add(AddFace([middle, walk[i % walk.Count], walk[(i + 1) % walk.Count], walk[(i + 2) % walk.Count]], source.Slot, source.Smooth));
                }

                continue;
            }

            for (var i = 0; i < walk.Count; i++) added.Add(AddFace([middle, walk[i], walk[(i + 1) % walk.Count]], source.Slot, source.Smooth));
        }

        // The faces beside the corners, each moved corner put in, a neighbor's corner put on an
        // edge it shares with a face that kept the corner, and a patch put in a corner it took.
        for (var face = 0; face < original; face++)
        {
            var known = _faces[face];
            if (!known.Corners.Any(rings.ContainsKey)) continue;

            var corners = new List<int>();
            var uvs = new List<Vec2>();
            var own = known.Uvs;
            fits.TryGetValue(face, out var fit);

            // A corner it had keeps its coordinates, and a new one takes the face's where it lies.
            void Put(int point, int index)
            {
                corners.Add(point);
                if (own is not null) uvs.Add(index >= 0 ? own[index] : fit.At(_positions[point]));
            }

            for (var i = 0; i < known.Corners.Length; i++)
            {
                var vertex = known.Corners[i];
                if (!rings.TryGetValue(vertex, out var ring))
                {
                    Put(vertex, i);
                    continue;
                }

                if (moved.TryGetValue((face, vertex), out var corner))
                {
                    Put(corner, -1);
                    continue;
                }

                var j = ring.IndexOf(face);
                var behind = ring[(j + ring.Count - 1) % ring.Count];
                var ahead = ring[(j + 1) % ring.Count];

                if (moved.TryGetValue((behind, vertex), out var before)) Put(before, -1);
                if (spliced.TryGetValue((face, vertex), out var patch))
                {
                    foreach (var point in patch.Skip(1).SkipLast(1)) Put(point, -1);
                }
                else
                {
                    Put(vertex, i);
                }

                if (moved.TryGetValue((ahead, vertex), out var after)) Put(after, -1);
            }

            known.Corners = [.. corners];
            known.Uvs = own is null ? null : [.. uvs];
        }

        if (select)
        {
            _selectedFaces.Clear();
            _selectedVertices.Clear();
            foreach (var face in added)
            {
                _selectedFaces.Add(face);
                _selectedVertices.UnionWith(_faces[face].Corners);
            }
        }

        Changed();
        Compact([]);
        return true;
    }

    /// <summary>
    /// Where a face's corner goes when its edges either side move in along it by a width each,
    /// the point the two moved edges' lines meet at.
    /// </summary>
    private static Vec3 OffsetCorner(Vec3 normal, Vec3 before, Vec3 at, Vec3 after, float inWidth, float outWidth)
    {
        var into = (at - before).Normalized;
        var outOf = (after - at).Normalized;
        var inFromBefore = Vec3.Cross(normal, into).Normalized;
        var inFromAfter = Vec3.Cross(normal, outOf).Normalized;

        // The first edge's line moved in, followed until it is the second's width in from the
        // second edge, or the first's moved corner where the two run straight on.
        var start = at + inFromBefore * inWidth;
        var across = Vec3.Dot(into, inFromAfter);
        if (MathF.Abs(across) < 1e-6f) return inWidth > 0f ? start : at + inFromAfter * outWidth;

        return start + into * ((outWidth - inWidth * Vec3.Dot(inFromBefore, inFromAfter)) / across);
    }

    /// <summary>
    /// A point of a round profile from one point to another, a fraction of the way, bulging toward
    /// the corner it replaces, a quarter circle where the two meet the corner square.
    /// </summary>
    private static Vec3 Profile(Vec3 from, Vec3 to, Vec3 corner, float fraction)
    {
        var center = from + to - corner;
        var (sin, cos) = MathF.SinCos(fraction * MathF.PI / 2f);
        return center + (from - center) * cos + (to - center) * sin;
    }

    /// <summary>The corner after a vertex round a face.</summary>
    private int After(int face, int vertex)
    {
        var corners = _faces[face].Corners;
        return corners[(Array.IndexOf(corners, vertex) + 1) % corners.Length];
    }

    /// <summary>The corner before a vertex round a face.</summary>
    private int Before(int face, int vertex)
    {
        var corners = _faces[face].Corners;
        return corners[(Array.IndexOf(corners, vertex) + corners.Length - 1) % corners.Length];
    }
}
