namespace Bevy;

/// <summary>
/// A polygon mesh shaped by operations, as a modeling program's mesh is, for blocking out a level or
/// a prop from code, from the console or by hand.
/// </summary>
/// <remarks>
/// <para>
/// A vertex is a point, and a face is a loop of three or more vertices, wound counter-clockwise
/// seen from the side it faces, as Bevy's front faces are. A face of any count is kept whole, so a
/// box's side is one face of four corners and a staircase's side one face of many, and is cut into
/// triangles only when the mesh is drawn. Edges are not kept but read from the faces, each pair of
/// corners that follow one another round a face, so no operation has edges to keep in step.
/// </para>
/// <para>
/// Each face has a material slot, one of <see cref="Slots"/>, the first of which every new face
/// takes. A slot is a name rather than a material, so a mesh is shaped before what it is drawn with
/// is decided, and every face of a slot is drawn with that slot's material.
/// </para>
/// <para>
/// The selection is a set of vertices and a set of faces, and the operations act on it. Selecting
/// a face selects its corners, so an operation on vertices, such as <see cref="Move"/>, moves the
/// faces selected, and an edge is selected where both its ends are. A primitive selects what it
/// made, so the next operation acts on it.
/// </para>
/// <para>
/// Measures are taken as given. The <see cref="GridStep"/> is where a point picked or dragged by
/// hand lands, and <see cref="Snap"/> puts the selection on it, so a typed measure is never rounded
/// away, as a move of a fifth of a meter on a one meter grid would be.
/// </para>
/// <para>
/// One program shapes a mesh at a time. Nothing here locks, as a list does not, since an edit
/// mesh is data a game owns, built into what is drawn when the game asks.
/// </para>
/// </remarks>
public sealed partial class EditMesh
{
    /// <summary>The finest grid step, a sixteenth of a meter.</summary>
    public const float FinestGrid = 1f / 16f;

    /// <summary>The coarsest grid step, sixty-four meters.</summary>
    public const float CoarsestGrid = 64f;

    /// <summary>The name of the first material slot, which every new face takes.</summary>
    public const string DefaultSlot = "default";

    private readonly List<Vec3> _positions = [];
    private readonly List<Face> _faces = [];
    private readonly List<string> _slots = [DefaultSlot];
    private readonly HashSet<int> _selectedVertices = [];
    private readonly HashSet<int> _selectedFaces = [];

    // The edges as the faces have them, read again after a change rather than kept in step, since
    // every operation that moves a corner between faces would otherwise have to mend them.
    private Dictionary<(int Low, int High), EdgeUse>? _edges;

    /// <summary>A face, its corners in order round it, its slot, and the angle it is smoothed across.</summary>
    private sealed class Face(int[] corners, int slot, float smooth = 0f)
    {
        public int[] Corners = corners;
        public int Slot = slot;

        /// <summary>The angle in degrees up to which its corners share a normal with the faces beside them, or zero for flat.</summary>
        public float Smooth = smooth;
    }

    /// <summary>How often the faces run along an edge from its lower vertex and from its higher.</summary>
    private struct EdgeUse
    {
        public int Forward;
        public int Backward;
    }

    /// <summary>How many vertices the mesh has.</summary>
    public int VertexCount => _positions.Count;

    /// <summary>How many faces the mesh has.</summary>
    public int FaceCount => _faces.Count;

    /// <summary>How many edges the faces have between them, an edge two faces share counted once.</summary>
    public int EdgeCount => EdgeUses().Count;

    /// <summary>The names of the material slots, the first of which every new face takes.</summary>
    public IReadOnlyList<string> Slots => _slots;

    /// <summary>A count that moves on every change to what the mesh draws as, and not on a change of the selection.</summary>
    /// <remarks>
    /// What <see cref="Modeling.Meshes"/> compares with the count it last built at, so a mesh is
    /// written again only once it changed, however often it is asked for.
    /// </remarks>
    public int Version { get; private set; }

    /// <summary>The grid step a point picked by hand lands on, and that <see cref="Snap"/> rounds to.</summary>
    /// <remarks>One meter until <see cref="Grid"/> sets another.</remarks>
    public float GridStep { get; private set; } = 1f;

    /// <summary>Where a vertex is.</summary>
    /// <param name="vertex">Its index, from zero to <see cref="VertexCount"/>.</param>
    public Vec3 Position(int vertex) => _positions[vertex];

    /// <summary>A face's vertices, in order round it, counter-clockwise seen from the side it faces.</summary>
    /// <param name="face">Its index, from zero to <see cref="FaceCount"/>.</param>
    public ReadOnlySpan<int> Corners(int face) => _faces[face].Corners;

    /// <summary>The name of a face's material slot.</summary>
    /// <param name="face">Its index, from zero to <see cref="FaceCount"/>.</param>
    public string SlotOf(int face) => _slots[_faces[face].Slot];

    /// <summary>The angle in degrees up to which a face is smoothed into those beside it, or zero where it is drawn flat.</summary>
    /// <param name="face">Its index, from zero to <see cref="FaceCount"/>.</param>
    public float SmoothOf(int face) => _faces[face].Smooth;

    /// <summary>A face's slot, as its index in <see cref="Slots"/>.</summary>
    internal int SlotIndexOf(int face) => _faces[face].Slot;

    /// <summary>A face's normal at twice its area, Newell's sum, which the drawing weighs a smoothed corner's normal by.</summary>
    internal Vec3 Area(int face) => Newell(_faces[face].Corners);

    /// <summary>The way a face faces, a length of one, or zero for a face with no area.</summary>
    /// <param name="face">Its index, from zero to <see cref="FaceCount"/>.</param>
    /// <remarks>
    /// Newell's sum over the face's edges, which gives a face whose corners are not quite in one
    /// plane, or a concave one, the normal its outline means, where the cross product of two edges
    /// would give the normal of whichever corner was taken.
    /// </remarks>
    public Vec3 Normal(int face) => Newell(_faces[face].Corners).Normalized;

    /// <summary>The middle of a face, the mean of its corners.</summary>
    /// <param name="face">Its index, from zero to <see cref="FaceCount"/>.</param>
    public Vec3 Center(int face)
    {
        var corners = _faces[face].Corners;
        var sum = Vec3.Zero;
        foreach (var corner in corners) sum += _positions[corner];
        return sum * (1f / corners.Length);
    }

    /// <summary>The edges, each once, its ends in the order of their indices.</summary>
    public IEnumerable<(int A, int B)> Edges() => EdgeUses().Keys.Select(edge => (edge.Low, edge.High));

    /// <summary>
    /// How many edges are not shared by two faces running along them in opposite directions, which
    /// is none for a closed mesh whose faces all face out or all face in.
    /// </summary>
    /// <remarks>
    /// An edge of a hole is used once, an edge where three faces meet three times, and an edge
    /// between a face turned the wrong way and its neighbor twice in one direction. Each of those
    /// leaves the inside open to the outside, so a collider of it or its <see cref="Volume"/> means
    /// nothing, and each counts here.
    /// </remarks>
    public int OpenEdges => EdgeUses().Values.Count(use => use.Forward != 1 || use.Backward != 1);

    /// <summary>Whether every edge is shared by two faces running along it in opposite directions.</summary>
    public bool IsClosed => _faces.Count > 0 && OpenEdges == 0;

    /// <summary>
    /// The space a closed mesh holds, in cubic meters, negative where its faces face in.
    /// </summary>
    /// <remarks>
    /// The sum over the faces of the solid each makes with the origin, which is the volume of a
    /// closed mesh wherever the origin is. A face is taken as a fan from its first corner, which
    /// gives a flat face its area whether it is convex or not, since the parts of the fan outside
    /// a concave face count against those inside. Summed in doubles, so a level of many faces far
    /// from the origin loses nothing in the sum. A mesh that is not closed has no inside, and the
    /// number means nothing, which <see cref="OpenEdges"/> says.
    /// </remarks>
    public float Volume
    {
        get
        {
            double sum = 0;
            foreach (var face in _faces)
            {
                var corners = face.Corners;
                var a = _positions[corners[0]];

                for (var i = 1; i < corners.Length - 1; i++)
                {
                    var b = _positions[corners[i]];
                    var c = _positions[corners[i + 1]];

                    // a · (b × c), the six times the signed solid of the origin and the triangle.
                    sum += a.X * ((double)b.Y * c.Z - (double)b.Z * c.Y)
                         + a.Y * ((double)b.Z * c.X - (double)b.X * c.Z)
                         + a.Z * ((double)b.X * c.Y - (double)b.Y * c.X);
                }
            }

            return (float)(sum / 6.0);
        }
    }

    /// <summary>The smallest box holding every vertex, or two zeros for a mesh with none.</summary>
    public (Vec3 Low, Vec3 High) Bounds => BoundsOf(Enumerable.Range(0, _positions.Count));

    /// <summary>Takes away every vertex, face and slot but the first, and the selection with them.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>The recipe keeps it as a line, <c>clear</c>, so an undo after it brings back what it took.</remarks>
    public EditMesh Clear()
    {
        _positions.Clear();
        _faces.Clear();
        _slots.RemoveRange(1, _slots.Count - 1);
        _selectedVertices.Clear();
        _selectedFaces.Clear();
        Changed();
        return Did("clear");
    }

    /// <summary>Says the shape changed, so what is read from the faces is read again and the drawing built again.</summary>
    private void Changed()
    {
        _edges = null;
        Version++;
    }

    private Dictionary<(int Low, int High), EdgeUse> EdgeUses()
    {
        if (_edges is { } known) return known;

        var edges = new Dictionary<(int Low, int High), EdgeUse>();
        foreach (var face in _faces)
        {
            var corners = face.Corners;
            for (var i = 0; i < corners.Length; i++)
            {
                var from = corners[i];
                var to = corners[(i + 1) % corners.Length];
                var key = from < to ? (from, to) : (to, from);

                edges.TryGetValue(key, out var use);
                if (from < to) use.Forward++;
                else use.Backward++;
                edges[key] = use;
            }
        }

        return _edges = edges;
    }

    private (Vec3 Low, Vec3 High) BoundsOf(IEnumerable<int> vertices)
    {
        var any = false;
        Vec3 low = default, high = default;

        foreach (var vertex in vertices)
        {
            var point = _positions[vertex];
            if (!any)
            {
                low = high = point;
                any = true;
                continue;
            }

            low = new Vec3(MathF.Min(low.X, point.X), MathF.Min(low.Y, point.Y), MathF.Min(low.Z, point.Z));
            high = new Vec3(MathF.Max(high.X, point.X), MathF.Max(high.Y, point.Y), MathF.Max(high.Z, point.Z));
        }

        return (low, high);
    }

    private Vec3 Newell(int[] corners)
    {
        double x = 0, y = 0, z = 0;
        for (var i = 0; i < corners.Length; i++)
        {
            var a = _positions[corners[i]];
            var b = _positions[corners[(i + 1) % corners.Length]];

            x += ((double)a.Y - b.Y) * ((double)a.Z + b.Z);
            y += ((double)a.Z - b.Z) * ((double)a.X + b.X);
            z += ((double)a.X - b.X) * ((double)a.Y + b.Y);
        }

        return new Vec3((float)x, (float)y, (float)z);
    }

    private int AddVertex(Vec3 position)
    {
        _positions.Add(position);
        return _positions.Count - 1;
    }

    private int AddFace(int[] corners, int slot = 0, float smooth = 0f)
    {
        _faces.Add(new Face(corners, slot, smooth));
        return _faces.Count - 1;
    }
}
