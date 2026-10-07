namespace Bevy;

/// <summary>What happens when a sound reaches its end.</summary>
public enum PlaybackMode
{
    /// <summary>Play through and stop, leaving the entity in place.</summary>
    Once = 0,

    /// <summary>Start again, for music and ambience.</summary>
    Loop = 1,

    /// <summary>
    /// Play through, then despawn the entity.
    /// </summary>
    /// <remarks>
    /// Suits a one-shot effect. Nothing has to remember to clean it up, and a game firing hundreds
    /// of them does not accumulate entities.
    /// </remarks>
    Despawn = 2,
}
