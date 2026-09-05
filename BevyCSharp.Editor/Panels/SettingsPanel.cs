using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// Everything the editor and the project keep that is not part of the world.
/// </summary>
/// <remarks>
/// <para>
/// A sheet over the whole window: a list of pages on the left, the chosen page on the right. That
/// is what every editor does with settings, and for a good reason — a page of preferences squeezed
/// into a column beside a hierarchy is a page nobody opens, and settings are read carefully and
/// rarely rather than glanced at continuously.
/// </para>
/// <para>
/// The panel knows about no setting in particular. <see cref="EditorSettings"/> is a table, the
/// pages are whatever has anything on it, and a row is drawn from what the entry says it is —
/// which is the same arrangement as the menu, the toolbar and the inspector, for the same reason:
/// adding a preference should be a line, not a panel.
/// </para>
/// </remarks>
[EditorPanel("panels/settings.html", Root = "#settings", Dock = EditorDock.Sheet, Layer = 60)]
public sealed partial class SettingsPanel
{
    /// <summary>How many pages the list can draw.</summary>
    public const int Pages = 20;

    /// <summary>How many rows one page can draw.</summary>
    public const int Rows = 28;

    /// <summary>What each page is called.</summary>
    [Bind("#sptext", Count = Pages)]
    public string[] PageNames = new string[Pages];

    /// <summary>Which pages stand for anything.</summary>
    [Show("#spage", Count = Pages)]
    public bool[] PageShown = new bool[Pages];

    /// <summary>Which page is open.</summary>
    [Bind("#s-page", Mode = BindMode.OneWay)]
    public string Where { get; private set; } = string.Empty;

    /// <summary>Each row's label.</summary>
    [Bind("#sname", Count = Rows)]
    public string[] Names = new string[Rows];

    /// <summary>The editor for anything typed in.</summary>
    [Bind("#sv", Count = Rows)]
    public string[] Values = new string[Rows];

    /// <summary>What a row shows when it is read and not edited.</summary>
    [Bind("#st", Count = Rows)]
    public string[] Texts = new string[Rows];

    /// <summary>The checkbox a flag draws.</summary>
    [Bind("#sc", Count = Rows)]
    public bool[] Flags = new bool[Rows];

    /// <summary>What a row's button says.</summary>
    [Bind("#sbtext", Count = Rows)]
    public string[] Buttons = new string[Rows];

    /// <summary>Which rows stand for anything.</summary>
    [Show("#srow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>Which rows draw a box to type in.</summary>
    [Show("#sv", Count = Rows)]
    public bool[] ShowValue = new bool[Rows];

    /// <summary>Which rows draw a plain value.</summary>
    [Show("#st", Count = Rows)]
    public bool[] ShowText = new bool[Rows];

    /// <summary>Which rows draw a checkbox.</summary>
    [Show("#sc", Count = Rows)]
    public bool[] ShowFlag = new bool[Rows];

    /// <summary>Which rows draw a button.</summary>
    [Show("#sb", Count = Rows)]
    public bool[] ShowButton = new bool[Rows];

    /// <summary>The page being shown, or empty for the first one there is.</summary>
    private string _page = string.Empty;

    /// <summary>What each row stands for.</summary>
    private readonly EditorSetting?[] _rows = new EditorSetting?[Rows];

    /// <summary>What each list row stands for.</summary>
    private readonly string[] _pages = new string[Pages];

    /// <summary>Fills the list and the page.</summary>
    [OnRefresh]
    public void Fill()
    {
        var pages = EditorSettings.Pages;

        if (_page.Length == 0 || !pages.Contains(_page))
        {
            _page = pages.Count > 0 ? pages[0] : string.Empty;
        }

        Where = _page;

        for (var i = 0; i < Pages; i++)
        {
            if (i >= pages.Count)
            {
                PageNames[i] = string.Empty;
                PageShown[i] = false;
                _pages[i] = string.Empty;
                continue;
            }

            // The open page is marked in its own text, since a row's class cannot be changed
            // while the editor runs.
            PageNames[i] = pages[i] == _page
                ? EditorIcons.Selected + " " + pages[i]
                : "  " + pages[i];

            PageShown[i] = true;
            _pages[i] = pages[i];
        }

        var entries = _page.Length == 0
            ? Array.Empty<EditorSetting>()
            : [.. EditorSettings.On(_page)];

        for (var row = 0; row < Rows; row++)
        {
            if (row >= entries.Length)
            {
                Clear(row);
                continue;
            }

            Write(row, entries[row]);
        }
    }

    /// <summary>Draws one setting.</summary>
    private void Write(int row, EditorSetting entry)
    {
        _rows[row] = entry;

        Names[row] = entry.Kind == SettingKind.Heading ? entry.Label : "  " + entry.Label;
        Shown[row] = true;

        ShowValue[row] = entry.Kind is SettingKind.Text or SettingKind.Number;
        ShowText[row] = entry.Kind == SettingKind.Fact;
        ShowFlag[row] = entry.Kind == SettingKind.Flag;
        ShowButton[row] = entry.Kind is SettingKind.Choice or SettingKind.Action;

        var says = entry.Read?.Invoke() ?? string.Empty;

        Values[row] = ShowValue[row] ? says : string.Empty;
        Texts[row] = ShowText[row] ? says : string.Empty;
        Flags[row] = ShowFlag[row] && says == "1";

        Buttons[row] = entry.Kind switch
        {
            SettingKind.Choice => says,
            SettingKind.Action => "Run",
            _ => string.Empty,
        };
    }

    /// <summary>Empties a row.</summary>
    private void Clear(int row)
    {
        _rows[row] = null;
        Names[row] = string.Empty;
        Values[row] = string.Empty;
        Texts[row] = string.Empty;
        Buttons[row] = string.Empty;
        Flags[row] = false;
        Shown[row] = false;
        ShowValue[row] = false;
        ShowText[row] = false;
        ShowFlag[row] = false;
        ShowButton[row] = false;
    }

    /// <summary>Opens a page.</summary>
    [Command("#spage", Count = Pages)]
    public void Open(int row)
    {
        if (!PageShown[row]) return;

        _page = _pages[row];
    }

    /// <summary>
    /// Hands every edited row back to whatever keeps its value.
    /// </summary>
    /// <remarks>
    /// The whole page at once rather than a row at a time, which is what the interface reports:
    /// a value that has not changed is written back identical, and a setting that cares can tell
    /// the difference itself.
    /// </remarks>
    [OnChange]
    public void Apply()
    {
        for (var row = 0; row < Rows; row++)
        {
            if (_rows[row] is not { Write: { } write } entry) continue;

            switch (entry.Kind)
            {
                case SettingKind.Text:
                case SettingKind.Number:
                    write(Values[row]);
                    break;

                case SettingKind.Flag:
                    write(Flags[row] ? "1" : "0");
                    break;
            }
        }
    }

    /// <summary>Runs an action, or offers the names a choice may take.</summary>
    [Command("#sb", Count = Rows)]
    public void Pressed(int row)
    {
        if (_rows[row] is not { } entry) return;

        if (entry.Kind == SettingKind.Action)
        {
            entry.Write?.Invoke(string.Empty);
            return;
        }

        if (entry.Options is not { Count: > 0 } options) return;

        var items = new List<MenuItem>();

        foreach (var option in options)
        {
            var chosen = option;
            items.Add(new MenuItem(chosen, MenuKind.Command, _ => entry.Write?.Invoke(chosen)));
        }

        var (x, y) = Under($"sb-{row}");
        EditorShell.ShowMenu(entry.Label, items, x, y);
    }

    /// <summary>Puts the sheet away.</summary>
    [Command("#s-close")]
    public void Done() => EditorShell.Conceal(this);

    /// <summary>Just under an element, which is where a list it opens belongs.</summary>
    private (float X, float Y) Under(string id)
    {
        if (Window is { } window && Xui.TryRect(window.Element(id), out var rect))
        {
            return (rect.X, rect.Bottom + 2f);
        }

        return EditorShell.Context?.Input.MousePosition ?? (0f, 0f);
    }
}
