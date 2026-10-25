namespace Bevy;

/// <summary>How finely the clouds are marched. See <see cref="CloudSettings.Quality"/>.</summary>
public enum CloudQuality
{
    /// <summary>48 steps along a way and 4 toward the sun.</summary>
    Low = 0,

    /// <summary>72 steps along a way and 6 toward the sun.</summary>
    Medium = 1,

    /// <summary>128 steps along a way and 6 toward the sun.</summary>
    High = 2,
}
