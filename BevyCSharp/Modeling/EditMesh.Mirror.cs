namespace Bevy;

// The operations that cut or copy across a plane or about an axis, and the one that eases the
// vertices toward their neighbors, as Blender's bisect, mirror, spin and smooth vertices do.
public sealed partial class EditMesh
{
    /// <summary>How near the mirror's plane a vertex is to be put on it and shared by both halves, a millimeter, as Blender's mirror merges.</summary>
    public const float MirrorMerge = 0.001f;

    /// <summary>The most steps a spin takes, 256.</summary>
    public const int MostSteps = 256;

    // How near a plane or an axis a point is to lie on it, for the knife and the spin.
    private const float OnPlane = 1e-5f;

    /// <summary>Cuts the selected faces in two where a plane crosses them.</summary>
    /// <param name="axis">The plane's normal, any length but zero, or an axis's letter in the recipe.</param>
    /// <param name="at">A point the plane passes through, or nothing for the middle of the box holding the selected faces.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// <para>
    /// Each edge of a selected face with its ends either side of the plane takes a point where the
    /// plane crosses it, and each selected face the plane crosses is cut in two between the two
    /// points on its outline, a corner on the plane counting as one, so a box cut through its
    /// middle square to X has the four faces the plane crosses cut in two along a loop round it. A
    /// face beside a cut face that shares a cut edge takes the point as a corner, so the mesh stays
    /// closed.
    /// </para>
    /// <para>
    /// A face the plane crosses more than twice, which only a concave face can be, takes the points
    /// on its edges and is left whole. The pieces are selected after, the first in the face's place
    /// and the second after the last face, and a face's own texture coordinates are cut with it.
    /// </para>
    /// </remarks>
    public EditMesh Knife(Vec3 axis, Vec3? at = null)
    {
        Direction(axis, nameof(axis));

        var normal = axis.Normalized;
        var picked = _selectedFaces.Order().ToArray();
        var through = at ?? SelectionMiddle();
        float Side(int vertex) => Vec3.Dot(normal, _positions[vertex] - through);

        // A point on each edge of a selected face whose ends lie either side of the plane.
        var crossings = new Dictionary<(int, int), int[]>();
        foreach (var index in picked)
        {
            var corners = _faces[index].Corners;
            for (var i = 0; i < corners.Length; i++)
            {
                var (from, to) = (corners[i], corners[(i + 1) % corners.Length]);
                var key = Key(from, to);
                var (a, b) = (Side(key.Item1), Side(key.Item2));
                if (crossings.ContainsKey(key) || MathF.Abs(a) <= OnPlane || MathF.Abs(b) <= OnPlane || (a > 0f) == (b > 0f)) continue;

                var (low, high) = (_positions[key.Item1], _positions[key.Item2]);
                crossings[key] = [AddVertex(low + (high - low) * (a / (a - b)))];
            }
        }

        var chosen = picked.ToHashSet();
        var count = _faces.Count;
        for (var index = 0; index < count; index++)
        {
            if (!chosen.Contains(index)) PutOnEdges(_faces[index], crossings);
        }

        _selectedFaces.Clear();
        _selectedVertices.Clear();
        foreach (var index in picked)
        {
            var face = _faces[index];
            PutOnEdges(face, crossings);
            Pick(index);

            // The corners on the plane where the outline passes from one side to the other.
            var corners = face.Corners;
            var sides = corners.Select(corner => MathF.Abs(Side(corner)) <= OnPlane ? 0 : MathF.Sign(Side(corner))).ToArray();
            var cuts = Enumerable.Range(0, corners.Length).Where(i => sides[i] == 0 && Crosses(sides, i)).ToArray();
            if (cuts is not [var first, var second] || second - first == 1 || second - first == corners.Length - 1) continue;

            int[] One(int from, int to) => Enumerable.Range(0, (to - from + corners.Length) % corners.Length + 1).Select(k => (from + k) % corners.Length).ToArray();
            var (kept, other) = (One(first, second), One(second, first));
            var uvs = face.Uvs;

            (face.Corners, face.Uvs) = (kept.Select(i => corners[i]).ToArray(), uvs is null ? null : kept.Select(i => uvs[i]).ToArray());
            Pick(AddFace(other.Select(i => corners[i]).ToArray(), face.Slot, face.Smooth, uvs is null ? null : other.Select(i => uvs[i]).ToArray()));
        }

        Changed();
        return Did($"knife {AxisWords(axis)}{(at is { } point ? " at " + V(point) : "")}");

        void Pick(int face)
        {
            _selectedFaces.Add(face);
            _selectedVertices.UnionWith(_faces[face].Corners);
        }
    }

    /// <summary>Copies the selected faces mirrored across a plane, the vertices on it shared by both halves.</summary>
    /// <param name="axis">The plane's normal, any length but zero, or an axis's letter in the recipe.</param>
    /// <param name="at">A point the plane passes through, or nothing for the origin, about which a half is usually modeled.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// <para>
    /// A selected vertex within <see cref="MirrorMerge"/> of the plane is put on it and kept as one
    /// vertex for both halves, so half a box open where it meets the plane, mirrored, is a whole
    /// box with no vertex twice and no edge open. A face lying in the plane would lie inside the
    /// whole and is taken away, so a closed half mirrored is closed too.
    /// </para>
    /// <para>
    /// Each copy runs its corners backwards, so it faces out as its original does, and keeps its
    /// slot, its smoothing and its own texture coordinates. Both halves are selected after.
    /// </para>
    /// </remarks>
    public EditMesh Mirror(Vec3 axis, Vec3? at = null)
    {
        Direction(axis, nameof(axis));

        var normal = axis.Normalized;
        var through = at ?? Vec3.Zero;
        var picked = _selectedFaces.Order().ToArray();
        var used = picked.SelectMany(face => _faces[face].Corners).ToHashSet();

        var onPlane = new HashSet<int>();
        foreach (var vertex in used)
        {
            var distance = Vec3.Dot(normal, _positions[vertex] - through);
            if (MathF.Abs(distance) > MirrorMerge) continue;
            _positions[vertex] -= normal * distance;
            onPlane.Add(vertex);
        }

        var inside = picked.Where(face => _faces[face].Corners.All(onPlane.Contains)).ToHashSet();
        var copies = new Dictionary<int, int>();
        var made = new List<int>();
        foreach (var index in picked)
        {
            if (inside.Contains(index)) continue;

            var face = _faces[index];
            var corners = face.Corners.Select(corner =>
            {
                if (onPlane.Contains(corner)) return corner;
                if (!copies.TryGetValue(corner, out var copy))
                {
                    var point = _positions[corner];
                    copies[corner] = copy = AddVertex(point - normal * (2f * Vec3.Dot(normal, point - through)));
                }

                return copy;
            }).Reverse().ToArray();

            made.Add(AddFace(corners, face.Slot, face.Smooth, face.Uvs is { } uvs ? [.. Enumerable.Reverse(uvs)] : null));
        }

        var kept = _selectedFaces.Where(face => !inside.Contains(face)).ToArray();
        var originals = kept.Select(face => _faces[face]).Concat(made.Select(face => _faces[face])).ToList();
        RemoveFaces(inside);

        _selectedFaces.Clear();
        _selectedVertices.Clear();
        for (var face = 0; face < _faces.Count; face++)
        {
            if (!originals.Contains(_faces[face])) continue;
            _selectedFaces.Add(face);
            _selectedVertices.UnionWith(_faces[face].Corners);
        }

        Compact([]);
        return Did($"mirror {AxisWords(axis)}{(at is { } point ? " at " + V(point) : "")}");
    }

    /// <summary>Sweeps the selected edges round an axis in steps, a face between each step and the next for each edge.</summary>
    /// <param name="axis">The axis, any length but zero, the turn counter-clockwise looking back along it.</param>
    /// <param name="degrees">How far round, up to a whole turn either way, which joins the last step to the first.</param>
    /// <param name="steps">How many steps the turn is made in, from one to <see cref="MostSteps"/>.</param>
    /// <param name="at">A point the axis passes through, or nothing for the origin.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// <para>
    /// The selected edges are a profile, and each is turned round the axis a step at a time, a four
    /// sided face joining it to where the step put it, so a profile of a vase's side drawn beside the
    /// Y axis and spun a whole turn is the vase's surface. A vertex on the axis stays where it is and
    /// its faces have three corners. A whole turn joins the last step back to the profile itself.
    /// </para>
    /// <para>
    /// Each face runs along its edge the other way from the face the edge came from, so that face
    /// is the sweep's start as a solid's end is, and the faces swept are selected after, in its
    /// slot. A profile's face facing the way its edges turn makes a surface facing in, which
    /// <see cref="Flip"/> turns to face out.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">No edge is selected.</exception>
    public EditMesh Spin(Vec3 axis, float degrees, int steps, Vec3? at = null)
    {
        Direction(axis, nameof(axis));
        if (!float.IsFinite(degrees) || degrees == 0f || MathF.Abs(degrees) > 360f) throw new ArgumentOutOfRangeException(nameof(degrees), degrees, "A spin turns by a finite angle, not zero and up to a whole turn either way.");
        if (steps < 1 || steps > MostSteps) throw new ArgumentOutOfRangeException(nameof(steps), steps, $"A spin takes from one to {MostSteps} steps.");

        // Each selected edge as a face runs along it, a selected face's way where one does.
        var directed = Directed();
        var profile = new List<(int From, int To, int Face)>();
        foreach (var (low, high) in EdgeUses().Keys.Order())
        {
            if (!_selectedVertices.Contains(low) || !_selectedVertices.Contains(high)) continue;

            var forward = directed.GetValueOrDefault((low, high), -1);
            var backward = directed.GetValueOrDefault((high, low), -1);
            profile.Add(forward >= 0 && (backward < 0 || _selectedFaces.Contains(forward) || !_selectedFaces.Contains(backward))
                ? (low, high, forward)
                : (high, low, backward));
        }

        if (profile.Count == 0) throw new InvalidOperationException("A spin sweeps the selected edges round, and no edge is selected.");

        var whole = MathF.Abs(degrees) == 360f;
        var center = at ?? Vec3.Zero;
        var along = axis.Normalized;
        var rings = whole ? steps : steps + 1;

        // Each profile vertex at each step, the vertex itself at the first and on the axis.
        var at0 = profile.SelectMany(edge => (int[])[edge.From, edge.To]).Distinct().ToArray();
        var turned = new Dictionary<(int Vertex, int Step), int>();
        foreach (var vertex in at0)
        {
            var offset = _positions[vertex] - center;
            var onAxis = (offset - along * Vec3.Dot(offset, along)).Length <= OnPlane;
            for (var step = 0; step < rings; step++)
            {
                turned[(vertex, step)] = step == 0 || onAxis
                    ? vertex
                    : AddVertex(center + Quat.FromAxisAngle(along, degrees * step / steps * MathF.PI / 180f) * offset);
            }
        }

        _selectedFaces.Clear();
        _selectedVertices.Clear();
        foreach (var (from, to, face) in profile)
        {
            var source = _faces[face];
            for (var step = 0; step < steps; step++)
            {
                var next = (step + 1) % rings;
                int[] corners = [turned[(to, step)], turned[(from, step)], turned[(from, next)], turned[(to, next)]];
                var distinct = corners.Where((corner, i) => corner != corners[(i + 1) % 4]).ToArray();
                if (distinct.Length < 3) continue;

                var made = AddFace(distinct, source.Slot, source.Smooth);
                _selectedFaces.Add(made);
                _selectedVertices.UnionWith(distinct);
            }
        }

        Changed();
        return Did($"spin {AxisWords(axis)} {F(degrees)} {steps}{(at is { } point ? " at " + V(point) : "")}");
    }

    /// <summary>Moves each selected vertex toward the mean of its neighbors along the edges, by a factor, as many times as asked.</summary>
    /// <param name="factor">How far toward the mean, above zero and up to one, the whole way.</param>
    /// <param name="repeat">How many times, from one to a hundred, each from where the last left the vertices.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// Every vertex moves from where all of them were before the pass, so the result does not
    /// depend on their order. The counts stay as they are, and a box's corners smoothed by a half
    /// move a sixth of its side in along each axis, toward the middle of their three neighbors.
    /// </remarks>
    public EditMesh Smooth(float factor, int repeat = 1)
    {
        if (!(factor > 0f && factor <= 1f)) throw new ArgumentOutOfRangeException(nameof(factor), factor, "A smoothing's factor lies above zero and up to one.");
        if (repeat < 1 || repeat > 100) throw new ArgumentOutOfRangeException(nameof(repeat), repeat, "A smoothing repeats from one to a hundred times.");

        var neighbors = new Dictionary<int, HashSet<int>>();
        void Join(int vertex, int other)
        {
            if (!_selectedVertices.Contains(vertex)) return;
            if (!neighbors.TryGetValue(vertex, out var set)) neighbors[vertex] = set = [];
            set.Add(other);
        }

        foreach (var (low, high) in EdgeUses().Keys)
        {
            Join(low, high);
            Join(high, low);
        }

        for (var pass = 0; pass < repeat; pass++)
        {
            var moved = neighbors.ToDictionary(entry => entry.Key, entry =>
            {
                var mean = Vec3.Zero;
                foreach (var other in entry.Value) mean += _positions[other];
                mean *= 1f / entry.Value.Count;
                var point = _positions[entry.Key];
                return point + (mean - point) * factor;
            });

            foreach (var (vertex, point) in moved) _positions[vertex] = point;
        }

        Changed();
        return Did(repeat == 1 ? $"smooth {F(factor)}" : $"smooth {F(factor)} {repeat}");
    }

    /// <summary>Whether the outline passes from one side to the other at a corner on the plane, the corners either side of it on the plane skipped.</summary>
    private static bool Crosses(int[] sides, int at)
    {
        var n = sides.Length;
        int Seen(int step)
        {
            for (var k = 1; k < n; k++)
            {
                var side = sides[((at + step * k) % n + n) % n];
                if (side != 0) return side;
            }

            return 0;
        }

        // A run of corners on the plane counts once, at its first corner.
        if (sides[(at + n - 1) % n] == 0) return false;
        return Seen(-1) * Seen(1) < 0;
    }

    private static void Direction(Vec3 axis, string name)
    {
        if (axis == Vec3.Zero || !float.IsFinite(axis.X + axis.Y + axis.Z)) throw new ArgumentException("A direction is finite and not zero.", name);
    }
}
