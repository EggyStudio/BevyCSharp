namespace Bevy;

/// <summary>
/// How a camera turns its linear picture into the one the screen shows, in place of Bevy's
/// tonemapper. See <see cref="Render.SetLook"/>.
/// </summary>
/// <remarks>
/// <para>
/// The light is multiplied by <see cref="Exposure"/>, brought under white by <see cref="Curve"/>,
/// saturated by <see cref="Saturation"/> and multiplied by <see cref="Tint"/> in linear light, then
/// pushed from the middle of what is shown by <see cref="Contrast"/> once encoded for the screen, in
/// that order, as 3DEngine's frame runs them, so the same numbers show the same picture in both
/// engines. A new one changes nothing about a picture whose every channel is under the knee.
/// </para>
/// <para>
/// Saturation in linear light moves a color away from the gray of its luminance as light mixes,
/// which keeps a dark color dark, and contrast once encoded moves a level about the middle gray the
/// eye sees as the middle, which deepens the shade without moving it.
/// </para>
/// </remarks>
public sealed class LookSettings
{
    /// <summary>The curve that brings light past white under it, <see cref="LookCurve.Knee"/> unless set.</summary>
    public LookCurve Curve { get; set; } = LookCurve.Knee;

    /// <summary>What the light is multiplied by before the curve, 1 unless set.</summary>
    /// <remarks>
    /// On top of the camera's own exposure (<see cref="Render.SetExposure"/>), which lights the
    /// scene's materials as they draw, so this brightens what Bevy's bloom and effects added too.
    /// </remarks>
    public float Exposure { get; set; } = 1f;

    /// <summary>The level past which <see cref="LookCurve.Knee"/> bends each channel toward white on its own, 0.8 unless set.</summary>
    public float Knee { get; set; } = 0.8f;

    /// <summary>
    /// How far <see cref="LookCurve.Knee"/> lets each channel past the knee go toward white on its own,
    /// from 0, where a color keeps its hue however bright, to 1, unless set.
    /// </summary>
    public float KneeStrength { get; set; } = 1f;

    /// <summary>What <see cref="LookCurve.Numbered"/> multiplies the light by first, 1.4 unless set.</summary>
    public float CurveExposure { get; set; } = 1.4f;

    /// <summary>How sharply <see cref="LookCurve.Numbered"/> brings light under white, 2 unless set, higher holding more of it near white.</summary>
    public float CurveWhite { get; set; } = 2f;

    /// <summary>The power <see cref="LookCurve.Numbered"/> raises the dark end by, 1.2 unless set, higher darkening the shadows.</summary>
    public float CurveLower { get; set; } = 1.2f;

    /// <summary>The power <see cref="LookCurve.Numbered"/> raises the bright end by, 1.5 unless set, higher darkening the highlights.</summary>
    public float CurveUpper { get; set; } = 1.5f;

    /// <summary>How far each color is moved from the gray of its luminance in linear light, after the curve, 1 unless set and 0 gray.</summary>
    public float Saturation { get; set; } = 1f;

    /// <summary>What each channel is multiplied by after the saturation, linear, white unless set.</summary>
    public (float R, float G, float B) Tint { get; set; } = (1f, 1f, 1f);

    /// <summary>How far each level shown is pushed from the middle once encoded for the screen, 1 unless set.</summary>
    public float Contrast { get; set; } = 1f;

    /// <summary>Whether the picture shown is dithered by half a level, as Bevy's tonemapper does, so a gradient does not band, on unless set.</summary>
    public bool Dither { get; set; } = true;

    /// <summary>
    /// A vivid look, <see cref="LookCurve.Numbered"/> at an exposure of 0.9, a white of 1.3 and
    /// powers of 1.05 and 1, the light raised by 1.6 before it, a saturation of 1.4 and a contrast
    /// of 1.12.
    /// </summary>
    /// <remarks>
    /// The look 3DEngine's block game is graded by, its sky deep blue at the top and its grass and
    /// roofs vivid in the afternoon sun. The curve darkens the shadows and the saturation in linear
    /// light colors them, and the contrast deepens the shade.
    /// </remarks>
    public static LookSettings Vivid() => new()
    {
        Curve = LookCurve.Numbered,
        Exposure = 1.6f,
        CurveExposure = 0.9f,
        CurveWhite = 1.3f,
        CurveLower = 1.05f,
        CurveUpper = 1f,
        Saturation = 1.4f,
        Contrast = 1.12f,
    };
}
