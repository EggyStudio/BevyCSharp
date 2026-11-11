namespace Bevy;

/// <summary>
/// The curve that maps what was rendered onto what a screen can show.
/// </summary>
/// <remarks>
/// A renderer works in light, which has no upper bound; a display has one. A tonemapper decides
/// what happens to the parts brighter than the screen can be, and the choice is a look rather
/// than a correctness question. It shows most on a camera drawing in high dynamic range, which is
/// <see cref="PostSettings.Hdr"/>.
/// </remarks>
public enum Tonemapper
{
    /// <summary>Clip anything brighter than white, as no tonemapping does.</summary>
    /// <remarks>
    /// The picture passes through untouched, so a camera's grade
    /// (<see cref="Render.SetColorGrading(Entity, GradingSettings?)"/>) and its dithering
    /// (<see cref="PostSettings.Dither"/>) do nothing under it, and Bevy warns once for a camera
    /// that asks for either. A 2D camera keeps both until it is given a tonemapper, since Bevy
    /// starts one with a curve that clips as this does and keeps them.
    /// </remarks>
    None = 0,

    /// <summary>The classic curve. Colors shift hue as they brighten.</summary>
    Reinhard = 1,

    /// <summary>The same on luminance only, so bright colors keep their hue better.</summary>
    ReinhardLuminance = 2,

    /// <summary>Film-like and high contrast, with deliberate hue shifts. Dramatic.</summary>
    AcesFitted = 3,

    /// <summary>Neutral and slightly desaturated, with almost no hue shift.</summary>
    AgX = 4,

    /// <summary>A plain transform, useful as a reference to judge the others against.</summary>
    SomewhatBoring = 5,

    /// <summary>Bevy's own, neutral and keeping saturation in the highlights.</summary>
    TonyMcMapface = 6,

    /// <summary>Blender's filmic curve, for matching a render done there.</summary>
    BlenderFilmic = 7,
}
