namespace Bevy;

/// <summary>
/// The commands every console has.
/// </summary>
/// <remarks>
/// Kept small on purpose. These are the ones that are about the console itself, so they are the
/// same wherever a console appears; everything about a particular program belongs to that program.
/// </remarks>
internal static class ConsoleBuiltins
{
    /// <summary>Lists the commands, or explains one.</summary>
    [Command("help", "Lists commands, or explains one: help <name>")]
    internal static string Help(string name)
    {
        if (name.Length == 0)
        {
            var names = ConsoleCommands.All.Select(command => command.Name);
            return "commands: " + string.Join(", ", names);
        }

        if (ConsoleCommands.Find(name) is not { } found) return $"unknown command: {name}";

        var usage = found.Usage.Length > 0 ? $"{found.Name} {found.Usage}" : found.Name;
        return found.Help.Length > 0 ? $"{usage} - {found.Help}" : usage;
    }

    /// <summary>Empties the log.</summary>
    [Command("clear", "Empties the console")]
    internal static void Clear() => ConsoleLog.Clear();

    /// <summary>Writes its arguments back.</summary>
    [Command("echo", "Writes what follows back: echo <text>")]
    internal static string Echo(string text) => text;

    /// <summary>Says how much has been said.</summary>
    [Command("log.count", "Says how many lines the log holds")]
    internal static string Count() =>
        $"{ConsoleLog.All().Length} kept, {ConsoleLog.Written} written";
}
