namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Where in the window a panel sits.
/// </summary>
/// <remarks>
/// <para>
/// Nine edges and corners, plus <see cref="Free"/> for a panel that keeps its own coordinates.
/// Deliberately not docking: a region is a place to put something, not a container that owns it,
/// so a panel can be moved between regions, given coordinates, or hidden without anything else
/// in the editor being rearranged around it.
/// </para>
/// <para>
/// The viewport is behind all of it, full screen. Nothing here reserves space from a scene view,
/// because there is no scene view to reserve it from.
/// </para>
/// </remarks>
public enum EditorRegion
{
    /// <summary>Wherever the placement's own coordinates say.</summary>
    Free,

    /// <summary>Top left corner, stacking downwards.</summary>
    TopLeft,

    /// <summary>Centred along the top, stacking to the right. What a toolbar wants.</summary>
    Top,

    /// <summary>Top right corner, stacking downwards.</summary>
    TopRight,

    /// <summary>Centred on the left edge, stacking downwards.</summary>
    Left,

    /// <summary>The middle of the window, stacking downwards.</summary>
    Centre,

    /// <summary>Centred on the right edge, stacking downwards.</summary>
    Right,

    /// <summary>Bottom left corner, stacking upwards.</summary>
    BottomLeft,

    /// <summary>Centred along the bottom, stacking to the right. What a status strip wants.</summary>
    Bottom,

    /// <summary>Bottom right corner, stacking upwards.</summary>
    BottomRight,
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
    /// What separates a flyout from a panel: a dropdown, a colour picker, an enum list and a
    /// context menu are all this, and everything else about them is their content.
    /// </remarks>
    OnOutsideClick,
}

/// <summary>
/// A panel's position, as data rather than as a stylesheet rule.
/// </summary>
/// <param name="Region">Which part of the window it belongs to.</param>
/// <param name="X">
/// For <see cref="EditorRegion.Free"/>, the left edge. For any other region, how far the panel is
/// pushed from where the region would otherwise put it.
/// </param>
/// <param name="Y">The same, vertically.</param>
/// <param name="Width">How wide, or <see cref="float.NaN"/> to be as wide as its contents.</param>
/// <param name="Height">How tall, or <see cref="float.NaN"/> to be as tall as its contents.</param>
/// <remarks>
/// The reason this is not CSS. Appearance belongs in a stylesheet, where it can be changed
/// without a rebuild; position has to be something the editor holds, because a layout that can
/// be saved, restored and rearranged by dragging is a table of these, and a rule inside a CSS
/// file is neither readable nor writable from the side doing the arranging.
/// </remarks>
public readonly record struct PanelPlacement(
    EditorRegion Region,
    float X = 0f,
    float Y = 0f,
    float Width = float.NaN,
    float Height = float.NaN)
{
    /// <summary>A panel in a region, sized by its contents unless told otherwise.</summary>
    public static PanelPlacement In(
        EditorRegion region, float width = float.NaN, float height = float.NaN) =>
        new(region, 0f, 0f, width, height);

    /// <summary>A panel at its own coordinates.</summary>
    public static PanelPlacement At(
        float x, float y, float width = float.NaN, float height = float.NaN) =>
        new(EditorRegion.Free, x, y, width, height);

    /// <summary>The same placement, moved to a point of its own.</summary>
    public PanelPlacement MovedTo(float x, float y) =>
        this with { Region = EditorRegion.Free, X = x, Y = y };

    /// <summary>How this reads in a saved layout.</summary>
    public override string ToString() =>
        $"{Region} {Number(X)} {Number(Y)} {Number(Width)} {Number(Height)}";

    /// <summary>Reads back what <see cref="ToString"/> wrote.</summary>
    public static bool TryParse(string text, out PanelPlacement placement)
    {
        placement = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1) return false;
        if (!Enum.TryParse<EditorRegion>(parts[0], ignoreCase: true, out var region)) return false;

        placement = new PanelPlacement(
            region,
            Value(parts, 1),
            Value(parts, 2),
            Value(parts, 3),
            Value(parts, 4));

        return true;
    }

    /// <summary>A number as a layout file writes it, with <c>auto</c> for what is not fixed.</summary>
    private static string Number(float value) =>
        float.IsNaN(value) ? "auto" : value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

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
/// The CSS id of the panel's outermost element, which is what gets placed. A panel without one
/// is left where its stylesheet puts it.
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
