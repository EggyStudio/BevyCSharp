namespace Bevy;

/// <summary>
/// Bevy's <c>Timer</c>, counting toward a duration as it is ticked, once or over and over, as a value
/// a behavior keeps on its entity.
/// </summary>
/// <remarks>
/// <para>
/// Named apart from Bevy's, since <c>System.Threading.Timer</c> is among the names a C# program
/// imports by default, and the two would be ambiguous in every game. A struct, so a cooldown on a button or a frame's time on a sprite is a field of the behavior on
/// that entity, as it is a component's field in Bevy. <see cref="Tick"/> changes the field it is
/// called on, so it is called on the field itself, <c>Cooldown.Tick(ctx.Time.Delta)</c> inside the
/// behavior's own method, and not on a copy taken out of it.
/// </para>
/// <para>
/// Ticked as Bevy ticks its own. A paused one does not move, a finished one that runs once stays
/// at its duration, and a repeating one starts again with what the tick ran past, counting the times
/// it ran out in that tick, so a long frame over a short timer misses none.
/// </para>
/// </remarks>
public struct GameTimer : IEquatable<GameTimer>
{
    /// <summary>How long it runs, in seconds.</summary>
    public float Duration;

    /// <summary>Whether it runs out once or over and over.</summary>
    public TimerMode Mode;

    /// <summary>How far it has run since it started or last started again, in seconds.</summary>
    public float Elapsed;

    /// <summary>Whether a tick leaves it where it is.</summary>
    public bool Paused;

    private bool _finished;
    private int _timesFinishedThisTick;

    /// <summary>A timer of <paramref name="seconds"/> that runs once or over and over, as Bevy's <c>Timer::from_seconds</c>.</summary>
    public static GameTimer FromSeconds(float seconds, TimerMode mode) => new() { Duration = seconds, Mode = mode };

    /// <summary>
    /// Whether it has run out, which a timer that runs once stays, and a repeating one is on the
    /// tick it ran out in alone.
    /// </summary>
    public readonly bool Finished => _finished;

    /// <summary>Whether the last tick ran it out.</summary>
    public readonly bool JustFinished => _timesFinishedThisTick > 0;

    /// <summary>How many times the last tick ran it out, which for a repeating one may be more than one.</summary>
    public readonly int TimesFinishedThisTick => _timesFinishedThisTick;

    /// <summary>How much of its duration has run, from zero to one, and one for a timer of no duration.</summary>
    public readonly float Fraction => Duration == 0f ? 1f : Elapsed / Duration;

    /// <summary>How long it has left to run, in seconds.</summary>
    public readonly float Remaining => Duration - Elapsed;

    /// <summary>
    /// Runs it on by <paramref name="delta"/> seconds, and answers it as it is after, so
    /// <c>timer.Tick(delta).JustFinished</c> reads as Bevy's <c>timer.tick(delta).just_finished()</c>.
    /// </summary>
    /// <param name="delta">How long to run it, as <see cref="Time.Delta"/> gives a frame's.</param>
    public GameTimer Tick(float delta)
    {
        if (Paused)
        {
            _timesFinishedThisTick = 0;
            if (Mode == TimerMode.Repeating) _finished = false;
            return this;
        }

        if (Mode != TimerMode.Repeating && _finished)
        {
            _timesFinishedThisTick = 0;
            return this;
        }

        Elapsed += delta;
        _finished = Elapsed >= Duration;

        if (!_finished)
        {
            _timesFinishedThisTick = 0;
        }
        else if (Mode == TimerMode.Repeating)
        {
            if (Duration > 0f)
            {
                _timesFinishedThisTick = (int)MathF.Floor(Elapsed / Duration);
                Elapsed -= _timesFinishedThisTick * Duration;
            }
            else
            {
                (_timesFinishedThisTick, Elapsed) = (int.MaxValue, 0f);
            }
        }
        else
        {
            (_timesFinishedThisTick, Elapsed) = (1, Duration);
        }

        return this;
    }

    /// <summary>Stops it where it is until <see cref="Unpause"/>.</summary>
    public void Pause() => Paused = true;

    /// <summary>Lets it run again from where it was paused.</summary>
    public void Unpause() => Paused = false;

    /// <summary>Starts it again from nothing, unfinished, its duration and mode kept.</summary>
    public void Reset() => (Elapsed, _finished, _timesFinishedThisTick) = (0f, false, 0);

    /// <inheritdoc/>
    public readonly bool Equals(GameTimer other) =>
        Duration == other.Duration && Mode == other.Mode && Elapsed == other.Elapsed && Paused == other.Paused
        && _finished == other._finished && _timesFinishedThisTick == other._timesFinishedThisTick;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is GameTimer other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => HashCode.Combine(Duration, Mode, Elapsed, Paused, _finished, _timesFinishedThisTick);

    /// <summary>Compares two timers.</summary>
    public static bool operator ==(GameTimer a, GameTimer b) => a.Equals(b);

    /// <summary>Compares two timers.</summary>
    public static bool operator !=(GameTimer a, GameTimer b) => !a.Equals(b);
}
