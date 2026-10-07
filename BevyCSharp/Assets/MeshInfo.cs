namespace Bevy;

/// <summary>What a mesh is made of, read without copying its vertices.</summary>
/// <param name="Vertices">How many vertices.</param>
/// <param name="Indices">How many indices, or zero for a mesh drawn without them.</param>
/// <param name="IndexBits">16 or 32, the width of an index, or zero for none.</param>
/// <param name="Topology">What the vertices are joined into.</param>
/// <param name="Attributes">Which attributes it has beyond positions.</param>
/// <param name="Min">The corner of its bounds with the smallest coordinates.</param>
/// <param name="Max">The corner with the largest.</param>
public readonly record struct MeshInfo(
    int Vertices,
    int Indices,
    int IndexBits,
    MeshTopology Topology,
    MeshAttributes Attributes,
    Vec3 Min,
    Vec3 Max)
{
    /// <summary>How many triangles it draws, or zero for a mesh of lines or points.</summary>
    public int Triangles => Topology switch
    {
        MeshTopology.Triangles => (Indices > 0 ? Indices : Vertices) / 3,
        MeshTopology.TriangleStrip => Math.Max(0, (Indices > 0 ? Indices : Vertices) - 2),
        _ => 0,
    };

    /// <summary>How large its bounds are along each axis.</summary>
    public Vec3 Size => Max - Min;
}
