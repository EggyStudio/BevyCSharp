using System.Diagnostics;
using System.Text;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// <c>build/test.py</c>, which runs the tests in the workflow and for a working session, writes a
/// page within its limits whatever the run held, says a process that is lost and runs the suite
/// again in parts after it, and reads the bridge's failures from what cargo prints, as N 6.7 and
/// N 6.8 of NORM.md have it.
/// </summary>
/// <remarks>
/// The path taken after a loss runs on no day the suite passes, so these tests keep it working.
/// Stand-ins for <c>dotnet</c> (<c>dotnet_standin.py</c>) and <c>cargo</c> (<c>cargo_standin.py</c>)
/// hang, grow, die, fail or do not build where asked, so they need no suite and no bridge.
/// </remarks>
public sealed class TestScriptTests : IDisposable
{
    private static readonly string Root = FindRoot();
    private readonly TestFolder _folder = new("bcs-test-script-");

    public void Dispose() => _folder.Dispose();

    private static readonly string[] Causes =
        ["alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel", "india", "juliett", "kilo", "lima"];

    [SkippableFact]
    public void APageOf500FailuresOf12CausesBeside100000LinesOfOutputKeepsToItsLimits()
    {
        var python = Needs.Python();

        // The most frequent cause first, the last two past the ten a page shows.
        int[] counts = [100, 80, 70, 60, 50, 40, 30, 25, 20, 12, 8, 5];
        var results = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?><TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>");
        var test = 0;
        for (var cause = 0; cause < Causes.Length; cause++)
            for (var i = 0; i < counts[cause]; i++, test++)
                results.Append($"<UnitTestResult testName=\"Bevy.Tests.Class{test % 7}.Test{test}\" outcome=\"Failed\"><Output><ErrorInfo>")
                    .Append($"<Message>System.InvalidOperationException : the {Causes[cause]} system broke on frame {test} reading /tmp/run{test}/state.bin")
                    .Append(string.Concat(Enumerable.Range(1, 7).Select(line => $"\n  a place it names, {line} of 7")))
                    .Append("</Message>")
                    .Append($"<StackTrace>   at Bevy.{char.ToUpperInvariant(Causes[cause][0])}{Causes[cause][1..]}System.Run(Int32 frame) in /src/Systems/Run.cs:line {test}\n")
                    .Append($"   at Bevy.Tests.Class{test % 7}.Test{test}() in /src/Tests/Class.cs:line 12</StackTrace>")
                    .Append("</ErrorInfo></Output></UnitTestResult>");
        results.Append("</Results></TestRun>");
        File.WriteAllText(_folder.File("results-the-suite.trx"), results.ToString());

        var output = new StringBuilder();
        for (var i = 0; i < 100_000; i++)
            output.AppendLine(i % 5 < 3 ? $"[BevyCSharp] System 'Tests.Thrower' threw on frame {i}" : $"read asset {i} of the level");
        File.WriteAllText(_folder.File("output-the-suite.txt"), output.ToString());

        var (exit, _) = Script(python, new(), "--read", _folder.Path);

        Assert.Equal(1, exit);
        var page = File.ReadAllLines(_folder.File("digest.md"));
        Assert.True(page.Length <= 200, $"the page is {page.Length} lines");
        Assert.All(page, line => Assert.True(line.Length <= 240, $"a line of {line.Length} characters"));
        var text = string.Join("\n", page);
        Assert.Contains("500 failed, of 12 causes", text);
        foreach (var cause in Causes[..10]) Assert.Contains($"the {cause} system broke", text);
        Assert.DoesNotContain("the kilo system", text);
        Assert.DoesNotContain("the lima system", text);
        Assert.Contains("**100 × System.InvalidOperationException** at `Bevy.AlphaSystem.Run`", text);
        Assert.Contains("60,000 ×", text);
        Assert.Contains("a place it names, 4 of 7", text);
        Assert.DoesNotContain("a place it names, 5 of 7", text);
        Assert.Contains("(3 lines more)", text);
        Assert.True(File.Exists(_folder.File("digest.json")));

        // As a run on GitHub gives them, where the annotations are all a reader who is not signed in
        // sees, ten errors, each a cause whole, and a notice with the head and the repeated lines.
        var (_, annotated) = Script(python, new() { ["GITHUB_ACTIONS"] = "true", ["GITHUB_STEP_SUMMARY"] = _folder.File("summary.md") }, "--read", _folder.Path);
        var lines = annotated.Split(["\r\n", "\n"], StringSplitOptions.None);
        var errors = lines.Where(line => line.StartsWith("::error ", StringComparison.Ordinal)).ToList();
        Assert.Equal(10, errors.Count);
        Assert.All(errors, line => Assert.True(line.Contains("%0Aat Bevy.", StringComparison.Ordinal) && line.Contains("`Bevy.Tests.Class", StringComparison.Ordinal), line));
        Assert.Single(lines, line => line.StartsWith("::notice ", StringComparison.Ordinal) && line.Contains("500 failed") && line.Contains("60,000 ×"));
        Assert.Contains("500 failed, of 12 causes", File.ReadAllText(_folder.File("summary.md")));
    }

    /// <summary>
    /// The lines the output repeated most are its warnings, errors and lines with no level, so a
    /// banner each app logs at its start is passed over for an error a system logs each frame, and
    /// the section is left out where nothing of those repeats.
    /// </summary>
    [SkippableFact]
    public void TheRepeatedLinesAreItsWarningsAndErrorsAndTheSectionGoesWhereNoneRepeat()
    {
        var python = Needs.Python();
        File.WriteAllText(_folder.File("results-the-suite.trx"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>"
            + "<UnitTestResult testName=\"Bevy.Tests.AlphaTests.Passes\" outcome=\"Passed\" /></Results></TestRun>");

        // Each app's start logs its adapter at INFO, colored as Bevy colors it, and one system logs
        // the same error each frame.
        var output = new StringBuilder();
        for (var app = 0; app < 300; app++)
        {
            output.AppendLine($"\u001b[2m2026-10-06T16:45:{app % 60:00}.176569Z\u001b[0m \u001b[32m INFO\u001b[0m \u001b[2mbevy_render::renderer\u001b[0m\u001b[2m:\u001b[0m AdapterInfo {{ name: \"Test GPU\", device: {app} }}");
            output.AppendLine($"2026-10-06T16:45:{app % 60:00}.176569Z DEBUG bevy_app: the app took {app} ms to start");
        }
        for (var frame = 0; frame < 40; frame++)
            output.AppendLine($"2026-10-06T16:46:00.000000Z ERROR bevy_csharp: the thrower failed on frame {frame}");
        File.WriteAllText(_folder.File("output-the-suite.txt"), output.ToString());

        Script(python, new(), "--read", _folder.Path);
        var page = File.ReadAllText(_folder.File("digest.md"));
        Assert.Contains("### Repeated most in the output", page);
        Assert.Contains("40 × `2026-10-06T16:46:00.000000Z ERROR bevy_csharp: the thrower failed on frame 0`", page);
        Assert.DoesNotContain("AdapterInfo", page);
        Assert.DoesNotContain("took", page);

        // The banners alone, which repeat and count for nothing.
        File.WriteAllText(_folder.File("output-the-suite.txt"),
            string.Concat(Enumerable.Range(0, 300).Select(app => $"2026-10-06T16:45:00.000000Z  INFO bevy_render::renderer: AdapterInfo {{ device: {app} }}\n")));
        Script(python, new(), "--read", _folder.Path);
        Assert.DoesNotContain("Repeated most", File.ReadAllText(_folder.File("digest.md")));
    }

    [SkippableTheory]
    [InlineData("hang", "ended at its time limit")]
    [InlineData("grow", "ended at its memory limit")]
    [InlineData("die", "lost to a crash")]
    public void ALostSuiteIsSaidFirstAndRunsAgainInParts(string mode, string said)
    {
        var python = Needs.Python();
        var (exit, log) = Script(python, new() { ["BCS_STANDIN"] = mode },
            "suite", "--dotnet", $"{python} BevyCSharp.Tests/Build/dotnet_standin.py",
            "--results", _folder.Path, "--timeout-minutes", "0.1", "--memory-mb", "200");

        Assert.Equal(1, exit);
        var page = File.ReadAllLines(_folder.File("digest.md"));
        Assert.Contains("110 passed, 0 failed, 0 skipped, 0 without a result", page[0]);
        Assert.StartsWith($"### Lost: the suite, {said}", page.First(line => line.StartsWith("### ", StringComparison.Ordinal)));
        Assert.Contains(page, line => line.StartsWith("- AlphaTests to CharlieTests: 105 passed", StringComparison.Ordinal));
        Assert.Contains(page, line => line.StartsWith("- NormTests: 5 passed", StringComparison.Ordinal));
        Assert.Contains(page, line => line.StartsWith("- everything else: 0 passed", StringComparison.Ordinal));
        Assert.EndsWith("end of the page " + new string('=', 30), log.TrimEnd());
    }

    [SkippableFact]
    public void ABridgeTestThatFailsIsReadFromWhatCargoPrintsUnderItsFailures()
    {
        var python = Needs.Python();
        var (exit, _) = Script(python, new() { ["BCS_CARGO_STANDIN"] = "fail" },
            "bridge", "--cargo", $"{python} BevyCSharp.Tests/Build/cargo_standin.py", "--results", _folder.Path);

        Assert.Equal(1, exit);
        var text = File.ReadAllText(_folder.File("digest.md"));
        Assert.Contains("2 passed, 1 failed, 0 skipped", text);
        Assert.Contains("**1 × panic** at `bevy_csharp/src/log.rs:139`", text);
        Assert.Contains("assertion `left == right` failed", text);
        Assert.Contains("`log::tests::a_line_too_long`", text);
    }

    [SkippableFact]
    public void ABridgeThatDoesNotBuildIsSaidAsLostWithItsErrors()
    {
        var python = Needs.Python();
        var (exit, _) = Script(python, new() { ["BCS_CARGO_STANDIN"] = "build" },
            "bridge", "--cargo", $"{python} BevyCSharp.Tests/Build/cargo_standin.py", "--results", _folder.Path);

        Assert.Equal(1, exit);
        var text = File.ReadAllText(_folder.File("digest.md"));
        Assert.Contains("### Lost: the bridge, did not build", text);
        Assert.Contains("error[E0425]: cannot find function `unset` in this scope", text);
    }

    /// <summary>Runs build/test.py with the environment and arguments given, and returns its exit code and what it printed.</summary>
    private static (int Exit, string Log) Script(string python, Dictionary<string, string> environment, params string[] arguments)
    {
        // Read as the UTF-8 the script writes, where Windows would read it in the console's code
        // page and turn the page's × into another character.
        var start = new ProcessStartInfo(python)
        {
            WorkingDirectory = Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add(Path.Combine("build", "test.py"));
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        start.Environment.Remove("GITHUB_ACTIONS");
        start.Environment.Remove("GITHUB_STEP_SUMMARY");
        foreach (var (name, value) in environment) start.Environment[name] = value;

        using var script = Process.Start(start)!;
        var log = script.StandardOutput.ReadToEndAsync();
        var errors = script.StandardError.ReadToEndAsync();
        Assert.True(script.WaitForExit(120_000), "the script ends its processes at their limits");
        return (script.ExitCode, log.Result + errors.Result);
    }

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The checkout's root was not found above the test assembly.");
    }
}
