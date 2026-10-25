namespace Bevy;

/// <summary>Which part of the day the sun's height makes it. See <see cref="SkyClock.Phase"/>.</summary>
public enum DayPhase
{
    /// <summary>The sun more than twelve degrees under the horizon, the sky dark.</summary>
    Night = 0,

    /// <summary>The sun between twelve degrees under the horizon and six above it, in the morning.</summary>
    Dawn = 1,

    /// <summary>The sun more than six degrees above the horizon.</summary>
    Day = 2,

    /// <summary>The sun between six degrees above the horizon and twelve under it, in the evening.</summary>
    Dusk = 3,
}
