namespace Bevy;

// The selection, which the operations act on. A face picked brings its corners with it, so the
// vertices selected are always at least those of the faces selected, and an operation on vertices
// moves the faces an operation on faces would act on.
public sealed partial class EditMesh
{
    /// <summary>How many vertices are selected, the corners of the faces selected among them.</summary>
    public int SelectedVertexCount => _selectedVertices.Count;

    /// <summary>How many faces are selected.</summary>
    public int SelectedFaceCount => _selectedFaces.Count;

    /// <summary>How many edges are selected, which is those whose two ends are.</summary>
    public int SelectedEdgeCount => EdgeUses().Keys.Count(edge => _selectedVertices.Contains(edge.Low) && _selectedVertices.Contains(edge.High));

    /// <summary>Whether a vertex is selected.</summary>
    /// <param name="vertex">Its index, from zero to <see cref="VertexCount"/>.</param>
    public bool IsVertexSelected(int vertex) => _selectedVertices.Contains(vertex);

    /// <summary>Whether a face is selected.</summary>
    /// <param name="face">Its index, from zero to <see cref="FaceCount"/>.</param>
    public bool IsFaceSelected(int face) => _selectedFaces.Contains(face);

    /// <summary>Selects every vertex and every face.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SelectAll()
    {
        _selectedVertices.UnionWith(Enumerable.Range(0, _positions.Count));
        _selectedFaces.UnionWith(Enumerable.Range(0, _faces.Count));
        return Did("select all");
    }

    /// <summary>Selects nothing.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SelectNone()
    {
        _selectedVertices.Clear();
        _selectedFaces.Clear();
        return Did("select none");
    }

    /// <summary>Selects what was not selected, and leaves what was.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// The faces that were not selected are selected with their corners, and the vertices that
    /// were not selected are selected too, so a box with its top selected has its other five faces
    /// selected after, and with them all eight corners, since each of the top's corners is a
    /// corner of a side as well.
    /// </remarks>
    public EditMesh SelectInvert()
    {
        var faces = Enumerable.Range(0, _faces.Count).Where(face => !_selectedFaces.Contains(face)).ToArray();
        var vertices = Enumerable.Range(0, _positions.Count).Where(vertex => !_selectedVertices.Contains(vertex)).ToArray();

        _selectedFaces.Clear();
        _selectedVertices.Clear();
        _selectedVertices.UnionWith(vertices);
        PickFaces(faces, SelectionMode.Add);
        return Did("select invert");
    }

    /// <summary>Selects faces by their indices.</summary>
    /// <param name="faces">The faces, each from zero to <see cref="FaceCount"/>.</param>
    /// <param name="mode">Whether they are all that is selected, are added, or are taken away.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SelectFaces(IEnumerable<int> faces, SelectionMode mode = SelectionMode.Replace)
    {
        ArgumentNullException.ThrowIfNull(faces);
        var picked = faces.ToArray();

        foreach (var face in picked) ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)face, (uint)_faces.Count, nameof(faces));

        PickFaces(picked, mode);
        return Did($"select {Mode(mode)}face {string.Join(' ', picked)}");
    }

    /// <summary>Selects the faces that face nearest one way along an axis, such as up.</summary>
    /// <param name="direction">
    /// The way, taken as the direction along an axis nearest it, so <see cref="Vec3.UnitY"/> picks
    /// the faces facing more up than any other way along an axis.
    /// </param>
    /// <param name="mode">Whether they are all that is selected, are added, or are taken away.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// A face is picked by which of the six directions along the axes its normal is nearest, so
    /// every face is the floor, the ceiling or one of four walls, and a slope is the floor where it
    /// rises less steeply than it runs and a wall where it rises more. A slope that rises exactly
    /// as far as it runs, at forty-five degrees, is taken as a floor or a ceiling, where a character
    /// walks it, and a face between two walls at forty-five degrees as the wall along X.
    /// </remarks>
    public EditMesh SelectFacing(Vec3 direction, SelectionMode mode = SelectionMode.Replace)
    {
        if (direction == Vec3.Zero) throw new ArgumentException("A direction to select the faces facing it is not zero.", nameof(direction));

        var way = Nearest(direction);
        PickFaces(Enumerable.Range(0, _faces.Count).Where(face => Newell(_faces[face].Corners) is var normal && normal != Vec3.Zero && Nearest(normal) == way).ToArray(), mode);
        return Did($"select {Mode(mode)}{Way(way)}");
    }

    /// <summary>Selects the vertices inside a box, the faces all of whose corners are inside it, and the edges between.</summary>
    /// <param name="corner">One corner of the box.</param>
    /// <param name="opposite">The corner across from it.</param>
    /// <param name="mode">Whether they are all that is selected, are added, or are taken away.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// A point on the box's side is inside it, by a hundredth of a millimeter, so a box drawn along
    /// a grid line picks the vertices on it, which a measure that came out a little short after a
    /// rotation would otherwise miss. Taking away removes the vertices inside and every face with
    /// a corner among them, since a face is selected only with all its corners.
    /// </remarks>
    public EditMesh SelectInside(Vec3 corner, Vec3 opposite, SelectionMode mode = SelectionMode.Replace)
    {
        const float Margin = 1e-5f;
        var low = new Vec3(MathF.Min(corner.X, opposite.X), MathF.Min(corner.Y, opposite.Y), MathF.Min(corner.Z, opposite.Z)) - new Vec3(Margin);
        var high = new Vec3(MathF.Max(corner.X, opposite.X), MathF.Max(corner.Y, opposite.Y), MathF.Max(corner.Z, opposite.Z)) + new Vec3(Margin);

        var inside = Enumerable.Range(0, _positions.Count).Where(vertex =>
        {
            var point = _positions[vertex];
            return point.X >= low.X && point.Y >= low.Y && point.Z >= low.Z
                && point.X <= high.X && point.Y <= high.Y && point.Z <= high.Z;
        }).ToHashSet();

        var faces = Enumerable.Range(0, _faces.Count).Where(face => _faces[face].Corners.All(inside.Contains));

        switch (mode)
        {
            case SelectionMode.Replace:
                _selectedVertices.Clear();
                _selectedFaces.Clear();
                _selectedVertices.UnionWith(inside);
                _selectedFaces.UnionWith(faces);
                break;
            case SelectionMode.Add:
                _selectedVertices.UnionWith(inside);
                _selectedFaces.UnionWith(faces);
                break;
            default:
                _selectedVertices.ExceptWith(inside);
                _selectedFaces.RemoveWhere(face => _faces[face].Corners.Any(inside.Contains));
                break;
        }

        return Did($"select {Mode(mode)}box {V(corner)} {V(opposite)}");
    }

    /// <summary>Selects the faces in a material slot.</summary>
    /// <param name="slot">The slot's name, one of <see cref="Slots"/>.</param>
    /// <param name="mode">Whether they are all that is selected, are added, or are taken away.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>A name no slot has selects no face, as a slot with no faces left does.</remarks>
    public EditMesh SelectSlot(string slot, SelectionMode mode = SelectionMode.Replace)
    {
        ArgumentException.ThrowIfNullOrEmpty(slot);

        var index = _slots.IndexOf(slot);
        PickFaces(Enumerable.Range(0, _faces.Count).Where(face => _faces[face].Slot == index).ToArray(), mode);
        return Did($"select {Mode(mode)}slot {slot}");
    }

    /// <summary>
    /// Selects faces with their corners, adds them, or takes them away with the corners no face
    /// left selected has, and leaves alone a vertex selected alone that is no corner of theirs.
    /// </summary>
    private void PickFaces(int[] faces, SelectionMode mode)
    {
        switch (mode)
        {
            case SelectionMode.Replace:
                _selectedVertices.Clear();
                _selectedFaces.Clear();
                goto case SelectionMode.Add;

            case SelectionMode.Add:
                foreach (var face in faces)
                {
                    _selectedFaces.Add(face);
                    _selectedVertices.UnionWith(_faces[face].Corners);
                }

                break;

            default:
                _selectedFaces.ExceptWith(faces);
                var kept = _selectedFaces.SelectMany(face => _faces[face].Corners).ToHashSet();
                foreach (var face in faces)
                {
                    foreach (var corner in _faces[face].Corners)
                    {
                        if (!kept.Contains(corner)) _selectedVertices.Remove(corner);
                    }
                }

                break;
        }
    }

    /// <summary>
    /// Which of the six directions along the axes a direction is nearest, as the axis, 0 for X, 1
    /// for Y and 2 for Z, and its sign, Y first where two are as near, then X.
    /// </summary>
    private static (int Axis, bool Negative) Nearest(Vec3 direction)
    {
        float x = MathF.Abs(direction.X), y = MathF.Abs(direction.Y), z = MathF.Abs(direction.Z);

        if (y >= x && y >= z) return (1, direction.Y < 0f);
        if (x >= z) return (0, direction.X < 0f);
        return (2, direction.Z < 0f);
    }

    /// <summary>The vertices an operation on vertices moves, in the order of their indices.</summary>
    private int[] SelectedVertices() => _selectedVertices.Order().ToArray();
}
