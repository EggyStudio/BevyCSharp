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
}
