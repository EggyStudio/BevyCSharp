namespace Bevy;

/// <summary>
/// How much global illumination is worked out, from none to the full set of cascades of probes
/// (<see cref="RenderGlobalIllumination.extension(Render).SetGlobalIllumination"/>).
/// </summary>
public enum GlobalIllumination
{
    /// <summary>None, the light Bevy's lighting gives alone.</summary>
    Off,

    /// <summary>Two cascades of probes, 64 directions each, for a slow GPU.</summary>
    Low,

    /// <summary>Four cascades, 64 directions in the first two and 256 in the two after.</summary>
    Medium,

    /// <summary>Four cascades, 64 directions in the first and 256 in each after.</summary>
    High,
}
