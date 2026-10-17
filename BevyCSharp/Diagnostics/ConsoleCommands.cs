namespace Bevy;

/// <summary>
/// Everything that can be typed into the console.
/// </summary>
/// <remarks>
/// <para>
/// Filled by generated registrations, one per assembly, so a game that declares a command
/// contributes it by existing. Anything can add one at runtime as well, as a tool built on top of
/// this does.
/// </para>
/// <para>
/// The console is the one part of a program that is expected to reach everything, and this is the
/// whole of the mechanism, a name, a line of help, and something to run.
/// </para>
/// </remarks>
public static class ConsoleCommands
{
    private static readonly object Gate = new();

    private static readonly SortedDictionary<string, ConsoleCommand> Registered =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every command, in the order they are listed.</summary>
    public static IReadOnlyList<ConsoleCommand> All
    {
        get
        {
            lock (Gate) return [.. Registered.Values];
        }
    }

    /// <summary>Adds a command, replacing one of the same name.</summary>
    public static void Add(ConsoleCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Name)) return;

        lock (Gate) Registered[command.Name] = command;
    }

    /// <summary>Adds a command written out in place.</summary>
    public static void Add(string name, string help, Func<string[], string?> run) =>
        Add(new ConsoleCommand(name, help, string.Empty, run));

    /// <summary>Takes one back out.</summary>
    public static bool Remove(string name)
    {
        lock (Gate) return Registered.Remove(name);
    }

    /// <summary>The command of a given name, or <see langword="null"/>.</summary>
    public static ConsoleCommand? Find(string name)
    {
        lock (Gate) return Registered.GetValueOrDefault(name);
    }

    /// <summary>The commands whose names start with what has been typed so far.</summary>
    public static IReadOnlyList<ConsoleCommand> Starting(string prefix)
    {
        lock (Gate)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return [.. Registered.Values];

            return
            [
                .. Registered.Values.Where(command =>
                    command.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)),
            ];
        }
    }

    /// <summary>
    /// Runs a line as it was typed, and answers with what to write back.
    /// </summary>
    /// <remarks>
    /// Everything a command can do wrong is answered with a sentence rather than an exception,
    /// because the console is a place where people type things that are not quite right, and a
    /// program that stops because somebody misspelled a name is not a tool.
    /// </remarks>
    public static string? Run(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        var trimmed = line.Trim();
        var cut = trimmed.IndexOf(' ');
        var name = cut < 0 ? trimmed : trimmed[..cut];
        var rest = cut < 0 ? string.Empty : trimmed[(cut + 1)..].Trim();

        if (Find(name) is not { } command) return $"unknown command: {name}";

        try
        {
            // A command that takes the whole line is handed the whole line, spaces, quotes and all.
            // Splitting it into words and joining them back up is lossy in exactly the cases that
            // matter, such as a fragment of C# with a string literal in it, or a path with two
            // spaces.
            return command.Parameters is [{ TakesLine: true }]
                ? command.Run([Unwrap(rest)])
                : command.Run(Split(rest));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            return $"{name} failed: {error.Message}";
        }
    }

    /// <summary>
    /// A line with one pair of enclosing quotes taken off, if it has them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For the command that takes everything after its name. Somebody typing <c>do "Spawn/Cube"</c>
    /// means the path without the quotes, and something sending a line over a socket has to quote a
    /// value that contains spaces for it to survive as one. Both need the same thing back.
    /// </para>
    /// <para>
    /// Only a line that is entirely one quoted run is unwrapped, so a fragment that merely contains
    /// quotes, such as <c>eval world.SetName(e, "x")</c>, is left exactly as it was written.
    /// </para>
    /// </remarks>
    public static string Unwrap(string line)
    {
        if (line.Length < 2 || line[0] != '"' || line[^1] != '"') return line;

        var inside = line[1..^1];
        var escaped = false;

        foreach (var character in inside)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (character == '\\') escaped = true;
            else if (character == '"') return line;
        }

        return inside.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    /// <summary>
    /// A line of arguments as words, keeping quoted runs together.
    /// </summary>
    /// <remarks>
    /// The one piece of syntax a console needs. A name with a space in it is one argument when it
    /// is quoted. Anything more is a language, and this is not one.
    /// </remarks>
    public static string[] Split(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return [];

        var words = new List<string>();
        var word = new System.Text.StringBuilder();
        var quoted = false;

        foreach (var character in line)
        {
            if (character == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (!quoted && char.IsWhiteSpace(character))
            {
                if (word.Length > 0) words.Add(word.ToString());
                word.Clear();
                continue;
            }

            word.Append(character);
        }

        if (word.Length > 0) words.Add(word.ToString());

        return [.. words];
    }
}
