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
    /// <para>
    /// Suits a one-shot effect. Nothing has to remember to clean it up, and a game firing hundreds
    /// of them does not accumulate entities.
    /// </para>
    /// <para>
    /// Played to a device, such a sound is played once and despawned at its end by the bridge
    /// rather than by Bevy, whose despawn of it never gives the entity's index back to the world,
    /// so a game playing effects for an hour would climb to more indices than it ever holds.
    /// Bevy's <c>PlaybackSettings</c> on it, read through the reflected wrappers, say <c>Once</c>
    /// for that reason.
    /// </para>
    /// </remarks>
    Despawn = 2,
}
