namespace Bevy;

/// <summary>How a camera filters the shadow maps it reads. See <see cref="Render.SetShadowFiltering"/>.</summary>
public enum ShadowFiltering
{
    /// <summary>Two by two texels, filtered by the hardware. Cheapest and blockiest.</summary>
    Hardware2x2 = 0,

    /// <summary>A Gaussian over a wider area, which is Bevy's default.</summary>
    Gaussian = 1,

    /// <summary>
    /// A pattern that changes every frame, for temporal antialiasing to average into a smooth
    /// edge. The quietest with soft shadows, and noisy without temporal antialiasing.
    /// </summary>
    Temporal = 2,
}
