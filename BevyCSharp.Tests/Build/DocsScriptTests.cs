using System.Diagnostics;
using System.Text;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// <c>build/docs-on-package.py</c>, which builds every C# block of the guide against the packed
/// package and names a block that no longer builds by its page and line, as an annotation where it
/// runs in the workflow, and <c>build/check-docs.py</c>, which follows every link of the README, the
/// cheatsheet and the guide into this repository to a file that is here and an anchor to a heading
/// that file has, with no request made (NORM.md, N 4.3).
/// </summary>
/// <remarks>
/// <para>
/// A guide shows code a reader copies into a game, and a block that stopped compiling, a call
/// renamed or made internal since it was written, teaches what the package no longer does. The page
/// here holds a block that builds after the lines its comment gives, one calling what the library
/// has not got, and one marked as not compiled, which is left out. Taken from 3DEngine's test of
/// its script (its <c>a4f31573</c>).
/// </para>
/// <para>
/// The links are followed here on every system, so a page moved or a heading renamed without the
/// links to it fails the suite rather than a reader. The build workflow's docs job ran the script
/// on every push with <c>--external</c>, which asks each link off this repository as well; those are
/// asked by a run by hand before a package, since a site that does not answer for a minute is no
/// fault of a commit. 3DEngine follows its links in C#, in its <c>DocumentLinkTests</c>; this runs
/// the script a writer of the guide runs, so the two cannot disagree.
/// </para>
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
        Skip.IfNot(Directory.Exists(package) && Directory.EnumerateFiles(package, "BevyCSharp.*.nupkg").Any(NormTests.EnginePackage),
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

    [SkippableFact]
    public void EveryLinkIntoTheRepositoryReachesAFileAndAHeading()
    {
        var (exit, said) = CheckDocs(Needs.Python(), Path.Combine(CheatsheetTests.RepositoryRoot(), "build", "check-docs.py"));

        Assert.True(exit == 0, said);
    }

    /// <summary>
    /// A link to a page that is not there, and an anchor a page does not have, each fail the check
    /// and are named.
    /// </summary>
    /// <remarks>
    /// The script reads the checkout it stands in, so a copy of it is put in a checkout of its own
    /// with a README, a cheatsheet and one page of a guide.
    /// </remarks>
    [SkippableFact]
    public void ALinkToAPageThatIsNotThereFails()
    {
        var python = Needs.Python();
        Directory.CreateDirectory(_folder.File("build"));
        Directory.CreateDirectory(_folder.File("docs"));
        File.Copy(Path.Combine(CheatsheetTests.RepositoryRoot(), "build", "check-docs.py"), _folder.File(Path.Combine("build", "check-docs.py")));

        const string Blob = "https://github.com/EggyStudio/BevyCSharp/blob/main/";
        File.WriteAllText(_folder.File("README.md"), $"# A game\n\nSee [the guide]({Blob}docs/guide.md) and [the gone page]({Blob}docs/gone.md).\n");
        File.WriteAllText(_folder.File("CHEATSHEET.md"), "# Cheatsheet\n");
        File.WriteAllText(_folder.File(Path.Combine("docs", "guide.md")), "# Guide\n\n## Drawing\n\n[Drawing](#drawing), [Sound](#sound).\n");

        var (exit, said) = CheckDocs(python, _folder.File(Path.Combine("build", "check-docs.py")));

        Assert.True(exit == 1, said);
        Assert.Contains($"README.md: '{Blob}docs/gone.md' names nothing here", said);
        Assert.Contains("'#sound' names a heading", said);
        Assert.DoesNotContain("guide.md' names nothing here", said);
        Assert.DoesNotContain("'#drawing'", said);
    }

    /// <summary>Runs the copy of <c>build/check-docs.py</c> given, without <c>--external</c>, and returns its exit code and what it printed.</summary>
    private static (int Exit, string Said) CheckDocs(string python, string script)
    {
        var start = new ProcessStartInfo(python)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add(script);
        start.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        start.Environment["PYTHONIOENCODING"] = "utf-8";

        using var check = Process.Start(start)!;
        var printed = check.StandardOutput.ReadToEndAsync();
        var errors = check.StandardError.ReadToEndAsync();
        Assert.True(check.WaitForExit(60_000), "the check ends");
        return (check.ExitCode, (printed.Result + errors.Result).Replace("\r", ""));
    }
}
