using System.Diagnostics;
using System.Globalization;
using System.Text;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// <c>build/test.py</c>, which runs the tests in the workflow and on a contributor's machine,
/// writes a page within its limits whatever the run held, says a process that is lost and runs the
/// suite again in parts after it, and reads the bridge's failures from what cargo prints, as N 6.7
/// and N 6.8 of NORM.md have it. <c>build/step.py</c>, which runs each step of the pack workflow's
/// jobs on Linux, says what failed in a step that fails having said nothing, and
/// <c>build/bcs-answer.py</c> what a <c>bcs</c> command that failed answered.
/// </summary>
/// <remarks>
/// <para>
/// The path taken after a loss runs on no day the suite passes, so these tests keep it working.
/// Stand-ins for <c>dotnet</c> (<c>dotnet_standin.py</c>) and <c>cargo</c> (<c>cargo_standin.py</c>)
/// hang, grow, die, fail or do not build where asked, so they need no suite and no bridge.
/// </para>
/// <para>
/// The pack run of 421d4e1 ended its game job with an exit code and nothing else, what stopped
/// Courtyard being in the game's log, which no reader of the run's annotations sees. The lines
/// both scripts write are cut to the page's width by <c>build/page.py</c>, which they share.
/// </para>
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

    // The hang case's memory limit is a terabyte, which no process of a runner holds, so the time
    // limit alone ends it. The stand-in sleeping holds 10 MB on Linux, and on the Windows runner of
    // the push of dd2149ee the tree the script read under it once passed the 200 MB the other cases
    // are held to before its six seconds did.
    [SkippableTheory]
    [InlineData("hang", "ended at its time limit", 1 << 20)]
    [InlineData("grow", "ended at its memory limit, its largest process read at", 200)]
    [InlineData("die", "lost to a crash", 200)]
    [InlineData("cap", "stopped at its memory cap", 200)]
    public void ALostSuiteIsSaidFirstAndRunsAgainInParts(string mode, string said, int megabytes)
    {
        var python = Needs.Python();
        var (exit, log) = Script(python, new() { ["BCS_STANDIN"] = mode },
            "suite", "--dotnet", $"{python} BevyCSharp.Tests/Build/dotnet_standin.py",
            "--results", _folder.Path, "--timeout-minutes", "0.1", "--memory-mb", megabytes.ToString(CultureInfo.InvariantCulture));

        Assert.Equal(1, exit);
        var page = File.ReadAllLines(_folder.File("digest.md"));
        Assert.Contains("110 passed, 0 failed, 0 skipped, 0 without a result", page[0]);
        Assert.StartsWith($"### Lost: the suite, {said}", page.First(line => line.StartsWith("### ", StringComparison.Ordinal)));
        if (mode == "cap") Assert.Contains(page, line => line.Contains("past its cap of 3.75 GB", StringComparison.Ordinal));
        if (mode == "grow") Assert.Contains(page, line => line.StartsWith("### Lost", StringComparison.Ordinal) && line.Contains("MB past the 200 MB allowed", StringComparison.Ordinal));
        Assert.Contains(page, line => line.StartsWith("- AlphaTests to CharlieTests: 105 passed", StringComparison.Ordinal));
        Assert.Contains(page, line => line.StartsWith("- NormTests: 5 passed", StringComparison.Ordinal));
        Assert.Contains(page, line => line.StartsWith("- everything else: 0 passed", StringComparison.Ordinal));
        Assert.EndsWith("end of the page " + new string('=', 30), log.TrimEnd());
    }

    /// <summary>
    /// A process created before the parent its recorded id names is no child of it, so the memory
    /// the tree is held to is read without it, and a process whose system gives no creation time is
    /// taken by its parent's id alone.
    /// </summary>
    /// <remarks>
    /// Windows gives an orphan to no other parent and uses an id again, so a test host left from an
    /// earlier run can name an id a process of the tree was given since. The table is given here,
    /// since no system makes one on request. In it 10 is the process watched, made at 500; 11 its child,
    /// made after it; 12 a host of 900 MB made at 100, before 10, under the id 10 took after its
    /// parent's; 13 a child of 12; and 20 a child of 10 with no creation time, as on Linux and macOS.
    /// </remarks>
    [SkippableFact]
    public void AProcessCreatedBeforeTheParentItsIdNamesIsLeftOutOfTheTree()
    {
        var python = Needs.Python();
        // Loaded as the script loads its page module, from build/ with no compiled copy left there.
        const string code = """
            import importlib.util, sys
            sys.dont_write_bytecode = True
            sys.path.insert(0, "build")
            spec = importlib.util.spec_from_file_location("test_script", "build/test.py")
            script = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(script)
            table = {10: (1, 10 << 20, 500), 11: (10, 20 << 20, 600), 12: (10, 900 << 20, 100),
                     13: (12, 300 << 20, 700), 20: (10, 40 << 20, None)}
            script.process_table = lambda: table
            print(sorted(script.tree_under(10, table)), script.largest_in_tree(10))
            """;

        var start = new ProcessStartInfo(python) { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(code);
        using var run = Process.Start(start)!;
        var said = run.StandardOutput.ReadToEndAsync();
        var errors = run.StandardError.ReadToEndAsync();
        Assert.True(run.WaitForExit(30_000));

        Assert.Equal("[10, 11, 20] 40", said.Result.Trim() + errors.Result.Trim());
    }

    /// <summary>
    /// Two cases of a theory whose arguments are cut to the same name are both counted, as the
    /// suite's own tally counts them, and the same cases read again from a part are not.
    /// </summary>
    [SkippableFact]
    public void CasesCutToOneNameAreCountedApart()
    {
        var python = Needs.Python();
        const string cut = "Bevy.Tests.ScriptTests.Compiles(script: &quot;[Behavior] public partial struct&quot;···)";
        var cases = $"<UnitTestResult testName=\"{cut}\" outcome=\"Passed\" /><UnitTestResult testName=\"{cut}\" outcome=\"Passed\" />";

        foreach (var file in new[] { "results-the-suite.trx", "results-ScriptTests.trx" })
        {
            File.WriteAllText(_folder.File(file),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>"
                + cases + "</Results></TestRun>");
        }

        Script(python, new(), "--read", _folder.Path);
        Assert.Contains("2 passed, 0 failed, 0 skipped", File.ReadAllLines(_folder.File("digest.md"))[0]);
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

    // A workflow's steps as GitHub reads them, a step run through build/step.py being given its
    // script as GitHub writes it to a file.
    private const string Workflow = """
        jobs:
          game:
            steps:
              - uses: actions/checkout@v5

              - name: Pack
                run: echo packing ${{ inputs.version }} && exit 6

              # The play.
              - name: Play Courtyard
                env:
                  SHOTS: shots
                run: |
                  echo opening
                  cp ../game.log logs/game.log
                  ok=$(echo fine)
                  echo "$ok"
                  status=$(sh -c 'echo "no app is serving" >&2; exit 4')
                  echo never

              - name: Walk
                run: |
                  walk() { sh -c 'exit 3'; }
                  walk North

              - run: build/fetch-slang.sh
        """;

    [SkippableFact]
    public void AStepWhoseCommandFailsIntoAVariableIsNamedWithTheCommandItsCodeAndTheLogsWarnings()
    {
        var python = StepNeeds();

        // As Bevy writes its log, colored and stamped, an information line among a warning and an error.
        File.WriteAllLines(_folder.File("game.log"),
        [
            "\u001b[2m2026-10-06T20:19:03.453266Z\u001b[0m \u001b[32m INFO\u001b[0m bevy_render::renderer: AdapterInfo { name: \"llvmpipe\" }",
            "\u001b[2m2026-10-06T20:19:03.453281Z\u001b[0m \u001b[33m WARN\u001b[0m bevy_render::renderer: The selected adapter is using a driver that only supports software rendering.",
            "\u001b[2m2026-10-06T20:19:09.100000Z\u001b[0m \u001b[31mERROR\u001b[0m bevy_csharp: " + new string('x', 400),
        ]);
        var (exit, log, summary) = Step(python, StepOf("Play Courtyard"));

        Assert.Equal(4, exit);
        Assert.Contains("fine", log);
        Assert.DoesNotContain("never", log);
        var error = Assert.Single(log.Split('\n'), line => line.StartsWith("::error", StringComparison.Ordinal));
        Assert.StartsWith("::error title=Play Courtyard%3A exit code 4::", error);
        Assert.Contains("`status=$(sh -c 'echo \"no app is serving\" >&2; exit 4')` on line 5 of the step ended with exit code 4.", error);
        Assert.Contains("%0A    no app is serving", error);
        Assert.Contains("game.log, its last lines at a warning or worse:%0A    WARN bevy_render::renderer: The selected adapter", error);
        Assert.DoesNotContain("AdapterInfo", error);
        Assert.All(error.Split("%0A"), line => Assert.True(line.Length <= 240 + 120, "a line of the error is cut to the page's width"));
        Assert.Contains("### Play Courtyard: exit code 4", summary);
    }

    [SkippableFact]
    public void AFunctionThatFailsIsNamedAndAStepThatSaysItsOwnErrorIsGivenNone()
    {
        var python = StepNeeds();

        var (exit, log, _) = Step(python, StepOf("Walk"));
        Assert.Equal(3, exit);
        Assert.Contains("::error title=Walk%3A exit code 3::`sh -c 'exit 3'` on line 1 of the step ended with exit code 3.", log);

        File.WriteAllText(_folder.File("said.sh"), "echo \"::error::Courtyard: the runner did not reach the goal\"\nexit 1\n");
        (exit, log, _) = Step(python, _folder.File("said.sh"));
        Assert.Equal(1, exit);
        Assert.Single(log.Split('\n'), line => line.StartsWith("::error", StringComparison.Ordinal));

        File.WriteAllText(_folder.File("passes.sh"), "echo played\n");
        (exit, log, _) = Step(python, _folder.File("passes.sh"));
        Assert.Equal(0, exit);
        Assert.Equal("played", log.Trim());
    }

    [SkippableFact]
    public void AStepIsNamedThroughItsFilledExpressionsAndOneNotInTheWorkflowByItsFirstLine()
    {
        var python = StepNeeds();

        // An expression stands for what GitHub filled it with, here a version.
        File.WriteAllText(_folder.File("pack.sh"), "echo packing 0.4.1 && exit 6\n");
        var (_, log, _) = Step(python, _folder.File("pack.sh"));
        Assert.Contains("::error title=Pack%3A exit code 6::", log);

        File.WriteAllText(_folder.File("slang.sh"), "build/fetch-slang.sh\n");
        (var exit, log, _) = Step(python, _folder.File("slang.sh"));
        Assert.NotEqual(0, exit);
        Assert.Contains($"::error title=Run build/fetch-slang.sh%3A exit code {exit}::", log);

        File.WriteAllText(_folder.File("elsewhere.sh"), "echo first\nexit 5\n");
        (_, log, _) = Step(python, _folder.File("elsewhere.sh"));
        Assert.Contains("::error title=The step beginning `echo first`%3A exit code 5::The step ended with exit code 5 by its own exit, no command failing.", log);
    }

    [SkippableFact]
    public void ABcsAnswerThatFailedIsSaidWithItsCodeItsSentenceAndTheEndOfTheLogItNames()
    {
        var python = Needs.Python();

        File.WriteAllLines(_folder.File("sample.log"),
        [
            "\u001b[2m2026-10-07T15:02:11.000000Z\u001b[0m \u001b[32m INFO\u001b[0m bevy_render::renderer: AdapterInfo { name: \"WARP\" }",
            "Unhandled exception. System.DllNotFoundException: bevy_csharp",
        ]);
        var answer = $$"""
            {"command":"open","success":false,"data":{"log":{{System.Text.Json.JsonSerializer.Serialize(_folder.File("sample.log"))}}},
             "errors":[{"code":"NOT_READY","message":"BevyCSharp.FeatureTest did not start serving within 90 seconds."}]}
            """;

        var (exit, log) = Answer(python, answer, "./bcs open --feature-test", "6");

        Assert.Equal(6, exit);
        var error = Assert.Single(log.Split('\n'), line => line.StartsWith("::error", StringComparison.Ordinal));
        Assert.StartsWith("::error title=./bcs open --feature-test on Windows::", error);
        Assert.Contains("ended with exit code 6, and bcs said NOT_READY, BevyCSharp.FeatureTest did not start serving within 90 seconds.", error);
        Assert.Contains("%0A    INFO bevy_render::renderer: AdapterInfo", error);
        Assert.Contains("%0A    Unhandled exception. System.DllNotFoundException: bevy_csharp", error);
        Assert.DoesNotContain("\u001b", error, StringComparison.Ordinal);

        // An answer that is not JSON, as a bcs that ended in an exception of its own gives, is said
        // as it was, and the log bcs keeps for the program named is read where the answer names none.
        (exit, log) = Answer(python, "Unhandled exception in bcs", "./bcs stop", "1", "NoSuchProgram");
        Assert.Equal(1, exit);
        Assert.Contains("and bcs said an answer that is not JSON, Unhandled exception in bcs.", log);
        Assert.Contains(Path.Combine("build", "sessions", "NoSuchProgram.log"), log);
    }

    /// <summary>Runs build/bcs-answer.py over an answer, as the workflow's step pipes one in.</summary>
    private static (int Exit, string Log) Answer(string python, string answer, params string[] arguments)
    {
        var start = new ProcessStartInfo(python)
        {
            WorkingDirectory = Root,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add(Path.Combine(Root, "build", "bcs-answer.py"));
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["GITHUB_ACTIONS"] = "true";
        start.Environment["RUNNER_OS"] = "Windows";
        start.Environment.Remove("GITHUB_STEP_SUMMARY");
        start.Environment["PYTHONDONTWRITEBYTECODE"] = "1";

        using var run = Process.Start(start)!;
        run.StandardInput.Write(answer);
        run.StandardInput.Close();
        var log = run.StandardOutput.ReadToEndAsync();
        var errors = run.StandardError.ReadToEndAsync();
        Assert.True(run.WaitForExit(60_000), "the script ends");
        return (run.ExitCode, log.Result.Replace("\r", "") + errors.Result);
    }

    /// <summary>Python and bash, which build/step.py runs a step in, on a system whose jobs it runs.</summary>
    private static string StepNeeds()
    {
        Skip.If(OperatingSystem.IsWindows(), "build/step.py runs the steps of the jobs on Linux, in bash");
        return Needs.Python();
    }

    /// <summary>
    /// Runs build/step.py on the script, as GitHub runs it, against the workflow above and the
    /// folder's logs, and returns its exit code, what it printed and the summary it wrote.
    /// </summary>
    private (int Exit, string Log, string Summary) Step(string python, string script)
    {
        File.WriteAllText(_folder.File("package.yml"), Workflow);
        Directory.CreateDirectory(_folder.File("play"));
        Directory.CreateDirectory(_folder.File(Path.Combine("play", "logs")));
        var summary = _folder.File("summary.md");
        File.Delete(summary);

        var start = new ProcessStartInfo(python)
        {
            WorkingDirectory = _folder.File("play"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in new[] { Path.Combine(Root, "build", "step.py"), script, "--workflow", _folder.File("package.yml"), "--logs", _folder.File(Path.Combine("play", "logs")) })
            start.ArgumentList.Add(argument);
        start.Environment["GITHUB_ACTIONS"] = "true";
        start.Environment["GITHUB_STEP_SUMMARY"] = summary;
        start.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        start.Environment.Remove("BCS_STEP_LOGS");

        using var step = Process.Start(start)!;
        var log = step.StandardOutput.ReadToEndAsync();
        var errors = step.StandardError.ReadToEndAsync();
        Assert.True(step.WaitForExit(60_000), "the step ends");
        return (step.ExitCode, log.Result.Replace("\r", "") + errors.Result, File.Exists(summary) ? File.ReadAllText(summary) : "");
    }

    /// <summary>The script of the named step, as GitHub writes it to a file to run.</summary>
    private string StepOf(string name)
    {
        var lines = Workflow.Split('\n');
        var at = Array.FindIndex(lines, line => line.Trim() == $"- name: {name}");
        var run = Array.FindIndex(lines, at, line => line.Trim() == "run: |");
        var indent = lines[run].Length - lines[run].TrimStart().Length;
        var block = lines.Skip(run + 1).TakeWhile(line => line.Trim().Length == 0 || line.Length - line.TrimStart().Length > indent).ToList();
        var depth = block.Where(line => line.Trim().Length > 0).Min(line => line.Length - line.TrimStart().Length);
        var path = _folder.File(name.Replace(' ', '-') + ".sh");
        File.WriteAllText(path, string.Join("\n", block.Select(line => line.Length >= depth ? line[depth..] : "")).TrimEnd() + "\n");
        return path;
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
