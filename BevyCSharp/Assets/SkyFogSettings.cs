namespace Bevy;

/// <summary>
/// The fog <see cref="Render.SetSkyFog"/> draws over a camera's picture, in the color the sky has at
/// the horizon along each way.
/// </summary>
/// <remarks>
/// <para>
/// The fog thickens as a layer over the ground, its density falling by e every
/// <see cref="Height"/> above <see cref="Base"/>, so a hill's top stands clear of a valley's haze, and
/// at its base it takes away 95% of a point's light <see cref="Visibility"/> away. Its color along
/// each way is the sky's a few degrees above the horizon there, read from the camera's environment
/// map, which an atmosphere lights (<see cref="Render.SetSkyLighting"/>), so a building far off
/// fades into the sky behind it, warm toward the sun and blue away from it.
/// </para>
/// <para>
/// Below the horizon, where the camera draws nothing, the sky takes the horizon's color along each
/// way and darkens toward <see cref="GroundShade"/> of it over <see cref="BandDegrees"/>, in place of
/// the even gray an atmosphere draws there for the planet's ground.
/// </para>
/// </remarks>
public sealed class SkyFogSettings
{
    /// <summary>How far the fog takes away 95% of a point's light at the layer's base, in world units, 600 unless set.</summary>
    public float Visibility { get; set; } = 600f;

    /// <summary>How far from the eye the fog starts, in world units, at the eye unless set.</summary>
    public float Start { get; set; }

    /// <summary>How far up the layer's density falls by e, in world units, 80 unless set.</summary>
    public float Height { get; set; } = 80f;

    /// <summary>The height of the layer's base, where it is densest, 0 unless set.</summary>
    public float Base { get; set; }

    /// <summary>
    /// How many degrees above the horizon the fog's color is read, a way's own height held between
    /// the horizon and this, 3 unless set.
    /// </summary>
    /// <remarks>
    /// The sky brightens toward the horizon and the sun, so a few degrees up reads the haze a far
    /// building stands in rather than the brightest line at the horizon itself.
    /// </remarks>
    public float HorizonDegrees { get; set; } = 3f;

    /// <summary>The share of the horizon's color left at the bottom of the band below the horizon, 0.35 unless set.</summary>
    public float GroundShade { get; set; } = 0.35f;

    /// <summary>Over how many degrees below the horizon the band darkens to <see cref="GroundShade"/>, 25 unless set.</summary>
    public float BandDegrees { get; set; } = 25f;

    /// <summary>
    /// The fog's own color in place of the sky's, linear, times <see cref="Brightness"/>, or none to
    /// read the sky's, which a camera with no environment map cannot.
    /// </summary>
    public (float R, float G, float B)? Color { get; set; }

    /// <summary>How bright <see cref="Color"/> is, in nits, the camera's exposure taking it as it takes the scene, 5,000 unless set.</summary>
    public float Brightness { get; set; } = 5000f;
}
