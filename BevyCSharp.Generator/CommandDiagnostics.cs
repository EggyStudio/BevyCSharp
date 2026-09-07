using Microsoft.CodeAnalysis;

namespace Bevy.Generator;

/// <summary>What can be wrong with a method that asked to be a console command.</summary>
/// <remarks>
/// Reported where the method is written, so the answer arrives in the editor rather than as a
/// command that silently is not there.
/// </remarks>
internal static class CommandDiagnostics
{
    private const string Category = "BevyCSharp";

    /// <summary>A command that is not static.</summary>
    internal static readonly DiagnosticDescriptor NotStatic = new(
        "BCS200",
        "Console command must be static",
        "'{0}' is marked [Command] but is not static. A command has to be callable when nothing "
        + "in particular is selected, so it cannot need an instance.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>A command nothing outside its type can call.</summary>
    internal static readonly DiagnosticDescriptor NotReachable = new(
        "BCS201",
        "Console command must be reachable",
        "'{0}' is marked [Command] but is private. Generated registration calls it by name, so it "
        + "has to be public or internal.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>A command that answers with something the console cannot show.</summary>
    internal static readonly DiagnosticDescriptor WrongReturn = new(
        "BCS202",
        "Console command must return void or string",
        "'{0}' is marked [Command] but returns something else. A command writes a line back or "
        + "writes nothing.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>A command that takes something the console cannot read from a word.</summary>
    internal static readonly DiagnosticDescriptor WrongParameter = new(
        "BCS203",
        "Console command parameter cannot be typed",
        "'{0}' is marked [Command] but takes a {1}. A command's parameters are read from what "
        + "somebody typed, so they can be strings, numbers or flags.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
