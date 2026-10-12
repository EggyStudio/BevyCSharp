using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>Everything C# refreshes at the top of a frame.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeFrameState
{
    /// <summary>Timing snapshot.</summary>
    public NativeTime Time;

    /// <summary>Input snapshot.</summary>
    public NativeInput Input;
}
