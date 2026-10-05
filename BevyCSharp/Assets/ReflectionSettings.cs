namespace Bevy;

/// <summary>How a camera's screen-space reflections are traced. The defaults are Bevy's.</summary>
public sealed class ReflectionSettings
{
    /// <summary>
    /// The roughness at which reflections start to appear and at which they are whole. Smoother
    /// than the first, a surface gets none, which is Bevy's choice, because a mirror-smooth surface
    /// shows the flaws of a screen-space trace most, and is left to a reflection probe.
    /// </summary>
    public (float Start, float Full) FadeInRoughness { get; set; } = (0.08f, 0.12f);

    /// <summary>The roughness at which reflections start to fade and at which they are gone.</summary>
    public (float Start, float Gone) FadeOutRoughness { get; set; } = (0.55f, 0.6f);

    /// <summary>
    /// Where reflections stop at the edge of the picture and where they are whole, as fractions of
    /// it, which hides the edge of what can be reflected.
    /// </summary>
    public (float Gone, float Full) EdgeFade { get; set; } = (0f, 0f);

    /// <summary>
    /// How thick what the depth buffer holds is taken to be, in world units, since a picture's
    /// depth says where a surface starts but not where it ends.
    /// </summary>
    public float Thickness { get; set; } = 0.25f;

    /// <summary>Steps of the first march along the ray.</summary>
    public uint Steps { get; set; } = 10;

    /// <summary>How the steps spread out: one for even steps, more for finer ones near the start.</summary>
    public float StepExponent { get; set; } = 1f;

    /// <summary>Steps of the search that narrows a hit down.</summary>
    public uint RefineSteps { get; set; } = 5;

    /// <summary>Whether a hit is refined once more by where the ray and the surface cross.</summary>
    public bool Secant { get; set; } = true;
}
