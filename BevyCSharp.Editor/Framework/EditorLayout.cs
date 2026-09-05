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
/// and changed by dragging, and none of those need a different mechanism from the others. A
/// stylesheet cannot do any of it.
/// </para>
/// <para>
/// The screen is three columns. The side columns run the full height and are as wide as their
/// contents until somebody drags their inner edge, up to a third of the window each. Everything
/// between them is the viewport, and the viewport is split along its bottom by whichever tab is
/// open, up to half the window. Those three numbers are the whole of the arrangement, and all
/// three are draggable.
/// </para>
/// </remarks>
public sealed class EditorLayout
{
    private readonly Dictionary<string, PanelPlacement> _overrides = [];

    /// <summary>How far a panel keeps from the edge of the window.</summary>
    public float Margin { get; set; } = 8f;

    /// <summary>How far panels keep from each other.</summary>
    public float Gap { get; set; } = 8f;

    /// <summary>How wide the left column is, or <see cref="float.NaN"/> to follow its contents.</summary>
    public float LeftWidth { get; set; } = float.NaN;

    /// <summary>The same on the right.</summary>
    public float RightWidth { get; set; } = float.NaN;

    /// <summary>How tall the open tab is.</summary>
    public float BottomHeight { get; set; } = 190f;

    /// <summary>The narrowest a column can be dragged.</summary>
    public const float MinimumColumn = 160f;

    /// <summary>The shortest the tab band can be dragged.</summary>
    public const float MinimumBand = 90f;

    /// <summary>What the docks left for the world, which is everything they do not cover.</summary>
    /// <remarks>
    /// What a viewport button is placed against, what a click has to be inside to count as a click
    /// on the scene, and where the orientation cross is drawn. Updated every time panels are
    /// arranged.
    /// </remarks>
    public UiRect Viewport { get; private set; }

    /// <summary>Where the left column's inner edge is, for dragging it.</summary>
    public float LeftEdge { get; private set; }

    /// <summary>Where the right column's inner edge is.</summary>
    public float RightEdge { get; private set; }

    /// <summary>Where the top of the open tab is, for dragging it.</summary>
    public float BottomEdge { get; private set; }

    /// <summary>Whether a tab is open, so its edge is worth dragging.</summary>
    public bool BandOpen { get; private set; }

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

    /// <summary>Forgets every override and every dragged size.</summary>
    public void ResetAll()
    {
        _overrides.Clear();
        LeftWidth = float.NaN;
        RightWidth = float.NaN;
        BottomHeight = 190f;
    }

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

        float width = windowWidth;
        float height = windowHeight;

        var placed = new List<Placed>();
        foreach (var panel in panels)
        {
            if (panel.Window is not { IsOpen: true } window) continue;
            if (window.Root.IsNone) continue;
            if (!Xui.TryRect(window.Root, out var rect)) continue;

            // Layering is applied here rather than when the panel opened, because its elements did
            // not exist yet then: a document is built a frame or two after it is asked for.
            window.Layer(panel.Chrome.Layer);

            placed.Add(new Placed(panel, PlacementOf(panel), rect));
        }

        var left = ColumnWidth(placed, EditorDock.Left, LeftWidth, width);
        var right = ColumnWidth(placed, EditorDock.Right, RightWidth, width);

        var strip = Tallest(placed, EditorDock.Strip);
        var band = Members(placed, EditorDock.Bottom).Count > 0
            ? Math.Clamp(BottomHeight, MinimumBand, height * 0.5f)
            : 0f;

        // The window is a top split and a bottom one. The bottom holds the open tab and, under it,
        // the tabs and the key list on one row; both run the whole width, because nothing is
        // beside them. The top holds the three columns and gets whatever is left.
        var stripTop = height - strip;
        var bandTop = stripTop - band;

        var viewportLeft = left > 0f ? left + (Margin * 2f) : Margin;
        var viewportRight = right > 0f ? width - right - (Margin * 2f) : width - Margin;

        LeftEdge = viewportLeft - Margin;
        RightEdge = viewportRight + Margin;
        BottomEdge = bandTop;
        BandOpen = band > 0f;

        Viewport = new UiRect(
            viewportLeft,
            Margin,
            MathF.Max(0f, viewportRight - viewportLeft),
            MathF.Max(0f, bandTop - Margin));

        Column(placed, EditorDock.Left, Margin, Margin, bandTop, left, fromLeft: true);
        Column(placed, EditorDock.Right, width - Margin, Margin, bandTop, right, fromLeft: false);

        Band(placed, Margin, width - Margin, bandTop, band);
        Strip(placed, Margin, width - Margin, stripTop);
        Corners(placed, Viewport);
        Free(placed, width, height);
    }

    /// <summary>One panel, where it wants to be, and where it currently is.</summary>
    private readonly record struct Placed(IEditorPanel Panel, PanelPlacement Placement, UiRect Rect);

    /// <summary>
    /// How wide a column is: what was dragged, or what its widest panel measured.
    /// </summary>
    /// <remarks>
    /// A third of the window at most, whichever way it was decided. A column wider than that is
    /// not a column any more, and the viewport is the point of the editor.
    /// </remarks>
    private float ColumnWidth(List<Placed> placed, EditorDock dock, float dragged, float width)
    {
        var members = Members(placed, dock);
        if (members.Count == 0) return 0f;

        if (!float.IsNaN(dragged))
            return Math.Clamp(dragged, MinimumColumn, width / 3f);

        var widest = 0f;
        foreach (var entry in members) widest = MathF.Max(widest, Width(entry));

        return MathF.Min(widest, width / 3f);
    }

    /// <summary>
    /// Stacks a column's panels from the top, each as tall as its contents.
    /// </summary>
    /// <remarks>
    /// Content height rather than a share of the column: a panel with four rows in it should be
    /// four rows tall. What stops one growing past the window is the room left after the panels
    /// above it, which is also what makes an open tab shorten whatever is above it.
    /// </remarks>
    private void Column(
        List<Placed> placed,
        EditorDock dock,
        float edge,
        float top,
        float bottom,
        float columnWidth,
        bool fromLeft)
    {
        var run = top;

        foreach (var entry in Members(placed, dock))
        {
            var room = MathF.Max(0f, bottom - run);
            if (room <= 0f) break;

            var window = entry.Panel.Window!;
            var x = fromLeft ? edge : edge - columnWidth;

            // Handed back to its contents and capped at the room, rather than told a height. A
            // panel told a height measures that height, so the next frame's answer to "how tall
            // are your contents" is the height it was given — which is a tree that grows a dozen
            // rows inside a panel that never changes size. A maximum leaves the measurement where
            // it belongs and only stops it running past the column.
            var tall = float.IsNaN(entry.Placement.Height) ? Xui.Auto : entry.Placement.Height;

            window.LimitTo(Xui.Auto, room);

            window.PlaceAt(
                x + entry.Placement.X,
                run + entry.Placement.Y,
                columnWidth,
                tall,
                entry.Rect);

            run += MathF.Min(entry.Rect.Height, room) + Gap;
        }
    }

    /// <summary>
    /// Puts the open tab in the band between the viewport and the strip.
    /// </summary>
    /// <remarks>
    /// The whole width of the window, not the viewport's: the band and the strip under it are the
    /// bottom split, and the columns are the top one. <paramref name="top"/> is the edge the two
    /// splits share, and that edge is what a drag moves.
    /// </remarks>
    private static void Band(List<Placed> placed, float left, float right, float top, float height)
    {
        if (height <= 0f) return;

        foreach (var entry in Members(placed, EditorDock.Bottom))
        {
            entry.Panel.Window!.PlaceAt(
                left,
                top,
                MathF.Max(0f, right - left),
                height,
                entry.Rect);
        }
    }

    /// <summary>
    /// Lays the bottom split out: the tabs from the left, and whatever else shares the row after
    /// them.
    /// </summary>
    /// <remarks>
    /// One row across the whole window, because the strip is the bottom split and the columns are
    /// the top one. The tabs go where a browser puts them, and the key list follows on the same
    /// line rather than floating above it, so the bottom of the screen is one band of the same
    /// height instead of two things at two heights.
    /// </remarks>
    private void Strip(List<Placed> placed, float left, float right, float top)
    {
        var run = left;

        foreach (var entry in Members(placed, EditorDock.Strip))
        {
            var window = entry.Panel.Window!;

            // Never given the row's height, only its top. A member told how tall the row is would
            // measure that height next frame, and the row is as tall as its tallest member: two
            // frames of that and the strip is the whole window.
            window.PlaceAt(
                run + entry.Placement.X,
                top + entry.Placement.Y,
                entry.Placement.Width,
                entry.Placement.Height,
                entry.Rect);

            run += Width(entry) + Gap;
            if (run >= right) break;
        }
    }

    /// <summary>Places whatever floats in the viewport's corners.</summary>
    /// <remarks>
    /// Against the viewport rather than the window, so a button in a corner follows the panels: it
    /// moves inwards when a column opens and back out when one is closed, which is the whole point
    /// of putting it in the viewport rather than in a bar of its own.
    /// </remarks>
    private void Corners(List<Placed> placed, UiRect viewport)
    {
        foreach (var entry in placed)
        {
            var (x, y) = entry.Placement.Dock switch
            {
                EditorDock.ViewportTopLeft => (viewport.X + Margin, viewport.Y + Margin),
                EditorDock.ViewportTop => (
                    viewport.X + ((viewport.Width - Width(entry)) * 0.5f),
                    viewport.Y + Margin),
                EditorDock.ViewportTopRight => (
                    viewport.Right - Margin - Width(entry),
                    viewport.Y + Margin),
                EditorDock.ViewportBottomLeft => (
                    viewport.X + Margin,
                    viewport.Bottom - Margin - Height(entry)),
                EditorDock.ViewportBottomRight => (
                    viewport.Right - Margin - Width(entry),
                    viewport.Bottom - Margin - Height(entry)),
                _ => (float.NaN, float.NaN),
            };

            if (float.IsNaN(x)) continue;

            entry.Panel.Window!.PlaceAt(
                x + entry.Placement.X,
                y + entry.Placement.Y,
                entry.Placement.Width,
                entry.Placement.Height,
                entry.Rect);
        }
    }

    /// <summary>
    /// Places whatever carries its own coordinates, kept inside the window.
    /// </summary>
    /// <remarks>
    /// A flyout is opened at the thing that opened it, and the thing that opened it may be near an
    /// edge: a menu on the button at the foot of a panel would hang off the bottom of the screen
    /// and show its title and nothing else. Clamping against what it measured rather than against
    /// a guess is what makes a menu open upwards from a button at the bottom without anything
    /// having to ask for that.
    /// </remarks>
    private void Free(List<Placed> placed, float width, float height)
    {
        foreach (var entry in placed)
        {
            if (entry.Placement.Dock != EditorDock.Floating) continue;

            var x = entry.Placement.X;
            var y = entry.Placement.Y;

            if (entry.Rect.Width > 0f)
            {
                x = MathF.Max(Margin, MathF.Min(x, width - Margin - entry.Rect.Width));
            }

            if (entry.Rect.Height > 0f)
            {
                y = MathF.Max(Margin, MathF.Min(y, height - Margin - entry.Rect.Height));
            }

            entry.Panel.Window!.PlaceAt(
                x,
                y,
                entry.Placement.Width,
                entry.Placement.Height,
                entry.Rect);
        }
    }

    /// <summary>How tall the tallest panel of a dock is.</summary>
    private static float Tallest(List<Placed> placed, EditorDock dock)
    {
        var tallest = 0f;
        foreach (var entry in Members(placed, dock)) tallest = MathF.Max(tallest, Height(entry));

        return tallest;
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
    /// has one of and the wrong one for a menu, which is why a menu is placed where it was opened
    /// rather than by the layout.
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
        text.AppendLine($"left = {Size(LeftWidth)}");
        text.AppendLine($"right = {Size(RightWidth)}");
        text.AppendLine($"bottom = {BottomHeight}");

        foreach (var (panel, placement) in _overrides.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            text.AppendLine($"{panel} = {placement}");

        return text.ToString();
    }

    /// <summary>A dragged size as the file writes it, with <c>auto</c> for one never dragged.</summary>
    private static string Size(float value) => float.IsNaN(value) ? "auto" : value.ToString("0.#");

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

                case "left":
                    LeftWidth = float.TryParse(value, out var leftWidth) ? leftWidth : float.NaN;
                    continue;

                case "right":
                    RightWidth = float.TryParse(value, out var rightWidth) ? rightWidth : float.NaN;
                    continue;

                case "bottom" when float.TryParse(value, out var bottom):
                    BottomHeight = bottom;
                    continue;
            }

            if (PanelPlacement.TryParse(value, out var placement)) _overrides[name] = placement;
        }
    }
}
