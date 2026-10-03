using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>What a monitor is and where it sits.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeMonitor
{
    /// <summary>Width in physical pixels.</summary>
    public uint Width;

    /// <summary>Height in physical pixels.</summary>
    public uint Height;

    /// <summary>Desktop-space left edge.</summary>
    public int X;

    /// <summary>Desktop-space top edge.</summary>
    public int Y;

    /// <summary>Refresh rate in millihertz, or 0 when unreported.</summary>
    public uint RefreshMillihertz;

    /// <summary>Physical pixels per logical pixel.</summary>
    public float ScaleFactor;
}

/// <summary>One thing the window reported.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeWindowEvent
{
    /// <summary>0 resized, 1 focus, 2 close requested, 3 scale, 4 cursor entered, 5 cursor left.</summary>
    public int Kind;

    /// <summary>Width, focus flag, or scale factor.</summary>
    public float A;

    /// <summary>Height, for a resize.</summary>
    public float B;
}

/// <summary>Where the window is and how large, as <c>bcs_window_place</c> writes it.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeWindowPlace
{
    /// <summary>Non-zero when the platform has said where the window is.</summary>
    public int HasPosition;

    /// <summary>The window's top left corner, in physical pixels.</summary>
    public int X;

    /// <summary>See <see cref="X"/>.</summary>
    public int Y;

    /// <summary>Width in logical pixels.</summary>
    public uint Width;

    /// <summary>Height in logical pixels.</summary>
    public uint Height;

    /// <summary>Non-zero while maximized.</summary>
    public int Maximized;
}
