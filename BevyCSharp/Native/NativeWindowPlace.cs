using System.Runtime.InteropServices;

namespace Bevy.Interop;

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
