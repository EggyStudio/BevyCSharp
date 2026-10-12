using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>How a camera should see, handed to the bridge when one is spawned.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeCameraConfig
{
    /// <summary>0 perspective, 1 orthographic.</summary>
    public int Projection;

    /// <summary>Vertical field of view in degrees. Perspective only.</summary>
    public float FovDegrees;

    /// <summary>World units visible vertically. Orthographic only.</summary>
    public float OrthoHeight;

    /// <summary>Nearest visible distance.</summary>
    public float Near;

    /// <summary>Furthest visible distance.</summary>
    public float Far;

    /// <summary>0 world clear color, 1 the one below, 2 no clear.</summary>
    public int ClearMode;

    /// <summary>Clear color red.</summary>
    public float ClearR;

    /// <summary>Clear color green.</summary>
    public float ClearG;

    /// <summary>Clear color blue.</summary>
    public float ClearB;

    /// <summary>Clear color alpha.</summary>
    public float ClearA;

    /// <summary>Draw order; higher draws over lower.</summary>
    public int Order;

    /// <summary>Non-zero to draw into part of the window.</summary>
    public int HasViewport;

    /// <summary>Viewport left, in physical pixels.</summary>
    public uint ViewportX;

    /// <summary>Viewport top, in physical pixels.</summary>
    public uint ViewportY;

    /// <summary>Viewport width, in physical pixels.</summary>
    public uint ViewportWidth;

    /// <summary>Viewport height, in physical pixels.</summary>
    public uint ViewportHeight;

    /// <summary>A bit per render layer; 0 for the default layer.</summary>
    public uint Layers;
}
