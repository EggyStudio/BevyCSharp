namespace Bevy;

/// <summary>A mesh described vertex by vertex, for <see cref="Render.CreateMesh(MeshData)"/>.</summary>
public sealed class MeshData
{
    /// <summary>Where each vertex is. Required.</summary>
    public Vec3[] Positions { get; set; } = [];

    /// <summary>Which way each vertex faces, or null to have them worked out for triangles.</summary>
    public Vec3[]? Normals { get; set; }

    /// <summary>Texture coordinates, two floats a vertex, or null.</summary>
    public float[]? Uvs { get; set; }

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
