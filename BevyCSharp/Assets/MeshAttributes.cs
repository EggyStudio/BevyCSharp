namespace Bevy;

/// <summary>Which vertex attributes a mesh has, beyond its positions.</summary>
[Flags]
public enum MeshAttributes : uint
{
    /// <summary>Positions alone.</summary>
    None = 0,

    /// <summary>A normal a vertex, which lighting needs.</summary>
    Normals = 1,

    /// <summary>A tangent a vertex, which a normal map needs.</summary>
    Tangents = 2,

    /// <summary>Texture coordinates, which a texture needs.</summary>
    Uvs = 4,

    /// <summary>A second set of texture coordinates, usually for a light map.</summary>
    SecondUvs = 8,

    /// <summary>A color a vertex.</summary>
    Colors = 16,

    /// <summary>Which joints of a skeleton move a vertex.</summary>
    Joints = 32,

    /// <summary>How much each of those joints does.</summary>
    Weights = 64,
}
