namespace Bevy;

/// <summary>Whose air <see cref="AtmosphereSettings"/> scatters light through.</summary>
/// <remarks>
/// Each is Bevy's own, a planet of its size with its air's makeup, from the ground to where the
/// air ends, so a sky changes as the real one would rather than by a tint. Mars's air needs a
/// picture of how its dust scatters light, <see cref="AtmosphereSettings.DustPhase"/>.
/// </remarks>
public enum SkyPlanet
{
    /// <summary>Earth's air, blue overhead and red at sunset.</summary>
    Earth = 0,

    /// <summary>Mars's thin dusty air, a hazy tan by day.</summary>
    Mars = 1,
}
