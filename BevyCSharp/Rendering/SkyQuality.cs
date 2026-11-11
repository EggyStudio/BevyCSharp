namespace Bevy;

/// <summary>How finely the sky is computed.</summary>
/// <remarks>
/// Every one of these draws the same sky. What changes is how smooth a gradient across it is and
/// how much of a frame it costs, so this is a graphics setting rather than an artistic one.
/// </remarks>
public enum SkyQuality
{
    /// <summary>What Bevy chose, which suits most scenes.</summary>
    Default = 0,

    /// <summary>Half the samples, for a machine that needs the frame back.</summary>
    Cheap = 1,

    /// <summary>Twice the samples, for a sky that fills the screen and has to be smooth.</summary>
    Fine = 2,
}
