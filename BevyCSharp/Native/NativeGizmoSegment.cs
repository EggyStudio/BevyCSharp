using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One line of a run of gizmo lines, as <c>bcs_gizmo_lines</c> takes it.</summary>
/// <remarks>
/// Fifteen numbers a line, where <see cref="NativeGizmoConfig"/> carries every number any shape
/// reads, so a run of thousands crosses as lines. The bridge asserts the same offsets.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct NativeGizmoSegment
{
    /// <summary>Where it begins, X.</summary>
    public float StartX;

    /// <summary>Where it begins, Y.</summary>
    public float StartY;

    /// <summary>Where it begins, Z.</summary>
    public float StartZ;

    /// <summary>Where it ends, X.</summary>
    public float EndX;

    /// <summary>Where it ends, Y.</summary>
    public float EndY;

    /// <summary>Where it ends, Z.</summary>
    public float EndZ;

    /// <summary>Its color at the start, red, linear.</summary>
    public float ColorR;

    /// <summary>Its color at the start, green.</summary>
    public float ColorG;

    /// <summary>Its color at the start, blue.</summary>
    public float ColorB;

    /// <summary>Its color at the start, alpha.</summary>
    public float ColorA;

    /// <summary>Its color at the end, red, linear.</summary>
    public float EndColorR;

    /// <summary>Its color at the end, green.</summary>
    public float EndColorG;

    /// <summary>Its color at the end, blue.</summary>
    public float EndColorB;

    /// <summary>Its color at the end, alpha.</summary>
    public float EndColorA;

    /// <summary>Non-zero where it fades from one color to the other.</summary>
    public int Fades;
}
