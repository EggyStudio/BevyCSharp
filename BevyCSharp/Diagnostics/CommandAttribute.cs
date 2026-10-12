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
/// the name, spaces and all, for a command that takes a sentence. Returning a string writes that
/// string back to the console; returning nothing writes nothing.
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
