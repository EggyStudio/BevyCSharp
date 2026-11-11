namespace Bevy;

/// <summary>
/// The two parts of a color a Feathers color plane sets, across and down, Bevy's
/// <c>FeathersColorPlane</c>, for <see cref="Feathers.ColorPlane"/>.
/// </summary>
/// <remarks>
/// The plane reports the point picked as a <see cref="ValueChange{T}"/> of a <see cref="Vec2"/>,
/// each from zero to one, and is drawn from Bevy's reflected <c>ColorPlaneValueRef</c>, the two
/// parts it sets and the third it holds still.
/// </remarks>
public enum ColorPlaneAxes
{
    /// <summary>Red across and green down.</summary>
    RedGreen = 0,

    /// <summary>Red across and blue down.</summary>
    RedBlue = 1,

    /// <summary>Green across and blue down.</summary>
    GreenBlue = 2,

    /// <summary>Hue across and saturation down, in HSL.</summary>
    HueSaturation = 3,

    /// <summary>Hue across and lightness down, in HSL.</summary>
    HueLightness = 4,

    /// <summary>Hue across and saturation down, in Okhsl.</summary>
    OkhslHueSaturation = 5,

    /// <summary>Hue across and lightness down, in Okhsl.</summary>
    OkhslHueLightness = 6,
}
