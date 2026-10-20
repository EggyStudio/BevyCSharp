namespace Bevy;

// What moves the selection, the grid it lands on, and the slot its faces are drawn with.
public sealed partial class EditMesh
{
    /// <summary>Moves the selected vertices.</summary>
    /// <param name="by">How far, in meters along each axis.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh Move(Vec3 by)
    {
        foreach (var vertex in SelectedVertices()) _positions[vertex] += by;

        Changed();
        return this;
    }

    /// <summary>Turns the selected vertices about an axis.</summary>
    /// <param name="axis">The axis, any length but zero, the turn counter-clockwise looking back along it.</param>
    /// <param name="degrees">How far.</param>
    /// <param name="pivot">
    /// A point the axis passes through, or nothing for the middle of the box holding the
    /// selection, so a turn of a whole part turns it in place.
    /// </param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// The middle of the box holding the selection rather than the mean of its vertices, which a
    /// side with more vertices than the other would pull toward it, so a quarter turn of a part
    /// that sits on the grid leaves it on the grid.
    /// </remarks>
    public EditMesh Rotate(Vec3 axis, float degrees, Vec3? pivot = null)
    {
        if (axis == Vec3.Zero) throw new ArgumentException("An axis to turn about is not zero.", nameof(axis));

        var vertices = SelectedVertices();
        var center = pivot ?? Middle(vertices);
        var turn = Quat.FromAxisAngle(axis.Normalized, degrees * MathF.PI / 180f);

        foreach (var vertex in vertices) _positions[vertex] = center + turn * (_positions[vertex] - center);

        Changed();
        return this;
    }

    /// <summary>Scales the selected vertices toward or away from a point.</summary>
    /// <param name="factor">How much along each axis, one leaving that axis as it is.</param>
    /// <param name="pivot">The point that stays where it is, or nothing for the middle of the box holding the selection.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// A negative factor mirrors the selection, which would leave a face turned inside out, so a
    /// face whose every corner was scaled is wound the other way when the factors' product is
    /// negative, and faces out as it did. A factor of zero flattens the selection onto a plane,
    /// which faces with no area are left of, as a modeling program leaves them.
    /// </remarks>
    public EditMesh Scale(Vec3 factor, Vec3? pivot = null)
    {
        var vertices = SelectedVertices();
        var center = pivot ?? Middle(vertices);

        foreach (var vertex in vertices)
        {
            var offset = _positions[vertex] - center;
            _positions[vertex] = center + new Vec3(offset.X * factor.X, offset.Y * factor.Y, offset.Z * factor.Z);
        }

        if (factor.X * factor.Y * factor.Z < 0f)
        {
            var moved = vertices.ToHashSet();
            foreach (var face in _faces)
            {
                if (face.Corners.All(moved.Contains)) Array.Reverse(face.Corners);
            }
        }

        Changed();
        return this;
    }

    /// <summary>Scales the selected vertices by one factor along every axis.</summary>
    /// <param name="factor">How much, one leaving it as it is.</param>
    /// <param name="pivot">The point that stays where it is, or nothing for the middle of the box holding the selection.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh Scale(float factor, Vec3? pivot = null) => Scale(new Vec3(factor), pivot);

    /// <summary>Sets the grid step a point picked by hand lands on and <see cref="Snap"/> rounds to.</summary>
    /// <param name="step">
    /// A meter or a power of two of it, from <see cref="FinestGrid"/>, a sixteenth, to
    /// <see cref="CoarsestGrid"/>.
    /// </param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// A power of two, so every finer step divides a coarser one and a point on a coarse grid is
    /// on every finer one, and exact in a float, so a point on the grid stays on it however many
    /// steps it is moved.
    /// </remarks>
    public EditMesh Grid(float step)
    {
        if (!(step >= FinestGrid && step <= CoarsestGrid) || !float.IsPow2(step))
        {
            throw new ArgumentOutOfRangeException(nameof(step), step, "A grid step is a meter or a power of two of it, from a sixteenth to sixty-four.");
        }

        GridStep = step;
        return this;
    }

    /// <summary>Puts each selected vertex on the nearest point of the grid.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>A coordinate halfway between two steps goes to the one further from zero.</remarks>
    public EditMesh Snap()
    {
        var step = GridStep;
        foreach (var vertex in SelectedVertices())
        {
            var point = _positions[vertex];
            _positions[vertex] = new Vec3(OnGrid(point.X, step), OnGrid(point.Y, step), OnGrid(point.Z, step));
        }

        Changed();
        return this;
    }

    /// <summary>Puts the selected faces in a material slot, which is made where the mesh has none of that name.</summary>
    /// <param name="slot">The slot's name.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// A slot holds a name rather than a material, so a mesh is shaped and its faces sorted into
    /// floors, walls and trims before what each is drawn with is decided, and every face of a slot
    /// is drawn with the one material that slot is given.
    /// </remarks>
    public EditMesh Material(string slot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slot);
        if (slot.Any(char.IsWhiteSpace)) throw new ArgumentException("A slot's name is one word.", nameof(slot));

        var index = _slots.IndexOf(slot);
        if (index < 0)
        {
            _slots.Add(slot);
            index = _slots.Count - 1;
        }

        foreach (var face in _selectedFaces) _faces[face].Slot = index;
        return this;
    }

    /// <summary>The middle of the box holding some vertices.</summary>
    private Vec3 Middle(int[] vertices)
    {
        var (low, high) = BoundsOf(vertices);
        return (low + high) * 0.5f;
    }

    private static float OnGrid(float value, float step) => MathF.Round(value / step, MidpointRounding.AwayFromZero) * step;
}
