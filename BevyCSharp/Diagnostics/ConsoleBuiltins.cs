using Diagnostics;

namespace Bevy;

/// <summary>
/// The commands every console has.
/// </summary>
/// <remarks>
/// Kept small on purpose. These are the ones that are about the console itself, so they are the
/// same wherever a console appears; everything about a particular program belongs to that program.
/// The Diagnostics module's registry holds <c>help</c>, <c>list</c> and <c>reset</c> of its own.
/// </remarks>
internal static class ConsoleBuiltins
{
    /// <summary>Empties the log.</summary>
    [Command("clear", "Empties the console")]
    internal static void Clear() => ConsoleLog.Clear();

    /// <summary>Writes its arguments back.</summary>
    [Command("echo", "Writes what follows back: echo <text>")]
    internal static string Echo(string text) => text;

    /// <summary>Says how much has been said.</summary>
    [Command("log.count", "Says how many lines the log holds")]
    internal static string Count() =>
        $"{ConsoleLog.All().Count} kept, {ConsoleLog.Written} written";
}
