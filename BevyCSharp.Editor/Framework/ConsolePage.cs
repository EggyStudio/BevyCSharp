using System.Text;
using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Where the console is drawn.
/// </summary>
/// <remarks>
/// <para>
/// What every game has had under Escape since Quake: it drops out of the top, takes the keyboard,
/// and what is typed into it runs. The lines it shows are whatever the program wrote, because
/// <see cref="ConsoleLog"/> tees the output stream, so a <c>Console.WriteLine</c> anywhere appears
/// here without anybody arranging it.
/// </para>
/// <para>
/// What a console does is <see cref="ConsoleView"/>'s; this is the markup and the two attributes
/// that show it. Hidden rather than removed when it is put away, so what was typed and how far it
/// was scrolled survive being closed.
/// </para>
/// </remarks>
public static class ConsolePage
{
    private static readonly ConsoleView View = new();

    private static int _shown;

    /// <summary>Whether the console is up.</summary>
    public static bool IsOpen { get; private set; }

    /// <summary>What the console knows, for a command that wants to ask it something.</summary>
    public static ConsoleView Console => View;

    /// <summary>Drops the console in, or puts it away.</summary>
    public static void Toggle()
    {
        var sheet = EditorShell.Part("console");
        if (!sheet.Exists) return;

        IsOpen = !IsOpen;
        Dom.ToggleClass(sheet, "hidden", !IsOpen);

        var entry = EditorShell.Part("console-entry");
        if (!entry.Exists) return;

        // The keyboard goes with it, both ways: a console that opens without taking the keyboard
        // needs a click before it can be typed in, and one that keeps it after closing eats every
        // key the editor binds.
        if (IsOpen) Dom.Focus(entry);
        else Dom.Blur();
    }

    /// <summary>Shows whatever has been written since the last look.</summary>
    public static void Draw()
    {
        if (!IsOpen || _shown == ConsoleLog.Written) return;

        _shown = ConsoleLog.Written;

        var markup = new StringBuilder();

        foreach (var line in View.Lines())
        {
            // The level is written on the row and the stylesheet colors it, so what a warning
            // looks like is one rule rather than a decision taken here per line.
            markup.Append($"<div class=\"line\" data-level=\"{Level(line.Level)}\">")
                .Append(EditorShell.Escape(ConsoleView.Written(line)))
                .Append("</div>");
        }

        EditorShell.Fill("console-lines", markup.ToString());
    }

    /// <summary>What a level is called in the stylesheet.</summary>
    private static string Level(LogLevel level) => level switch
    {
        LogLevel.Warning => "warn",
        LogLevel.Error => "error",
        LogLevel.Echo => "said",
        _ => "info",
    };

    /// <summary>Runs what was typed.</summary>
    public static void Submit()
    {
        var entry = EditorShell.Part("console-entry");
        if (!entry.Exists) return;

        var typed = Dom.GetValue(entry);
        Dom.SetValue(entry, string.Empty);

        View.Run(typed);
    }
}
