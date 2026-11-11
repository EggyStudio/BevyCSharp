namespace Bevy;

/// <summary>How many drops and flakes fall at the heaviest. See <see cref="PrecipitationSettings.Quality"/>.</summary>
public enum PrecipitationQuality
{
    /// <summary>16,384 at the heaviest.</summary>
    Low = 0,

    /// <summary>32,768 at the heaviest.</summary>
    Medium = 1,

    /// <summary>65,536 at the heaviest.</summary>
    High = 2,
}
