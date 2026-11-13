using System.Diagnostics.CodeAnalysis;
using Bevy.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The build defaults' generator gives a program's executable built in Debug the initializer that
/// has it watch its assets, and nothing to a Release build, a library, a test project or a project
/// whose build says none of it.
/// </summary>
/// <remarks>
/// The build's properties reach the generator as the package's props make them visible, which a
/// compilation here is given through its options as the compiler gives them.
/// </remarks>
public sealed class BuildDefaultsGeneratorTests
{
    [Theory]
    [InlineData("Debug", "Exe", null, true)]
    [InlineData("Debug", "WinExe", "false", true)]
    [InlineData("Release", "Exe", null, false)]
    [InlineData("Debug", "Library", null, false)]
    [InlineData("Debug", "Exe", "true", false)]
    [InlineData(null, null, null, false)]
    public void OnlyAProgramsExecutableBuiltInDebugIsGivenTheInitializer(string? configuration, string? output, string? test, bool given)
    {
        // The engine's Core named outright, where the generator finds what it calls.
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Append(typeof(BuildDefaults).Assembly)
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Distinct()
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));
        var compilation = CSharpCompilation.Create(
            "Game",
            [CSharpSyntaxTree.ParseText("var answer = 42;", path: "Program.cs")],
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var properties = new Dictionary<string, string>();
        if (configuration is not null) properties["build_property.Configuration"] = configuration;
        if (output is not null) properties["build_property.OutputType"] = output;
        if (test is not null) properties["build_property.IsTestProject"] = test;

        var run = CSharpGeneratorDriver.Create([new BuildDefaultsGenerator().AsSourceGenerator()], optionsProvider: new Options(properties))
            .RunGeneratorsAndUpdateCompilation(compilation, out var generated, out var errors);

        Assert.Empty(errors);
        var initializer = run.GetRunResult().GeneratedTrees.SingleOrDefault(tree => tree.FilePath.EndsWith("BuildDefaults.g.cs", StringComparison.Ordinal));
        Assert.Equal(given, initializer is not null);
        if (initializer is null) return;

        Assert.Contains("global::Bevy.BuildDefaults.ProgramBuiltInDebug(typeof(BuildDefaultsRegistration).Assembly)", initializer.ToString());
        Assert.Empty(generated.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>The build's properties as the compiler hands an analyzer's options over.</summary>
    private sealed class Options(Dictionary<string, string> properties) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Values(properties);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Values.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Values.Empty;
    }

    private sealed class Values(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        public static readonly Values Empty = new([]);

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);

        public override IEnumerable<string> Keys => values.Keys;
    }
}
