using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// What the editor has been saying.
/// </summary>
/// <remarks>
/// The other thing an editor needs along the bottom, and the reason the tab strip holds more than
/// one: a script that will not compile says why, and a person who is editing scripts inside the
/// editor should not have to find the terminal it was started from to read it.
/// </remarks>
[EditorPanel(
    "panels/console.html",
    Root = "#console",
    Dock = EditorDock.Bottom,
    Order = 10)]
public sealed partial class ConsolePanel
{
    /// <summary>How many lines the document can draw.</summary>
    public const int Rows = 14;

    /// <summary>What each row says.</summary>
    [Bind("#crow", Count = Rows)]
    public string[] Lines = new string[Rows];

    /// <summary>Which rows stand for a line.</summary>
    [Show("#crow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>How much has been said.</summary>
    [Bind("#c-count", Mode = BindMode.OneWay)]
    public string Summary { get; private set; } = string.Empty;

    /// <summary>How far up the log the pool is looking, counted from the newest line.</summary>
    private int _scroll;

    /// <summary>How many lines there were when the panel last drew, so new ones bring it back.</summary>
    private int _seen;

    /// <summary>Fills the rows with the last of the log.</summary>
    [OnRefresh]
    public void Fill()
    {
        Roll();

        var all = EditorLog.All();

        // A new line pulls the view back to the bottom, which is what a log that is being watched
        // should do, and what a log being scrolled through should not: scrolling is only undone
        // by something new arriving.
        if (EditorLog.Written != _seen)
        {
            _seen = EditorLog.Written;
            _scroll = 0;
        }

        var last = Math.Max(0, all.Length - _scroll);
        var first = Math.Max(0, last - Rows);

        var written = 0;
        for (var i = first; i < last; i++)
        {
            Lines[written] = all[i];
            Shown[written] = true;
            written++;
        }

        for (var i = written; i < Rows; i++)
        {
            Lines[i] = string.Empty;
            Shown[i] = false;
        }

        Summary = all.Length == 0 ? "nothing yet" : $"{last}/{all.Length}";
    }

    /// <summary>Scrolls back through the log when the wheel is rolled over it.</summary>
    private void Roll()
    {
        if (EditorShell.Context is not { } ctx) return;

        var wheel = ctx.Input.WheelY;
        if (wheel == 0f) return;
        if (Window?.Covers(ctx.Input.MouseX, ctx.Input.MouseY) != true) return;

        _scroll = Math.Clamp(
            _scroll + ((int)wheel * 3), 0, Math.Max(0, EditorLog.All().Length - Rows));
    }

    /// <summary>Empties the log.</summary>
    [Command("#c-clear")]
    public void Clear()
    {
        EditorLog.Clear();
        _scroll = 0;
    }
}
