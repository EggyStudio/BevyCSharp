namespace Bevy;

/// <summary>How a <see cref="ScatteringTerm"/> thins with height.</summary>
public enum ScatteringFalloff
{
    /// <summary>Evenly, from densest at the ground to none where the air ends.</summary>
    Linear = 0,

    /// <summary>Quickly near the ground and slowly above, as a gas does, by <see cref="ScatteringTerm.FalloffScale"/>.</summary>
    Exponential = 1,

    /// <summary>
    /// A layer densest at <see cref="ScatteringTerm.TentCenter"/> and thinning to none
    /// <see cref="ScatteringTerm.TentWidth"/> wide, as ozone lies.
    /// </summary>
    Tent = 2,
}
