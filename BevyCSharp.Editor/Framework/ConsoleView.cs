using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What a console does, apart from where it is drawn.
/// </summary>
/// <remarks>
/// <para>
/// There are two consoles: the tab along the bottom, which is where somebody reads a log, and the
/// one a key drops into the middle of the window, which is where somebody types one command and
/// dismisses it. They differ in size and in nothing else, so what they have in common lives here
/// and each of them is a document and a handful of bindings.
/// </para>
/// <para>
/// It keeps what was typed, what was typed before, and what is being looked for. Everything it
/// knows about the log and the commands it asks the library for, so a game's console and the
/// editor's are the same console pointed at the same two lists.
/// </para>
/// </remarks>
public sealed class ConsoleView
{
    /// <summary>Which levels are shown.</summary>
    /// <remarks>
    /// All of them, until somebody says otherwise. A console that hides errors by default is a
    /// console that lies, and the first thing anybody does with one is look for an error.
    /// </remarks>
    public bool ShowInfo { get; set; } = true;

    /// <inheritdoc cref="ShowInfo"/>
    public bool ShowWarnings { get; set; } = true;

    /// <inheritdoc cref="ShowInfo"/>
    public bool ShowErrors { get; set; } = true;

    /// <summary>What is being looked for, or nothing.</summary>
    public string Search { get; set; } = string.Empty;

    /// <summary>How far up the log the view is looking, counted in lines from the newest.</summary>
    public int Scroll { get; set; }

    /// <summary>What has been typed and run, newest last.</summary>
    private readonly List<string> _history = [];

    /// <summary>How far back through the history the arrows have gone, or minus one.</summary>
    private int _recalled = -1;

    /// <summary>The lines worth showing, oldest first.</summary>
    public LogLine[] Lines()
    {
        var kept = new List<LogLine>();

        // Trimmed, because a filter that finds nothing on account of a space somebody typed after
        // the word is a filter that looks broken.
        var wanted = Search.Trim();

        foreach (var line in ConsoleLog.All())
        {
            if (!Shows(line.Level)) continue;

            if (wanted.Length > 0
                && !line.Text.Contains(wanted, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            kept.Add(line);
        }

        return [.. kept];
    }

    /// <summary>Whether a level is one of the ones being shown.</summary>
    private bool Shows(LogLevel level) => level switch
    {
        LogLevel.Warning => ShowWarnings,
        LogLevel.Error => ShowErrors,
        LogLevel.Echo => true,
        _ => ShowInfo,
    };

    /// <summary>
    /// Runs a line, and writes both it and its answer into the log.
    /// </summary>
    /// <remarks>
    /// What was typed is written back first. A console that shows only answers is one where you
    /// cannot tell which question a line is answering, which matters most exactly when several
    /// commands are run quickly.
    /// </remarks>
    public void Run(string line)
    {
        var typed = line.Trim();
        if (typed.Length == 0) return;

        ConsoleLog.Write(LogLevel.Echo, "> " + typed);

        // Remembered once, however many times in a row it is run: what the arrows are for is
        // getting back to a command, and ten copies of the same one is nine presses of nothing.
        if (_history.Count == 0 || _history[^1] != typed) _history.Add(typed);

        _recalled = -1;
        Scroll = 0;

        if (ConsoleCommands.Run(typed) is { Length: > 0 } answer)
            ConsoleLog.Write(LogLevel.Echo, answer);
    }

    /// <summary>What was typed before this, or what is already there at the end of the list.</summary>
    public string Back(string current)
    {
        if (_history.Count == 0) return current;

        _recalled = _recalled < 0 ? _history.Count - 1 : Math.Max(0, _recalled - 1);
        return _history[_recalled];
    }

    /// <summary>The other way, and back to nothing once the end is reached.</summary>
    public string Forward(string current)
    {
        if (_recalled < 0) return current;

        _recalled++;

        if (_recalled < _history.Count) return _history[_recalled];

        _recalled = -1;
        return string.Empty;
    }

    /// <summary>
    /// The command a half-typed name would become, or nothing.
    /// </summary>
    /// <remarks>
    /// The first that starts with what is there. Offered as a whole line rather than as the rest
    /// of the word, so that pressing the key that takes it replaces what was typed and nothing has
    /// to be worked out about where the caret is.
    /// </remarks>
    public string? Completion(string typed)
    {
        var word = typed.Trim();
        if (word.Length == 0 || word.Contains(' ')) return null;

        foreach (var command in ConsoleCommands.Starting(word))
        {
            if (string.Equals(command.Name, word, StringComparison.OrdinalIgnoreCase)) return null;

            return command.Name;
        }

        return null;
    }

    /// <summary>What to say under the input: what it would complete to, or what it takes.</summary>
    public string Hint(string typed)
    {
        if (Completion(typed) is { } completion)
        {
            var found = ConsoleCommands.Find(completion);
            return found is { Help.Length: > 0 } ? $"{completion} - {found.Help}" : completion;
        }

        var word = typed.Trim();
        var cut = word.IndexOf(' ');
        var name = cut < 0 ? word : word[..cut];

        if (name.Length == 0 || ConsoleCommands.Find(name) is not { } command) return string.Empty;

        return command.Usage.Length > 0
            ? $"{command.Name} {command.Usage} - {command.Help}"
            : $"{command.Name} - {command.Help}";
    }

    /// <summary>
    /// How a line reads on screen, with its repeat count when it has one.
    /// </summary>
    /// <remarks>
    /// Cut if it is very long. A log row is one line and does not wrap, so what is past the edge
    /// cannot be read however wide the window is, and a text node of several thousand characters
    /// is one the interface lays out and then draws nothing of at all.
    /// </remarks>
    public static string Written(LogLine line)
    {
        var text = line.Count > 1 ? $"{line.Text}  ({line.Count})" : line.Text;

        return text.Length <= Longest ? text : string.Concat(text.AsSpan(0, Longest), "...");
    }

    /// <summary>How much of a line is drawn.</summary>
    private const int Longest = 300;

    /// <summary>Which class a line wears, so its level can be seen rather than read.</summary>
    public static string Dress(LogLevel level) => level switch
    {
        LogLevel.Warning => "log warn",
        LogLevel.Error => "log bad",
        LogLevel.Echo => "log said",
        _ => "log",
    };
}
