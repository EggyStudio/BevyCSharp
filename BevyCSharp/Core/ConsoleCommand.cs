namespace Bevy;

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
    string Name, string Help, string Usage, Func<string[], string?> Run)
{
    /// <summary>
    /// What it takes, in order.
    /// </summary>
    /// <remarks>
    /// Empty for a command added at runtime, which is written as a function over words and has no
    /// declaration to read.
    /// </remarks>
    public IReadOnlyList<CommandParameter> Parameters { get; init; } = [];
}
