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
