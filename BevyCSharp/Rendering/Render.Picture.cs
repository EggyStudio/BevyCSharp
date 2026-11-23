namespace Bevy;

// The picture the first window's cameras draw, its size, shape, fit and filter, which the render
// page's settings are in text.
public static partial class Render
{
    /// <summary>
    /// The picture the first window's cameras draw, its scale or exact size, shape, fit and filter,
    /// set at once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The settings on the panel's <c>render</c> page are this value in text, so setting it moves
    /// their rows and a player's choice on them reads here. What a game sets is its own and not a
    /// player's, so the settings file keeps the player's last choice for the next start, as it
    /// keeps a window size a player chose over one the game set.
    /// </para>
    /// <para>
    /// <see cref="Bevy.Picture.Size"/> fixes the render size whatever the window's, which no
    /// setting does. While it is held, <c>render.scale</c> stands unused and its row says the game
    /// fixes the size, and setting a picture with no size brings the scale back. The size in use is
    /// <see cref="PictureSize"/>.
    /// </para>
    /// <para>
    /// A run that shows no window, a headless or an offscreen one, keeps the value and draws
    /// nothing differently.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The scale is not above 0 and at most 2, a side of the size is 0 or above 16384, a side of
    /// the shape is not a number above 0, or the fit or the filter is none of its choices.
    /// </exception>
    public static Picture Picture
    {
        get => PictureSettings.Current;
        set => PictureSettings.Set(value);
    }

    /// <summary>
    /// The size in pixels the scene is drawn at in this frame, the window's own where its picture
    /// is, or none where no window is shown.
    /// </summary>
    /// <remarks>
    /// The scale or the exact size of <see cref="Picture"/>, cut to its shape, and twice the
    /// window's along a side at the most. It follows the window as it is resized, from the frame
    /// after.
    /// </remarks>
    public static (uint Width, uint Height)? PictureSize => Window.PictureInUse()?.Size;

    /// <summary>
    /// The part of the window the picture stands in, from the window's corner in its logical
    /// pixels, or none where no window is shown.
    /// </summary>
    /// <remarks>
    /// Inside the bars of a picture whose shape is kept, and the whole window where the picture is
    /// the window's own or stretched over it. <c>Ui.SetOnPicture</c> keeps a node over it.
    /// </remarks>
    public static (float X, float Y, float Width, float Height)? PictureArea => Window.PictureInUse()?.Area;
}
