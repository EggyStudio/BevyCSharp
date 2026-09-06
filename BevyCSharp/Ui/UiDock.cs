namespace Bevy;

/// <summary>
/// Which part of the screen a panel belongs to.
/// </summary>
/// <remarks>
/// <para>
/// The screen is three columns: a panel column on the left, a panel column on the right, and
/// everything between them, which is the viewport. The viewport is split again along its bottom
/// by whatever tab is open, and the tab strip sits at the very bottom edge of it.
/// </para>
/// <para>
/// The columns run the full height of the window. Nothing is pushed down by the toolbar, because
/// the toolbar is not a bar across the top: it is a handful of buttons floating in the viewport's
/// corners, which is where an editor puts them when the viewport is the whole background.
/// </para>
/// </remarks>
public enum UiDock
{
    /// <summary>Wherever the placement's own coordinates say. Menus and dragged windows.</summary>
    Floating,

    /// <summary>The left column.</summary>
    Left,

    /// <summary>The right column.</summary>
    Right,

    /// <summary>The band along the bottom of the viewport: whichever tab is open.</summary>
    Bottom,

    /// <summary>The tab strip, at the very bottom edge of the viewport.</summary>
    Strip,

    /// <summary>Floating in the viewport's top left corner.</summary>
    ViewportTopLeft,

    /// <summary>
    /// Floating along the top, centred on the window rather than on the viewport.
    /// </summary>
    /// <remarks>
    /// The middle of the screen is a place a hand learns. Centring this on the viewport would move
    /// it whenever a column opened, which is a tool that is somewhere else every time it is
    /// wanted.
    /// </remarks>
    ViewportTop,

    /// <summary>Floating in the viewport's top right corner.</summary>
    ViewportTopRight,

    /// <summary>Floating in the viewport's bottom left corner, above the tab strip.</summary>
    ViewportBottomLeft,

    /// <summary>Floating in the viewport's bottom right corner, above the tab strip.</summary>
    ViewportBottomRight,

    /// <summary>
    /// The whole window, over everything.
    /// </summary>
    /// <remarks>
    /// For the things that are not part of looking at a scene: settings, a project browser, an
    /// about box. They want the room and they want the whole of somebody's attention, and
    /// squeezing one into a column beside a hierarchy is what makes people not open it.
    /// </remarks>
    Sheet,
}

/// <summary>What makes a panel go away.</summary>
public enum UiDismiss
{
    /// <summary>Nothing but being closed. What an ordinary panel does.</summary>
    Never,

    /// <summary>
    /// A press anywhere outside it.
    /// </summary>
    /// <remarks>
    /// What separates a flyout from a panel: a menu, a colour picker, an enum list and a context
    /// menu are all this, and everything else about them is their content.
    /// </remarks>
    OnOutsideClick,
}
