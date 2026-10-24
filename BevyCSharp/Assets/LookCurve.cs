namespace Bevy;

/// <summary>The curve a camera's look brings light past white under by. See <see cref="LookSettings"/>.</summary>
public enum LookCurve
{
    /// <summary>
    /// The brightest channel left alone up to 0.9 and bent smoothly toward white past it, the other
    /// two scaled with it, so a bright orange stays orange where a clamp per channel turns it
    /// white, with each channel past <see cref="LookSettings.Knee"/> let go toward white on its own
    /// by <see cref="LookSettings.KneeStrength"/>.
    /// </summary>
    /// <remarks>
    /// Everything under the knee is shown as it was drawn, so a scene lit to show as it should needs
    /// no curve to undo. A light bright in all three channels goes to white as film and a path-traced
    /// reference's clip take it, and a saturated light keeps its hue up to the knee.
    /// </remarks>
    Knee = 0,

    /// <summary>
    /// A curve set by four numbers, the light times <see cref="LookSettings.CurveExposure"/>, brought
    /// under 1 by a white curve of <see cref="LookSettings.CurveWhite"/>, then raised by a power
    /// running from <see cref="LookSettings.CurveLower"/> in the dark to
    /// <see cref="LookSettings.CurveUpper"/> in the light, and shown at a gamma of 2.2.
    /// </summary>
    /// <remarks>
    /// The curve the BSL shaders tonemap Minecraft's picture by, numbered as their settings are, so
    /// a look copied from them is set here by the same numbers. It darkens the shadows and holds the
    /// highlights back, which with a saturation above 1 gives a vivid picture.
    /// </remarks>
    Numbered = 1,
}
