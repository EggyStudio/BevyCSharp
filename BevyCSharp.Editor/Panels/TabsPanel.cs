using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// The tabs along the very bottom.
/// </summary>
/// <remarks>
/// <para>
/// A row of names for the panels that are wanted often and not continuously. Clicking one opens
/// its panel along the bottom; clicking it again minimises it back to the name. Dragging one along
/// the strip changes the order, which is the whole of a tab's position.
/// </para>
/// <para>
/// Nothing here knows what any of the tabs are. <see cref="EditorTabs"/> holds a name and a way to
/// make a panel, and this draws whatever is in that list, so adding a tab is a line of
/// registration rather than a change to this panel.
/// </para>
/// </remarks>
[EditorPanel("panels/tabs.html", Root = "#tabs", Dock = EditorDock.Strip, Order = 0, Layer = 5)]
public sealed partial class TabsPanel
{
    /// <summary>How many tabs the document can draw.</summary>
    public const int Tabs = 8;

    /// <summary>What each tab says.</summary>
    [Bind("#tabtext", Count = Tabs)]
    public string[] Labels = new string[Tabs];

    /// <summary>Which tabs stand for anything.</summary>
    [Show("#tab", Count = Tabs)]
    public bool[] Shown = new bool[Tabs];

    /// <summary>Which tabs wear the dot, being the ones whose panel is open.</summary>
    [Show("#tabdot", Count = Tabs)]
    public bool[] Marked = new bool[Tabs];

    /// <summary>The tab a drag started on, for reordering.</summary>
    private int _dragging = -1;

    /// <summary>Draws the strip from the list of tabs.</summary>
    [OnRefresh]
    public void Fill()
    {
        var entries = EditorTabs.All;

        for (var i = 0; i < Tabs; i++)
        {
            if (i >= entries.Count)
            {
                Labels[i] = string.Empty;
                Shown[i] = false;
                Marked[i] = false;
                continue;
            }

            var entry = entries[i];

            // An open tab wears a dot rather than a mark in its own text, since a row's class
            // cannot be changed while the editor runs and a character in front of the name moves
            // the name every time one is opened.
            Labels[i] = entry.Name;
            Shown[i] = true;
            Marked[i] = entry.IsOpen;
        }

        Drag();
    }

    /// <summary>Opens or minimises a tab.</summary>
    [Command("#tab", Count = Tabs)]
    public void Press(int index)
    {
        if (index >= EditorTabs.All.Count) return;

        // A drag that ended somewhere else was a reorder, and the click that ends it is not also
        // a press on the tab it landed on.
        if (_reordered)
        {
            _reordered = false;
            return;
        }

        EditorTabs.Toggle(EditorTabs.All[index]);
    }

    /// <summary>Whether the last release finished a reorder rather than a press.</summary>
    private bool _reordered;

    /// <summary>Moves a tab when it is dragged along the strip.</summary>
    private void Drag()
    {
        if (EditorShell.Context is not { } ctx) return;
        if (Window is not { IsOpen: true } window) return;

        var (x, y) = ctx.Input.MousePosition;

        if (ctx.Input.MousePressed(MouseButton.Left)) _dragging = TabAt(window, x, y);

        if (!ctx.Input.MouseReleased(MouseButton.Left)) return;

        var from = _dragging;
        _dragging = -1;

        if (from < 0) return;

        var onto = TabAt(window, x, y);
        if (onto < 0 || onto == from) return;

        EditorTabs.Reorder(from, onto);
        _reordered = true;
    }

    /// <summary>Which tab a point is over, or -1.</summary>
    private int TabAt(EditorWindow window, float x, float y)
    {
        for (var i = 0; i < Tabs; i++)
        {
            if (!Shown[i]) continue;

            var element = window.Element($"tab-{i}");
            if (element.IsNone) continue;
            if (!Xui.TryRect(element, out var rect)) continue;
            if (rect.Contains(x, y)) return i;
        }

        return -1;
    }
}
