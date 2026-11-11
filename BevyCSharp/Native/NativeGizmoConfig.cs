using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One debug shape to draw this frame.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeGizmoConfig
{
    /// <summary>0 line, 1 sphere, 2 axes.</summary>
    public int Kind;

    /// <summary>Start, center or position, X.</summary>
    public float StartX;

    /// <summary>Start, center or position, Y.</summary>
    public float StartY;

    /// <summary>Start, center or position, Z.</summary>
    public float StartZ;

    /// <summary>Line end, X.</summary>
    public float EndX;

    /// <summary>Line end, Y.</summary>
    public float EndY;

    /// <summary>Line end, Z.</summary>
    public float EndZ;

    /// <summary>Orientation, X.</summary>
    public float RotationX;

    /// <summary>Orientation, Y.</summary>
    public float RotationY;

    /// <summary>Orientation, Z.</summary>
    public float RotationZ;

    /// <summary>Orientation, W.</summary>
    public float RotationW;

    /// <summary>Sphere radius or axis length.</summary>
    public float Radius;

    /// <summary>Color red.</summary>
    public float ColorR;

    /// <summary>Color green.</summary>
    public float ColorG;

    /// <summary>Color blue.</summary>
    public float ColorB;

    /// <summary>Color alpha.</summary>
    public float ColorA;

    /// <summary>What a fading line reaches at its far end, red.</summary>
    public float EndColorR;

    /// <summary>Far end green.</summary>
    public float EndColorG;

    /// <summary>Far end blue.</summary>
    public float EndColorB;

    /// <summary>Far end alpha.</summary>
    public float EndColorA;

    /// <summary>Whether the scene can hide it. 0 is depth tested, anything else draws in front.</summary>
    public int InFront;
}
