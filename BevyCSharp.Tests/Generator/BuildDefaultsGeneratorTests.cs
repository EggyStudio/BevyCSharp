using System.Diagnostics.CodeAnalysis;
using Bevy.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The build defaults' generator gives a program's executable built in Debug the initializer that
/// has it watch its project's assets, and nothing to a Release build, a library, a test project,
/// one on Microsoft's Testing Platform among them, or a project whose build says none of it.
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
        var properties = new Dictionary<string, string>();
        if (configuration is not null) properties["build_property.Configuration"] = configuration;
        if (output is not null) properties["build_property.OutputType"] = output;
        if (test is not null) properties["build_property.IsTestProject"] = test;

        var initializer = Generate(properties);

        Assert.Equal(given, initializer is not null);
        if (initializer is not null) Assert.Contains("global::Bevy.BuildDefaults.ProgramBuiltInDebug(typeof(BuildDefaultsRegistration).Assembly)", initializer);
    }

    /// <summary>
    /// A test executable on Microsoft's Testing Platform, which sets IsTestingPlatformApplication
    /// and may leave IsTestProject unset, is given nothing.
    /// </summary>
    [Fact]
    public void ATestExecutableOnTheTestingPlatformIsGivenNothing()
    {
        var initializer = Generate(new Dictionary<string, string>
        {
            ["build_property.Configuration"] = "Debug",
            ["build_property.OutputType"] = "Exe",
            ["build_property.IsTestingPlatformApplication"] = "true",
        });

        Assert.Null(initializer);
    }

    /// <summary>
    /// The initializer hands over the project's folder and its build folder as the build named them,
    /// a quote in a folder's name kept, and compiles.
    /// </summary>
    [Fact]
    public void TheInitializerHandsOverTheProjectsFolders()
    {
        var initializer = Generate(new Dictionary<string, string>
        {
            ["build_property.Configuration"] = "Debug",
            ["build_property.OutputType"] = "Exe",
            ["build_property.MSBuildProjectDirectory"] = "/home/someone/My \"Game\"",
            ["build_property.BaseIntermediateOutputPath"] = "obj/",
        });

        Assert.Contains("ProgramBuiltInDebug(typeof(BuildDefaultsRegistration).Assembly, @\"/home/someone/My \"\"Game\"\"\", @\"obj/\")", initializer);
    }

    /// <summary>
    /// Runs the generator over a small program with the build's properties, and answers the
    /// initializer it gave, or nothing, having checked the program compiles with it.
    /// </summary>
    private static string? Generate(Dictionary<string, string> properties)
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

        var run = CSharpGeneratorDriver.Create([new BuildDefaultsGenerator().AsSourceGenerator()], optionsProvider: new Options(properties))
            .RunGeneratorsAndUpdateCompilation(compilation, out var generated, out var errors);

        Assert.Empty(errors);
        Assert.Empty(generated.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return run.GetRunResult().GeneratedTrees.SingleOrDefault(tree => tree.FilePath.EndsWith("BuildDefaults.g.cs", StringComparison.Ordinal))?.ToString();
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
