namespace Bevy;

/// <summary>How many samples a pixel of Bevy's ambient occlusion takes.</summary>
public enum AmbientOcclusionQuality
{
    /// <summary>Four, plus what temporal antialiasing adds.</summary>
    Low = 0,

    /// <summary>Eight.</summary>
    Medium = 1,

    /// <summary>Eighteen, which is Bevy's own choice.</summary>
    High = 2,

    /// <summary>Fifty-four.</summary>
    Ultra = 3,
}
