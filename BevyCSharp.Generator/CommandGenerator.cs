using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bevy.Generator;

/// <summary>
/// Turns <c>[Command]</c> methods into console commands.
/// </summary>
/// <remarks>
/// <para>
/// Found at compile time and registered by a module initialiser, so a console reflects over
/// nothing at runtime, a command survives trimming, and a command that does not compile is not a
/// command. The alternative is scanning every loaded assembly on the first keystroke, which is
/// what a console usually does and what makes it the slowest thing in a program to open.
/// </para>
/// <para>
/// What is emitted per method is a small function that takes the words after the command's name,
/// turns them into the parameters the method declares, and calls it. Anything that cannot be
/// turned into a parameter is answered with a sentence, because a console is a place where people
/// type things that are not quite right.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class CommandGenerator : IIncrementalGenerator
{
    private const string CommandAttribute = "Bevy.CommandAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var commands = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                CommandAttribute,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => Extract(ctx))
            .Where(static command => command is not null)
            .Collect();

        context.RegisterSourceOutput(commands, static (spc, found) =>
        {
            var models = new List<CommandModel>();

            foreach (var command in found)
            {
                if (command is null) continue;

                if (command.Diagnostic is { } diagnostic)
                {
                    spc.ReportDiagnostic(diagnostic);
                    continue;
                }

                models.Add(command);
            }

            if (models.Count == 0) return;

            spc.AddSource("ConsoleCommandRegistration.g.cs", Emit(models));
        });
    }

    /// <summary>One command, or what is wrong with the method that asked to be one.</summary>
    private sealed record CommandModel(
        string Name,
        string Help,
        string Usage,
        string Call,
        EquatableArray<string> Parameters,
        bool ReturnsText,
        bool TakesLine,
        Diagnostic? Diagnostic = null);

    /// <summary>Reads one method into a command.</summary>
    private static CommandModel? Extract(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol method) return null;

        var attribute = context.Attributes.FirstOrDefault();
        var written = attribute?.ConstructorArguments.Length > 0
            ? attribute.ConstructorArguments[0].Value as string
            : null;

        var help = attribute?.ConstructorArguments.Length > 1
            ? attribute.ConstructorArguments[1].Value as string ?? string.Empty
            : string.Empty;

        var name = string.IsNullOrWhiteSpace(written)
            ? method.Name.ToLowerInvariant()
            : written!.Trim();

        var location = method.Locations.FirstOrDefault() ?? Location.None;

        if (!method.IsStatic)
        {
            return Refused(
                name, location, CommandDiagnostics.NotStatic, method.Name);
        }

        if (method.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
        {
            return Refused(
                name, location, CommandDiagnostics.NotReachable, method.Name);
        }

        var returnsText = method.ReturnType.SpecialType == SpecialType.System_String;
        if (!returnsText && !method.ReturnsVoid)
        {
            return Refused(
                name, location, CommandDiagnostics.WrongReturn, method.Name);
        }

        var parameters = new List<string>();
        foreach (var parameter in method.Parameters)
        {
            if (Reader(parameter.Type) is not { } reader)
            {
                return Refused(
                    name,
                    location,
                    CommandDiagnostics.WrongParameter,
                    method.Name,
                    parameter.Type.ToDisplayString());
            }

            parameters.Add(reader);
        }

        // One string parameter takes the whole of what was typed after the name, spaces and all,
        // which is what a command that takes a sentence wants. Anything else is words.
        var takesLine = parameters.Count == 1 && parameters[0] == "text";

        return new CommandModel(
            name,
            help,
            Usage(method),
            method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                + "." + method.Name,
            new EquatableArray<string>([.. parameters]),
            returnsText,
            takesLine);
    }

    /// <summary>A command that will not do, with the reason attached.</summary>
    private static CommandModel Refused(
        string name, Location location, DiagnosticDescriptor rule, params object?[] arguments) =>
        new(
            name,
            string.Empty,
            string.Empty,
            string.Empty,
            EquatableArray<string>.Empty,
            false,
            false,
            Diagnostic.Create(rule, location, arguments));

    /// <summary>How one parameter is read from a word, or nothing when it cannot be.</summary>
    private static string? Reader(ITypeSymbol type) => type.SpecialType switch
    {
        SpecialType.System_String => "text",
        SpecialType.System_Boolean => "flag",
        SpecialType.System_Int32 => "whole",
        SpecialType.System_Int64 => "long",
        SpecialType.System_Single => "single",
        SpecialType.System_Double => "number",
        _ => null,
    };

    /// <summary>What the arguments look like, as a person would write them.</summary>
    private static string Usage(IMethodSymbol method) => string.Join(
        " ",
        method.Parameters.Select(parameter => "<" + parameter.Name + ">"));

    /// <summary>Writes the registration for every command in the assembly.</summary>
    private static string Emit(List<CommandModel> models)
    {
        var source = new StringBuilder(4096);

        source.Append("// <auto-generated />\n")
            .Append("#nullable enable\n\n")
            .Append("namespace Bevy.Generated;\n\n")
            .Append("/// <summary>What this assembly offers the console.</summary>\n")
            .Append("internal static class ConsoleCommandRegistration\n")
            .Append("{\n")
            .Append("    /// <summary>\n")
            .Append("    /// Registers this assembly's commands as soon as the CLR touches it, so a\n")
            .Append("    /// console can offer them without scanning for them.\n")
            .Append("    /// </summary>\n")
            .Append("    [global::System.Runtime.CompilerServices.ModuleInitializer]\n")
            .Append("    internal static void Initialize()\n")
            .Append("    {\n");

        foreach (var model in models.OrderBy(command => command.Name, System.StringComparer.Ordinal))
        {
            source.Append("        global::Bevy.ConsoleCommands.Add(new global::Bevy.ConsoleCommand(\n")
                .Append("            ").Append(Quote(model.Name)).Append(",\n")
                .Append("            ").Append(Quote(model.Help)).Append(",\n")
                .Append("            ").Append(Quote(model.Usage)).Append(",\n")
                .Append("            static words =>\n")
                .Append("            {\n");

            EmitBody(source, model);

            source.Append("            }));\n\n");
        }

        source.Append("    }\n");
        EmitReaders(source);
        source.Append("}\n");

        return source.ToString();
    }

    /// <summary>Writes the body that turns words into arguments and calls the method.</summary>
    private static void EmitBody(StringBuilder source, CommandModel model)
    {
        var parameters = model.Parameters.Items;

        if (model.TakesLine)
        {
            source.Append("                var line = string.Join(\" \", words);\n");
            source.Append("                ");
            if (model.ReturnsText) source.Append("return ");
            source.Append(model.Call).Append("(line);\n");
            if (!model.ReturnsText) source.Append("                return null;\n");
            return;
        }

        if (parameters.Count > 0)
        {
            source.Append("                if (words.Length < ").Append(parameters.Count)
                .Append(") return \"needs ").Append(parameters.Count)
                .Append(parameters.Count == 1 ? " argument\";\n" : " arguments\";\n\n");
        }

        for (var i = 0; i < parameters.Count; i++)
        {
            source.Append("                if (!Read").Append(Title(parameters[i]))
                .Append("(words[").Append(i).Append("], out var argument").Append(i)
                .Append(")) return $\"not a ").Append(parameters[i])
                .Append(": {words[").Append(i).Append("]}\";\n");
        }

        if (parameters.Count > 0) source.Append('\n');

        source.Append("                ");
        if (model.ReturnsText) source.Append("return ");
        source.Append(model.Call).Append('(');

        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0) source.Append(", ");
            source.Append("argument").Append(i);
        }

        source.Append(");\n");

        if (!model.ReturnsText) source.Append("                return null;\n");
    }

    /// <summary>Writes the readers the bodies call, once per assembly.</summary>
    private static void EmitReaders(StringBuilder source) => source
        .Append("\n    /// <summary>A word as itself.</summary>\n")
        .Append("    private static bool ReadText(string word, out string value)\n")
        .Append("    {\n        value = word;\n        return true;\n    }\n")
        .Append("\n    /// <summary>A word as a flag, in the words people type for one.</summary>\n")
        .Append("    private static bool ReadFlag(string word, out bool value)\n")
        .Append("    {\n")
        .Append("        switch (word.ToLowerInvariant())\n")
        .Append("        {\n")
        .Append("            case \"1\" or \"on\" or \"true\" or \"yes\":\n")
        .Append("                value = true;\n                return true;\n\n")
        .Append("            case \"0\" or \"off\" or \"false\" or \"no\":\n")
        .Append("                value = false;\n                return true;\n\n")
        .Append("            default:\n")
        .Append("                value = false;\n                return false;\n")
        .Append("        }\n    }\n")
        .Append("\n    /// <summary>A word as a whole number.</summary>\n")
        .Append("    private static bool ReadWhole(string word, out int value) =>\n")
        .Append("        int.TryParse(word, global::System.Globalization.NumberStyles.Integer,\n")
        .Append("            global::System.Globalization.CultureInfo.InvariantCulture, out value);\n")
        .Append("\n    /// <summary>A word as a long whole number.</summary>\n")
        .Append("    private static bool ReadLong(string word, out long value) =>\n")
        .Append("        long.TryParse(word, global::System.Globalization.NumberStyles.Integer,\n")
        .Append("            global::System.Globalization.CultureInfo.InvariantCulture, out value);\n")
        .Append("\n    /// <summary>A word as a number.</summary>\n")
        .Append("    private static bool ReadSingle(string word, out float value) =>\n")
        .Append("        float.TryParse(word, global::System.Globalization.NumberStyles.Float,\n")
        .Append("            global::System.Globalization.CultureInfo.InvariantCulture, out value);\n")
        .Append("\n    /// <inheritdoc cref=\"ReadSingle\"/>\n")
        .Append("    private static bool ReadNumber(string word, out double value) =>\n")
        .Append("        double.TryParse(word, global::System.Globalization.NumberStyles.Float,\n")
        .Append("            global::System.Globalization.CultureInfo.InvariantCulture, out value);\n");

    /// <summary>A reader's name with its first letter raised.</summary>
    private static string Title(string reader) =>
        char.ToUpperInvariant(reader[0]) + reader.Substring(1);

    /// <summary>A string as C# source.</summary>
    private static string Quote(string text) =>
        "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
