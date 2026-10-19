using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Holds every example that configures its app to the catalog's entry for it, so the plugins it
/// asks for are there when it runs.
/// </summary>
/// <remarks>
/// An example's <c>Configure</c> runs only where <c>Catalog.cs</c> passes it beside the example's
/// <c>Build</c>, and one written and left out of the entry compiles and opens without what it asks
/// for, its startup throwing where it first reaches for it and its capture black or without its
/// controls. The source is read with Roslyn, since the examples are not referenced from here.
/// </remarks>
public sealed class ExampleCatalogTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void EveryExampleThatConfiguresItsAppIsGivenThatToTheCatalog()
    {
        var examples = Path.Combine(Root, "BevyCSharp.Examples");
        var configuring = new List<string>();
        foreach (var file in Directory.EnumerateFiles(examples, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;

            var source = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
            configuring.AddRange(source.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(type => type.Members.OfType<MethodDeclarationSyntax>().Any(IsConfigure))
                .Select(type => type.Identifier.Text));
        }

        // Each entry's arguments after its name, as "Type.Build" and "Type.Configure".
        var catalog = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(examples, "Catalog.cs"))).GetRoot();
        var passed = catalog.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>()
            .Select(entry => entry.ArgumentList?.Arguments.Skip(1).Select(argument => argument.Expression.ToString()).ToArray() ?? [])
            .Where(arguments => arguments.Length > 0)
            .ToList();

        var dropped = configuring
            .Where(type => passed.Any(arguments => arguments[0] == $"{type}.Build"))
            .Where(type => !passed.Any(arguments => arguments.Contains($"{type}.Configure")))
            .ToList();

        Assert.True(configuring.Count > 0, "No example's Configure was found, so the search misses the examples.");
        Assert.True(dropped.Count == 0,
            $"These examples configure their app and the catalog never passes it: {string.Join(", ", dropped)}");
    }

    // A public static Configure taking the app's Config, the form the catalog takes.
    private static bool IsConfigure(MethodDeclarationSyntax method) =>
        method.Identifier.Text == "Configure"
        && method.Modifiers.Any(SyntaxKind.StaticKeyword)
        && method.ParameterList.Parameters is [{ Type: { } type }]
        && type.ToString() == "Config";

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "BevyCSharp.sln")) || Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The repository's root was not found above the test assembly.");
    }
}
