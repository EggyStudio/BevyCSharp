using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// The console a key drops into the middle of the window.
/// </summary>
/// <remarks>
/// <para>
/// The same console as the tab along the bottom, in the shape somebody wants when they already
/// know what to type: it appears where the eye is, takes one line, shows what answered, and goes
/// away. Every game with a console has had this key since before any of them had a tab.
/// </para>
/// <para>
/// It shares everything but its size with the tab, which is why neither of them holds any of it:
/// the log, the commands and the history all live outside the panels.
/// </para>
/// </remarks>
[UiPanel(
    "panels/quick.html",
    Root = "#quick",
    Dock = UiDock.Centre,
    Dismiss = UiDismiss.Never,
    Layer = 90)]
public sealed partial class QuickConsolePanel
{
    /// <summary>How many lines it shows.</summary>
    public const int Rows = 10;

    /// <summary>What each row says.</summary>
    [Bind("#qrow", Count = Rows)]
    public string[] Lines = new string[Rows];

    /// <summary>Which rows stand for a line.</summary>
    [Show("#qrow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>How much has been said.</summary>
    [Bind("#q-count", Mode = BindMode.OneWay)]
    public string Summary { get; private set; } = string.Empty;

    /// <summary>What has been typed but not yet run.</summary>
    [Bind("#q-input")]
    public string Typed = string.Empty;

    /// <summary>What the half-typed line would become, or what it takes.</summary>
    [Bind("#q-hint", Mode = BindMode.OneWay)]
    public string Hint { get; private set; } = string.Empty;

    /// <summary>Everything this console does that is not about being drawn here.</summary>
    private readonly ConsoleView _view = new();

    /// <summary>What class each row wears, so it is written once.</summary>
    private readonly string[] _dressed = new string[Rows];

    /// <summary>Whether the keyboard has been handed to the input since it opened.</summary>
    private bool _asked;

    /// <summary>Fills the rows with the last of the log.</summary>
    [OnRefresh]
    public void Fill()
    {
        Keys();
        Focus();

        var all = _view.Lines();
        var first = Math.Max(0, all.Length - Rows);

        for (var row = 0; row < Rows; row++)
        {
            var index = first + row;

            if (index >= all.Length)
            {
                Lines[row] = string.Empty;
                Shown[row] = false;
                continue;
            }

            Lines[row] = ConsoleView.Written(all[index]);
            Shown[row] = true;
            Dress(row, ConsoleView.Dress(all[index].Level));
        }

        Summary = ConsoleCommands.All.Count switch
        {
            0 => "no commands",
            1 => "1 command",
            var many => $"{many} commands",
        };

        Hint = _view.Hint(Typed);
    }

    /// <summary>
    /// Hands the keyboard to the input when it opens.
    /// </summary>
    /// <remarks>
    /// The point of a console opened by a key is that it is typed into without anything else being
    /// pressed first. One that has to be clicked into is one that costs more than finding the tab.
    /// </remarks>
    private void Focus()
    {
        if (_asked) return;
        if (Window is not { IsOpen: true } window) return;

        var input = window.Element("q-input");
        if (input.IsNone) return;

        Xui.Focus(input);
        _asked = true;
    }

    /// <summary>Runs what was typed, and answers the keys a console answers.</summary>
    private void Keys()
    {
        if (EditorShell.Context is not { } ctx) return;
        if (Window is not { IsOpen: true } window) return;
        if (PanelBinding.Focused != window.Element("q-input")) return;

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

        var element = window.Element($"qrow-{row}");
        if (element.IsNone) return;

        Xui.SetClass(element, wanted);
        _dressed[row] = wanted;
    }

    /// <summary>
    /// Shows it, or puts it away if it is already up.
    /// </summary>
    /// <remarks>
    /// The key that opens it closes it, which is what a key that summons something has always
    /// done. Closing gives the keyboard back, so the next thing typed goes wherever it would have
    /// gone.
    /// </remarks>
    public static void Toggle()
    {
        var panel = EditorShell.Find<QuickConsolePanel>();

        if (panel is not null && EditorShell.IsShowing(panel))
        {
            Xui.Blur();
            EditorShell.Conceal(panel);
            return;
        }

        panel ??= EditorShell.Show(new QuickConsolePanel());
        panel._asked = false;
        panel.Typed = string.Empty;

        EditorShell.Reveal(panel);
    }

    /// <summary>Whether it is up, so the key that closes everything can start with this.</summary>
    public static bool IsOpen =>
        EditorShell.Find<QuickConsolePanel>() is { } panel && EditorShell.IsShowing(panel);
}
