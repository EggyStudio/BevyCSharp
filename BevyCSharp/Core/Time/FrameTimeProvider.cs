namespace Bevy;

/// <summary>
/// The app's clock as a <see cref="TimeProvider"/>, whose timers fire from the frame loop as the
/// clock passes them, which <see cref="Time.Provider"/> hands out.
/// </summary>
/// <remarks>
/// <para>
/// The time is the game's, <see cref="Time.ElapsedSeconds"/>, so it stands still while the game
/// is paused and runs at its speed, and a run on a set clock, as a capture is, passes the same time
/// each frame on any device. A timer is kept until the frame whose clock reaches it, and its
/// callback runs there, on the thread the frame runs on, as <see cref="Time"/> is refreshed and
/// before any of the frame's systems. So a task awaiting <c>Task.Delay</c> on this provider goes on
/// in that frame, and what it hands over is there for the frame's systems to find.
/// </para>
/// <para>
/// A periodic timer fires once a frame at most, its next time a period after the last one it was
/// due, so a frame that passes several periods fires it once and the rest follow in the frames
/// after.
/// </para>
/// </remarks>
internal sealed class FrameTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<FrameTimer> _timers = [];
    private readonly DateTimeOffset _started = TimeProvider.System.GetUtcNow();
    private long _ticks;

    /// <summary>The game's seconds since the app started, as a count of ticks.</summary>
    internal TimeSpan Elapsed => TimeSpan.FromTicks(Interlocked.Read(ref _ticks));

    /// <inheritdoc/>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc/>
    public override long GetTimestamp() => Interlocked.Read(ref _ticks);

    /// <inheritdoc/>
    /// <remarks>The moment the app started, on the machine's clock, and the game's time since.</remarks>
    public override DateTimeOffset GetUtcNow() => _started + Elapsed;

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var timer = new FrameTimer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    /// <summary>
    /// Moves the clock to the game's time of this frame and fires every timer it has reached.
    /// </summary>
    internal void Advance(double elapsedSeconds)
    {
        var now = TimeSpan.FromSeconds(elapsedSeconds);
        Interlocked.Exchange(ref _ticks, now.Ticks);

        List<FrameTimer>? due = null;
        lock (_gate)
        {
            foreach (var timer in _timers)
            {
                if (timer.Due is { } at && at <= now) (due ??= []).Add(timer);
            }

            foreach (var timer in due ?? [])
            {
                // Once, or again a period on, which a frame past several periods fires once.
                timer.Due = timer.Period is { } period ? timer.Due + period : null;
                if (timer.Due is null) _timers.Remove(timer);
            }
        }

        // Outside the lock, since a callback may start or change a timer of its own.
        foreach (var timer in due ?? []) timer.Fire();
    }

    private void Schedule(FrameTimer timer, TimeSpan dueTime, TimeSpan period)
    {
        lock (_gate)
        {
            _timers.Remove(timer);
            if (dueTime == Timeout.InfiniteTimeSpan) return;

            ArgumentOutOfRangeException.ThrowIfLessThan(dueTime, TimeSpan.Zero);
            timer.Due = Elapsed + dueTime;
            timer.Period = period == Timeout.InfiniteTimeSpan || period == TimeSpan.Zero ? null : period;
            _timers.Add(timer);
        }
    }

    private void Forget(FrameTimer timer)
    {
        lock (_gate) _timers.Remove(timer);
    }

    /// <summary>One timer of the provider's, due at a game time.</summary>
    private sealed class FrameTimer(FrameTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        /// <summary>When it fires next, or nothing while it is stopped.</summary>
        internal TimeSpan? Due { get; set; }

        /// <summary>How long after each firing it fires again, or nothing for once.</summary>
        internal TimeSpan? Period { get; set; }

        internal void Fire() => callback(state);

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            owner.Schedule(this, dueTime, period);
            return true;
        }

        public void Dispose() => owner.Forget(this);

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
