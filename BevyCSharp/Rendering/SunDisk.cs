namespace Bevy;

/// <summary>
/// The disk of the sun a directional light stands for, which an atmosphere draws in its sky, as
/// Bevy's <c>SunDisk</c> describes it.
/// </summary>
/// <param name="AngularSize">How wide it looks from the scene, as an angle in radians.</param>
/// <param name="Intensity">
/// How bright it is against the brightness Bevy draws it at, one for that, above one to overexpose
/// it, and zero to draw no disk at all while the light still scatters through the air.
/// </param>
/// <remarks>
/// It changes how the sun looks and nothing it lights, neither the light's brightness nor its
/// shadows. A light given none is drawn with <see cref="Earth"/>, so a sky of Mars gives its sun
/// <see cref="Mars"/>, which looks smaller from farther away. <see cref="Render.SetSunDisk"/> puts
/// one on a light.
/// </remarks>
public readonly record struct SunDisk(float AngularSize, float Intensity = 1f)
{
    /// <summary>The sun as it looks from earth, about half a degree across.</summary>
    public static SunDisk Earth => new(0.00930842f);

    /// <summary>The sun as it looks from Mars, two thirds as wide as from earth.</summary>
    public static SunDisk Mars => new(0.00615f);

    /// <summary>No disk, the light still lighting the sky and the scene.</summary>
    public static SunDisk Off => new(0f, 0f);
}
