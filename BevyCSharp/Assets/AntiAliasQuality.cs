namespace Bevy;

/// <summary>How hard an antialiasing pass looks for an edge.</summary>
public enum AntiAliasQuality
{
    /// <summary>Fastest, and misses edges.</summary>
    Low = 0,

    /// <summary>The usual compromise.</summary>
    Medium = 1,

    /// <summary>Catches more, costs more.</summary>
    High = 2,

    /// <summary>As much as the pass can do.</summary>
    Ultra = 3,
}
