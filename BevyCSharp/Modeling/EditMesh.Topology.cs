namespace Bevy;

// The operations that change which vertices and faces there are, rather than where they are.
public sealed partial class EditMesh
{
    /// <summary>The angle a face is smoothed across when <see cref="Shade"/> names none, thirty degrees, as Blender's is.</summary>
    public const float DefaultSmoothAngle = 30f;

    // How many recipes a join is reading inside one another, so a model that joins itself is
    // refused rather than read until the stack runs out.
    [ThreadStatic]
    private static int _joinDepth;

    /// <summary>Pushes the selected faces out along their normals, the sides between them and what they left filled.</summary>
    /// <param name="distance">How far each face goes along its normal, in meters, inward where it is less than zero.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// <para>
    /// The faces selected move together as one region, as a modeling program's extrude moves them,
    /// so two faces side by side stay joined and a side is filled along the region's edge alone.
    /// A corner where faces of the region meet at an angle moves so each of them goes the whole
    /// distance along its own normal, so the top and a side of a box pushed out by a meter stand a
    /// meter out each, rather than less along a corner's mean.
    /// </para>
    /// <para>
    /// The region's faces are selected after, moved, so another extrude pushes them further. A face
    /// whose edge was open, as a plane's are, takes a side there too, so a plane pushed up is a box
    /// open underneath. Four vertices more than a box's top had, and as many sides, make a box whose
    /// top was pushed up a box of twelve vertices and ten faces.
    /// </para>
    /// </remarks>
    public EditMesh Extrude(float distance)
    {
        if (!float.IsFinite(distance)) throw new ArgumentOutOfRangeException(nameof(distance), distance, "An extrude's distance is a finite number.");

        ExtrudeRegion(normals => Offset(normals, distance));
        return Did($"extrude {F(distance)}");
    }

    /// <summary>Pushes the selected faces out by one vector, the sides between them and what they left filled.</summary>
    /// <param name="by">How far, in meters along each axis, the same for every face.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>As <see cref="Extrude(float)"/>, with every corner of the region moved by the same vector, as a wall is pushed up from a floor's edge.</remarks>
    public EditMesh Extrude(Vec3 by)
    {
        ExtrudeRegion(_ => by);
        return Did($"extrude {V(by)}");
    }

    /// <summary>Makes each selected face a smaller face inside it, with a band of faces round it.</summary>
    /// <param name="distance">How far in from its edges the smaller face's edges are, in meters, above zero.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// Each face on its own, its corners moved in along the line halfway between their two edges so
    /// each edge moves in by the distance, the smaller faces selected after, so an extrude after an
    /// inset pulls out a panel or pushes in a window's recess. A distance as wide as the face turns
    /// the smaller face inside out, which the operation leaves as asked.
    /// </remarks>
    public EditMesh Inset(float distance)
    {
        if (!(distance > 0f) || !float.IsFinite(distance)) throw new ArgumentOutOfRangeException(nameof(distance), distance, "An inset's distance is a finite number above zero.");

        var picked = _selectedFaces.Order().ToArray();
        _selectedVertices.Clear();

        foreach (var index in picked)
        {
            var face = _faces[index];
            var corners = face.Corners;
            var normal = Newell(corners).Normalized;
            var inner = new int[corners.Length];

            for (var i = 0; i < corners.Length; i++)
            {
                var before = _positions[corners[(i + corners.Length - 1) % corners.Length]];
                var at = _positions[corners[i]];
                var after = _positions[corners[(i + 1) % corners.Length]];

                // Inward of an edge running counter-clockwise about the normal is the normal crossed
                // with it, and the corner moves so it is the distance in from both its edges.
                var inFromBefore = Vec3.Cross(normal, at - before).Normalized;
                var inFromAfter = Vec3.Cross(normal, after - at).Normalized;
                var meet = 1f + Vec3.Dot(inFromBefore, inFromAfter);
                var offset = meet > 1e-4f ? (inFromBefore + inFromAfter) * (distance / meet) : inFromBefore * distance;
                inner[i] = AddVertex(at + offset);
            }

            for (var i = 0; i < corners.Length; i++)
            {
                var next = (i + 1) % corners.Length;
                AddFace([corners[i], corners[next], inner[next], inner[i]], face.Slot, face.Smooth);
            }

            // The smaller face keeps the coordinates the face had where its corners moved to.
            var fit = FitOf(face);
            face.Corners = inner;
            if (fit is { } map) face.Uvs = inner.Select(corner => map.At(_positions[corner])).ToArray();
            _selectedVertices.UnionWith(inner);
        }

        Changed();
        return Did($"inset {F(distance)}");
    }

    /// <summary>Takes away the selected faces, or where none is selected the selected vertices with every face using them.</summary>
    /// <param name="fill">Whether each hole that leaves is closed with one face, as a cap.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// A vertex no face uses after is taken away too. A hole filled takes a face running round it
    /// the way the faces taken away did, so it faces the way they faced, and the faces filling are
    /// what is selected after, where nothing is otherwise.
    /// </remarks>
    public EditMesh Delete(bool fill = false)
    {
        var going = _selectedFaces.Count > 0
            ? _selectedFaces.ToHashSet()
            : Enumerable.Range(0, _faces.Count).Where(face => _faces[face].Corners.Any(_selectedVertices.Contains)).ToHashSet();
        var vertices = _selectedFaces.Count > 0 ? [] : _selectedVertices.ToHashSet();

        // The edges of the faces taken away that a face staying runs along the other way, which
        // are the rims of the holes, followed round into loops.
        var caps = new List<(int[] Corners, int Slot)>();
        if (fill)
        {
            var kept = new HashSet<(int, int)>();
            for (var face = 0; face < _faces.Count; face++)
            {
                if (going.Contains(face)) continue;
                var corners = _faces[face].Corners;
                for (var i = 0; i < corners.Length; i++) kept.Add((corners[i], corners[(i + 1) % corners.Length]));
            }

            var rim = new Dictionary<int, (int To, int Slot)>();
            foreach (var face in going.Order())
            {
                var corners = _faces[face].Corners;
                for (var i = 0; i < corners.Length; i++)
                {
                    var (from, to) = (corners[i], corners[(i + 1) % corners.Length]);
                    if (kept.Contains((to, from))) rim.TryAdd(from, (to, _faces[face].Slot));
                }
            }

            while (rim.Count > 0)
            {
                var start = rim.Keys.Min();
                var loop = new List<int> { start };
                var slot = rim[start].Slot;
                var at = start;
                while (rim.Remove(at, out var step) && step.To != start)
                {
                    loop.Add(step.To);
                    at = step.To;
                }

                if (loop.Count >= 3) caps.Add((loop.ToArray(), slot));
            }
        }

        RemoveFaces(going);
        _selectedVertices.Clear();
        _selectedFaces.Clear();

        foreach (var (corners, slot) in caps) _selectedFaces.Add(AddFace(corners, slot));
        foreach (var face in _selectedFaces) _selectedVertices.UnionWith(_faces[face].Corners);

        Compact(vertices);
        return Did(fill ? "delete fill" : "delete");
    }

    /// <summary>Joins the selected vertices nearer one another than a distance into one, where the first of them is.</summary>
    /// <param name="distance">How near, in meters, zero joining only those in one place.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// A face left with a corner twice in a row loses the second, and one left with fewer than three
    /// corners is taken away, as a side between two faces moved onto each other is. What is joined
    /// is the vertex of the lowest index among those near it, so a recipe joins the same way each
    /// time it is run.
    /// </remarks>
    public EditMesh Merge(float distance)
    {
        if (!(distance >= 0f) || !float.IsFinite(distance)) throw new ArgumentOutOfRangeException(nameof(distance), distance, "A merge's distance is a finite number, zero or more.");

        // Each vertex goes to the first kept one within the distance, found among those in its own
        // cell of a grid as wide as the distance and the cells beside it.
        var cell = Math.Max(distance, 1e-6f);
        var cells = new Dictionary<(long, long, long), List<int>>();
        var into = new Dictionary<int, int>();

        foreach (var vertex in SelectedVertices())
        {
            var point = _positions[vertex];
            var (cx, cy, cz) = ((long)MathF.Floor(point.X / cell), (long)MathF.Floor(point.Y / cell), (long)MathF.Floor(point.Z / cell));
            var found = -1;

            for (var dx = -1; dx <= 1 && found < 0; dx++)
            for (var dy = -1; dy <= 1 && found < 0; dy++)
            for (var dz = -1; dz <= 1 && found < 0; dz++)
            {
                if (!cells.TryGetValue((cx + dx, cy + dy, cz + dz), out var near)) continue;
                foreach (var kept in near)
                {
                    if ((_positions[kept] - point).Length > distance) continue;
                    found = kept;
                    break;
                }
            }

            if (found >= 0)
            {
                into[vertex] = found;
                continue;
            }

            if (!cells.TryGetValue((cx, cy, cz), out var here)) cells[(cx, cy, cz)] = here = [];
            here.Add(vertex);
        }

        if (into.Count > 0)
        {
            var gone = new HashSet<int>();
            for (var index = 0; index < _faces.Count; index++)
            {
                var face = _faces[index];
                var corners = face.Corners.Select(corner => into.GetValueOrDefault(corner, corner)).ToList();
                var uvs = face.Uvs?.ToList();

                // A corner the same as the one before it, round to the first, said once, its
                // coordinates going with it.
                for (var i = corners.Count - 1; i >= 0 && corners.Count > 0; i--)
                {
                    if (corners[i] != corners[(i + 1) % corners.Count] || corners.Count <= 1) continue;
                    corners.RemoveAt(i);
                    uvs?.RemoveAt(i);
                }

                if (corners.Count < 3) gone.Add(index);
                else (face.Corners, face.Uvs) = ([.. corners], uvs is null ? null : [.. uvs]);
            }

            _selectedVertices.ExceptWith(into.Keys);
            RemoveFaces(gone);
        }

        Compact([]);
        return Did($"merge {F(distance)}");
    }

    /// <summary>Turns the selected faces to face the other way, their corners run backwards.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh Flip()
    {
        foreach (var face in _selectedFaces) _faces[face].Reverse();

        Changed();
        return Did("flip");
    }

    /// <summary>Cuts each selected face of more than three corners into triangles, as the drawing cuts it.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// By ear clipping, so a concave face is cut inside its outline, the first triangle kept at the
    /// face's index and the rest after the last face, each in the face's slot. The triangles are
    /// what is selected after.
    /// </remarks>
    public EditMesh Triangulate()
    {
        foreach (var index in _selectedFaces.Order().ToArray())
        {
            var face = _faces[index];
            if (face.Corners.Length == 3) continue;

            var (corners, uvs) = (face.Corners, face.Uvs);
            var triangles = EarClipping.Cut(corners.Select(corner => _positions[corner]).ToArray(), Newell(corners));
            face.Corners = [corners[triangles[0]], corners[triangles[1]], corners[triangles[2]]];
            face.Uvs = uvs is null ? null : [uvs[triangles[0]], uvs[triangles[1]], uvs[triangles[2]]];

            for (var t = 3; t < triangles.Length; t += 3)
            {
                Vec2[]? own = uvs is null ? null : [uvs[triangles[t]], uvs[triangles[t + 1]], uvs[triangles[t + 2]]];
                _selectedFaces.Add(AddFace([corners[triangles[t]], corners[triangles[t + 1]], corners[triangles[t + 2]]], face.Slot, face.Smooth, own));
            }
        }

        Changed();
        return Did("triangulate");
    }

    /// <summary>Copies the selected faces where they are, with vertices of their own, and selects the copy.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>The copy shares no vertex with what it was copied from, so moving it after leaves the first where it was.</remarks>
    public EditMesh Duplicate()
    {
        var copies = new Dictionary<int, int>();
        var made = new List<int>();

        foreach (var index in _selectedFaces.Order().ToArray())
        {
            var face = _faces[index];
            var corners = face.Corners.Select(corner =>
            {
                if (!copies.TryGetValue(corner, out var copy)) copies[corner] = copy = AddVertex(_positions[corner]);
                return copy;
            }).ToArray();
            made.Add(AddFace(corners, face.Slot, face.Smooth, face.Uvs is { } uvs ? [.. uvs] : null));
        }

        _selectedFaces.Clear();
        _selectedFaces.UnionWith(made);
        _selectedVertices.Clear();
        _selectedVertices.UnionWith(copies.Values);

        Changed();
        return Did("duplicate");
    }

    /// <summary>Adds another model's mesh, made from its recipe's file, to this one, and selects it.</summary>
    /// <param name="path">The model's file, relative to the asset root unless it is a full path.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// The other model's faces keep their slots by name, a slot this mesh has none of being made, so
    /// a pillar joined into a hall is drawn with the hall's material for the pillar's slot. The
    /// recipe keeps the file's path rather than what it held, so a change to the pillar's model
    /// reaches every model that joins it the next time each is made. The other model is taken as it
    /// is built, its modifiers applied.
    /// </remarks>
    /// <exception cref="FileNotFoundException">There is no such model.</exception>
    /// <exception cref="InvalidOperationException">The model joins itself, through others or not.</exception>
    public EditMesh Join(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path.Any(char.IsWhiteSpace)) throw new ArgumentException("A joined model's path is one word, with no space in it.", nameof(path));

        var other = Nested(path).Evaluated();
        var offset = _positions.Count;
        _positions.AddRange(other._positions);

        var made = new List<int>();
        foreach (var face in other._faces)
        {
            var slot = SlotIndex(other._slots[face.Slot], other._looks[face.Slot]);
            made.Add(AddFace(face.Corners.Select(corner => corner + offset).ToArray(), slot, face.Smooth, face.Uvs is { } uvs ? [.. uvs] : null));
        }

        _selectedFaces.Clear();
        _selectedFaces.UnionWith(made);
        _selectedVertices.Clear();
        _selectedVertices.UnionWith(Enumerable.Range(offset, other._positions.Count));

        Changed();
        return Did($"join {path}");
    }

    /// <summary>Draws the selected faces smoothed into one another, or flat.</summary>
    /// <param name="smooth">Whether they are smoothed.</param>
    /// <param name="degrees">The angle between two faces up to which their shared corners take one normal, so a cylinder's sides are round and its ends stay flat.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh Shade(bool smooth, float degrees = DefaultSmoothAngle)
    {
        if (smooth && !(degrees > 0f && degrees <= 180f)) throw new ArgumentOutOfRangeException(nameof(degrees), degrees, "A smoothing angle is above zero and up to 180 degrees.");

        foreach (var face in _selectedFaces) _faces[face].Smooth = smooth ? degrees : 0f;

        Changed();
        return Did(smooth ? $"shade smooth {F(degrees)}" : "shade flat");
    }

    /// <summary>
    /// Pushes out the selected faces as a region, each new corner moved by what <paramref name="offset"/>
    /// answers for the normals of the region's faces round it.
    /// </summary>
    private void ExtrudeRegion(Func<List<Vec3>, Vec3> offset)
    {
        var region = _selectedFaces.Order().ToArray();
        if (region.Length == 0) return;

        var inside = new HashSet<(int, int)>();
        foreach (var face in region)
        {
            var corners = _faces[face].Corners;
            for (var i = 0; i < corners.Length; i++) inside.Add((corners[i], corners[(i + 1) % corners.Length]));
        }

        // The region's rim, its edges no other face of it runs along the other way, in the order
        // the faces have them, and the normals of the region's faces round each of its corners.
        var rim = new List<(int From, int To, int Slot, float Smooth)>();
        var normals = new Dictionary<int, List<Vec3>>();
        foreach (var face in region)
        {
            var corners = _faces[face].Corners;
            var normal = Newell(corners).Normalized;
            for (var i = 0; i < corners.Length; i++)
            {
                var (from, to) = (corners[i], corners[(i + 1) % corners.Length]);
                if (!inside.Contains((to, from))) rim.Add((from, to, _faces[face].Slot, _faces[face].Smooth));

                if (!normals.TryGetValue(from, out var around)) normals[from] = around = [];
                if (!around.Any(known => (known - normal).LengthSquared < 1e-8f)) around.Add(normal);
            }
        }

        // A corner on the rim is left where it is for the faces outside the region, and the region
        // takes a copy of it, moved, while a corner inside the region moves itself.
        var copies = new Dictionary<int, int>();
        foreach (var (from, to, _, _) in rim)
        {
            foreach (var corner in (ReadOnlySpan<int>)[from, to])
            {
                if (!copies.ContainsKey(corner)) copies[corner] = AddVertex(_positions[corner]);
            }
        }

        foreach (var (corner, around) in normals)
        {
            var moved = copies.GetValueOrDefault(corner, corner);
            _positions[moved] += offset(around);
        }

        foreach (var face in region)
        {
            var corners = _faces[face].Corners;
            for (var i = 0; i < corners.Length; i++) corners[i] = copies.GetValueOrDefault(corners[i], corners[i]);
        }

        foreach (var (from, to, slot, smooth) in rim) AddFace([from, to, copies[to], copies[from]], slot, smooth);

        _selectedVertices.Clear();
        foreach (var face in region) _selectedVertices.UnionWith(_faces[face].Corners);
        Changed();
    }

    /// <summary>
    /// The move that puts a corner a distance out along each of the normals of the faces round it,
    /// the nearest to it in least squares where they cannot all be met, as at the tip of a pyramid.
    /// </summary>
    private static Vec3 Offset(List<Vec3> normals, float distance)
    {
        if (normals.Count == 1) return normals[0] * distance;

        if (normals.Count == 2)
        {
            var meet = 1f + Vec3.Dot(normals[0], normals[1]);
            return meet > 1e-4f ? (normals[0] + normals[1]) * (distance / meet) : normals[0] * distance;
        }

        // The sum of each normal's outer product with itself, and of the normals, the normal
        // equations of v · n = d for every n, solved by Cramer's rule.
        double xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0, bx = 0, by = 0, bz = 0;
        foreach (var n in normals)
        {
            xx += n.X * n.X; xy += n.X * n.Y; xz += n.X * n.Z;
            yy += n.Y * n.Y; yz += n.Y * n.Z; zz += n.Z * n.Z;
            bx += n.X * distance; by += n.Y * distance; bz += n.Z * distance;
        }

        var det = xx * (yy * zz - yz * yz) - xy * (xy * zz - yz * xz) + xz * (xy * yz - yy * xz);
        if (Math.Abs(det) < 1e-9)
        {
            var mean = Vec3.Zero;
            foreach (var n in normals) mean += n;
            return mean.Normalized * distance;
        }

        var x = (bx * (yy * zz - yz * yz) - xy * (by * zz - yz * bz) + xz * (by * yz - yy * bz)) / det;
        var y = (xx * (by * zz - yz * bz) - bx * (xy * zz - yz * xz) + xz * (xy * bz - by * xz)) / det;
        var z = (xx * (yy * bz - by * yz) - xy * (xy * bz - by * xz) + bx * (xy * yz - yy * xz)) / det;
        return new Vec3((float)x, (float)y, (float)z);
    }

    /// <summary>Takes faces away, the selection's indices moved to where the faces after them went.</summary>
    private void RemoveFaces(HashSet<int> going)
    {
        if (going.Count == 0) return;

        var moved = new Dictionary<int, int>();
        var kept = new List<Face>(_faces.Count - going.Count);
        for (var face = 0; face < _faces.Count; face++)
        {
            if (going.Contains(face)) continue;
            moved[face] = kept.Count;
            kept.Add(_faces[face]);
        }

        _faces.Clear();
        _faces.AddRange(kept);

        var selected = _selectedFaces.Where(moved.ContainsKey).Select(face => moved[face]).ToArray();
        _selectedFaces.Clear();
        _selectedFaces.UnionWith(selected);
        Changed();
    }

    /// <summary>Takes away the vertices given and every vertex no face uses, and numbers the rest again in order.</summary>
    private void Compact(HashSet<int> going)
    {
        var used = new HashSet<int>();
        foreach (var face in _faces) used.UnionWith(face.Corners);

        var moved = new int[_positions.Count];
        var kept = new List<Vec3>(used.Count);
        for (var vertex = 0; vertex < _positions.Count; vertex++)
        {
            if (!used.Contains(vertex) || going.Contains(vertex))
            {
                moved[vertex] = -1;
                continue;
            }

            moved[vertex] = kept.Count;
            kept.Add(_positions[vertex]);
        }

        _positions.Clear();
        _positions.AddRange(kept);
        foreach (var face in _faces)
        {
            for (var i = 0; i < face.Corners.Length; i++) face.Corners[i] = moved[face.Corners[i]];
        }

        var selected = _selectedVertices.Where(vertex => moved[vertex] >= 0).Select(vertex => moved[vertex]).ToArray();
        _selectedVertices.Clear();
        _selectedVertices.UnionWith(selected);
        Changed();
    }
}
