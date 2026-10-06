namespace Bevy;

/// <summary>
/// What a camera does to the picture after the scene has been drawn.
/// </summary>
/// <remarks>
/// One settings object for the whole pipeline rather than a call per effect, because these are
/// decided together. Bloom needs a high dynamic range target, and multisampling and an antialiasing
/// pass are two answers to the same question. Every field is applied on every call, so an effect
/// this object leaves off is taken off the camera. Turning bloom off is the same call as turning it
/// on, which suits a settings screen.
/// </remarks>
public sealed class PostSettings
{
    /// <summary>Which curve maps the rendered range onto the display.</summary>
    public Tonemapper Tonemapper { get; set; } = Tonemapper.TonyMcMapface;

    /// <summary>
    /// Dither before quantizing to the display's bit depth.
    /// </summary>
    /// <remarks>
    /// Hides the banding a smooth gradient shows otherwise, at the cost of a little noise. Bevy
    /// leaves this on, and so does this.
    /// </remarks>
    public bool Dither { get; set; } = true;

    /// <summary>
    /// Draw into a high dynamic range target.
    /// </summary>
    /// <remarks>
    /// What lets a highlight be brighter than white instead of clipping there, and what bloom
    /// reads to decide where to scatter. Costs memory and bandwidth, so it is off unless asked
    /// for.
    /// </remarks>
    public bool Hdr { get; set; }

    /// <summary>
    /// Samples per pixel taken while the scene is rasterized: 1, 2, 4 or 8.
    /// </summary>
    /// <remarks>
    /// Smooths the edges of geometry and nothing else. Four is Bevy's own; one turns it off, as a
    /// game leaning on <see cref="AntiAlias"/> does, and <see cref="AntiAliasPass.Temporal"/>
    /// requires it.
    /// <para>
    /// Cameras drawing to the same window or image draw into one picture, so they have to agree,
    /// and a mismatch is a validation error that ends the app. The bridge settles it each frame
    /// by giving them all the fewest samples any of them asks for, and logs once for each camera it
    /// lowers. A game that sets four here and sees one has another camera on the same target,
    /// often the interface's overlay, which always draws with one.
    /// </para>
    /// </remarks>
    public int Msaa { get; set; } = 4;

    /// <summary>An antialiasing pass over the finished picture.</summary>
    public AntiAliasPass AntiAlias { get; set; } = AntiAliasPass.None;

    /// <summary>
    /// How hard that pass looks for an edge.
    /// </summary>
    /// <remarks>
    /// Read by <see cref="AntiAliasPass.Fxaa"/> and <see cref="AntiAliasPass.Smaa"/>. Temporal
    /// antialiasing has no such setting, because how much it catches is decided by how many frames
    /// it has to work from.
    /// </remarks>
    public AntiAliasQuality Quality { get; set; } = AntiAliasQuality.Medium;

    /// <summary>
    /// Contrast adaptive sharpening, from 0 for none to 1 for as much as it does.
    /// </summary>
    /// <remarks>Puts back some of the crispness an antialiasing pass takes away.</remarks>
    public float Sharpen { get; set; }

    /// <summary>
    /// Scatter light out of the brightest parts of the picture.
    /// </summary>
    /// <remarks>
    /// Needs <see cref="Hdr"/> to have anything to work with, because without it nothing is
    /// brighter than white, so nothing is bright enough to glow. To make one object glow harder,
    /// raise its material's emissive color rather than this.
    /// </remarks>
    public bool Bloom { get; set; }

    /// <summary>How much light is scattered.</summary>
    public float BloomIntensity { get; set; } = 0.15f;

    /// <summary>Brightness a pixel has to reach before it blooms at all.</summary>
    /// <remarks>Zero blooms everything a little, which is the physically-minded choice.</remarks>
    public float BloomThreshold { get; set; }

    /// <summary>How gradually that threshold takes effect.</summary>
    public float BloomThresholdSoftness { get; set; }

    /// <summary>How the scattered light is mixed back in.</summary>
    public BloomMode BloomMode { get; set; } = BloomMode.EnergyConserving;

    /// <summary>High dynamic range with a gentle bloom over it.</summary>
    public static PostSettings Glow => new() { Hdr = true, Bloom = true };
}
