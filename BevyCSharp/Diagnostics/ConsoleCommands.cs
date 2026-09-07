namespace Bevy;

/// <summary>
/// Marks a static method as something that can be typed into the console.
/// </summary>
/// <remarks>
/// <para>
/// The method is found at compile time and registered by generated code, so a console does not
/// reflect over anything at runtime and a command that does not compile is not a command.
/// </para>
/// <para>
/// Static only. A command has to be callable when nothing in particular is selected and nothing in
/// particular is playing, and a command that needs an instance is a command that needs to explain
/// which one.
/// </para>
/// <para>
/// The parameters may be strings, whole or fractional numbers, or flags, and they are taken from
/// the words after the command's name. A single string parameter is handed everything typed after
/// the name, spaces and all, which is what a command that takes a sentence wants. Returning a
/// string writes that string back to the console; returning nothing writes nothing.
/// </para>
/// </remarks>
/// <param name="name">What is typed to run it, or nothing for the method's own name, lowercased.</param>
/// <param name="help">One line about what it does, which <c>help</c> lists.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandAttribute(string name = "", string help = "") : Attribute
{
    /// <summary>What is typed to run it.</summary>
    public string Name { get; } = name;

    /// <summary>One line about what it does.</summary>
    public string Help { get; } = help;
}

/// <summary>
/// One thing that can be typed into the console.
/// </summary>
/// <param name="Name">What is typed to run it.</param>
/// <param name="Help">One line about what it does.</param>
/// <param name="Usage">What its arguments are, as a person would write them.</param>
/// <param name="Run">
/// Runs it with the words that followed the name, and answers with what to write back, or nothing.
/// </param>
public sealed record ConsoleCommand(
    string Name, string Help, string Usage, Func<string[], string?> Run);

/// <summary>
/// Everything that can be typed into the console.
/// </summary>
/// <remarks>
/// <para>
/// Filled by generated registrations, one per assembly, so a game that declares a command
/// contributes it by existing. Anything can add one at runtime as well, which is what a tool built
/// on top of this does.
/// </para>
/// <para>
/// The console is the one part of a program that is expected to reach everything, and this is the
/// whole of the mechanism: a name, a line of help, and something to run.
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
    /// Everything a command can do wrong is answered with a sentence rather than an exception: the
    /// console is a place where people type things that are not quite right, and a program that
    /// stops because somebody misspelled a name is not a tool.
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
            return command.Run(Split(rest));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            return $"{name} failed: {error.Message}";
        }
    }

    /// <summary>
    /// A line of arguments as words, keeping quoted runs together.
    /// </summary>
    /// <remarks>
    /// The one piece of syntax a console needs: a name with a space in it is one argument when it
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
