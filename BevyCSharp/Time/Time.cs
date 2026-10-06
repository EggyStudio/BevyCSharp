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
public sealed unsafe class Time
{
    private const double FpsSmoothing = 0.1;

    /// <summary>How many more frames a step runs before the clock stops again, or zero.</summary>
    private int _stepping;

    /// <summary>Seconds since the app started.</summary>
    public double ElapsedSeconds { get; private set; }

    /// <summary>Seconds since the previous frame, clamped by Bevy.</summary>
    public double DeltaSeconds { get; private set; }

    /// <summary>Seconds since the previous frame, unclamped.</summary>
    public double RawDeltaSeconds { get; private set; }

    /// <summary>Frames completed since the app started.</summary>
    public ulong FrameCount { get; private set; }

    /// <summary>
    /// Seconds one <see cref="Stage.FixedUpdate"/> step covers.
    /// </summary>
    /// <remarks>
    /// A constant, set by <see cref="Config.FixedHz"/> and defaulting to Bevy's 64 Hz, not a
    /// reading that varies with the frame. That is the point. Integrate with this and the same
    /// inputs give the same results on any machine, where integrating with
    /// <see cref="DeltaSeconds"/> ties the result to how fast the frame happened to be.
    /// </remarks>
    public double FixedDeltaSeconds { get; private set; }

    /// <summary>Instantaneous frames per second for the last frame.</summary>
    public double Fps => DeltaSeconds > 0.0 ? 1.0 / DeltaSeconds : 0.0;

    /// <summary>
    /// Frames per second smoothed with an exponential moving average, which suits a HUD, because
    /// the raw value is far too jittery to read.
    /// </summary>
    public double SmoothedFps { get; private set; }

    /// <summary><see cref="DeltaSeconds"/> as a float, the usual type in gameplay maths.</summary>
    public float Delta => (float)DeltaSeconds;

    /// <summary><see cref="ElapsedSeconds"/> as a float.</summary>
    public float Elapsed => (float)ElapsedSeconds;

    /// <summary><see cref="FixedDeltaSeconds"/> as a float, for a fixed-step behavior.</summary>
    public float FixedDelta => (float)FixedDeltaSeconds;

    /// <summary>Copies a native snapshot into this frame's state.</summary>
    internal void Update(in NativeTime snapshot)
    {
        ElapsedSeconds = snapshot.ElapsedSeconds;
        DeltaSeconds = snapshot.DeltaSeconds;
        RawDeltaSeconds = snapshot.RawDeltaSeconds;
        FrameCount = snapshot.FrameCount;
        FixedDeltaSeconds = snapshot.FixedDeltaSeconds;

        var instantaneous = Fps;
        SmoothedFps = SmoothedFps <= 0.0
            ? instantaneous
            : SmoothedFps + (instantaneous - SmoothedFps) * FpsSmoothing;

        // Read after Bevy has advanced the clock for this frame, so the frame a step was asked for
        // in is followed by exactly the frames it asked for, each with a delta, and then a stop.
        if (_stepping > 0 && --_stepping == 0) Pause();
    }

    /// <summary>
    /// How far the fixed clock has run past its last <see cref="Stage.FixedUpdate"/> step, as a
    /// share of a step, from zero up to less than one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy runs as many fixed steps in a frame as the time gone has room for, and what is left
    /// over waits for the next frame. Something moved in fixed steps and drawn every frame is drawn
    /// this far between its place before the last step and its place after it, which keeps it
    /// smooth when the frame rate and the step rate differ:
    /// </para>
    /// <code>
    /// var drawn = body.Previous + (body.Current - body.Previous) * ctx.Time.FixedOverstep;
    /// </code>
    /// <para>
    /// Read from the engine when asked, since it is right only once the frame's fixed steps have
    /// run, which is after the top of the frame the rest of this class is taken at, so a system in
    /// <see cref="Stage.Update"/> reads this frame's. Zero where there is no engine to ask.
    /// </para>
    /// </remarks>
    public float FixedOverstep
    {
        get
        {
            float fraction;
            return Native.bcs_time_fixed_overstep(&fraction) == 0 ? fraction : 0f;
        }
    }

    /// <summary>Whether the game's clock is stopped.</summary>
    /// <remarks>
    /// <para>
    /// Bevy's virtual clock, which <see cref="DeltaSeconds"/> and the fixed timestep both come
    /// from, so a paused game reads a delta of zero and runs no <see cref="Stage.FixedUpdate"/>,
    /// while everything else, the window, the interface and the frame counter, goes on. A game's
    /// pause screen and a debugger stopping the world are the same switch.
    /// </para>
    /// <para>
    /// Read from the engine when asked rather than mirrored each frame, since most frames nobody
    /// asks. False where there is no engine to ask.
    /// </para>
    /// </remarks>
    public bool Paused
    {
        get
        {
            int paused;
            return Native.bcs_time_virtual(&paused, null) == 0 && paused != 0;
        }
    }

    /// <summary>How many seconds of game time pass for each second of the wall's, one by default.</summary>
    public float Speed
    {
        get
        {
            float speed;
            return Native.bcs_time_virtual(null, &speed) == 0 ? speed : 1f;
        }
    }

    /// <summary>
    /// Seconds each frame advances the clock by, or zero where it reads the machine's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Starts at <see cref="Config.FrameSeconds"/> and changes from the next frame. A test sets it
    /// for one frame to make a slow frame on purpose, and sets it back. Zero lets the machine's
    /// clock run again from where the set clock had reached, so the game's time goes on from there
    /// whether the frames had run ahead of the machine or behind it.
    /// </para>
    /// <para>
    /// Read from the engine when asked, and zero where there is no engine to ask.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value set is negative or not a number.</exception>
    public double FrameSeconds
    {
        get
        {
            double seconds;
            return Native.bcs_time_frame_seconds(&seconds) == 0 ? seconds : 0.0;
        }
        set
        {
            if (!(value >= 0.0) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "A frame lasts zero seconds, for the machine's clock, or a length more than zero.");

            Native.Check(Native.bcs_time_set_frame_seconds(value), "Time.FrameSeconds");
        }
    }

    /// <summary>Stops the game's clock, from the next frame.</summary>
    public void Pause()
    {
        _stepping = 0;
        Native.Check(Native.bcs_time_set_virtual(1, -1f), "Time.Pause");
    }

    /// <summary>Starts the game's clock again, from the next frame.</summary>
    public void Resume()
    {
        _stepping = 0;
        Native.Check(Native.bcs_time_set_virtual(0, -1f), "Time.Resume");
    }

    /// <summary>
    /// Runs the clock for a number of frames and stops it again, to watch a paused game move a
    /// frame at a time.
    /// </summary>
    /// <param name="frames">How many frames to run, at least one.</param>
    /// <remarks>
    /// Each frame stepped through has the delta it would have had, at <see cref="Speed"/>, so a
    /// step is the game as it plays, slowed to the pace of whoever presses it.
    /// </remarks>
    public void Step(int frames = 1)
    {
        Native.Check(Native.bcs_time_set_virtual(0, -1f), "Time.Step");
        _stepping = Math.Max(1, frames);
    }

    /// <summary>Sets how fast the game's clock runs against the wall's.</summary>
    /// <param name="speed">Seconds of game time a second, zero or more. Two is double speed.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="speed"/> is negative or not a number.</exception>
    public void SetSpeed(float speed)
    {
        if (!(speed >= 0f)) throw new ArgumentOutOfRangeException(nameof(speed), speed, "A clock runs forward, at zero or more.");

        Native.Check(Native.bcs_time_set_virtual(Paused ? 1 : 0, speed), "Time.SetSpeed");
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"Time(frame={FrameCount}, elapsed={ElapsedSeconds:F2}s, delta={DeltaSeconds * 1000:F2}ms, "
        + $"fps={SmoothedFps:F1})";
}
