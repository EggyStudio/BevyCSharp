using System.Diagnostics;
using System.Text;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// <c>build/docs-on-package.py</c>, which builds every C# block of the guide against the packed
/// package and names a block that no longer builds by its page and line, as an annotation where it
/// runs in the workflow.
/// </summary>
/// <remarks>
/// A guide shows code a reader copies into a game, and a block that stopped compiling, a call
/// renamed or made internal since it was written, teaches what the package no longer does. The page
/// here holds a block that builds after the lines its comment gives, one calling what the library
/// has not got, and one marked as not compiled, which is left out. Taken from 3DEngine's test of
/// its script (its <c>a4f31573</c>).
/// </remarks>
public sealed class DocsScriptTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-docs-script-");

    public void Dispose() => _folder.Dispose();

    [SkippableFact]
    public void AStaleBlockIsNamedByItsPageAndLineAGoodOneBuildsAndASkippedOneIsLeftOut()
    {
        var python = Needs.Python();
        var package = Path.Combine(CheatsheetTests.RepositoryRoot(), "build", "package");
        Skip.IfNot(Directory.Exists(package) && Directory.EnumerateFiles(package, "BevyCSharp.*.nupkg").Any(),
            "build/package holds no BevyCSharp package to build the blocks on");

        var page = _folder.File("guide.md");
        File.WriteAllText(page, string.Join("\n",
        [
            "# A guide",                                                            // 1
            "",                                                                     // 2
            "<!-- compiled with:",                                                  // 3
            "Entity entity = default;",                                             // 4
            "-->",                                                                  // 5
            "```csharp",                                                            // 6
            "Render.SetMesh(ctx.Ecs, entity, Render.CreateMesh(MeshShape.Cuboid));", // 7
            "```",                                                                  // 8
            "",                                                                     // 9
            "```csharp",                                                            // 10
            "// A call the library has not got.",                                   // 11
            "Render.DrawCubeSideways(1f);",                                         // 12
            "```",                                                                  // 13
            "",                                                                     // 14
            "<!-- not compiled: a sketch, in Slang -->",                            // 15
            "```csharp",                                                            // 16
            "float4 main() : SV_Target { return 1; }",                              // 17
            "```",                                                                  // 18
            "",
        ]));

        var start = new ProcessStartInfo(python)
        {
            WorkingDirectory = CheatsheetTests.RepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add(Path.Combine("build", "docs-on-package.py"));
        start.ArgumentList.Add(page);
        start.Environment["GITHUB_ACTIONS"] = "true";

        using var script = Process.Start(start)!;
        var output = script.StandardOutput.ReadToEndAsync();
        var errors = script.StandardError.ReadToEndAsync();
        Assert.True(script.WaitForExit(300_000), "the script builds one small project");
        var log = output.Result.Replace("\r", "") + errors.Result;
        var lines = log.Split('\n');

        Assert.NotEqual(0, script.ExitCode);
        Assert.Contains(lines, line => line.EndsWith("guide.md:12: block 2: CS0117 'Render' does not contain a definition for 'DrawCubeSideways'", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("::error title=", StringComparison.Ordinal) && line.Contains("guide.md%2C block 2::CS0117", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains("block 1:", StringComparison.Ordinal) || line.Contains("block 3:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("1 error(s) in the guides' blocks, of 2 built", StringComparison.Ordinal));
    }
}
