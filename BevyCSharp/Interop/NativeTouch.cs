using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One finger on a touchscreen, as the bridge reports it.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeTouch
{
    /// <summary>Identifies this finger while it stays down.</summary>
    public ulong Id;

    /// <summary>Position in physical window pixels.</summary>
    public float X;

    /// <summary>Position in physical window pixels.</summary>
    public float Y;

    /// <summary>0 held, 1 started this frame, 2 ended this frame.</summary>
    public int Phase;

    /// <summary>Padding, so the array strides evenly.</summary>
    public int Padding;
}
