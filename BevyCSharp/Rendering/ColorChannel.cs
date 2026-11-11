namespace Bevy;

/// <summary>
/// What a Feathers color slider sets, Bevy's <c>ColorChannel</c>, for
/// <see cref="Feathers.ColorSlider"/>.
/// </summary>
/// <remarks>
/// The slider draws its track as the color it sets would look along it, from the color Bevy's
/// reflected <c>SliderBaseColorRef</c> holds, which the game keeps in step with the color.
/// </remarks>
public enum ColorChannel
{
    /// <summary>Red, from zero to one.</summary>
    Red = 0,

    /// <summary>Green, from zero to one.</summary>
    Green = 1,

    /// <summary>Blue, from zero to one.</summary>
    Blue = 2,

    /// <summary>Hue in HSL, in degrees.</summary>
    HslHue = 3,

    /// <summary>Saturation in HSL, from zero to one.</summary>
    HslSaturation = 4,

    /// <summary>Lightness in HSL, from zero to one.</summary>
    HslLightness = 5,

    /// <summary>Alpha, from zero to one, drawn over a checkered pattern.</summary>
    Alpha = 6,

    /// <summary>Hue in Okhsl, in degrees.</summary>
    OkhslHue = 7,

    /// <summary>Saturation in Okhsl, from zero to one.</summary>
    OkhslSaturation = 8,

    /// <summary>Lightness in Okhsl, from zero to one.</summary>
    OkhslLightness = 9,
}
