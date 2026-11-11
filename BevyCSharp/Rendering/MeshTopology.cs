namespace Bevy;

/// <summary>How a mesh's vertices join up.</summary>
public enum MeshTopology
{
    /// <summary>Every three vertices, or three indices, are a triangle.</summary>
    Triangles = 0,

    /// <summary>Every two are a line.</summary>
    Lines = 1,

    /// <summary>Every one is a point.</summary>
    Points = 2,

    /// <summary>Each joins the one before it with a line.</summary>
    LineStrip = 3,

    /// <summary>Each makes a triangle with the two before it.</summary>
    TriangleStrip = 4,
}
