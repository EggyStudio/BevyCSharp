namespace Bevy;

/// <summary>
/// One of Bevy's color spaces, each named for its type and its components in their order, for
/// <see cref="Color.From"/> and <see cref="Color.In"/>.
/// </summary>
public enum ColorSpace
{
    /// <summary>Bevy's <c>Srgba</c>, red, green, blue and alpha as a display shows them.</summary>
    Srgb = 0,

    /// <summary>Bevy's <c>LinearRgba</c>, red, green, blue and alpha as light adds them, which <see cref="Color"/> holds.</summary>
    LinearRgb = 1,

    /// <summary>Bevy's <c>Hsla</c>, hue in degrees, saturation, lightness and alpha.</summary>
    Hsl = 2,

    /// <summary>Bevy's <c>Hsva</c>, hue in degrees, saturation, value and alpha.</summary>
    Hsv = 3,

    /// <summary>Bevy's <c>Hwba</c>, hue in degrees, whiteness, blackness and alpha.</summary>
    Hwb = 4,

    /// <summary>Bevy's <c>Laba</c>, lightness, a, b and alpha, in CIE L*a*b*.</summary>
    Lab = 5,

    /// <summary>Bevy's <c>Lcha</c>, lightness, chroma, hue in degrees and alpha, in CIE LCh.</summary>
    Lch = 6,

    /// <summary>Bevy's <c>Oklaba</c>, lightness, a, b and alpha, in Oklab.</summary>
    Oklab = 7,

    /// <summary>Bevy's <c>Oklcha</c>, lightness, chroma, hue in degrees and alpha, in Oklch.</summary>
    Oklch = 8,

    /// <summary>Bevy's <c>Okhsla</c>, hue in degrees, saturation, lightness and alpha, in Okhsl.</summary>
    Okhsl = 9,

    /// <summary>Bevy's <c>Okhsva</c>, hue in degrees, saturation, value and alpha, in Okhsv.</summary>
    Okhsv = 10,

    /// <summary>Bevy's <c>Okhwba</c>, hue in degrees, whiteness, blackness and alpha, in Okhwb.</summary>
    Okhwb = 11,

    /// <summary>Bevy's <c>Xyza</c>, x, y, z and alpha, in CIE XYZ.</summary>
    Xyz = 12,
}
