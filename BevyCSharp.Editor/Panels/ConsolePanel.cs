using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// What the editor has been saying, and somewhere to answer it.
/// </summary>
/// <remarks>
/// <para>
/// The other thing an editor needs along the bottom, and the reason the tab strip holds more than
/// one: a script that will not compile says why, and a person who is editing scripts inside the
/// editor should not have to find the terminal it was started from to read it.
/// </para>
/// <para>
/// It reads and writes, like every console since the first one. What is typed goes to the same
/// list of commands the quick console uses, which is the same list a game gets by declaring one.
/// </para>
/// </remarks>
[UiPanel(
    "panels/console.html",
    Root = "#console",
    Dock = UiDock.Bottom,
    Dismiss = UiDismiss.OnOutsideClick,
    Layer = 30,
    Order = 10)]
public sealed partial class ConsolePanel
{
    /// <summary>How many lines the document can draw.</summary>
    public const int Rows = 18;

    /// <summary>What each row says.</summary>
    [Bind("#crow", Count = Rows)]
    public string[] Lines = new string[Rows];

    /// <summary>Which rows stand for a line.</summary>
    [Show("#crow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>How much has been said.</summary>
    [Bind("#c-count", Mode = BindMode.OneWay)]
    public string Summary { get; private set; } = string.Empty;

    /// <summary>What to look for, or nothing to see everything.</summary>
    [Bind("#c-filter")]
    public string Filter = string.Empty;

    /// <summary>What has been typed but not yet run.</summary>
    [Bind("#c-input")]
    public string Typed = string.Empty;

    /// <summary>What the half-typed line would become, or what it takes.</summary>
    [Bind("#c-hint", Mode = BindMode.OneWay)]
    public string Hint { get; private set; } = string.Empty;

    /// <summary>Everything this console does that is not about being drawn here.</summary>
    private readonly ConsoleView _view = new();

    /// <summary>What class each row wears, so it is written once.</summary>
    private readonly string[] _dressed = new string[Rows];

    /// <summary>How many lines there were when it last drew, so new ones bring it back.</summary>
    private int _seen;

    /// <summary>Fills the rows with the last of the log.</summary>
    [OnRefresh]
    public void Fill()
    {
        Roll();
        Keys();

        _view.Search = Filter;

        var all = _view.Lines();

        // A new line brings the view back to the end. Somebody who has scrolled up is reading, and
        // is left where they were until they scroll back down.
        if (ConsoleLog.Written != _seen)
        {
            _seen = ConsoleLog.Written;
            if (_view.Scroll == 0) _view.Scroll = 0;
        }

        // Only as many as there is room for. The band is as tall as the tab strip gives it, and a
        // console that fills all eighteen of its rows regardless has its newest lines clipped off
        // the bottom, which for a log is exactly the wrong end.
        var room = Fits();

        _view.Scroll = Math.Clamp(_view.Scroll, 0, Math.Max(0, all.Length - room));

        var last = all.Length - _view.Scroll;
        var first = Math.Max(0, last - room);

        for (var row = 0; row < Rows; row++)
        {
            var index = first + row;

            if (index >= last)
            {
                Lines[row] = string.Empty;
                Shown[row] = false;
                continue;
            }

            Lines[row] = ConsoleView.Written(all[index]);
            Shown[row] = true;
            Dress(row, ConsoleView.Dress(all[index].Level));
        }

        Summary = Describe(all.Length, ConsoleLog.Written);
        Hint = _view.Hint(Typed);

        Wear("c-info", _view.ShowInfo);
        Wear("c-warn", _view.ShowWarnings);
        Wear("c-bad", _view.ShowErrors);
    }

    /// <summary>
    /// How many rows there is room for.
    /// </summary>
    /// <remarks>
    /// Measured, and measurable: unlike a panel that is as tall as its contents, the band is given
    /// a height and the list takes what is left of it, so asking how tall the list is does not ask
    /// how tall the list is.
    /// </remarks>
    private int Fits()
    {
        if (Window is not { IsOpen: true } window) return Rows;
        if (!Xui.TryRect(window.Element("c-rows"), out var rect) || rect.Height <= 0f) return Rows;

        return Math.Clamp((int)(rect.Height / LineHeight), 1, Rows);
    }

    /// <summary>How tall one line is, with the gap under it, as the stylesheet has it.</summary>
    private const float LineHeight = 15f;

    /// <summary>Runs what was typed, and answers the keys a console answers.</summary>
    /// <remarks>
    /// Read from the keyboard rather than from a submit event, because the interface does not
    /// report one. What it does report is which element has the keyboard, which is enough: the
    /// keys only mean this while somebody is typing here.
    /// </remarks>
    private void Keys()
    {
        if (EditorShell.Context is not { } ctx) return;
        if (Window is not { IsOpen: true } window) return;
        if (PanelBinding.Focused != window.Element("c-input")) return;

        if (ctx.Input.KeyPressed(Key.Enter) || ctx.Input.KeyPressed(Key.NumpadEnter))
        {
            _view.Run(Typed);
            Typed = string.Empty;
            return;
        }

        if (ctx.Input.KeyPressed(Key.Tab) && _view.Completion(Typed) is { } completion)
        {
            Typed = completion + " ";
            return;
        }

        if (ctx.Input.KeyPressed(Key.ArrowUp)) Typed = _view.Back(Typed);
        if (ctx.Input.KeyPressed(Key.ArrowDown)) Typed = _view.Forward(Typed);
    }

    /// <summary>Runs a line, as pressing return does.</summary>
    /// <remarks>
    /// Public so that something other than a key press can drive it: a test, a probe, or one part
    /// of the editor asking another to say something.
    /// </remarks>
    public void Run(string line)
    {
        _view.Run(line);
        Typed = string.Empty;
    }

    /// <summary>Takes what the half-typed line would become, as the tab key does.</summary>
    public void Complete()
    {
        if (_view.Completion(Typed) is { } completion) Typed = completion + " ";
    }

    /// <summary>Says how loudly a row is speaking, by the class it wears.</summary>
    private void Dress(int row, string wanted)
    {
        if (_dressed[row] == wanted) return;
        if (Window is not { IsOpen: true } window) return;

        var element = window.Element($"crow-{row}");
        if (element.IsNone) return;

        Xui.SetClass(element, wanted);
        _dressed[row] = wanted;
    }

    /// <summary>Says whether a level is being shown, by the class its chip wears.</summary>
    private void Wear(string element, bool on)
    {
        var wanted = on ? "chip" : "chip off";
        if (_chips.TryGetValue(element, out var already) && already == wanted) return;
        if (Window is not { IsOpen: true } window) return;

        var found = window.Element(element);
        if (found.IsNone) return;

        Xui.SetClass(found, wanted);
        _chips[element] = wanted;
    }

    /// <summary>What each chip is wearing, so it is written once.</summary>
    private readonly Dictionary<string, string> _chips = [];

    /// <summary>Shows or hides the ordinary lines.</summary>
    [OnClick("#c-info")]
    public void ToggleInfo() => _view.ShowInfo = !_view.ShowInfo;

    /// <summary>The same for warnings.</summary>
    [OnClick("#c-warn")]
    public void ToggleWarnings() => _view.ShowWarnings = !_view.ShowWarnings;

    /// <summary>And for errors.</summary>
    [OnClick("#c-bad")]
    public void ToggleErrors() => _view.ShowErrors = !_view.ShowErrors;

    /// <summary>How the count reads.</summary>
    private static string Describe(int showing, int written) => written == 0
        ? "nothing yet"
        : showing == written ? $"{written} lines" : $"{showing} of {written}";

    /// <summary>Scrolls the log when the wheel is rolled over the panel.</summary>
    private void Roll()
    {
        if (EditorShell.Context is not { } ctx) return;

        var wheel = ctx.Input.WheelY;
        if (wheel == 0f) return;
        if (Window?.Covers(ctx.Input.MouseX, ctx.Input.MouseY) != true) return;

        _view.Scroll += (int)wheel * 3;
    }

    /// <summary>Forgets everything the log holds.</summary>
    [OnClick("#c-clear")]
    public void Clear()
    {
        ConsoleLog.Clear();
        _view.Scroll = 0;
    }
}
