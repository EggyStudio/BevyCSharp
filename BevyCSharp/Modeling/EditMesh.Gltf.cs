namespace Bevy;

// The model as a glTF file, written for Blender and any program that reads glTF, and read from one
// into faces again, so a level blocked out in the game is finished elsewhere and a model made
// elsewhere is cut and shaped in the game.
public sealed partial class EditMesh
{
    /// <summary>Writes the mesh, built with its modifiers, and its slots' materials as a glTF 2.0 file.</summary>
    /// <param name="path">The file, relative to the asset root unless it is a full path, ending in <c>.gltf</c>.</param>
    /// <param name="embedded">Whether the buffer is written inside the file, rather than as a <c>.bin</c> of the same name beside it.</param>
    /// <returns>The file's full path.</returns>
    /// <remarks>
    /// <para>
    /// One mesh of a primitive for each slot with faces, of positions, normals, texture coordinates
    /// and indices as the engine draws them, and a material for each slot, its color, metalness,
    /// roughness and emissive color, and its textures named by their paths from the file's folder,
    /// so Blender opens the model with its textures where the files are where the game has them.
    /// The engine's own writer, in managed code, so a game exports with nothing but the library.
    /// </para>
    /// <para>
    /// The recipe is unchanged, and writing the file is no line of it, as saving is not. A slot drawn
    /// with the prototype grid is written as the prototype's color.
    /// </para>
    /// </remarks>
    public string Export(string path, bool embedded = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return GltfWriter.Write(this, path, embedded);
    }

    /// <summary>Adds the meshes of a glTF file, read in the engine's managed reader, to this mesh as faces, and selects them.</summary>
    /// <param name="path">The file, <c>.gltf</c> or <c>.glb</c>, relative to the asset root unless it is a full path.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// <para>
    /// The file's triangles are read in its scene's space, their corners within a hundredth of a
    /// millimeter of one another made one vertex, and each two triangles that share an edge, lie in
    /// one plane, are in one material and meet with the same texture coordinates along the edge
    /// joined into one face, so a box from Blender is six faces of four corners again. A region that
    /// would join into a face with a hole in it stays triangles.
    /// </para>
    /// <para>
    /// Each material becomes a slot of its name, with its color, factors and textures where the
    /// mesh has no slot of the name or has one that gives nothing, a face keeps its texture
    /// coordinates as its own, and a face
    /// whose corners' normals bend away from its own is smoothed at <see cref="DefaultSmoothAngle"/>.
    /// The recipe keeps the file's path, as <c>import models/crate.gltf</c>, so the model is read
    /// again each time the recipe is run.
    /// </para>
    /// </remarks>
    /// <exception cref="FileNotFoundException">There is no such file, or a buffer it names is not there.</exception>
    /// <exception cref="FormatException">It is not glTF the reader reads.</exception>
    public EditMesh Import(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path.Any(char.IsWhiteSpace)) throw new ArgumentException("An imported model's path is one word, with no space in it.", nameof(path));

        var (triangles, materials) = GltfReader.Read(path);
        var made = AddTriangles(triangles, materials);

        _selectedFaces.Clear();
        _selectedVertices.Clear();
        foreach (var face in made)
        {
            _selectedFaces.Add(face);
            _selectedVertices.UnionWith(_faces[face].Corners);
        }

        Changed();
        return Did($"import {path}");
    }

    /// <summary>Adds triangles as faces, welded and joined where they lie in one plane, and answers the faces made.</summary>
    private List<int> AddTriangles(List<GltfReader.Triangle> triangles, List<GltfReader.Material> materials)
    {
        const float Weld = 1e-5f;

        // Each corner welded to a vertex added before within the weld, in cells as wide as it.
        var cells = new Dictionary<(long, long, long), List<int>>();
        var first = _positions.Count;
        int Vertex(Vec3 point)
        {
            var (cx, cy, cz) = ((long)MathF.Floor(point.X / Weld), (long)MathF.Floor(point.Y / Weld), (long)MathF.Floor(point.Z / Weld));
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            for (var dz = -1; dz <= 1; dz++)
            {
                if (!cells.TryGetValue((cx + dx, cy + dy, cz + dz), out var near)) continue;
                foreach (var known in near)
                {
                    if ((_positions[known] - point).Length <= Weld) return known;
                }
            }

            var made = AddVertex(point);
            if (!cells.TryGetValue((cx, cy, cz), out var here)) cells[(cx, cy, cz)] = here = [];
            here.Add(made);
            return made;
        }

        // A slot of the material's name that gives nothing takes the material's look, so a model
        // imported into a new mesh keeps the look its first slot had.
        var slots = new Dictionary<int, int>();
        int Slot(int material)
        {
            if (slots.TryGetValue(material, out var known)) return known;
            if (material < 0 || material >= materials.Count) return slots[material] = 0;

            var (name, look) = materials[material];
            var index = SlotIndex(name, look);
            if (_looks[index].IsPrototype) _looks[index] = look;
            return slots[material] = index;
        }

        // The triangles kept, their corners welded and those with no area left out.
        var kept = new List<(int[] Corners, Vec2[]? Uvs, int Slot, bool Smooth, Vec3 Normal)>();
        foreach (var triangle in triangles)
        {
            GltfReader.Corner[] corners = [triangle.A, triangle.B, triangle.C];
            int[] welded = [Vertex(triangle.A.Position), Vertex(triangle.B.Position), Vertex(triangle.C.Position)];
            if (welded[0] == welded[1] || welded[1] == welded[2] || welded[0] == welded[2]) continue;

            var normal = Newell(welded);
            if (normal.LengthSquared < 1e-20f) continue;
            normal = normal.Normalized;

            // Smoothed where a corner's normal leans from the face's by more than a degree.
            var smooth = corners.Any(corner => corner.Normal is { } given && Vec3.Dot(given, normal) < 0.99985f);
            Vec2[]? uvs = corners.All(corner => corner.Uv is not null) ? [.. corners.Select(corner => corner.Uv!.Value)] : null;
            kept.Add((welded, uvs, Slot(triangle.Material), smooth, normal));
        }

        // Two triangles sharing an edge the other way join where they are in one plane and one slot,
        // smoothed alike, and their texture coordinates agree along the edge and across it.
        var group = Enumerable.Range(0, kept.Count).ToArray();
        int Root(int at) => group[at] == at ? at : group[at] = Root(group[at]);

        var directed = new Dictionary<(int, int), int>();
        for (var t = 0; t < kept.Count; t++)
        {
            for (var i = 0; i < 3; i++) directed.TryAdd((kept[t].Corners[i], kept[t].Corners[(i + 1) % 3]), t);
        }

        for (var t = 0; t < kept.Count; t++)
        {
            var (corners, uvs, slot, smooth, normal) = kept[t];
            for (var i = 0; i < 3; i++)
            {
                if (!directed.TryGetValue((corners[(i + 1) % 3], corners[i]), out var other) || other <= t) continue;

                var beside = kept[other];
                if (beside.Slot != slot || beside.Smooth != smooth || Vec3.Dot(beside.Normal, normal) < 0.99999f) continue;

                var far = beside.Corners.First(corner => corner != corners[i] && corner != corners[(i + 1) % 3]);
                if (MathF.Abs(Vec3.Dot(_positions[far] - _positions[corners[i]], normal)) > 1e-4f) continue;
                if (!SameCoordinates(corners, uvs, beside.Corners, beside.Uvs)) continue;

                group[Root(other)] = Root(t);
            }
        }

        var made = new List<int>();
        foreach (var members in Enumerable.Range(0, kept.Count).GroupBy(Root))
        {
            var (_, _, slot, smooth, _) = kept[members.Key];
            var angle = smooth ? DefaultSmoothAngle : 0f;
            var loop = members.Count() > 1 ? JoinedLoop(members.Select(t => kept[t].Corners)) : null;

            if (loop is null)
            {
                foreach (var t in members) made.Add(AddFace([.. kept[t].Corners], slot, angle, kept[t].Uvs));
                continue;
            }

            // A corner's coordinates are the same in every triangle of the region, as their joining asked.
            Vec2[]? uvs = null;
            if (kept[members.Key].Uvs is not null)
            {
                var at = new Dictionary<int, Vec2>();
                foreach (var t in members)
                {
                    for (var i = 0; i < 3; i++) at.TryAdd(kept[t].Corners[i], kept[t].Uvs![i]);
                }

                uvs = [.. loop.Select(corner => at[corner])];
            }

            made.Add(AddFace([.. loop], slot, angle, uvs));
        }

        // A vertex every one of whose triangles had no area belongs to no face.
        if (_positions.Count > first) Compact([]);
        return made;
    }

    /// <summary>Whether two triangles sharing an edge have the same coordinates at its ends and lie in one mapping across it.</summary>
    private bool SameCoordinates(int[] one, Vec2[]? oneUvs, int[] other, Vec2[]? otherUvs)
    {
        if (oneUvs is null || otherUvs is null) return oneUvs is null && otherUvs is null;

        Vec2 Of(int[] corners, Vec2[] uvs, int vertex) => uvs[Array.IndexOf(corners, vertex)];
        foreach (var vertex in other)
        {
            if (Array.IndexOf(one, vertex) < 0) continue;
            if ((Of(one, oneUvs, vertex) - Of(other, otherUvs, vertex)).LengthSquared > 1e-10f) return false;
        }

        // The far corner of the second where the first's coordinates, carried across, put it.
        var face = new Face(one, 0, 0f, oneUvs);
        if (FitOf(face) is not { } map) return true;
        var far = other.First(vertex => Array.IndexOf(one, vertex) < 0);
        return (map.At(_positions[far]) - Of(other, otherUvs, far)).LengthSquared <= 1e-8f;
    }

    /// <summary>The one loop round a region of triangles, or nothing where its rim is more than one loop or meets itself.</summary>
    private static List<int>? JoinedLoop(IEnumerable<int[]> triangles)
    {
        var inside = new HashSet<(int, int)>();
        foreach (var corners in triangles)
        {
            for (var i = 0; i < 3; i++) inside.Add((corners[i], corners[(i + 1) % 3]));
        }

        var next = new Dictionary<int, int>();
        foreach (var (from, to) in inside)
        {
            if (inside.Contains((to, from))) continue;
            if (!next.TryAdd(from, to)) return null;
        }

        var loops = Loops(next);
        return loops.Count == 1 && loops[0].Count == next.Count ? loops[0] : null;
    }
}
