namespace Bevy;

/// <summary>
/// How a picture smaller than its window is brought up to it, the choices of
/// <c>render.filter</c>.
/// </summary>
public enum PictureFilter
{
    /// <summary>Filtered, each window pixel a blend of the picture pixels round it, soft.</summary>
    Linear = 0,

    /// <summary>
    /// Each window pixel from the picture pixel under it, sharp, and even only at a whole ratio.
    /// </summary>
    Nearest = 1,

    /// <summary>
    /// Each window pixel inside a picture pixel from it whole and one across an edge a blend of the
    /// two, so every picture pixel is a block of one width that holds still at any ratio, equal to
    /// <see cref="Nearest"/> at a whole one.
    /// </summary>
    Sharp = 2,
}
