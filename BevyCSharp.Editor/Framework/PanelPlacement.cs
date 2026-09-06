using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// A panel's position, as data rather than as a stylesheet rule.
/// </summary>
/// <param name="Dock">Which part of the screen it belongs to.</param>
/// <param name="X">For <see cref="UiDock.Floating"/>, the left edge. Otherwise an offset.</param>
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
    UiDock Dock,
    float X = 0f,
    float Y = 0f,
    float Width = float.NaN,
    float Height = float.NaN,
    int Order = 0)
{
    /// <summary>A panel in a dock, sized by its contents unless told otherwise.</summary>
    public static PanelPlacement In(UiDock dock, int order = 0) => new(dock, Order: order);

    /// <summary>A panel at its own coordinates.</summary>
    public static PanelPlacement At(
        float x, float y, float width = float.NaN, float height = float.NaN) =>
        new(UiDock.Floating, x, y, width, height);

    /// <summary>The same placement, moved to a point of its own.</summary>
    public PanelPlacement MovedTo((float X, float Y) point) => MovedTo(point.X, point.Y);

    /// <summary>The same placement, somewhere else.</summary>
    public PanelPlacement MovedTo(float x, float y) =>
        this with { Dock = UiDock.Floating, X = x, Y = y };

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
        if (!Enum.TryParse<UiDock>(parts[0], ignoreCase: true, out var dock)) return false;

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
/// What the shell makes of what a panel declared.
/// </summary>
/// <remarks>
/// The library holds a panel's declaration as plain numbers, because what a dock is and what
/// dismisses a panel are decisions of whatever arranges them rather than of the document. This is
/// the shell reading those numbers back as the words it thinks in.
/// </remarks>
public static class PanelChromeExtensions
{
    /// <summary>Where the panel says it starts.</summary>
    public static PanelPlacement Placement(this PanelChrome chrome)
    {
        ArgumentNullException.ThrowIfNull(chrome);

        return new PanelPlacement(
            (UiDock)chrome.Dock,
            chrome.X,
            chrome.Y,
            chrome.Width,
            chrome.Height,
            chrome.Order);
    }

    /// <summary>What makes the panel go away.</summary>
    public static UiDismiss Dismissal(this PanelChrome chrome)
    {
        ArgumentNullException.ThrowIfNull(chrome);

        return (UiDismiss)chrome.Dismiss;
    }
}
