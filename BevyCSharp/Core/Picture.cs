namespace Bevy;

/// <summary>
/// The picture the first window's cameras draw, its size as a share of the window's or an exact
/// one, its shape, how it stands in the window and how it is brought to it.
/// </summary>
/// <remarks>
/// <para>
/// The scene is drawn at the picture's size and the picture is brought to the window at the frame's
/// end, so a picture smaller than the window costs less and a larger one, up to twice the window's,
/// is brought down smoothly for a finer edge. The interface, ImGui's panels and Bevy's own, is
/// drawn after the picture at the window's own size, so it stays sharp whatever the picture's.
/// </para>
/// <para>
/// The settings on the panel's <c>render</c> page are this value as text, <c>render.scale</c>,
/// <c>render.aspect</c>, <c>render.fit</c> and <c>render.filter</c>, so a player's choice and a
/// game's are one, and <c>Render.Picture</c> sets and reads them all at once. Only the
/// exact size is a game's alone and kept by no setting, and while a game holds one the scale stands
/// unused and its row says so.
/// </para>
/// <para>
/// <c>new Picture()</c> is the window's own picture, at its size and shape, and <c>default</c> is
/// no picture at all, refused where one is set, since its scale is 0.
/// </para>
/// </remarks>
public readonly record struct Picture
{
    /// <summary>The window's own picture, at its size and shape.</summary>
    public Picture()
    {
    }

    /// <summary>
    /// The picture's share of the window's size along each side, above 0 and up to 2, 1 for the
    /// window's own size.
    /// </summary>
    public float Scale { get; init; } = 1f;

    /// <summary>
    /// The picture's size in pixels in place of <see cref="Scale"/>, or none to follow the window
    /// by the scale.
    /// </summary>
    /// <remarks>
    /// Twice the window's along a side at the most, a larger size being brought down to it. A game
    /// fixes the render size with it where its picture must not follow the window, as pixel art
    /// drawn at one size does.
    /// </remarks>
    public (uint Width, uint Height)? Size { get; init; }

    /// <summary>
    /// The picture's shape as a width and a height, as (4, 3), or none for the shape of its size.
    /// </summary>
    /// <remarks>
    /// The picture is the largest of the shape inside its size, so a 4 to 3 shape cut from a 16 to
    /// 9 window's picture keeps its height and loses a quarter of its width.
    /// </remarks>
    public (float Width, float Height)? Aspect { get; init; }

    /// <summary>How a picture of a shape unlike the window's stands in it.</summary>
    public PictureFit Fit { get; init; }

    /// <summary>How a picture smaller than the window is brought up to it.</summary>
    /// <remarks>
    /// A picture larger than the window is brought down smoothly whatever the filter, which only
    /// says how a picture is enlarged.
    /// </remarks>
    public PictureFilter Filter { get; init; }
}
