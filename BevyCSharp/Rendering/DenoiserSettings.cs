namespace Bevy;

/// <summary>
/// What the bridge's denoiser over ray-traced lighting filters by, given to
/// <see cref="Render.SetRayTracedDenoiser"/>, at the defaults of the filter it is.
/// </summary>
/// <remarks>
/// <para>
/// The denoiser is spatiotemporal variance-guided filtering (Schied and others, 2017), and these are
/// its four numbers with the paper's defaults. The history's clamp says how many frames a pixel
/// averages at most, and the three widths say how different a neighboring pixel may be, in depth,
/// in the way it faces and in brightness, before each pass of the filter stops taking from it.
/// </para>
/// <para>
/// Each setting trades the same two things, a calmer picture against a sharper one that follows
/// change sooner. A longer history and wider widths take more from the past and from neighbors,
/// so the picture is quieter and a light that moves leaves a longer wake, and shadow edges and
/// small detail blur. The defaults are where the paper set them for scenes like a game's.
/// </para>
/// </remarks>
public sealed class DenoiserSettings
{
    /// <summary>
    /// How many frames of history a pixel keeps at most, 32 by default and at least 1.
    /// </summary>
    /// <remarks>
    /// Each frame adds the new light as one part in as many frames as the history holds, up to
    /// this, so a still view grows quieter for this many frames and a light that changes is followed
    /// within as many. One keeps no history, leaving the filter in space alone.
    /// </remarks>
    public int HistoryLimit { get; set; } = 32;

    /// <summary>
    /// How far a neighbor's depth may differ from the middle's, against the depth's change across
    /// the screen, 1 by default. The paper's sigma z.
    /// </summary>
    public float DepthWidth { get; set; } = 1f;

    /// <summary>
    /// The power a neighbor's agreement in the way it faces is raised to, 128 by default. The
    /// paper's sigma n.
    /// </summary>
    /// <remarks>
    /// Higher is stricter, so the filter stops sooner at a corner. At 128 a surface turned ten
    /// degrees from the middle's weighs about a seventh.
    /// </remarks>
    public float NormalPower { get; set; } = 128f;

    /// <summary>
    /// How far a neighbor's brightness may differ from the middle's, against the noise the filter
    /// has measured there, 4 by default. The paper's sigma l.
    /// </summary>
    /// <remarks>
    /// Wider blurs more of what is left of the noise and more of a shadow's edge with it.
    /// </remarks>
    public float LuminanceWidth { get; set; } = 4f;
}
