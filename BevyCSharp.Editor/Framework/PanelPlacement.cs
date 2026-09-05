namespace BevyCSharp.Editor.Framework;

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
public enum EditorDock
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
}

/// <summary>What makes a panel go away.</summary>
public enum PanelDismiss
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

/// <summary>
/// A panel's position, as data rather than as a stylesheet rule.
/// </summary>
/// <param name="Dock">Which part of the screen it belongs to.</param>
/// <param name="X">For <see cref="EditorDock.Floating"/>, the left edge. Otherwise an offset.</param>
/// <param name="Y">The same, vertically.</param>
/// <param name="Width">How wide, or <see cref="float.NaN"/> to leave it to the stylesheet.</param>
/// <param name="Height">How tall, or <see cref="float.NaN"/> to be as tall as its contents.</param>
/// <param name="Order">Where it sits among the other panels of its dock. Lower is first.</param>
/// <remarks>
/// The reason this is not CSS. Appearance belongs in a stylesheet, where it can be changed
/// without a rebuild; position has to be something the editor holds, because a layout that can be
/// saved, restored and rearranged by dragging is a table of these, and a rule inside a CSS file is
/// neither readable nor writable from the side doing the arranging.
/// </remarks>
public readonly record struct PanelPlacement(
    EditorDock Dock,
    float X = 0f,
    float Y = 0f,
    float Width = float.NaN,
    float Height = float.NaN,
    int Order = 0)
{
    /// <summary>A panel in a dock, sized by its contents unless told otherwise.</summary>
    public static PanelPlacement In(EditorDock dock, int order = 0) => new(dock, Order: order);

    /// <summary>A panel at its own coordinates.</summary>
    public static PanelPlacement At(
        float x, float y, float width = float.NaN, float height = float.NaN) =>
        new(EditorDock.Floating, x, y, width, height);

    /// <summary>The same placement, moved to a point of its own.</summary>
    public PanelPlacement MovedTo((float X, float Y) point) => MovedTo(point.X, point.Y);

    /// <summary>The same placement, somewhere else.</summary>
    public PanelPlacement MovedTo(float x, float y) =>
        this with { Dock = EditorDock.Floating, X = x, Y = y };

    /// <summary>How this reads in a saved layout.</summary>
    public override string ToString() =>
        $"{Dock} {Number(X)} {Number(Y)} {Number(Width)} {Number(Height)} {Order}";

    /// <summary>Reads back what <see cref="ToString"/> wrote.</summary>
    public static bool TryParse(string text, out PanelPlacement placement)
    {
        placement = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1) return false;
        if (!Enum.TryParse<EditorDock>(parts[0], ignoreCase: true, out var dock)) return false;

        placement = new PanelPlacement(
            dock,
            Value(parts, 1),
            Value(parts, 2),
            Value(parts, 3),
            Value(parts, 4),
            parts.Length > 5 && int.TryParse(parts[5], out var order) ? order : 0);

        return true;
    }

    /// <summary>A number as a layout file writes it, with <c>auto</c> for what is not fixed.</summary>
    private static string Number(float value) =>
        float.IsNaN(value)
            ? "auto"
            : value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>One number from a saved line, defaulting to what was not written.</summary>
    private static float Value(string[] parts, int index)
    {
        if (index >= parts.Length) return index >= 3 ? float.NaN : 0f;

        return float.TryParse(
            parts[index],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var value)
            ? value
            : float.NaN;
    }
}

/// <summary>
/// Everything about a panel that is true before it opens.
/// </summary>
/// <param name="Root">
/// The CSS id of the panel's outermost element, which is what gets placed. A panel without one is
/// left where its stylesheet puts it.
/// </param>
/// <param name="Placement">Where it starts, before any saved layout has its say.</param>
/// <param name="Dismiss">What makes it go away.</param>
/// <param name="Layer">Which panels it draws in front of.</param>
/// <remarks>
/// Generated from the attributes on the panel class, so a panel author writes a declaration and
/// not a constructor call. The shell reads it; nothing writes it.
/// </remarks>
public sealed record PanelChrome(
    string? Root = null,
    PanelPlacement Placement = default,
    PanelDismiss Dismiss = PanelDismiss.Never,
    int Layer = 0);
