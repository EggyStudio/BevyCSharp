namespace Bevy;

/// <summary>Whether a <see cref="GameTimer"/> runs out once or starts again each time it does.</summary>
public enum TimerMode
{
    /// <summary>Runs out once and stays finished until it is reset.</summary>
    Once,

    /// <summary>Starts again each time it runs out, keeping what the tick ran past.</summary>
    Repeating,
}
