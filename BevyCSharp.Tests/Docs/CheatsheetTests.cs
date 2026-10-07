using System.Reflection;
using System.Text.RegularExpressions;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// CHEATSHEET.md against the library it describes, so a public method added without its line, or a
/// line left for a method that is gone, fails the suite.
/// </summary>
/// <remarks>
/// <para>
/// A method is matched by its type, its name and its number of parameters, which tells overloads
/// apart without spelling the cheatsheet's types the way reflection names them. A type is the
/// heading its lines sit under, by its name in <c>Bevy</c>, with <c>Physics.</c> before one in
/// <c>Bevy.Physics</c> and its outer type before a nested one.
/// </para>
/// <para>
/// What the page leaves out is what a reader never calls by name: delegates, enums, the types the
/// compiler makes, an enumerator, accessors, and the members a record or <c>object</c> gives every
/// type. The reflection here is the test's, and the library itself reflects on nothing.
/// </para>
/// </remarks>
public sealed partial class CheatsheetTests
{
    [Fact]
    public void EveryPublicMethodHasItsLineAndEveryLineAMethod()
    {
        var library = typeof(App).Assembly.GetExportedTypes()
            .Where(Listed)
            .SelectMany(type => Methods(type).Select(method => $"{Key(type)}.{method.Name}/{method.GetParameters().Length}"))
            .ToHashSet();

        var listed = Lines(File.ReadAllLines(Path.Combine(RepositoryRoot(), "CHEATSHEET.md"))).ToHashSet();

        // Joined, so a failure names every method at once.
        Assert.True(library.SetEquals(listed),
            "Missing from CHEATSHEET.md: " + string.Join(", ", library.Except(listed).Order())
            + "\nOn CHEATSHEET.md and not in the library: " + string.Join(", ", listed.Except(library).Order()));
    }

    [Fact]
    public void ALineIsReadAsItsTypeItsNameAndItsNumberOfParameters()
    {
        string[] page =
        [
            "### `Render`",
            "```csharp",
            "static AssetHandle CreateMesh(string shape, float a = 1f, float b = 1f, float c = 1f);  // Builds a mesh",
            "static void SetClearColor((float R, float G, float B, float A) color);  // A tuple is one",
            "T Get<T>(Dictionary<int, string> map, float x = 1f);      // Commas inside brackets",
            "IEnumerable<(IPlugin Plugin, int Order)> GetPlugins();  // A tuple inside what it returns",
            "static void Size();",
            "```",
            "### `Physics.PhysicsWorld`",
            "```csharp",
            "void Remove(Entity entity);",
            "```",
            "void Prose(int a);",
        ];

        Assert.Equal(
            ["Render.CreateMesh/4", "Render.SetClearColor/1", "Render.Get/2", "Render.GetPlugins/0", "Render.Size/0", "Physics.PhysicsWorld.Remove/1"],
            Lines(page));
    }

    private static bool Listed(Type type) =>
        type.Namespace is "Bevy" or "Bevy.Physics"
        && !type.IsEnum
        && !typeof(Delegate).IsAssignableFrom(type)
        && !type.Name.Contains('<')
        && type.Name != "Enumerator"
        && Methods(type).Any();

    private static IEnumerable<MethodInfo> Methods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName
                && !method.Name.Contains('<')
                && !method.Name.StartsWith("get_", StringComparison.Ordinal)
                && !method.Name.StartsWith("set_", StringComparison.Ordinal)
                && method.Name is not ("Equals" or "GetHashCode" or "ToString" or "Deconstruct" or "PrintMembers" or "GetEnumerator"));

    /// <summary>A type as its heading names it, without its type parameters.</summary>
    private static string Key(Type type)
    {
        var name = type.DeclaringType is { } outer ? $"{Strip(outer.Name)}.{Strip(type.Name)}" : Strip(type.Name);
        return type.Namespace == "Bevy.Physics" ? "Physics." + name : name;

        static string Strip(string name) => name.Split('`')[0];
    }

    /// <summary>The methods the page's C# blocks list, as type, name and parameter count.</summary>
    private static IEnumerable<string> Lines(IEnumerable<string> lines)
    {
        var type = string.Empty;
        var inCode = false;

        foreach (var line in lines)
        {
            if (Heading().Match(line) is { Success: true } heading)
            {
                type = heading.Groups["type"].Value;
                continue;
            }

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inCode = line.StartsWith("```csharp", StringComparison.Ordinal) && !inCode;
                continue;
            }

            if (!inCode || Declaration().Match(line) is not { Success: true } match) continue;
            yield return $"{type}.{match.Groups["name"].Value}/{Count(match.Groups["parameters"].Value)}";
        }
    }

    /// <summary>How many parameters a list holds, counting the commas outside brackets.</summary>
    private static int Count(string parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters)) return 0;

        int depth = 0, count = 1;
        foreach (var c in parameters)
        {
            if (c is '<' or '(' or '[') depth++;
            else if (c is '>' or ')' or ']') depth--;
            else if (c == ',' && depth == 0) count++;
        }

        return count;
    }

    // A heading naming a type, generic arguments dropped: ### `EcsList<T>` is EcsList.
    [GeneratedRegex(@"^### `(?<type>[\w.]+)(?:<[^>]*>)?`")]
    private static partial Regex Heading();

    // A declaration on a line of its own: what it returns, its name, its parameters and a comment.
    [GeneratedRegex(@"^(?:static )?\S.*?\s+(?<name>[A-Z]\w*)(?:<[^>]*>)?\((?<parameters>.*)\);\s*(?://.*)?$")]
    private static partial Regex Declaration();

    internal static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BevyCSharp.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("The test runs from somewhere outside the checkout.");
    }
}
