namespace Bevy;

/// <summary>A mesh described vertex by vertex, for <see cref="Render.CreateMesh(MeshData)"/>.</summary>
public sealed class MeshData
{
    /// <summary>Where each vertex is. Required.</summary>
    public Vec3[] Positions { get; set; } = [];

    /// <summary>Which way each vertex faces, or null to have them worked out for triangles.</summary>
    public Vec3[]? Normals { get; set; }

    /// <summary>
    /// Whether a triangle mesh given no <see cref="Normals"/> has them worked out, which it does
    /// unless this is false.
    /// </summary>
    /// <remarks>
    /// False leaves the mesh without normals, for one whose vertex shader finds its normal in a
    /// <see cref="MeshStream"/> of its own, such as a voxel's face packed into four bytes, and never
    /// reads the twelve bytes a vertex worked-out normals would add. A position and four packed
    /// bytes make such a vertex sixteen bytes. Bevy places a normal only where the mesh has one and
    /// its prepass reads the position alone, so the mesh draws and casts its shadow, and a lit
    /// material of Bevy's own on it reads a normal of zero, which lights nothing.
    /// </remarks>
    public bool ComputeNormals { get; set; } = true;

    /// <summary>Texture coordinates, two floats a vertex, or null.</summary>
    public float[]? Uvs { get; set; }

    /// <summary>
    /// A second set of texture coordinates, two floats a vertex, or null.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>UV_1</c>, which a lightmap is read by and a material of the library's own may
    /// carry two numbers a vertex in, as a tree's leaf material carries the way out of its crown.
    /// A standard material reads no texture by it unless told to.
    /// </remarks>
    public float[]? SecondUvs { get; set; }

    /// <summary>Linear RGBA, four floats a vertex, or null.</summary>
    public float[]? Colors { get; set; }

    /// <summary>Which vertices make each shape, or null to take them in order.</summary>
    /// <remarks>
    /// In a strip, an index of <see cref="uint.MaxValue"/> breaks it there and starts it again
    /// from the next, so one mesh holds several strips.
    /// </remarks>
    public uint[]? Indices { get; set; }

    /// <summary>How the vertices join up.</summary>
    public MeshTopology Topology { get; set; } = MeshTopology.Triangles;

    /// <summary>Vertex streams of the game's own, each a value a vertex at a location of its own, or null.</summary>
    /// <remarks>
    /// Read by a vertex shader of the program's own, which declares each at its location's
    /// <c>TEXCOORDn</c> (see <see cref="MeshStream"/>). Bevy's own vertex shader reads none, so a
    /// material without a vertex stage of its own draws the mesh as though it had none.
    /// </remarks>
    public MeshStream[]? Streams { get; set; }
}
