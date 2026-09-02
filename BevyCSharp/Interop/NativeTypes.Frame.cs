using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>Frame timing mirrored from Bevy's <c>Time</c> resource.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeTime
{
    /// <summary>Seconds since app start.</summary>
    public double ElapsedSeconds;

    /// <summary>Seconds since the previous frame, clamped by Bevy's max delta.</summary>
    public double DeltaSeconds;

    /// <summary>Unclamped seconds since the previous frame.</summary>
    public double RawDeltaSeconds;

    /// <summary>Frames completed since app start.</summary>
    public ulong FrameCount;

    /// <summary>Seconds one <see cref="Stage.FixedUpdate"/> step covers.</summary>
    public double FixedDeltaSeconds;
}

/// <summary>Everything C# refreshes at the top of a frame.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeFrameState
{
    /// <summary>Timing snapshot.</summary>
    public NativeTime Time;

    /// <summary>Input snapshot.</summary>
    public NativeInput Input;
}
