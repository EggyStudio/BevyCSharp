namespace Bevy;

/// <summary>
/// One argument a command takes.
/// </summary>
/// <remarks>
/// Filled in by the generated registration, which knows what the method declared. It lets something
/// that has never seen a command compose a call to it. <see cref="ConsoleCommand.Usage"/> is a
/// sentence for a person, and this is the same thing for a program.
/// </remarks>
/// <param name="Name">The parameter's own name, as the method spelled it.</param>
/// <param name="Kind">
/// What a word is read as, <c>text</c>, <c>flag</c>, <c>whole</c>, <c>long</c>, <c>single</c> or
/// <c>number</c>.
/// </param>
/// <param name="TakesLine">
/// Whether this parameter takes everything typed after the name, spaces and all, rather than one
/// word. True only for a command whose single parameter is a string.
/// </param>
/// <param name="Optional">
/// Whether it can be left off, the method giving it a default, which only the last parameters can.
/// </param>
public readonly record struct CommandParameter(string Name, string Kind, bool TakesLine = false, bool Optional = false);
