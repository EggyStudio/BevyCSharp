using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>A mesh described vertex by vertex. Mirrors <c>BcsMeshData</c>.</summary>
public unsafe struct NativeMeshData
{
    /// <summary>Three floats a vertex.</summary>
    public float* Positions;

    /// <summary>How many vertices there are.</summary>
    public int VertexCount;

    /// <summary>Three floats a vertex, or null.</summary>
    public float* Normals;

    /// <summary>Two floats a vertex, or null.</summary>
    public float* Uvs;

    /// <summary>Four floats a vertex, or null.</summary>
    public float* Colors;

    /// <summary>Indices into the vertices, or null.</summary>
    public uint* Indices;

    /// <summary>How many indices there are.</summary>
    public int IndexCount;

    /// <summary>0 triangles, 1 lines, 2 points, 3 a line strip, 4 a triangle strip.</summary>
    public int Topology;

    /// <summary>Streams of the game's own, or null.</summary>
    public NativeMeshStream* Streams;

    /// <summary>How many streams there are.</summary>
    public int StreamCount;

    /// <summary>How the mesh is made, four bytes of bits: 1 leaves a triangle mesh without worked-out normals.</summary>
    public uint Flags;
}
