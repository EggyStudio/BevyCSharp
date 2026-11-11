using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>When Bevy marks a component added and last changed. Mirrors <c>BcsChangeTicks</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeChangeTicks
{
    /// <summary>The tick it was added at.</summary>
    public uint Added;

    /// <summary>The tick it last changed at.</summary>
    public uint Changed;

    /// <summary>One where it changed since the running system last ran, and two where it was added since.</summary>
    public uint Flags;
}
