using System.Text;
using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Works out where every open panel goes, and puts it there.
/// </summary>
/// <remarks>
/// <para>
/// A layout is a table of placements, one per panel, and this is the thing that reads it. That is
/// the whole design: because a placement is data, a layout can be saved, restored, edited by
/// hand and changed by dragging a window, and none of those need a different mechanism from the
/// others. A stylesheet cannot do any of it.
/// </para>
/// <para>
/// Panels in the same region stack. Sizes come from measuring what the layout actually produced
/// rather than from what was asked for, so a panel as tall as its contents stacks correctly
/// under one that is a fixed height, and a panel whose contents changed moves its neighbours the
/// same frame.
/// </para>
/// </remarks>
public sealed class EditorLayout
{
    private readonly Dictionary<string, PanelPlacement> _overrides = [];

    /// <summary>How far a panel keeps from the edge of the window.</summary>
    public float Margin { get; set; } = 10f;

    /// <summary>How far panels in the same region keep from each other.</summary>
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

        foreach (var region in Regions)
        {
            var members = new List<(IEditorPanel Panel, PanelPlacement Placement, UiRect Rect)>();

            foreach (var panel in panels)
            {
                var placement = PlacementOf(panel);
                if (placement.Region != region) continue;
                if (panel.Window is not { IsOpen: true } window) continue;
                if (window.Root.IsNone) continue;
                if (!Xui.TryRect(window.Root, out var rect)) continue;

                members.Add((panel, placement, rect));
            }

            if (members.Count == 0) continue;

            if (region == EditorRegion.Free)
            {
                foreach (var (panel, placement, rect) in members)
                {
                    panel.Window!.PlaceAt(
                        placement.X, placement.Y, placement.Width, placement.Height, rect);
                }

                continue;
            }

            Stack(region, members, windowWidth, windowHeight);
        }
    }

    /// <summary>Places one region's panels one after another along its stacking axis.</summary>
    private void Stack(
        EditorRegion region,
        List<(IEditorPanel Panel, PanelPlacement Placement, UiRect Rect)> members,
        float windowWidth,
        float windowHeight)
    {
        var horizontal = region is EditorRegion.Top or EditorRegion.Bottom;

        // How much room the whole stack needs, which only a centred region has to know: an edge
        // region starts at its edge and grows away from it.
        var total = 0f;
        foreach (var (_, placement, rect) in members)
        {
            var size = horizontal
                ? Size(placement.Width, rect.Width)
                : Size(placement.Height, rect.Height);

            total += size + Gap;
        }

        total = Math.Max(0f, total - Gap);

        var (across, down) = Anchors(region);
        var run = horizontal
            ? Start(across, windowWidth, total)
            : Start(down, windowHeight, total);

        foreach (var (panel, placement, rect) in members)
        {
            var width = Size(placement.Width, rect.Width);
            var height = Size(placement.Height, rect.Height);

            var x = horizontal ? run : Start(across, windowWidth, width);
            var y = horizontal ? Start(down, windowHeight, height) : run;

            // A region's own offsets nudge a panel without taking it out of its region, which is
            // what lets a saved layout say "the inspector, sixteen pixels lower".
            panel.Window!.PlaceAt(
                x + placement.X,
                y + placement.Y,
                placement.Width,
                placement.Height,
                rect);

            run += (horizontal ? width : height) + Gap;
        }
    }

    /// <summary>Where a run of <paramref name="size"/> starts along an axis of <paramref name="extent"/>.</summary>
    private float Start(Anchor anchor, float extent, float size) => anchor switch
    {
        Anchor.Start => Margin,
        Anchor.End => extent - Margin - size,
        _ => (extent - size) * 0.5f,
    };

    /// <summary>The size to lay out with: what was asked for, or what was measured.</summary>
    private static float Size(float asked, float measured) => float.IsNaN(asked) ? measured : asked;

    /// <summary>Which edge of each axis a region hangs from.</summary>
    private static (Anchor Across, Anchor Down) Anchors(EditorRegion region) => region switch
    {
        EditorRegion.TopLeft => (Anchor.Start, Anchor.Start),
        EditorRegion.Top => (Anchor.Middle, Anchor.Start),
        EditorRegion.TopRight => (Anchor.End, Anchor.Start),
        EditorRegion.Left => (Anchor.Start, Anchor.Middle),
        EditorRegion.Centre => (Anchor.Middle, Anchor.Middle),
        EditorRegion.Right => (Anchor.End, Anchor.Middle),
        EditorRegion.BottomLeft => (Anchor.Start, Anchor.End),
        EditorRegion.Bottom => (Anchor.Middle, Anchor.End),
        EditorRegion.BottomRight => (Anchor.End, Anchor.End),
        _ => (Anchor.Start, Anchor.Start),
    };

    /// <summary>Which end of an axis something is measured from.</summary>
    private enum Anchor
    {
        Start,
        Middle,
        End,
    }

    /// <summary>Every region, in the order they are arranged.</summary>
    private static readonly EditorRegion[] Regions = Enum.GetValues<EditorRegion>();

    /// <summary>What a panel is called in a saved layout.</summary>
    /// <remarks>
    /// The type's name rather than the instance, so a layout survives a restart. Two panels of
    /// the same type share a placement, which is the right answer for the panels an editor
    /// actually has one of and the wrong one for a flyout, which is why a flyout is placed
    /// where it was opened rather than by the layout.
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
