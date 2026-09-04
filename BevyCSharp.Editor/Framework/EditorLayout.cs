using System.Text;
using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Works out where every open panel goes, and puts it there.
/// </summary>
/// <remarks>
/// <para>
/// A layout is a table of placements, one per panel, and this is the thing that reads it. That is
/// the whole design: because a placement is data, a layout can be saved, restored, edited by hand
/// and changed by dragging a window, and none of those need a different mechanism from the others.
/// A stylesheet cannot do any of it.
/// </para>
/// <para>
/// The docks reflow around each other. The bottom band takes the width it needs and the side
/// columns end above it, so opening the asset browser shortens the world and the entity panels
/// rather than covering them. A panel that asks to fill takes whatever its column has left after
/// the panels above it, which is what makes a list as long as the screen allows.
/// </para>
/// </remarks>
public sealed class EditorLayout
{
    private readonly Dictionary<string, PanelPlacement> _overrides = [];

    /// <summary>How far a panel keeps from the edge of the window.</summary>
    public float Margin { get; set; } = 10f;

    /// <summary>How far panels keep from each other.</summary>
    public float Gap { get; set; } = 8f;

    /// <summary>Where a panel is, taking any override over what the panel itself asked for.</summary>
    public PanelPlacement PlacementOf(IEditorPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);
        return _overrides.TryGetValue(KeyOf(panel), out var placement)
            ? placement
            : panel.Chrome.Placement;
    }

    /// <summary>Moves a panel, which is what a drag and a loaded layout both do.</summary>
    public void Place(IEditorPanel panel, PanelPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(panel);
        _overrides[KeyOf(panel)] = placement;
    }

    /// <summary>Puts a panel back where its own declaration says.</summary>
    public void Reset(IEditorPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);
        _overrides.Remove(KeyOf(panel));
    }

    /// <summary>Forgets every override, putting the whole editor back to its declared layout.</summary>
    public void ResetAll() => _overrides.Clear();

    /// <summary>The rectangle the docks left for the world, which is everything they do not cover.</summary>
    /// <remarks>
    /// What a viewport gizmo is placed against and what a click has to be inside to count as a
    /// click on the scene. Updated every time the panels are arranged.
    /// </remarks>
    public UiRect Viewport { get; private set; }

    /// <summary>
    /// Places every open panel for this frame.
    /// </summary>
    /// <remarks>
    /// Runs every frame rather than when something changes, because what a panel measures is not
    /// something anything reports: its contents change, a stylesheet is edited while the editor
    /// runs, the window is resized. The writes themselves are compared first, so a frame in which
    /// nothing moved costs a handful of reads.
    /// </remarks>
    public void Arrange(IReadOnlyList<IEditorPanel> panels)
    {
        ArgumentNullException.ThrowIfNull(panels);

        var (windowWidth, windowHeight) = Window.Size();
        if (windowWidth == 0 || windowHeight == 0) return;

        var width = windowWidth;
        var height = windowHeight;

        var placed = new List<Placed>();
        foreach (var panel in panels)
        {
            if (panel.Window is not { IsOpen: true } window) continue;
            if (window.Root.IsNone) continue;
            if (!Xui.TryRect(window.Root, out var rect)) continue;

            // Layering is applied here rather than when the panel opened, because its elements
            // did not exist yet then: a document is built a frame or two after it is asked for.
            window.Layer(panel.Chrome.Layer);

            placed.Add(new Placed(panel, PlacementOf(panel), rect));
        }

        // The bands are measured before anything is moved, because each one decides how much room
        // the next has. Top first, then the strip along the bottom, then the bottom band, and the
        // columns take what is left.
        var top = Band(placed, EditorDock.Top);
        var strip = Band(placed, EditorDock.Strip);
        var bottom = Band(placed, EditorDock.Bottom);

        var contentTop = Margin + (top > 0f ? top + Gap : 0f);
        var stripTop = height - Margin - strip;
        var bottomTop = (strip > 0f ? stripTop - Gap : height - Margin) - bottom;
        var contentBottom = bottom > 0f ? bottomTop - Gap : (strip > 0f ? stripTop - Gap : height - Margin);

        var left = Column(placed, EditorDock.Left, Margin, contentTop, contentBottom, fromLeft: true);
        var right = Column(
            placed, EditorDock.Right, width - Margin, contentTop, contentBottom, fromLeft: false);

        Row(placed, EditorDock.Top, width, Margin);
        Row(placed, EditorDock.Bottom, width, bottomTop, stretch: true);
        Strip(placed, stripTop);
        Free(placed);

        Viewport = new UiRect(
            left,
            contentTop,
            MathF.Max(0f, width - left - (width - right)),
            MathF.Max(0f, contentBottom - contentTop));
    }

    /// <summary>One panel, where it wants to be, and where it currently is.</summary>
    private readonly record struct Placed(IEditorPanel Panel, PanelPlacement Placement, UiRect Rect);

    /// <summary>How tall a horizontal band is, which is the tallest thing in it.</summary>
    private float Band(List<Placed> placed, EditorDock dock)
    {
        var tallest = 0f;

        foreach (var entry in placed)
        {
            if (entry.Placement.Dock != dock) continue;

            var measured = float.IsNaN(entry.Placement.Height)
                ? entry.Rect.Height
                : entry.Placement.Height;

            tallest = MathF.Max(tallest, measured);
        }

        return tallest;
    }

    /// <summary>
    /// Stacks a column's panels and answers where its inner edge ended up.
    /// </summary>
    /// <remarks>
    /// A panel that asks to fill takes what the column has left after the ones above it, which is
    /// how a list ends up as long as the screen allows without knowing how tall the screen is.
    /// </remarks>
    private float Column(
        List<Placed> placed,
        EditorDock dock,
        float edge,
        float top,
        float bottom,
        bool fromLeft)
    {
        var members = Members(placed, dock);
        if (members.Count == 0) return edge;

        // What is left over after everything that sizes itself, shared by whatever asked to fill.
        var fixedHeight = 0f;
        var filling = 0;

        foreach (var entry in members)
        {
            if (entry.Placement.Fill) filling++;
            else fixedHeight += Height(entry) + Gap;
        }

        var spare = MathF.Max(0f, bottom - top - fixedHeight - (filling > 0 ? (filling - 1) * Gap : 0f));
        var share = filling > 0 ? spare / filling : 0f;

        var run = top;
        var widest = 0f;

        foreach (var entry in members)
        {
            var panelWidth = float.IsNaN(entry.Placement.Width)
                ? entry.Rect.Width
                : entry.Placement.Width;

            var panelHeight = entry.Placement.Fill ? share : Height(entry);
            var x = fromLeft ? edge : edge - panelWidth;

            entry.Panel.Window!.PlaceAt(
                x + entry.Placement.X,
                run + entry.Placement.Y,
                entry.Placement.Width,
                entry.Placement.Fill ? share : entry.Placement.Height,
                entry.Rect);

            run += panelHeight + Gap;
            widest = MathF.Max(widest, panelWidth);
        }

        return fromLeft ? edge + widest + Gap : edge - widest - Gap;
    }

    /// <summary>Lays a band's panels out side by side, centred unless told to stretch.</summary>
    private void Row(List<Placed> placed, EditorDock dock, float width, float top, bool stretch = false)
    {
        var members = Members(placed, dock);
        if (members.Count == 0) return;

        if (stretch)
        {
            foreach (var entry in members)
            {
                entry.Panel.Window!.PlaceAt(
                    Margin + entry.Placement.X,
                    top + entry.Placement.Y,
                    width - (Margin * 2f),
                    entry.Placement.Height,
                    entry.Rect);
            }

            return;
        }

        var total = 0f;
        foreach (var entry in members) total += Width(entry) + Gap;
        total = MathF.Max(0f, total - Gap);

        var run = (width - total) * 0.5f;

        foreach (var entry in members)
        {
            entry.Panel.Window!.PlaceAt(
                run + entry.Placement.X,
                top + entry.Placement.Y,
                entry.Placement.Width,
                entry.Placement.Height,
                entry.Rect);

            run += Width(entry) + Gap;
        }
    }

    /// <summary>Lays the bottom strip out from the left, which is where a tab bar belongs.</summary>
    private void Strip(List<Placed> placed, float top)
    {
        var run = Margin;

        foreach (var entry in Members(placed, EditorDock.Strip))
        {
            entry.Panel.Window!.PlaceAt(
                run + entry.Placement.X,
                top + entry.Placement.Y,
                entry.Placement.Width,
                entry.Placement.Height,
                entry.Rect);

            run += Width(entry) + Gap;
        }
    }

    /// <summary>Places whatever carries its own coordinates.</summary>
    private static void Free(List<Placed> placed)
    {
        foreach (var entry in placed)
        {
            if (entry.Placement.Dock != EditorDock.Floating) continue;

            entry.Panel.Window!.PlaceAt(
                entry.Placement.X,
                entry.Placement.Y,
                entry.Placement.Width,
                entry.Placement.Height,
                entry.Rect);
        }
    }

    /// <summary>A dock's panels, in the order they asked for.</summary>
    private static List<Placed> Members(List<Placed> placed, EditorDock dock)
    {
        var members = new List<Placed>();

        foreach (var entry in placed)
        {
            if (entry.Placement.Dock == dock) members.Add(entry);
        }

        members.Sort((a, b) => a.Placement.Order.CompareTo(b.Placement.Order));
        return members;
    }

    /// <summary>The height to lay a panel out with: what it asked for, or what it measured.</summary>
    private static float Height(Placed entry) =>
        float.IsNaN(entry.Placement.Height) ? entry.Rect.Height : entry.Placement.Height;

    /// <summary>The same, horizontally.</summary>
    private static float Width(Placed entry) =>
        float.IsNaN(entry.Placement.Width) ? entry.Rect.Width : entry.Placement.Width;

    /// <summary>What a panel is called in a saved layout.</summary>
    /// <remarks>
    /// The type's name rather than the instance, so a layout survives a restart. Two panels of the
    /// same type share a placement, which is the right answer for the panels an editor actually
    /// has one of and the wrong one for a flyout, which is why a flyout is placed where it was
    /// opened rather than by the layout.
    /// </remarks>
    private static string KeyOf(IEditorPanel panel) => panel.GetType().Name;

    // -- Saving
    //
    // A layout is a table, so it writes as one. Plain lines rather than JSON because it is meant
    // to be read and edited by hand as readily as by the editor.

    /// <summary>Writes the layout out, one panel per line.</summary>
    public string Describe()
    {
        var text = new StringBuilder();
        text.AppendLine("# BevyCSharp editor layout");
        text.AppendLine($"margin = {Margin}");
        text.AppendLine($"gap = {Gap}");

        foreach (var (panel, placement) in _overrides.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            text.AppendLine($"{panel} = {placement}");

        return text.ToString();
    }

    /// <summary>
    /// Reads a layout back, replacing every override with what it says.
    /// </summary>
    /// <remarks>
    /// A line naming a panel that is not open is kept rather than discarded, so a layout survives
    /// the panels it mentions being closed and reopened. A line that makes no sense is skipped:
    /// this file is meant to be edited by hand, and a typo in one line should not cost the rest.
    /// </remarks>
    public void Restore(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        _overrides.Clear();

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#') continue;

            var split = trimmed.IndexOf('=');
            if (split <= 0) continue;

            var name = trimmed[..split].Trim();
            var value = trimmed[(split + 1)..].Trim();

            switch (name)
            {
                case "margin" when float.TryParse(value, out var margin):
                    Margin = margin;
                    continue;

                case "gap" when float.TryParse(value, out var gap):
                    Gap = gap;
                    continue;
            }

            if (PanelPlacement.TryParse(value, out var placement)) _overrides[name] = placement;
        }
    }
}
