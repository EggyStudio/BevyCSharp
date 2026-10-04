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

/// <summary>What the bridge counted over a span of frames, mirroring <c>BcsProfile</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeProfile
{
    /// <summary>Frames counted whole.</summary>
    public ulong Frames;

    /// <summary>Their time from the top of one to the top of the next, in nanoseconds.</summary>
    public ulong FrameNanos;

    /// <summary>Their time from the top of <c>First</c> to the end of <c>Last</c>, in nanoseconds.</summary>
    public ulong ScheduleNanos;

    /// <summary>Managed systems run.</summary>
    public ulong ManagedCalls;

    /// <summary>Their time, the crossings they made included, in nanoseconds.</summary>
    public ulong ManagedNanos;

    /// <summary>Calls the managed systems made into the bridge.</summary>
    public ulong Crossings;

    /// <summary>Those calls' time inside the bridge, in nanoseconds.</summary>
    public ulong CrossingNanos;

    /// <summary>Render schedules run.</summary>
    public ulong Renders;

    /// <summary>Their time from first system to last, in nanoseconds.</summary>
    public ulong RenderNanos;
}
