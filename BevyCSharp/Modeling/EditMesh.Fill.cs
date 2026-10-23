namespace Bevy;

// The operations that join faces or close the space between them, bridging two loops with a band
// of faces, filling a hole with one face and dissolving faces into one, as Blender's bridge edge
// loops, fill and dissolve do.
public sealed partial class EditMesh
{
    /// <summary>Takes away the two selected faces, or two regions of them, and joins their rims with a band of four sided faces.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// <para>
    /// The selected faces make two regions, each with one rim of the same count of edges, such as
    /// a box's top and another box's bottom over it, or two ends of a hall. Both are taken away, and
    /// a face joins each edge of one rim to an edge of the other, the corners paired so the band is
    /// as short as it can be, so a box's top bridged to the bottom of a box over it makes one solid,
    /// the band its walls between the two.
    /// </para>
    /// <para>
    /// Each face of the band runs along its rim edge the way the face taken away did, so a closed
    /// solid stays closed. The band is selected after, in the slot of the first face taken away.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">The selected faces are not two regions with one rim each of the same count.</exception>
    public EditMesh Bridge()
    {
        var regions = Regions(_selectedFaces.Order().ToArray());
        if (regions.Count != 2) throw new InvalidOperationException($"A bridge joins two regions of selected faces, and {regions.Count} are selected.");

        var rims = regions.Select(region => Rims(region)).ToArray();
        if (rims.Any(rim => rim is not [_])) throw new InvalidOperationException("A bridge joins two regions with one rim each, and a region selected has a hole in it or runs round a corner twice.");

        var (first, second) = (rims[0]![0], rims[1]![0]);
        if (first.Count != second.Count) throw new InvalidOperationException($"A bridge joins two rims of the same count, and they have {first.Count} and {second.Count} edges.");

        // The second rim runs the other way round the band, so a corner of the first pairs with
        // the corner of the second that many places back, from the offset that makes the band
        // shortest.
        var n = first.Count;
        var offset = Enumerable.Range(0, n).MinBy(shift =>
            Enumerable.Range(0, n).Sum(i => (double)(_positions[first[i]] - _positions[second[((shift - i) % n + n) % n]]).Length));

        var source = _faces[regions[0].Min()];
        var band = new List<int[]>();
        for (var i = 0; i < n; i++)
        {
            var (here, next) = (((offset - i) % n + n) % n, ((offset - i - 1) % n + n) % n);
            band.Add([first[i], first[(i + 1) % n], second[next], second[here]]);
        }

        RemoveFaces(regions.SelectMany(region => region).ToHashSet());
        _selectedFaces.Clear();
        _selectedVertices.Clear();
        foreach (var corners in band)
        {
            _selectedFaces.Add(AddFace(corners, source.Slot, source.Smooth));
            _selectedVertices.UnionWith(corners);
        }

        Compact([]);
        return Did("bridge");
    }

    /// <summary>Closes each hole whose rim's edges are selected with one face.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// An edge one face runs along and no face runs back along is a hole's edge, and those with
    /// both ends selected are followed round into loops, each closed by a face running the other
    /// way, so it faces the way the faces round it face. A box with its top taken away and its
    /// corners selected is closed again by one face. The faces made are selected after, each in
    /// the slot of a face beside it.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No selected edge is a hole's.</exception>
    public EditMesh Fill()
    {
        var uses = EdgeUses();
        var slots = new Dictionary<int, int>();
        var next = new Dictionary<int, int>();
        foreach (var face in _faces)
        {
            var corners = face.Corners;
            for (var i = 0; i < corners.Length; i++)
            {
                var (from, to) = (corners[i], corners[(i + 1) % corners.Length]);
                var use = uses[Key(from, to)];
                if (use.Forward + use.Backward != 1 || !_selectedVertices.Contains(from) || !_selectedVertices.Contains(to)) continue;

                // The face filling runs back along the edge, from its far end to its near one.
                next.TryAdd(to, from);
                slots.TryAdd(to, face.Slot);
            }
        }

        var loops = Loops(next);
        if (loops.Count == 0) throw new InvalidOperationException("No selected edge is the edge of a hole, one face running along it and none back.");

        _selectedFaces.Clear();
        _selectedVertices.Clear();
        foreach (var loop in loops)
        {
            _selectedFaces.Add(AddFace([.. loop], slots[loop[0]]));
            _selectedVertices.UnionWith(loop);
        }

        Changed();
        return Did("fill");
    }

    /// <summary>Joins each region of selected faces into one face, its rim its corners, the vertices inside it taken away.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// The corners round the rim stay where they are, so a flat region is drawn as it was, and a
    /// region that is not flat makes a face whose corners do not lie in one plane, which the drawing
    /// cuts into triangles as any face. A region with a hole in it, whose rim is two loops, cannot be
    /// one face and is left. The faces made are selected after, each in the slot of the first face
    /// of its region, so a box's top subdivided into four faces dissolves into one face of eight
    /// corners.
    /// </remarks>
    public EditMesh DissolveFaces()
    {
        DissolveRegions(Regions(_selectedFaces.Order().ToArray()));
        return Did("dissolve");
    }

    /// <summary>Takes away the selected edges, each two faces either side joined into one.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// An edge is selected where both its ends are, and each one two faces share joins them, the
    /// faces joined across several edges becoming one, as <see cref="DissolveFaces"/> joins a
    /// region. A loop cut's loop selected and dissolved puts back the faces it cut.
    /// </remarks>
    public EditMesh DissolveEdges()
    {
        // The faces either side of each selected edge, joined into groups across the edges.
        var group = Enumerable.Range(0, _faces.Count).ToArray();
        int Root(int face) => group[face] == face ? face : group[face] = Root(group[face]);

        var directed = Directed();
        foreach (var (low, high) in EdgeUses().Keys)
        {
            if (!_selectedVertices.Contains(low) || !_selectedVertices.Contains(high)) continue;
            if (!directed.TryGetValue((low, high), out var one) || !directed.TryGetValue((high, low), out var other)) continue;
            group[Root(one)] = Root(other);
        }

        var regions = Enumerable.Range(0, _faces.Count).GroupBy(Root).Where(members => members.Count() > 1).Select(members => members.ToArray()).ToList();
        DissolveRegions(regions, _selectedVertices.ToHashSet());
        return Did("dissolve edges");
    }

    /// <summary>
    /// Makes each region one face of its rim, where it has one rim, takes away those of the vertices
    /// given that are left between two edges in a straight line, and selects what was made and what
    /// was left.
    /// </summary>
    private void DissolveRegions(List<int[]> regions, HashSet<int>? straighten = null)
    {
        var going = new HashSet<int>();
        var made = new List<(List<int> Corners, Face Source, Vec2[]? Uvs)>();
        var left = new List<int>();
        foreach (var region in regions)
        {
            if (Rims(region) is [var rim] && region.Length > 1)
            {
                // The face made takes the first face's own coordinates where it lies, if it had any.
                var source = _faces[region.Min()];
                going.UnionWith(region);
                made.Add((rim, source, FitOf(source) is { } fit ? [.. rim.Select(corner => fit.At(_positions[corner]))] : null));
            }
            else
            {
                left.AddRange(region);
            }
        }

        var keep = left.Select(face => _faces[face]).ToList();
        RemoveFaces(going);

        _selectedFaces.Clear();
        _selectedVertices.Clear();
        foreach (var face in keep)
        {
            var index = _faces.IndexOf(face);
            _selectedFaces.Add(index);
            _selectedVertices.UnionWith(face.Corners);
        }

        foreach (var (corners, source, uvs) in made)
        {
            _selectedFaces.Add(AddFace([.. corners], source.Slot, source.Smooth, uvs));
            _selectedVertices.UnionWith(corners);
        }

        if (straighten is not null) Straighten(straighten);
        Compact([]);
    }

    /// <summary>
    /// Takes each of the vertices given that is left with two edges, lying between their far ends
    /// in a straight line, out of the faces it is a corner of, so the shape stays as it is and a
    /// vertex only a dissolved edge needed goes with it.
    /// </summary>
    private void Straighten(HashSet<int> vertices)
    {
        Changed();
        var neighbors = new Dictionary<int, HashSet<int>>();
        void Join(int vertex, int other)
        {
            if (!vertices.Contains(vertex)) return;
            if (!neighbors.TryGetValue(vertex, out var set)) neighbors[vertex] = set = [];
            set.Add(other);
        }

        foreach (var (low, high) in EdgeUses().Keys)
        {
            Join(low, high);
            Join(high, low);
        }

        foreach (var (vertex, around) in neighbors)
        {
            if (around.Count != 2) continue;

            var (before, after) = (_positions[around.First()], _positions[around.Last()]);
            var (here, run) = (_positions[vertex], after - before);
            if (run.LengthSquared < 1e-12f || Vec3.Cross(here - before, run).Length > 1e-5f * run.Length || Vec3.Dot(here - before, after - here) <= 0f) continue;

            foreach (var face in _faces)
            {
                var at = Array.IndexOf(face.Corners, vertex);
                if (at < 0 || face.Corners.Length <= 3) continue;
                face.Corners = [.. face.Corners[..at], .. face.Corners[(at + 1)..]];
                if (face.Uvs is { } uvs) face.Uvs = [.. uvs[..at], .. uvs[(at + 1)..]];
            }
        }

        Changed();
    }

    /// <summary>The faces given, split into regions joined along edges two of them share running opposite ways.</summary>
    private List<int[]> Regions(int[] faces)
    {
        var inside = faces.ToHashSet();
        var directed = new Dictionary<(int, int), int>();
        foreach (var face in faces)
        {
            var corners = _faces[face].Corners;
            for (var i = 0; i < corners.Length; i++) directed.TryAdd((corners[i], corners[(i + 1) % corners.Length]), face);
        }

        var regions = new List<int[]>();
        var seen = new HashSet<int>();
        foreach (var start in faces)
        {
            if (!seen.Add(start)) continue;

            var region = new List<int> { start };
            for (var at = 0; at < region.Count; at++)
            {
                var corners = _faces[region[at]].Corners;
                for (var i = 0; i < corners.Length; i++)
                {
                    if (directed.TryGetValue((corners[(i + 1) % corners.Length], corners[i]), out var beside) && inside.Contains(beside) && seen.Add(beside)) region.Add(beside);
                }
            }

            regions.Add([.. region.Order()]);
        }

        return regions;
    }

    /// <summary>
    /// A region's rims, the edges of its faces no other face of it runs back along, followed into
    /// loops the way the faces run them, or nothing where a corner starts two of them.
    /// </summary>
    private List<List<int>>? Rims(int[] region)
    {
        var inside = new HashSet<(int, int)>();
        foreach (var face in region)
        {
            var corners = _faces[face].Corners;
            for (var i = 0; i < corners.Length; i++) inside.Add((corners[i], corners[(i + 1) % corners.Length]));
        }

        var next = new Dictionary<int, int>();
        foreach (var (from, to) in inside)
        {
            if (inside.Contains((to, from))) continue;
            if (!next.TryAdd(from, to)) return null;
        }

        return Loops(next);
    }

    /// <summary>Follows each vertex to the next round loops, the lowest vertex of each first, and drops what does not come round.</summary>
    private static List<List<int>> Loops(Dictionary<int, int> next)
    {
        var loops = new List<List<int>>();
        var left = new Dictionary<int, int>(next);
        while (left.Count > 0)
        {
            var start = left.Keys.Min();
            var loop = new List<int> { start };
            var at = start;
            var closed = false;
            while (left.Remove(at, out var to))
            {
                if (to == start)
                {
                    closed = true;
                    break;
                }

                loop.Add(to);
                at = to;
            }

            if (closed && loop.Count >= 3) loops.Add(loop);
        }

        return loops;
    }
}
