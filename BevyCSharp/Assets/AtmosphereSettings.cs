namespace Bevy;

/// <summary>
/// A sky computed from sunlight scattering through the air.
/// </summary>
/// <remarks>
/// <para>
/// Not a picture of a sky but a simulation of one. The color of every direction is worked out
/// from how far light travels through the air to reach it, so the horizon reddens, the zenith
/// stays blue, and the whole thing turns over as the sun moves. Distant geometry picks up the
/// same haze.
/// </para>
/// <para>
/// The sun is whichever directional light is in the scene, so its direction and color move the sky.
/// A scene with no directional light gets a night sky.
/// </para>
/// </remarks>
public sealed class AtmosphereSettings
{
    /// <summary>
    /// How thick the air is, as a multiple of earth's.
    /// </summary>
    /// <remarks>
    /// Above one for a hazier world, below one for a thinner and darker sky. One is earth.
    /// </remarks>
    public float Density { get; set; } = 1f;

    /// <summary>
    /// How large the planet is against the scene, for a world not measured in meters.
    /// </summary>
    /// <remarks>
    /// The planet is the size of a real one and its ground sits at the origin, so a scene in meters
    /// needs nothing here. A scene in kilometers needs a smaller number, since what matters is how
    /// far the camera moves through the air.
    /// </remarks>
    public float Scale { get; set; } = 1f;

    /// <summary>
    /// How far in front of the camera the haze is computed, in meters.
    /// </summary>
    /// <remarks>
    /// What decides where distant geometry fades into the sky. Zero leaves Bevy's own distance,
    /// which suits a scene measured in meters.
    /// </remarks>
    public float HazeDistance { get; set; }

    /// <summary>
    /// How finely the sky is computed.
    /// </summary>
    /// <remarks>
    /// One number rather than the dozen Bevy exposes, because every one of them trades the same
    /// thing and setting them apart is tuning a renderer rather than describing a sky. The sky is
    /// the same either way, and what changes is banding in a gradient and how much of a frame it
    /// costs.
    /// </remarks>
    public SkyQuality Quality { get; set; } = SkyQuality.Default;

    /// <summary>
    /// How much light the ground bounces back into the air, from zero to one.
    /// </summary>
    /// <remarks>
    /// What makes the underside of the haze bright over snow and dark over sea. Earth's is 0.3 and
    /// Mars's is 0.1. Zero leaves the one that belongs to the medium.
    /// </remarks>
    public float GroundAlbedo { get; set; }
}
