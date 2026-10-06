namespace Bevy;

/// <summary>
/// How a sprite is drawn.
/// </summary>
/// <remarks>
/// A sprite is a picture in the world rather than on the screen. It carries a
/// <see cref="Transform"/> like anything else, and a 2D camera decides what a world unit is worth
/// in pixels. For something pinned to the screen, use <see cref="Ui"/>.
/// </remarks>
/// <summary>
/// How a sprite's picture meets the size it is drawn at.
/// </summary>
public enum SpriteImageMode
{
    /// <summary>
    /// The picture's own size, stretched to <see cref="SpriteSettings.Size"/> when one is given.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Cut into nine, so the corners keep their size while the middle stretches.
    /// </summary>
    /// <remarks>A health bar or a dialog box drawn at any width from one small image.</remarks>
    Sliced = 1,

    /// <summary>Repeated across the sprite rather than stretched.</summary>
    Tiled = 2,

    /// <summary>
    /// Fitted inside the size, keeping the picture's proportions.
    /// </summary>
    /// <remarks>
    /// What a video player does with a film that is the wrong shape for the screen. Which edges
    /// are left over, and whether the picture is fitted inside the size or made to fill it, is
    /// <see cref="SpriteSettings.Scaling"/>.
    /// </remarks>
    Scaled = 3,
}
