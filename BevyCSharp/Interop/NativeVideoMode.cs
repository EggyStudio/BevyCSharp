using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One video mode a monitor can be driven at.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeVideoMode
{
    /// <summary>Width in physical pixels.</summary>
    public uint Width;

    /// <summary>Height in physical pixels.</summary>
    public uint Height;

    /// <summary>Bits per pixel.</summary>
    public uint BitDepth;

    /// <summary>Refresh rate in millihertz.</summary>
    public uint RefreshMillihertz;
}
