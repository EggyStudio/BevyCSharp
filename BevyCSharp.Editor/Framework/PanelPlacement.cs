namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Which part of the screen a panel belongs to.
/// </summary>
/// <remarks>
/// <para>
/// Four docks and a floating layer, not a grid of nine. A dock is a column or a band that its
/// panels share and that reflows when its neighbours change: opening the asset browser along the
/// bottom shortens the left and right columns rather than covering them.
/// </para>
/// <para>
/// The viewport is behind all of it, full screen. Nothing here reserves space from a scene view,
/// because there is no scene view to reserve it from: the docks float over the world.
/// </para>
/// </remarks>
public enum EditorDock
{
    /// <summary>Wherever the placement's own coordinates say. Flyouts and dragged windows.</summary>
    Floating,

    /// <summary>Centred along the top. The tools, and nothing else.</summary>
    Top,

    /// <summary>The left column: what is in the world.</summary>
    Left,

    /// <summary>The right column: what is selected, and what can be set.</summary>
    Right,

    /// <summary>The band along the bottom: the browsers that need width more than height.</summary>
    Bottom,

    /// <summary>The strip at the very bottom left: tabs, and what the keys do.</summary>
    Strip,
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
/// <param name="Fill">Whether it takes whatever height its dock has left over.</param>
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
    int Order = 0,
    bool Fill = false)
{
    /// <summary>A panel in a dock, sized by its contents unless told otherwise.</summary>
    public static PanelPlacement In(EditorDock dock, int order = 0, bool fill = false) =>
        new(dock, Order: order, Fill: fill);

    /// <summary>A panel at its own coordinates.</summary>
    public static PanelPlacement At(
        float x, float y, float width = float.NaN, float height = float.NaN) =>
        new(EditorDock.Floating, x, y, width, height);

    /// <summary>The same placement, moved to a point of its own.</summary>
    public PanelPlacement MovedTo(float x, float y) =>
        this with { Dock = EditorDock.Floating, X = x, Y = y, Fill = false };

    /// <summary>How this reads in a saved layout.</summary>
    public override string ToString() =>
        $"{Dock} {Number(X)} {Number(Y)} {Number(Width)} {Number(Height)} {Order} {Fill}";

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
            parts.Length > 5 && int.TryParse(parts[5], out var order) ? order : 0,
            parts.Length > 6 && bool.TryParse(parts[6], out var fill) && fill);

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
/// <param name="Handle">
/// The CSS id of the element a person drags the panel by, or <see langword="null"/> for a panel
/// that cannot be moved.
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
    string? Handle = null,
    PanelPlacement Placement = default,
    PanelDismiss Dismiss = PanelDismiss.Never,
    int Layer = 0);
