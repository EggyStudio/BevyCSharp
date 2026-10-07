using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Holds the examples to Bevy's components through their typed wrappers wherever a wrapper reaches,
/// so a string path in an example is one no wrapper could replace.
/// </summary>
/// <remarks>
/// <para>
/// The examples are what a reader copies, and a string path fails when it runs rather than when it
/// compiles, so one a wrapper could have been is a fault this finds. Each call into the string API
/// is read from the source with Roslyn, its component and path resolved from string literals and
/// the file's own <c>const string</c> fields and locals, and checked against the description the wrappers are
/// generated from, by the rules the generator follows.
/// </para>
/// <para>
/// A call whose path is worked out as the example runs cannot be checked, and is left to review,
/// and so is one inserting a component from JSON, which may set what no property can.
/// </para>
/// </remarks>
public sealed class ExampleStringPathTests
{
    private static readonly string Root = FindRoot();

    private static readonly HashSet<string> Calls =
    [
        "GetReflected", "SetReflected", "GetVariant", "SetVariant", "InsertReflected", "RemoveReflected",
        "GetReflectedColor", "SetReflectedColor", "GetReflectedAsset", "SetReflectedAsset",
    ];

    // The kinds a wrapper gives a property, as the generator's TypeOf has them, and an enum.
    private static readonly HashSet<string> Typed =
    [
        "Float", "Double", "Bool", "String", "Vec2", "Vec3", "Vec4", "Color", "Quat", "Entity", "Asset", "Int", "Enum",
    ];

    [Fact]
    public void NoExampleReachesAComponentByAStringPathItsWrapperCovers()
    {
        var description = Description.Read(Path.Combine(Root, "BevyCSharp", "Generated", "bevy-components.tsv"));
        var covered = new List<string>();

        foreach (var file in Directory.EnumerateFiles(Path.Combine(Root, "BevyCSharp.Examples"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;

            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var source = tree.GetRoot();
            var constants = source.DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Where(field => field.Modifiers.Any(SyntaxKind.ConstKeyword))
                .Select(field => field.Declaration)
                .Concat(source.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
                    .Where(local => local.IsConst)
                    .Select(local => local.Declaration))
                .SelectMany(declaration => declaration.Variables)
                .Where(variable => variable.Initializer?.Value is LiteralExpressionSyntax)
                .ToDictionary(
                    variable => variable.Identifier.Text,
                    variable => ((LiteralExpressionSyntax)variable.Initializer!.Value).Token.ValueText);

            foreach (var call in source.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (call.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: var name }
                    || !Calls.Contains(name))
                    continue;

                var arguments = call.ArgumentList.Arguments;
                if (arguments.Count < 2 || Resolve(arguments[1].Expression, constants) is not { } component) continue;
                if (!description.Components.Contains(component)) continue;

                var reachable = name switch
                {
                    "RemoveReflected" => true,
                    "InsertReflected" => arguments.Count == 2,
                    // Asking for the whole component only to see whether it is there is what a
                    // wrapper's Get answers.
                    "GetReflected" when arguments.Count == 2 => AskedForPresence(call),
                    _ => arguments.Count >= 3
                        && Resolve(arguments[2].Expression, constants) is { } path
                        && description.Covers(component, Rooted(path), json: name is "GetReflected" or "SetReflected"),
                };

                if (!reachable) continue;
                var line = tree.GetLineSpan(call.Span).StartLinePosition.Line + 1;
                covered.Add($"{Path.GetRelativePath(Root, file)}:{line} {name}({component}{(arguments.Count >= 3 ? ", " + arguments[2] : "")})");
            }
        }

        Assert.True(covered.Count == 0,
            $"{covered.Count} string-path calls in the examples have a wrapper to use instead:\n" + string.Join("\n", covered));
    }

    /// <summary>Whether a call's answer is only compared with null.</summary>
    private static bool AskedForPresence(InvocationExpressionSyntax call) => call.Parent switch
    {
        IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } }
            => literal.IsKind(SyntaxKind.NullLiteralExpression),
        IsPatternExpressionSyntax { Pattern: UnaryPatternSyntax { Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } } }
            => literal.IsKind(SyntaxKind.NullLiteralExpression),
        BinaryExpressionSyntax { Right: LiteralExpressionSyntax literal } => literal.IsKind(SyntaxKind.NullLiteralExpression),
        _ => false,
    };

    /// <summary>
    /// A reflect path as the description writes it, starting at a dot, since Bevy also takes a
    /// field's name alone.
    /// </summary>
    private static string Rooted(string path) => path.Length == 0 || path[0] is '.' or '[' ? path : "." + path;

    /// <summary>A string an argument names, a literal or a constant of the file's, or nothing.</summary>
    private static string? Resolve(ExpressionSyntax expression, Dictionary<string, string> constants) => expression switch
    {
        LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal.Token.ValueText,
        // The whole component, as an empty path names it.
        MemberAccessExpressionSyntax { Expression: PredefinedTypeSyntax, Name.Identifier.Text: "Empty" } => string.Empty,
        IdentifierNameSyntax identifier => constants.GetValueOrDefault(identifier.Identifier.Text),
        // A wrapper's own TypePath, which says the wrapper was there to be used.
        MemberAccessExpressionSyntax { Name.Identifier.Text: "TypePath" } => "wrapper",
        _ => null,
    };

    /// <summary>What the description says a wrapper can reach.</summary>
    private sealed class Description
    {
        public HashSet<string> Components { get; } = ["wrapper"];

        private readonly Dictionary<(string Component, string Path), (string Kind, string Name, string Extra)> _fields = [];
        private readonly Dictionary<(string Component, string Enum), List<(string Kind, string Row, string Extra)>> _variantKinds = [];

        // Each type a list holds as its items, with the kinds of its rows and what each names.
        private readonly Dictionary<string, List<(string Kind, string Name, string Extra)>> _items = [];
        private readonly Dictionary<(string Component, string Path), string> _variantPaths = [];

        public static Description Read(string path)
        {
            var description = new Description();
            foreach (var line in File.ReadLines(path))
            {
                var words = line.Split('\t');
                switch (words[0])
                {
                    case "component":
                        description.Components.Add(words[1]);
                        break;
                    case "item":
                        description._items[words[1]] = [];
                        break;
                    case "field" when description._items.TryGetValue(words[1], out var rows):
                        rows.Add((words[4], words[2], words[6]));
                        break;
                    case "field" when Typed.Contains(words[4]) || words[4] == "List":
                        description._fields[(words[1], words[3])] = (words[4], words[2], words[6]);
                        break;
                    case "variant":
                        var key = (words[1], words[2]);
                        if (!description._variantKinds.TryGetValue(key, out var kinds))
                            description._variantKinds[key] = kinds = [];
                        // The row an enum inside the variant is named by, as the generator finds it.
                        kinds.Add((words[6], words[4].Length == 0 ? words[2] + "." + words[3] : words[2] + "." + words[3] + "." + words[4], words[8]));
                        description._variantPaths.TryAdd((words[1], words[5]), words[2]);
                        break;
                }
            }

            return description;
        }

        /// <summary>
        /// Whether a wrapper has a property for the path, a field's own or one inside a variant of
        /// an enum whose variants hold only values a wrapper types.
        /// </summary>
        /// <param name="json">
        /// Whether the call reads or writes the value as JSON, which for an enum is its variant and
        /// whatever the variant holds, so it is covered only where the wrapper types that too.
        /// </param>
        public bool Covers(string component, string path, bool json)
        {
            if (component == "wrapper") return true;
            if (_fields.TryGetValue((component, path), out var field))
                return field.Kind == "List" ? Lists(field.Extra) : field.Kind != "Enum" || !json || Holds(component, field.Name);

            return _variantPaths.TryGetValue((component, path), out var owner) && Holds(component, owner);
        }

        /// <summary>
        /// Whether every value an enum's variants hold is one a wrapper types, an enum inside a
        /// variant being typed where its own variants' values are, or there are none.
        /// </summary>
        private bool Holds(string component, string owner) =>
            !_variantKinds.TryGetValue((component, owner), out var kinds)
            || kinds.All(held => Types(component, held.Kind, held.Row, held.Extra));

        /// <summary>
        /// Whether a wrapper types a list whose items are of a type, one the description describes by
        /// rows a wrapper types throughout, as the generator makes the record its items are.
        /// </summary>
        private bool Lists(string item) =>
            _items.TryGetValue(item, out var rows) && rows.Count > 0 && rows.All(row => Types(item, row.Kind, row.Name, row.Extra));

        /// <summary>Whether a wrapper types a value of a kind, an enum by its variants and a list by its items.</summary>
        private bool Types(string scope, string kind, string row, string extra) => kind switch
        {
            "Enum" => Holds(scope, row),
            "List" => Lists(extra),
            _ => Typed.Contains(kind),
        };
    }

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "BevyCSharp.sln")) || Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The repository's root was not found above the test assembly.");
    }
}
