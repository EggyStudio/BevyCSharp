using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's <c>BcsFieldDraw</c>, a mesh drawn that casts shadows as the scene's distance field
/// reads it, its mesh's number and generation, its sides, its matrix, its material's color and
/// emission, and its bounds in its own space.
/// </summary>
/// <remarks>
/// The offsets are the bridge's, written out, so a field reordered on one side cannot read another
/// field on the other, and a test on each side holds them to the same numbers.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 128)]
internal unsafe struct NativeFieldDraw
{
    /// <summary>The mesh's number, the same for every entity drawing it.</summary>
    [FieldOffset(0)] public uint Mesh;

    /// <summary>How many times Bevy said the mesh's data changed since it was numbered.</summary>
    [FieldOffset(4)] public uint Generation;

    /// <summary>Bit 0 where the mesh has no inside, its back faces drawn, bit 1 where it is skinned.</summary>
    [FieldOffset(8)] public uint Flags;

    /// <summary>The rows of the matrix from the mesh's own space to the world's, three of four.</summary>
    [FieldOffset(16)] public fixed float Rows[12];

    /// <summary>The material's base color in linear light, red, green and blue.</summary>
    [FieldOffset(64)] public fixed float Color[4];

    /// <summary>The material's emission in linear light.</summary>
    [FieldOffset(80)] public fixed float Emission[4];

    /// <summary>The least corner of the mesh's bounds in its own space.</summary>
    [FieldOffset(96)] public fixed float Low[4];

    /// <summary>The greatest corner of the mesh's bounds in its own space.</summary>
    [FieldOffset(112)] public fixed float High[4];
}

/// <summary>The bridge's <c>BcsFieldEye</c>, the camera the scene's distance field lies around.</summary>
[StructLayout(LayoutKind.Explicit, Size = 32)]
internal struct NativeFieldEye
{
    /// <summary>Where it is, X.</summary>
    [FieldOffset(0)] public float X;

    /// <summary>Where it is, Y.</summary>
    [FieldOffset(4)] public float Y;

    /// <summary>Where it is, Z.</summary>
    [FieldOffset(8)] public float Z;

    /// <summary>1 where there is a camera, 0 where there is none.</summary>
    [FieldOffset(12)] public uint Found;

    /// <summary>The way it faces, X.</summary>
    [FieldOffset(16)] public float ForwardX;

    /// <summary>The way it faces, Y.</summary>
    [FieldOffset(20)] public float ForwardY;

    /// <summary>The way it faces, Z.</summary>
    [FieldOffset(24)] public float ForwardZ;
}
