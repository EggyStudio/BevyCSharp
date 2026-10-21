namespace Bevy;

// The booleans, a solid added, taken away or kept where two overlap, and the openings and rooms made
// with them, each the mesh made again from what the partition (Csg) leaves, joined back into faces
// (FaceMerge).
public sealed partial class EditMesh
{
    /// <summary>How near a point is to a plane to lie in it where the mesh says no other, a hundredth of a millimeter.</summary>
    public const float DefaultEpsilon = 1e-5f;

    /// <summary>How far beyond a wall's faces a window or a door is cut, a centimeter, so its sides lie in no plane of the wall's.</summary>
    public const float OpeningMargin = 0.01f;

    private static readonly HashSet<string> Solids = new(StringComparer.Ordinal) { "box", "cylinder", "cone", "wedge", "stairs", "arch", "room", "ramp" };

    /// <summary>How near a point is to a plane to lie in it, for the booleans, and ten times it how near two points are to be one.</summary>
    public float BooleanEpsilon { get; private set; } = DefaultEpsilon;

    /// <summary>Sets how near a point is to a plane to lie in it, for the booleans.</summary>
    /// <param name="distance">Above zero and up to a centimeter.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh Epsilon(float distance)
    {
        if (!(distance > 0f && distance <= 0.01f)) throw new ArgumentOutOfRangeException(nameof(distance), distance, "A boolean's epsilon is above zero and up to a centimeter.");

        BooleanEpsilon = distance;
        return Did($"epsilon {F(distance)}");
    }

    /// <summary>Adds another model's solid, made from its file and moved by an offset, or a solid primitive, joined into this one where they overlap.</summary>
    /// <param name="path">
    /// The model's file, relative to the asset root unless it is a full path, or a solid
    /// primitive's line, such as <c>stairs 16 0.2 0.3 1.2 at 0.2 0.2 0.6</c>, made where its own
    /// words place it.
    /// </param>
    /// <param name="at">How far the other model is moved before it is joined, nothing for a primitive's line.</param>
    /// <returns>This mesh, its faces from the other model selected.</returns>
    /// <remarks>
    /// The faces where the two overlap are taken away and the rest joined, faces in one plane and one
    /// slot joined into one, so two boxes side by side make one box. Both are closed solids, as a
    /// boolean needs, and what is made is too. A primitive added rather than joined, as a flight of
    /// stairs standing on a floor and against a wall, is a second solid touching the first, whose
    /// edges a later boolean welds to the floor's and the wall's, four faces then running along each
    /// and every one of them counted open, so a level's stairs are joined from its one recipe.
    /// </remarks>
    public EditMesh Union(string path, Vec3 at = default) => Combine(path, at, Csg.Union, "union");

    /// <summary>Takes another model's solid, made from its file and moved by an offset, or a solid primitive, away from this one.</summary>
    /// <param name="path">The model's file, relative to the asset root unless it is a full path, or a solid primitive's line, as <see cref="Cut"/> takes it.</param>
    /// <param name="at">How far the other model is moved before it is taken away, nothing for a primitive's line.</param>
    /// <returns>This mesh, the faces the other model left in it selected.</returns>
    public EditMesh Subtract(string path, Vec3 at = default) => Combine(path, at, Csg.Subtract, "subtract");

    /// <summary>Keeps what this solid and another model's, made from its file and moved by an offset, or a solid primitive, both hold.</summary>
    /// <param name="path">The model's file, relative to the asset root unless it is a full path, or a solid primitive's line.</param>
    /// <param name="at">How far the other model is moved before it is kept to, nothing for a primitive's line.</param>
    /// <returns>This mesh, its faces from the other model selected.</returns>
    public EditMesh Intersect(string path, Vec3 at = default) => Combine(path, at, Csg.Intersect, "intersect");

    /// <summary>Takes a solid primitive, made where its line places it, away from this mesh.</summary>
    /// <param name="primitive">
    /// A solid primitive's line as a recipe has it, such as <c>box 1 1 2 at 1.5 1 -0.5</c> or
    /// <c>cylinder 0.3 3 16 at 2 0 2</c>, a box, cylinder, cone, wedge, stairs, arch, room or ramp.
    /// </param>
    /// <returns>This mesh, the faces the primitive left in it selected.</returns>
    /// <exception cref="ArgumentException">The line is not a solid primitive's.</exception>
    public EditMesh Cut(string primitive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primitive);

        var first = primitive.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
        if (!Solids.Contains(first)) throw new ArgumentException($"A cut is made with a solid primitive, a box, cylinder, cone, wedge, stairs, arch, room or ramp, and {first} is none.", nameof(primitive));

        var cutter = Solid(primitive);
        Apply(cutter, Csg.Subtract);
        return Did($"cut {cutter.Recipe[0]}");
    }

    /// <summary>Cuts a window through the wall whose face was selected first, measured on every selected face in its plane, as deep as the wall.</summary>
    /// <param name="width">How wide the opening is.</param>
    /// <param name="height">How tall it is.</param>
    /// <param name="along">How far its middle is from the face's left edge, seen from in front of the face.</param>
    /// <param name="sill">How far its bottom is above the face's bottom.</param>
    /// <returns>This mesh, the opening's sides selected.</returns>
    /// <remarks>
    /// The opening is a box cut through the wall a centimeter past each of its faces
    /// (<see cref="OpeningMargin"/>), as deep as a ray from the face finds the wall's other side, so
    /// a room's wall is cut through and the wall across the room is not, and no side of the opening
    /// lies in a plane of the wall's, which the booleans would cut as their planes say.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No face is selected, or the face is a floor or a ceiling rather than a wall.</exception>
    public EditMesh Window(float width, float height, float along, float sill)
    {
        Opening(width, height, along, sill, door: false);
        return Did($"cut window {F(width)} {F(height)} {F(along)} {F(sill)}");
    }

    /// <summary>Cuts a door through the wall whose face was selected first, from its bottom, as deep as the wall.</summary>
    /// <param name="width">How wide the opening is.</param>
    /// <param name="height">How tall it is, from the face's bottom.</param>
    /// <param name="along">How far its middle is from the face's left edge, seen from in front of the face.</param>
    /// <returns>This mesh, the opening's sides selected.</returns>
    /// <remarks>As <see cref="Window"/>, the box going a centimeter below the face's bottom, so the door stands on the floor.</remarks>
    /// <exception cref="InvalidOperationException">No face is selected, or the face is a floor or a ceiling rather than a wall.</exception>
    public EditMesh Door(float width, float height, float along)
    {
        Opening(width, height, along, 0f, door: true);
        return Did($"cut door {F(width)} {F(height)} {F(along)}");
    }

    /// <summary>Adds a room, a box with its inside hollowed out by a thickness of wall, floor and ceiling.</summary>
    /// <param name="size">Its outside width along X, height along Y and depth along Z.</param>
    /// <param name="thickness">How thick its walls, floor and ceiling are, less than half of every side.</param>
    /// <param name="at">Where its lowest corner goes, or its middle where <paramref name="centered"/> is set.</param>
    /// <param name="centered">Whether <paramref name="at"/> is its middle rather than its lowest corner.</param>
    /// <returns>This mesh, the room selected.</returns>
    /// <remarks>
    /// Two boxes, the outside facing out and the inside facing in, a closed solid of sixteen vertices
    /// and twelve faces holding the outside's volume less the inside's, which a window and a door are
    /// cut through.
    /// </remarks>
    public EditMesh Room(Vec3 size, float thickness, Vec3 at = default, bool centered = false)
    {
        Positive(size.X, nameof(size));
        Positive(size.Y, nameof(size));
        Positive(size.Z, nameof(size));
        Positive(thickness, nameof(thickness));
        if (2f * thickness >= MathF.Min(size.X, MathF.Min(size.Y, size.Z)))
            throw new ArgumentOutOfRangeException(nameof(thickness), thickness, "A room's walls are less than half as thick as its narrowest side.");

        var (first, firstFace) = AddPrism([new(0f, 0f), new(size.Z, 0f), new(size.Z, size.X), new(0f, size.X)], Axis.Y, 0f, size.Y);
        var (innerFace, t) = (_faces.Count, thickness);
        AddPrism([new(t, t), new(size.Z - t, t), new(size.Z - t, size.X - t), new(t, size.X - t)], Axis.Y, t, size.Y - t);
        for (var face = innerFace; face < _faces.Count; face++) _faces[face].Reverse();

        Placed(first, firstFace, at, centered);
        return Did($"room {V(size)} {F(thickness)}{Placement(at, centered)}");
    }

    /// <summary>Adds a ramp rising along Z, a wedge given in the words of a flight of stairs.</summary>
    /// <param name="rise">How high it rises.</param>
    /// <param name="run">How far along Z it rises over.</param>
    /// <param name="width">How wide it is, along X.</param>
    /// <param name="at">Where its lowest corner goes, or its middle where <paramref name="centered"/> is set.</param>
    /// <param name="centered">Whether <paramref name="at"/> is its middle rather than its lowest corner.</param>
    /// <returns>This mesh, the ramp selected.</returns>
    public EditMesh Ramp(float rise, float run, float width, Vec3 at = default, bool centered = false)
    {
        Positive(rise, nameof(rise));
        Positive(run, nameof(run));
        Positive(width, nameof(width));

        Prism([new(0f, 0f), new(0f, run), new(rise, run)], Axis.X, width, at, centered);
        return Did($"ramp {F(rise)} {F(run)} {F(width)}{Placement(at, centered)}");
    }

    /// <summary>
    /// The nearest face a ray meets, and how far along the ray, among the faces a test allows, or
    /// nothing where it meets none.
    /// </summary>
    /// <param name="origin">Where the ray starts.</param>
    /// <param name="direction">Which way it goes, a length of one.</param>
    /// <param name="allowed">Which faces it can meet, or nothing for every one.</param>
    internal (int Face, float Distance)? Cast(Vec3 origin, Vec3 direction, Func<int, bool>? allowed = null)
    {
        (int Face, float Distance)? nearest = null;
        for (var face = 0; face < _faces.Count; face++)
        {
            if (allowed is not null && !allowed(face)) continue;

            var corners = _faces[face].Corners;
            var points = corners.Select(corner => _positions[corner]).ToArray();
            var triangles = EarClipping.Cut(points, Newell(corners));
            for (var t = 0; t < triangles.Length; t += 3)
            {
                if (Meets(origin, direction, points[triangles[t]], points[triangles[t + 1]], points[triangles[t + 2]]) is { } distance
                    && (nearest is null || distance < nearest.Value.Distance))
                {
                    nearest = (face, distance);
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// Runs a boolean on this mesh and another model's, made from its file and moved, and records it.
    /// </summary>
    private EditMesh Combine(string path, Vec3 at, Func<IEnumerable<Csg.Polygon>, IEnumerable<Csg.Polygon>, double, List<Csg.Polygon>> operation, string word)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        // A solid primitive's line in the path's place, which a path of one word cannot be taken for.
        var words = path.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1 && Solids.Contains(words[0]))
        {
            if (at != Vec3.Zero) throw new ArgumentException("A primitive's line places it with its own at, and a boolean moves only a model read from a file.", nameof(at));

            var solid = Solid(path);
            Apply(solid, operation);
            return Did($"{word} {solid.Recipe[0]}");
        }

        if (path.Any(char.IsWhiteSpace)) throw new ArgumentException("A model's path in a boolean is one word, with no space in it, or a solid primitive's line.", nameof(path));

        var other = Nested(path);
        for (var vertex = 0; vertex < other._positions.Count; vertex++) other._positions[vertex] += at;

        Apply(other, operation);
        return Did($"{word} {path}{(at == Vec3.Zero ? "" : " at " + V(at))}");
    }

    /// <summary>
    /// Makes this mesh again from what a boolean of it and another leaves, the other's faces
    /// selected, its slots added by name.
    /// </summary>
    private void Apply(EditMesh other, Func<IEnumerable<Csg.Polygon>, IEnumerable<Csg.Polygon>, double, List<Csg.Polygon>> operation)
    {
        var epsilon = (double)BooleanEpsilon;
        var pieces = operation(Polygons(this, second: false), Polygons(other, second: true), epsilon);
        var (points, faces) = FaceMerge.Faces(pieces, epsilon * 10);

        _positions.Clear();
        _positions.AddRange(points.Select(point => new Vec3((float)point.X, (float)point.Y, (float)point.Z)));
        _faces.Clear();
        _selectedFaces.Clear();
        _selectedVertices.Clear();

        foreach (var (corners, source) in faces)
        {
            // A piece of a face with coordinates of its own takes them where the face had them.
            var slot = SlotIndex(source.Slot, source.Look);
            var uvs = source.Uvs is { } map ? corners.Select(corner => map.At(_positions[corner])).ToArray() : null;
            var face = AddFace(corners, slot, source.Smooth, uvs);
            if (!source.Second) continue;
            _selectedFaces.Add(face);
            _selectedVertices.UnionWith(corners);
        }

        Compact([]);
    }

    /// <summary>A mesh's faces as convex polygons for the partition, each face cut into triangles, with what it came from.</summary>
    private static IEnumerable<Csg.Polygon> Polygons(EditMesh mesh, bool second)
    {
        foreach (var face in mesh._faces)
        {
            var points = face.Corners.Select(corner => mesh._positions[corner]).ToArray();
            var triangles = EarClipping.Cut(points, mesh.Newell(face.Corners));
            var source = new Csg.Source(mesh._slots[face.Slot], face.Smooth, second, mesh._looks[face.Slot], mesh.FitOf(face));

            for (var t = 0; t < triangles.Length; t += 3)
            {
                var (a, b, c) = (Point(points[triangles[t]]), Point(points[triangles[t + 1]]), Point(points[triangles[t + 2]]));
                if (Csg.Point.Cross(b - a, c - a).Length < 1e-12) continue;
                yield return new Csg.Polygon([a, b, c], source);
            }
        }

        static Csg.Point Point(Vec3 at) => new(at.X, at.Y, at.Z);
    }

    /// <summary>Cuts a box through the wall whose face was selected first, from a height above its bottom.</summary>
    private void Opening(float width, float height, float along, float bottom, bool door)
    {
        Positive(width, nameof(width));
        Positive(height, nameof(height));
        if (!float.IsFinite(along)) throw new ArgumentOutOfRangeException(nameof(along), along, "An opening's place along the wall is a finite number.");
        if (!(bottom >= 0f) || !float.IsFinite(bottom)) throw new ArgumentOutOfRangeException(nameof(bottom), bottom, "A window's sill is zero or more.");
        if (_selectedFaces.Count == 0) throw new InvalidOperationException("A window or a door is cut through the wall whose face is selected first, and no face is selected.");

        var face = _selectedFaces.Min();
        var normal = Normal(face);
        if (MathF.Abs(normal.Y) > 0.7f) throw new InvalidOperationException("A window or a door is cut through a wall, a face facing sideways, and the face selected first is a floor or a ceiling.");

        // The wall's own frame, right along it seen from in front, up the world's up made square to
        // it, and out along its normal, which turns a box's corners into the wall's as they are.
        var right = Vec3.Cross(Vec3.UnitY, normal).Normalized;
        var up = Vec3.Cross(normal, right);
        var plane = Vec3.Dot(normal, _positions[_faces[face].Corners[0]]);

        // The wall is every selected face in the first one's plane, so a wall an earlier opening
        // cut into pieces is measured whole, from its left edge and its bottom.
        var corners = _selectedFaces
            .Where(other => Vec3.Dot(Normal(other), normal) > 0.9999f && MathF.Abs(Vec3.Dot(normal, _positions[_faces[other].Corners[0]]) - plane) < 1e-3f)
            .SelectMany(other => _faces[other].Corners.Select(corner => _positions[corner]))
            .ToArray();
        var left = corners.Min(point => Vec3.Dot(point, right));
        var low = corners.Min(point => Vec3.Dot(point, up));

        var foot = door ? low - OpeningMargin : low + bottom;
        var tall = door ? height + OpeningMargin : height;
        var middle = right * (left + along) + up * (foot + tall / 2f) + normal * plane;

        // As deep as the wall, the distance from the face to the nearest face behind it the ray leaves
        // the wall through, or the whole mesh's depth where it leaves through none.
        var start = middle + normal * OpeningMargin;
        var depth = Cast(start, -normal, other => other != face && Vec3.Dot(Normal(other), -normal) > 0f) is { } far
            ? far.Distance - OpeningMargin
            : (Bounds.High - Bounds.Low).Length;

        var cutter = new EditMesh().Box(new Vec3(width, tall, depth + 2f * OpeningMargin));
        cutter._slots[0] = _slots[0];
        var origin = right * (left + along - width / 2f) + up * foot + normal * (plane - depth - OpeningMargin);
        for (var vertex = 0; vertex < cutter._positions.Count; vertex++)
        {
            var local = cutter._positions[vertex];
            cutter._positions[vertex] = origin + right * local.X + up * local.Y + normal * local.Z;
        }

        Apply(cutter, Csg.Subtract);
    }

    /// <summary>Where a ray meets a triangle from either side, by Möller and Trumbore's test, or nothing.</summary>
    private static float? Meets(Vec3 origin, Vec3 direction, Vec3 a, Vec3 b, Vec3 c)
    {
        var (edge, other) = (b - a, c - a);
        var p = Vec3.Cross(direction, other);
        var det = Vec3.Dot(edge, p);
        if (MathF.Abs(det) < 1e-9f) return null;

        var inverse = 1f / det;
        var s = origin - a;
        var u = Vec3.Dot(s, p) * inverse;
        if (u < 0f || u > 1f) return null;

        var q = Vec3.Cross(s, edge);
        var v = Vec3.Dot(direction, q) * inverse;
        if (v < 0f || u + v > 1f) return null;

        var t = Vec3.Dot(other, q) * inverse;
        return t > 0f ? t : null;
    }

    /// <summary>
    /// A solid primitive made from its line for a boolean, its faces in this mesh's first slot, so a
    /// mesh whose first slot was renamed takes no slot called <see cref="DefaultSlot"/> from it.
    /// </summary>
    private EditMesh Solid(string line)
    {
        var solid = new EditMesh().Run(line);
        solid._slots[0] = _slots[0];
        return solid;
    }

    /// <summary>A model made from its file inside a join or a boolean, refused where models are read inside one another sixteen deep.</summary>
    private static EditMesh Nested(string path)
    {
        if (_joinDepth >= 16) throw new InvalidOperationException($"{path} reads models inside one another sixteen deep, which is a model reading itself.");

        _joinDepth++;
        try
        {
            return Load(path);
        }
        finally
        {
            _joinDepth--;
        }
    }
}
