namespace Bevy;

// The modifiers, operations kept in a list and run in order on a copy of the mesh each time it is
// built, so the mesh itself, the base, stays editable under them, as Blender's modifier stack
// leaves an object's mesh. Each is a line in the words its operation takes, added, moved, removed
// and applied by lines of the recipe like any other change.
public sealed partial class EditMesh
{
    /// <summary>The most levels a subdivision modifier takes, six.</summary>
    public const int MostLevels = 6;

    /// <summary>The most copies an array modifier makes, a thousand.</summary>
    public const int MostCopies = 1000;

    private readonly List<(string Line, Action<EditMesh> Apply)> _modifiers = [];
    private EditMesh? _built;
    private int _builtVersion = -1;
    private bool _baking;

    /// <summary>The modifiers, a line each in the words <see cref="AddModifier"/> takes, applied in this order at the build.</summary>
    public IReadOnlyList<string> Modifiers => [.. _modifiers.Select(modifier => modifier.Line)];

    /// <summary>Adds a modifier at the end of the stack, applied to a copy of the mesh each time it is built.</summary>
    /// <param name="modifier">The modifier's line, its kind and its measures, as the table in the remarks gives them.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// <para>
    /// A modifier changes what is drawn and collided with, <see cref="Built"/>, and leaves the
    /// mesh's own vertices and faces, the base, as they are, so the base is shaped under a stack
    /// of them and each is reordered, taken away or applied after. Each is run on everything,
    /// whatever is selected, as Blender's modifiers are.
    /// </para>
    /// <list type="table">
    /// <listheader><term>Line</term><description>What it does</description></listheader>
    /// <item><term><c>mirror x z [at x y z]</c></term><description>Mirrors across each axis's plane through the point, the origin unless given, as <see cref="Mirror"/></description></item>
    /// <item><term><c>array 3 2 0 0</c></term><description>Three copies, each two meters along X from the last</description></item>
    /// <item><term><c>array 5 along 0 0 0 4 0 0 4 0 4</c></term><description>Five copies spaced evenly along the line through the points, the first where the mesh is</description></item>
    /// <item><term><c>bevel 0.1 [2] [angle 30]</c></term><description>Bevels every edge, or those whose faces meet at more than the angle, as <see cref="Bevel"/></description></item>
    /// <item><term><c>subdivision 2</c></term><description>Catmull and Clark's subdivision surface to a level, up to <see cref="MostLevels"/></description></item>
    /// <item><term><c>solidify 0.1</c></term><description>Gives the surface a thickness, outward where above zero and inward where below</description></item>
    /// <item><term><c>boolean subtract models/hole.model [at x y z]</c></term><description>A boolean with another model or a solid primitive's line, as <see cref="Union"/>, <see cref="Subtract"/> and <see cref="Intersect"/></description></item>
    /// <item><term><c>triangulate</c></term><description>Cuts every face into triangles</description></item>
    /// </list>
    /// <para>
    /// A model file read by a boolean is read again whenever the mesh is built after a change of
    /// its own, so a change to that file reaches the build at the mesh's next change.
    /// </para>
    /// </remarks>
    /// <exception cref="FormatException">The line is no modifier, or its measures are not the modifier's.</exception>
    /// <exception cref="ArgumentException">A measure is out of the modifier's range.</exception>
    public EditMesh AddModifier(string modifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modifier);

        var words = modifier.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var read = new Reader(words, modifier).ReadModifier();
        _modifiers.Add(read);
        Changed();

        // Built once here, so a modifier that cannot run, such as a boolean with a model that is
        // not there, is refused as it is added rather than when the mesh is drawn.
        try
        {
            Evaluated();
        }
        catch
        {
            _modifiers.RemoveAt(_modifiers.Count - 1);
            Changed();
            throw;
        }

        return Did($"modifier add {read.Line}");
    }

    /// <summary>Moves a modifier to another place in the stack, the others closing up behind it.</summary>
    /// <param name="from">Its place, from zero.</param>
    /// <param name="to">The place it goes to, from zero.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh MoveModifier(int from, int to)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)from, (uint)_modifiers.Count, nameof(from));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)to, (uint)_modifiers.Count, nameof(to));

        var moving = _modifiers[from];
        _modifiers.RemoveAt(from);
        _modifiers.Insert(to, moving);
        Changed();
        return Did($"modifier move {from} {to}");
    }

    /// <summary>Takes a modifier out of the stack, leaving the base as it is.</summary>
    /// <param name="index">Its place, from zero.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh RemoveModifier(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_modifiers.Count, nameof(index));

        _modifiers.RemoveAt(index);
        Changed();
        return Did($"modifier remove {index}");
    }

    /// <summary>Bakes a modifier into the base, as though its operation were run on the whole mesh, and takes it out of the stack.</summary>
    /// <param name="index">Its place, from zero.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// The modifier runs on the base as it stands, whatever is before it in the stack, as Blender
    /// applies a modifier that is not the first, so applying the first keeps what is built the
    /// same. Everything is selected after, as the modifier's operation leaves it.
    /// </remarks>
    public EditMesh ApplyModifier(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_modifiers.Count, nameof(index));

        var (_, apply) = _modifiers[index];
        _modifiers.RemoveAt(index);
        SelectAllQuietly();

        // The operations it runs write no lines of their own, so the recipe holds the one line
        // that runs them all again.
        _baking = true;
        try
        {
            apply(this);
        }
        finally
        {
            _baking = false;
        }

        SelectAllQuietly();
        Changed();
        return Did($"modifier apply {index}");
    }

    /// <summary>A copy of the mesh with its modifiers applied in order, what is drawn and collided with.</summary>
    /// <returns>A mesh of its own, which a change to this one leaves as it is, with no recipe, selection or modifiers.</returns>
    /// <remarks>A copy of the mesh as it is where it has no modifiers.</remarks>
    public EditMesh Built() => Evaluated().Copy();

    /// <summary>The mesh with its modifiers applied, made once for each version, or this mesh where it has none.</summary>
    /// <remarks>Shared, so the build reads it and nothing changes it.</remarks>
    internal EditMesh Evaluated()
    {
        if (_modifiers.Count == 0) return this;
        if (_built is { } known && _builtVersion == Version) return known;

        var built = Copy();
        foreach (var (_, apply) in _modifiers)
        {
            built.SelectAllQuietly();
            apply(built);
            built._recipe.Clear();
        }

        built.SelectAllQuietly();
        (_built, _builtVersion) = (built, Version);
        return built;
    }

    /// <summary>A mesh of its own with this one's vertices, faces, slots and settings, and no recipe, selection or modifiers.</summary>
    private EditMesh Copy()
    {
        var copy = new EditMesh { GridStep = GridStep, BooleanEpsilon = BooleanEpsilon };
        copy._positions.AddRange(_positions);
        foreach (var face in _faces) copy._faces.Add(new Face([.. face.Corners], face.Slot, face.Smooth, face.Uvs is { } uvs ? [.. uvs] : null));
        copy._slots.Clear();
        copy._slots.AddRange(_slots);
        copy._looks.Clear();
        copy._looks.AddRange(_looks);
        copy.Changed();
        return copy;
    }

    private void SelectAllQuietly()
    {
        _selectedVertices.UnionWith(Enumerable.Range(0, _positions.Count));
        _selectedFaces.UnionWith(Enumerable.Range(0, _faces.Count));
    }

    /// <summary>Adds copies of every face, the copy of a number moved by what the offset answers for it.</summary>
    private void Copies(int count, Func<int, Vec3> offset)
    {
        var (vertices, faces) = (_positions.Count, _faces.Count);
        for (var copy = 1; copy < count; copy++)
        {
            var by = offset(copy);
            var first = _positions.Count;
            for (var vertex = 0; vertex < vertices; vertex++) _positions.Add(_positions[vertex] + by);
            for (var face = 0; face < faces; face++)
            {
                var source = _faces[face];
                AddFace([.. source.Corners.Select(corner => corner + first)], source.Slot, source.Smooth, source.Uvs is { } uvs ? [.. uvs] : null);
            }
        }

        Changed();
    }

    /// <summary>The offsets of copies spaced evenly along a line through points, the first copy at the first point.</summary>
    private static Func<int, Vec3> Along(Vec3[] points, int count)
    {
        var lengths = new float[points.Length];
        for (var i = 1; i < points.Length; i++) lengths[i] = lengths[i - 1] + (points[i] - points[i - 1]).Length;

        return copy =>
        {
            var distance = lengths[^1] * copy / (count - 1);
            var leg = 1;
            while (leg < points.Length - 1 && lengths[leg] < distance) leg++;

            var span = lengths[leg] - lengths[leg - 1];
            var fraction = span > 0f ? (distance - lengths[leg - 1]) / span : 0f;
            return points[leg - 1] + (points[leg] - points[leg - 1]) * fraction - points[0];
        };
    }

    /// <summary>The edges whose two faces meet at more than an angle, or every edge two faces share where none is given.</summary>
    private HashSet<(int Low, int High)> EdgesPast(float? degrees)
    {
        var directed = Directed();
        var least = degrees is { } angle ? MathF.Cos(angle * MathF.PI / 180f) : 2f;
        return EdgeUses().Keys.Where(edge =>
            directed.TryGetValue((edge.Low, edge.High), out var one) && directed.TryGetValue((edge.High, edge.Low), out var other)
            && Vec3.Dot(Normal(one), Normal(other)) < least).ToHashSet();
    }
}
