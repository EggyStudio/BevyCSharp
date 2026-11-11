using System.Runtime.InteropServices;

namespace Bevy.Interop;

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
