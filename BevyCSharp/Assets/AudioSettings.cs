namespace Bevy;

/// <summary>How a sound should be played.</summary>
public sealed class AudioSettings
{
    /// <summary>What happens when it reaches the end.</summary>
    public PlaybackMode Mode { get; set; } = PlaybackMode.Once;

    /// <summary>Loudness, where 1 is the sound as recorded and 0 is silence.</summary>
    public float Volume { get; set; } = 1f;

    /// <summary>
    /// The bus it plays on, such as <c>"music"</c> or <c>"effects"</c>, or none. See
    /// <see cref="Audio.SetBusVolume"/>.
    /// </summary>
    /// <remarks>
    /// A bus is a name and a volume, and a sound on one is heard at its own volume times the bus's,
    /// so a settings screen's music slider is one call rather than a walk over every sound playing.
    /// A bus nobody has set is at one.
    /// </remarks>
    public string? Bus { get; set; }

    /// <summary>
    /// Playback rate. 1 is as recorded.
    /// </summary>
    /// <remarks>
    /// Changes pitch with it, because it resamples rather than time-stretches. Slight variation on
    /// a repeated effect keeps it from sounding mechanical.
    /// </remarks>
    public float Speed { get; set; } = 1f;

    /// <summary>Start paused, to be released later with <see cref="Audio.Resume"/>.</summary>
    public bool Paused { get; set; }

    /// <summary>
    /// Place the sound in the world rather than in both ears equally.
    /// </summary>
    /// <remarks>
    /// A spatial sound is heard from where its entity's <see cref="Transform"/> is, quieter with
    /// distance and further to one side as it moves across. It takes two things: this, and an
    /// entity to hear from, which <see cref="Audio.SetListener(Entity, float)"/> nominates.
    /// </remarks>
    public bool Spatial { get; set; }

    /// <summary>
    /// Scale applied to the distance between the sound and the listener.
    /// </summary>
    /// <remarks>
    /// A world measured in meters needs nothing here. One measured in pixels does, because a sound
    /// a hundred units away would otherwise be inaudible, and a scale of <c>0.01</c> makes that a
    /// meter. Zero leaves Bevy's own scale in place.
    /// </remarks>
    public float SpatialScale { get; set; }

    /// <summary>
    /// Where in the clip to start, in seconds. Zero starts at the beginning.
    /// </summary>
    /// <remarks>
    /// With <see cref="Play"/> this is how one file holds several effects. A sheet of footsteps or
    /// gunshots is cut by naming where each one begins and how long it runs, which costs one decode
    /// rather than one file each.
    /// </remarks>
    public float Start { get; set; }

    /// <summary>How much of the clip to play from there, in seconds. Zero plays to the end.</summary>
    public float Play { get; set; }

    /// <summary>Plays once and cleans up after itself.</summary>
    public static AudioSettings Effect => new() { Mode = PlaybackMode.Despawn };

    /// <summary>Loops quietly, for music.</summary>
    public static AudioSettings Music => new() { Mode = PlaybackMode.Loop, Volume = 0.5f };
}
