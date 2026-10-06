namespace Bevy;

/// <summary>How a clip is played, for <see cref="Animation.Play"/>.</summary>
public sealed class AnimationSettings
{
    /// <summary>Whether it plays over and over, rather than once and holding its last pose.</summary>
    public bool Repeat { get; set; }

    /// <summary>
    /// How many times it plays before holding its last pose, where it does not repeat. Zero and one
    /// are once.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>RepeatAnimation::Count</c>, for a gesture made twice or a bell rung three times.
    /// <see cref="Animation.SetRepeat"/> changes it while the clip plays, without starting it over.
    /// </remarks>
    public uint Times { get; set; }

    /// <summary>How fast, one being as it was made, two twice as fast, and below zero backwards.</summary>
    public float Speed { get; set; } = 1f;

    /// <summary>
    /// Seconds over which whatever played before fades out as this fades in, or zero for a cut.
    /// </summary>
    /// <remarks>
    /// A walk turning into a run over a fifth of a second keeps a character from snapping between
    /// poses. Both clips play during the fade, each weighted by how far it has gone.
    /// </remarks>
    public float Blend { get; set; }
}
