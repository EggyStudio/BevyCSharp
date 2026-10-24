namespace Bevy;

/// <summary>
/// How much global illumination is worked out, from none to the full set of cascades of probes, as
/// 3DEngine's levels are (<see cref="Render.SetGlobalIllumination"/>).
/// </summary>
internal enum GlobalIllumination
{
    /// <summary>None, the light Bevy's lighting gives alone.</summary>
    Off,

    /// <summary>Two cascades of probes, 64 directions each, for a slow GPU.</summary>
    Low,

    /// <summary>Three cascades, 64, 64 and 256 directions.</summary>
    Medium,

    /// <summary>Four cascades, 64 directions in the first and 256 in each after.</summary>
    High,
}
