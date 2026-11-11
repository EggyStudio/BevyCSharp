using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>What a mesh holds, without its vertices, as the bridge reports it.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeMeshInfo
{
    /// <summary>How many vertices.</summary>
    public uint Vertices;

    /// <summary>How many indices, or zero for none.</summary>
    public uint Indices;

    /// <summary>16 or 32, or zero for no indices.</summary>
    public uint IndexBits;

    /// <summary>0 triangles, 1 a strip, 2 lines, 3 a line strip, 4 points.</summary>
    public uint Topology;

    /// <summary>One bit per attribute, as <see cref="MeshAttributes"/> numbers them.</summary>
    public uint Attributes;

    /// <summary>The bounds' smallest corner.</summary>
    public float MinX, MinY, MinZ;

    /// <summary>The bounds' largest corner.</summary>
    public float MaxX, MaxY, MaxZ;
}
