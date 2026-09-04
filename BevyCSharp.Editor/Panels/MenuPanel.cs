using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// One level of a menu, as a flyout.
/// </summary>
/// <remarks>
/// <para>
/// The same panel serves the hamburger menu, a right-click on the world, a right-click on an
/// entity, an enum field's dropdown and the list of components that can be added. All of those
/// are a list of rows to choose from, and building four of them would have produced four sets of
/// bugs.
/// </para>
/// <para>
/// It shows one level at a time rather than opening a second flyout beside itself: a submenu row
/// replaces the contents and the title becomes the way back. That reads well at this size, and it
/// avoids a tree of windows each of which would need its own dismissal rules.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/menu.html",
    Root = "#menu",
    Dismiss = PanelDismiss.OnOutsideClick,
    Layer = 100)]
public sealed partial class MenuPanel
{
    /// <summary>How many rows the document declares.</summary>
    public const int Rows = 18;

    /// <summary>Opens a menu at a path in the editor's own table.</summary>
    public MenuPanel(string path = "", string? title = null)
    {
        _path = path;
        _title = title;
    }

    /// <summary>Opens a menu over a list built for the occasion, such as an enum's values.</summary>
    /// <remarks>
    /// A flat list rather than a tree: the rows are not part of the editor's menu table and have
    /// nothing under them, so there is nowhere to navigate to.
    /// </remarks>
    public MenuPanel(string title, IReadOnlyList<MenuItem> items)
    {
        _title = title;
        _items = items;
    }

    private readonly string _path = string.Empty;
    private readonly string? _title;
    private readonly IReadOnlyList<MenuItem>? _items;

    /// <summary>Which level is showing, when the menu is a tree.</summary>
    private string _level = string.Empty;

    /// <summary>Which page of a level longer than the rows, counted in pages.</summary>
    private int _page;

    /// <summary>What each row stands for, so a click can be turned back into an item.</summary>
    private readonly MenuItem?[] _rows = new MenuItem?[Rows];

    /// <summary>What each row says.</summary>
    [Bind("#mrow", Count = Rows)]
    public string[] Labels = new string[Rows];

    /// <summary>Which rows stand for anything.</summary>
    [Show("#mrow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>The level being shown, which is also the way back out of it.</summary>
    [Bind("#menu-title", Mode = BindMode.OneWay)]
    public string Title { get; private set; } = string.Empty;

    /// <summary>Fills the rows from the level or the list.</summary>
    [OnRefresh]
    public void Fill()
    {
        if (_level.Length == 0 && _path.Length > 0) _level = _path;

        var items = _items ?? EditorMenu.Level(_level);
        var paged = items.Count > Rows;
        var room = paged ? Rows - 1 : Rows;
        var start = _page * room;

        Title = _title ?? (_level.Length == 0 ? "Menu" : "< " + _level.Replace("/", " / "));

        var written = 0;

        for (var i = start; i < items.Count && written < room; i++)
        {
            var item = items[i];

            Labels[written] = item.Kind switch
            {
                MenuKind.Separator => "  ----",
                MenuKind.Submenu => item.Label + "   >",
                MenuKind.Toggle => (item.Checked?.Invoke() == true ? "[x] " : "[ ] ") + item.Label,
                _ => "  " + item.Label,
            };

            _rows[written] = item;
            Shown[written] = true;
            written++;
        }

        if (paged && written < Rows)
        {
            Labels[written] = start + room < items.Count ? "  more..." : "  back to the start";
            _rows[written] = null;
            Shown[written] = true;
            written++;
        }

        for (var i = written; i < Rows; i++)
        {
            Labels[i] = string.Empty;
            _rows[i] = null;
            Shown[i] = false;
        }
    }

    /// <summary>Runs, toggles, or opens whatever a row stands for.</summary>
    [Command("#mrow", Count = Rows)]
    public void Choose(int row)
    {
        if (!Shown[row]) return;

        if (_rows[row] is not { } item)
        {
            // The paging row: on to the next page, or round to the first.
            var items = _items ?? EditorMenu.Level(_level);
            var room = Rows - 1;
            _page = (_page + 1) * room < items.Count ? _page + 1 : 0;
            return;
        }

        switch (item.Kind)
        {
            case MenuKind.Separator:
                return;

            case MenuKind.Submenu:
                _level = item.Path;
                _page = 0;
                return;

            default:
                if (item.Enabled?.Invoke() == false) return;

                item.Run?.Invoke(EditorShell.Ecs);

                // A toggle leaves the menu open, because turning three things on in a row is the
                // ordinary use of one. Anything else has done its job and gets out of the way.
                if (item.Kind != MenuKind.Toggle) EditorShell.Hide(this);

                return;
        }
    }

    /// <summary>Goes back up a level, or closes at the top.</summary>
    [Command("#menu-title")]
    public void Back()
    {
        if (_items is not null || _level.Length == 0 || _level == _path)
        {
            EditorShell.Hide(this);
            return;
        }

        var cut = _level.LastIndexOf('/');
        _level = cut < 0 ? string.Empty : _level[..cut];
        _page = 0;
    }
}
