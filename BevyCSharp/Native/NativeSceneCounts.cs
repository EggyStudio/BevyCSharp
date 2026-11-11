using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's <c>BcsSceneCounts</c>, what the debug overlay reads of the scene, the meshes some
/// view saw and their triangles, the lights by kind with those casting shadow maps, and the camera
/// the view is drawn by.
/// </summary>
/// <remarks>
/// The offsets are the bridge's, written out, so a field reordered on one side cannot read another
/// field on the other, and a test on each side holds them to the same numbers.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 72)]
internal struct NativeSceneCounts
{
    /// <summary>3D meshes some view saw in the last frame.</summary>
    [FieldOffset(0)] public uint Meshes;

    /// <summary>Their triangles, of the meshes whose data the game's world keeps.</summary>
    [FieldOffset(8)] public ulong Triangles;

    /// <summary>Directional lights.</summary>
    [FieldOffset(16)] public uint Directional;

    /// <summary>Point lights.</summary>
    [FieldOffset(20)] public uint Point;

    /// <summary>Spot lights.</summary>
    [FieldOffset(24)] public uint Spot;

    /// <summary>Lights of the three kinds casting shadow maps.</summary>
    [FieldOffset(28)] public uint Shadowed;

    /// <summary>The camera the view is drawn by, its bits, or zero where there is none.</summary>
    [FieldOffset(32)] public ulong Camera;

    /// <summary>Its vertical field of view in radians, zero for a projection that has none.</summary>
    [FieldOffset(40)] public float Fov;

    /// <summary>Where it is, X.</summary>
    [FieldOffset(44)] public float X;

    /// <summary>Where it is, Y.</summary>
    [FieldOffset(48)] public float Y;

    /// <summary>Where it is, Z.</summary>
    [FieldOffset(52)] public float Z;

    /// <summary>The way it faces, X.</summary>
    [FieldOffset(56)] public float ForwardX;

    /// <summary>The way it faces, Y.</summary>
    [FieldOffset(60)] public float ForwardY;

    /// <summary>The way it faces, Z.</summary>
    [FieldOffset(64)] public float ForwardZ;
}
