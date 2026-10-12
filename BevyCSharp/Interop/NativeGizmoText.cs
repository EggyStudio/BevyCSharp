using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>Where and how a run of gizmo text is drawn. Mirrors <c>BcsGizmoText</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeGizmoText
{
    /// <summary>Where its anchor sits.</summary>
    public fixed float Position[3];

    /// <summary>Which way it faces, as a quaternion.</summary>
    public fixed float Rotation[4];

    /// <summary>The height of a capital letter.</summary>
    public float Size;

    /// <summary>The point of its bounds at the position, from minus a half to a half.</summary>
    public fixed float Anchor[2];

    /// <summary>Linear RGBA.</summary>
    public fixed float Color[4];

    /// <summary>Non-zero to draw over everything.</summary>
    public int InFront;
}
