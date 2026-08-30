using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Frame timing, mirrored from Bevy's <c>Time</c> resource.
/// </summary>
/// <remarks>
/// Refreshed once per frame during <see cref="Stage.FrameSync"/>. Bevy already clamps
/// <see cref="DeltaSeconds"/> so a stalled frame cannot tunnel your physics; the unclamped
/// value is available as <see cref="RawDeltaSeconds"/> if you need wall-clock time.
/// </remarks>
public sealed class Time
{
    private const double FpsSmoothing = 0.1;

    /// <summary>Seconds since the app started.</summary>
    public double ElapsedSeconds { get; private set; }

    /// <summary>Seconds since the previous frame, clamped by Bevy.</summary>
    public double DeltaSeconds { get; private set; }

    /// <summary>Seconds since the previous frame, unclamped.</summary>
    public double RawDeltaSeconds { get; private set; }

    /// <summary>Frames completed since the app started.</summary>
    public ulong FrameCount { get; private set; }

    /// <summary>Instantaneous frames per second for the last frame.</summary>
    public double Fps => DeltaSeconds > 0.0 ? 1.0 / DeltaSeconds : 0.0;

    /// <summary>
    /// Frames per second smoothed with an exponential moving average, which is what you want
    /// on a HUD - the raw value is far too jittery to read.
    /// </summary>
    public double SmoothedFps { get; private set; }

    /// <summary><see cref="DeltaSeconds"/> as a float, the usual type in gameplay maths.</summary>
    public float Delta => (float)DeltaSeconds;

    /// <summary><see cref="ElapsedSeconds"/> as a float.</summary>
    public float Elapsed => (float)ElapsedSeconds;

    /// <summary>Copies a native snapshot into this frame's state.</summary>
    internal void Update(in NativeTime snapshot)
    {
        ElapsedSeconds = snapshot.ElapsedSeconds;
        DeltaSeconds = snapshot.DeltaSeconds;
        RawDeltaSeconds = snapshot.RawDeltaSeconds;
        FrameCount = snapshot.FrameCount;

        var instantaneous = Fps;
        SmoothedFps = SmoothedFps <= 0.0
            ? instantaneous
            : SmoothedFps + (instantaneous - SmoothedFps) * FpsSmoothing;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"Time(frame={FrameCount}, elapsed={ElapsedSeconds:F2}s, delta={DeltaSeconds * 1000:F2}ms, "
        + $"fps={SmoothedFps:F1})";
}
