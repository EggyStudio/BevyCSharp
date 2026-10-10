using Bevy;
using Bevy.CodeFixes;
using Bevy.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The fixes an editor offers for the behavior and command generators' diagnostics, each applied as
/// an editor applies it, after which the generator reports nothing on the code.
/// </summary>
/// <remarks>Taken from 3DEngine's tests of its own fixes (its <c>c6b529d4</c>).</remarks>
public sealed class GeneratorCodeFixTests
{
    // Runs a generator over source as an editor would hold it, applies the fix of the given title
    // to its first diagnostic of the given id, and returns the fixed source with the diagnostics
    // the generator reports on it.
    private static async Task<(string Text, Diagnostic[] After)> Fix(ISourceGenerator generator, string source, string id, string title)
    {
        using var workspace = new AdhocWorkspace();
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(App).Assembly.Location));
        var project = workspace.AddProject("Fixed", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(references);
        var document = project.AddDocument("Game.cs", SourceText.From(source));

        var diagnostic = (await Diagnose(document.Project, generator)).First(found => found.Id == id);
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await new GeneratorCodeFixes().RegisterCodeFixesAsync(context);
        Assert.Contains(title, actions.Select(action => action.Title));

        var operations = await actions.First(action => action.Title == title).GetOperationsAsync(CancellationToken.None);
        var solution = Assert.Single(operations.OfType<ApplyChangesOperation>()).ChangedSolution;
        var fixedDocument = solution.GetDocument(document.Id)!;
        return ((await fixedDocument.GetTextAsync()).ToString(), await Diagnose(fixedDocument.Project, generator));
    }

    private static async Task<Diagnostic[]> Diagnose(Project project, ISourceGenerator generator)
    {
        var compilation = await project.GetCompilationAsync();
        CSharpGeneratorDriver.Create(generator).RunGeneratorsAndUpdateCompilation(compilation!, out _, out var diagnostics);
        return [.. diagnostics];
    }

    private static ISourceGenerator Behaviors => new BehaviorGenerator().AsSourceGenerator();

    private static ISourceGenerator Commands => new CommandGenerator().AsSourceGenerator();

    [Fact]
    public async Task ABehaviorIsMadePartial()
    {
        var (text, after) = await Fix(Behaviors, """
            using Bevy;
            [Behavior]
            public struct Spin
            {
                [OnUpdate] public static void Turn(BehaviorContext ctx) { }
            }
            """, GeneratorCodeFixes.NotPartial, "Make the behavior partial");

        Assert.Contains("public partial struct Spin", text);
        Assert.Empty(after);
    }

    [Fact]
    public async Task AStageMethodIsGivenTheContextItTakes()
    {
        var (text, after) = await Fix(Behaviors, """
            using Bevy;
            [Behavior]
            public partial struct Spin
            {
                [OnUpdate] public static void Turn(float speed) { }
            }
            """, GeneratorCodeFixes.BadSignature, "Take the BehaviorContext a stage method is given");

        Assert.Contains("[OnUpdate] public static void Turn(BehaviorContext ctx) { }", text);
        Assert.Empty(after);
    }

    [Fact]
    public async Task TheContextMovesFirstAndTheComponentsStayAfterIt()
    {
        var (text, after) = await Fix(Behaviors, """
            using Bevy;
            [Behavior]
            public partial struct Spin
            {
                public float Speed;
                [OnUpdate] public void Turn(ref Transform transform, BehaviorContext context, float speed) { }
            }
            """, GeneratorCodeFixes.BadSignature, "Take the BehaviorContext a stage method is given");

        Assert.Contains("[OnUpdate] public void Turn(BehaviorContext context, ref Transform transform) { }", text);
        Assert.Empty(after);
    }

    [Fact]
    public async Task OneStageIsKeptOfSeveral()
    {
        var (text, after) = await Fix(Behaviors, """
            using Bevy;
            [Behavior]
            public partial struct Spin
            {
                [OnUpdate]
                [OnLast]
                public static void Turn(BehaviorContext ctx) { }
            }
            """, GeneratorCodeFixes.MultipleStages, "Run in OnLast alone");

        Assert.DoesNotContain("[OnUpdate]", text);
        Assert.Contains("[OnLast]", text);
        Assert.Empty(after);
    }

    [Fact]
    public async Task ACommandIsMadeStatic()
    {
        var (text, after) = await Fix(Commands, """
            using Bevy;
            public class Tools
            {
                [Command("hello", "Says hello: hello")]
                internal string Hello(string line) => "hello";
            }
            """, GeneratorCodeFixes.NotStatic, "Make the command static");

        Assert.Contains("internal static string Hello(string line)", text);
        Assert.Empty(after);
    }

    [Fact]
    public async Task ACommandIsMadeReachable()
    {
        var (text, after) = await Fix(Commands, """
            using Bevy;
            public static class Tools
            {
                [Command("hello", "Says hello: hello")]
                private static string Hello(string line) => "hello";
            }
            """, GeneratorCodeFixes.NotReachable, "Make the command internal");

        Assert.Contains("internal static string Hello(string line)", text);
        Assert.Empty(after);
    }
}
