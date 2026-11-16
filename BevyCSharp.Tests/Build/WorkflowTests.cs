using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Every expression in the workflows names only the contexts GitHub gives the place it stands in,
/// so a file GitHub refuses is seen here before a push rather than as a run with no jobs after it,
/// and the workflows hold what a run's checks and its page rest on.
/// </summary>
/// <remarks>
/// <para>
/// A job's <c>env</c> that reads <c>runner.temp</c>, which GitHub gives to steps alone, has the
/// workflow refused, ending within a minute with no job and no word of why. The file parses as
/// YAML, and nothing that runs here reads it as GitHub does.
/// </para>
/// <para>
/// The places and what each allows are GitHub's table of context availability. A place the table
/// here does not have fails as such, so a new kind of place is looked up rather than passed over.
/// An expression stands in <c>${{ }}</c>, and an <c>if</c> is one whole, as GitHub reads it.
/// </para>
/// </remarks>
public sealed class WorkflowTests
{
    private static readonly string Root = FindRoot();

    private const string Step = "github needs strategy matrix job runner env vars secrets steps inputs";

    // Each place, as the keys that lead to it with * for a job's or a step's own name, and the
    // contexts GitHub gives it. The first that matches a line's keys is its place.
    private static readonly (string[] Path, string Allowed)[] Places =
    [
        (["on", "workflow_call", "inputs"], "github inputs vars"),
        (["on"], "github inputs vars"),
        (["env"], "github secrets inputs vars"),
        (["concurrency"], "github inputs vars"),
        (["run-name"], "github inputs vars"),
        (["jobs", "*", "steps", "*", "if"], "github needs strategy matrix job runner env vars steps inputs"),
        (["jobs", "*", "steps", "*"], Step),
        (["jobs", "*", "outputs"], Step),
        (["jobs", "*", "env"], "github needs strategy matrix vars secrets inputs"),
        (["jobs", "*", "if"], "github needs vars inputs"),
        (["jobs", "*", "strategy"], "github needs vars inputs"),
        (["jobs", "*", "runs-on"], "github needs strategy matrix vars inputs"),
        (["jobs", "*", "name"], "github needs strategy matrix vars inputs"),
        (["jobs", "*", "timeout-minutes"], "github needs strategy matrix vars inputs"),
        (["jobs", "*", "continue-on-error"], "github needs strategy matrix vars inputs"),
        (["jobs", "*", "environment"], "github needs strategy matrix vars inputs"),
        (["jobs", "*", "concurrency"], "github needs strategy matrix vars inputs"),
        (["jobs", "*", "defaults"], "github needs strategy matrix env vars inputs"),
        (["jobs", "*", "with"], "github needs strategy matrix vars inputs"),
        (["jobs", "*", "secrets"], "github needs strategy matrix secrets vars inputs"),
    ];

    private static readonly Regex Context = new(@"(?<![\w.'""-])(github|needs|strategy|matrix|job|runner|env|vars|secrets|steps|inputs)(?=\s*[.\[])");

    [Fact]
    public void EveryExpressionInTheWorkflowsNamesOnlyTheContextsItsPlaceAllows()
    {
        var found = new List<string>();
        var checkedCount = 0;
        foreach (var workflow in Directory.EnumerateFiles(Path.Combine(Root, ".github", "workflows"), "*.yml").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(workflow);
            foreach (var (line, place, contexts) in Expressions(File.ReadAllText(workflow)))
            {
                checkedCount++;
                if (Allowed(place) is not { } allowed)
                    found.Add($"{name}:{line} is in {string.Join('.', place)}, a place the table here does not have");
                else
                    found.AddRange(contexts.Where(c => !allowed.Contains(c)).Select(c => $"{name}:{line} reads {c} in {string.Join('.', place)}, which allows {string.Join(", ", allowed)}"));
            }
        }

        Assert.True(checkedCount > 20, $"only {checkedCount} expressions were found, so the walk lost its way");
        Assert.True(found.Count == 0, "A workflow GitHub would refuse:\n  " + string.Join("\n  ", found));
    }

    [Theory]
    [InlineData("jobs:\n  game:\n    env:\n      LOGS: ${{ runner.temp }}/logs\n", "runner")]
    [InlineData("jobs:\n  game:\n    if: runner.os == 'Linux'\n", "runner")]
    [InlineData("jobs:\n  game:\n    runs-on: ${{ steps.pick.outputs.os }}\n", "steps")]
    [InlineData("on:\n  workflow_call:\n    inputs:\n      v:\n        default: ${{ secrets.KEY }}\n", "secrets")]
    [InlineData("jobs:\n  game:\n    steps:\n      - name: Play\n        env:\n          LOGS: ${{ runner.temp }}/logs\n        run: echo ${{ inputs.version }}\n", null)]
    [InlineData("jobs:\n  game:\n    steps:\n      - if: runner.os == 'Linux'\n        run: |\n          echo env: ${{ matrix.rid }}\n          if: not a key\n", null)]
    [InlineData("jobs:\n  test:\n    runs-on: ${{ matrix.os }}\n    strategy:\n      matrix:\n        os: ${{ fromJSON(inputs.test_os) }}\n", null)]
    public void AContextAPlaceDoesNotAllowIsFoundAndNoneElse(string workflow, string? refused)
    {
        var found = Expressions(workflow)
            .SelectMany(e => e.Contexts.Where(c => Allowed(e.Place) is { } allowed && !allowed.Contains(c)))
            .ToList();
        Assert.Equal(refused is null ? [] : [refused], found);
    }

    /// <summary>
    /// The build workflow's test job checks out the whole history, which N 7.2 reads every commit
    /// message of since the rule began, where a checkout of the last commit alone left it skipped.
    /// </summary>
    [Fact]
    public void TheTestJobChecksOutTheWholeHistoryTheMessageRuleReads()
    {
        var checkout = Steps(Jobs(Workflow("build.yml"))["test"]).Single(step => step.Contains("uses: actions/checkout@", StringComparison.Ordinal));
        Assert.Matches(@"\n +fetch-depth: 0\n", checkout);
    }

    /// <summary>
    /// A publish is refused off the default branch by a job the build needs, so before the build's
    /// hour, since a branch's version is one the default branch gives a commit of its own.
    /// </summary>
    [Fact]
    public void APublishOffTheDefaultBranchIsRefusedBeforeTheBuild()
    {
        var jobs = Jobs(Workflow("pack.yml"));
        var needs = Regex.Match(jobs["build"], @"\n    needs: \[([^\]]*)\]").Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains(needs, need => jobs.TryGetValue(need, out var job) && Steps(job).Any(step =>
            step.Contains("inputs.publish", StringComparison.Ordinal)
            && step.Contains("github.event.repository.default_branch", StringComparison.Ordinal)
            && step.Contains("exit 1", StringComparison.Ordinal)));
    }

    /// <summary>
    /// The pack run's page says the packages were pushed only where the push said so of each, and
    /// names those whose versions nuget.org held, which a push that skips a version it holds
    /// passes over with no failure.
    /// </summary>
    [SkippableTheory]
    [InlineData("pushed", "Pushed to nuget.org.")]
    [InlineData("held pushed", "Pushed 1 to nuget.org, the rest left as they were, since nuget.org held their versions, BevyCSharp.0.6.9.nupkg.")]
    [InlineData("held held", "Nothing pushed, since nuget.org held the version of every package, BevyCSharp.0.6.9.nupkg, BevyCSharp.Templates.0.6.9.nupkg.")]
    public void ThePageSaysWhichPackagesWerePushedAndWhichNuGetHeld(string pushes, string said)
    {
        Skip.If(OperatingSystem.IsWindows(), "runs the step in bash, as the workflow runs it on Linux");

        // What dotnet nuget push says of a package it pushed and of one whose version the feed held.
        string[] packages = ["BevyCSharp.0.6.9.nupkg", "BevyCSharp.Templates.0.6.9.nupkg"];
        var output = string.Concat(pushes.Split(' ').Select((push, i) =>
            $"Pushing {packages[i]} to 'https://www.nuget.org/api/v2/package'...\n  PUT https://www.nuget.org/api/v2/package/\n" + (push == "held"
                ? $"  Conflict https://www.nuget.org/api/v2/package/ 512ms\nPackage '/home/runner/work/BevyCSharp/BevyCSharp/build/package/{packages[i]}' already exists at feed 'https://www.nuget.org/api/v2/package/'.\n"
                : "  Created https://www.nuget.org/api/v2/package/ 2048ms\nYour package was pushed.\n")));

        var script = Run(Steps(Jobs(Workflow("pack.yml"))["pack"]).Single(step => step.Contains("name: Say what was made", StringComparison.Ordinal)))
            .Replace("${{ steps.version.outputs.version }}", "0.6.9", StringComparison.Ordinal)
            .Replace("${{ inputs.publish }}", "true", StringComparison.Ordinal);
        Assert.DoesNotContain("${{", script, StringComparison.Ordinal);

        using var folder = new TestFolder("bcs-pushed-");
        File.WriteAllText(folder.File("push.txt"), output);
        var start = new ProcessStartInfo("bash") { RedirectStandardError = true };
        foreach (var argument in new[] { "-e", "-o", "pipefail", "-c", script }) start.ArgumentList.Add(argument);
        start.Environment["RUNNER_TEMP"] = folder.Path;
        start.Environment["GITHUB_STEP_SUMMARY"] = folder.File("summary.md");
        using var bash = Process.Start(start)!;
        var error = bash.StandardError.ReadToEnd();
        bash.WaitForExit();

        Assert.True(bash.ExitCode == 0, $"the step ended with {bash.ExitCode}: {error}");
        Assert.Equal(["### BevyCSharp 0.6.9", said], File.ReadAllLines(folder.File("summary.md")));
    }

    /// <summary>
    /// Every folder a step of a job puts its captures or its logs in is kept by an upload of that
    /// job that runs whether the job passed or failed, so a failed play leaves what says why.
    /// </summary>
    [Fact]
    public void EveryFolderAStepLogsIntoIsKeptByAnUploadThatRunsAlways()
    {
        var found = new List<string>();
        var checkedCount = 0;
        foreach (var name in new[] { "build.yml", "pack.yml" })
        {
            foreach (var (job, text) in Jobs(Workflow(name)))
            {
                var steps = Steps(text);
                var kept = steps
                    .Where(step => step.Contains("uses: actions/upload-artifact@", StringComparison.Ordinal) && step.Contains("if: always()", StringComparison.Ordinal))
                    .SelectMany(step => Regex.Matches(step, @"(?:path: |\n {12})(\S[^\n]*)").Select(match => Folder(match.Groups[1].Value)))
                    .ToList();
                foreach (Match logs in steps.SelectMany(step => Regex.Matches(step, @"\n +(?:SHOTS|BCS_STEP_LOGS): ([^\n]+)")))
                {
                    checkedCount++;
                    var folder = Folder(logs.Groups[1].Value);
                    if (!kept.Any(path => folder == path || folder.StartsWith(path + "/", StringComparison.Ordinal)))
                        found.Add($"{name}'s {job} logs into {folder}, which no upload of the job that runs always keeps");
                }
            }
        }

        Assert.True(checkedCount > 5, $"only {checkedCount} folders were found, so the walk lost its way");
        Assert.True(found.Count == 0, string.Join("\n", found));
    }

    /// <summary>
    /// Every action from outside GitHub is named by a commit, so a tag moved or a branch pushed
    /// elsewhere changes nothing that builds the bridges or holds the token nuget.org takes.
    /// </summary>
    [Fact]
    public void EveryActionFromOutsideGitHubIsNamedByACommit()
    {
        var actions = Directory.EnumerateFiles(Path.Combine(Root, ".github", "workflows"), "*.yml")
            .SelectMany(workflow => Regex.Matches(File.ReadAllText(workflow), @"uses: (\S+)").Select(match => match.Groups[1].Value))
            .ToList();
        var named = actions.Where(action => !action.StartsWith("./", StringComparison.Ordinal) && !action.StartsWith("actions/", StringComparison.Ordinal)).ToList();

        Assert.True(named.Count >= 3, $"only {named.Count} actions from outside GitHub were found, so the walk lost its way");
        Assert.All(named, action => Assert.Matches(@"^[\w.-]+/[\w.-]+@[0-9a-f]{40}$", action));
    }

    // A workflow's text, its lines ended by \n alone.
    private static string Workflow(string name) => File.ReadAllText(Path.Combine(Root, ".github", "workflows", name)).ReplaceLineEndings("\n");

    // Each job's text by its name, from its key two spaces in under jobs to the next.
    private static Dictionary<string, string> Jobs(string workflow)
    {
        var body = workflow[(workflow.IndexOf("\njobs:\n", StringComparison.Ordinal) + 1)..];
        var keys = Regex.Matches(body, @"^  ([\w-]+):\n", RegexOptions.Multiline);
        return keys.Select((key, i) => (key.Groups[1].Value, body[key.Index..(i + 1 < keys.Count ? keys[i + 1].Index : body.Length)]))
            .ToDictionary(job => job.Item1, job => job.Item2);
    }

    // Each step's text, from its dash six spaces in to the next, a newline before each line.
    private static List<string> Steps(string job) =>
        [.. Regex.Split(job, @"(?=\n      - )").Skip(1)];

    // A step's run block, its lines taken out of the step's indent.
    private static string Run(string step)
    {
        var block = Regex.Match(step, @"\n        run: \|\n((?: {10}.*\n|\n)+)");
        Assert.True(block.Success, "the step has no run block");
        return string.Join('\n', block.Groups[1].Value.Split('\n').Select(line => line.Length >= 10 ? line[10..] : line));
    }

    // A folder as an upload names it, the workspace's own folders by their paths within it.
    private static string Folder(string path) => path.Trim().Replace("${{ github.workspace }}/", "", StringComparison.Ordinal);

    private static HashSet<string>? Allowed(IReadOnlyList<string> place)
    {
        foreach (var (path, allowed) in Places)
        {
            if (place.Count < path.Length) continue;
            if (path.Select((key, i) => key == "*" || key == place[i]).All(match => match))
                return [.. allowed.Split(' ')];
        }

        return null;
    }

    /// <summary>
    /// Each line holding an expression, its keys from the top, a list's item standing as *, and the
    /// contexts it reads, from a walk of the YAML by its indentation that takes a block scalar's
    /// lines as its key's whatever they look like.
    /// </summary>
    private static IEnumerable<(int Line, IReadOnlyList<string> Place, IReadOnlyList<string> Contexts)> Expressions(string workflow)
    {
        var lines = workflow.ReplaceLineEndings("\n").Split('\n');
        var stack = new List<(int Indent, string Key)>();
        var blockIndent = -1;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            // Inside a block scalar, every line deeper than its key is the key's text.
            if (blockIndent >= 0)
            {
                if (trimmed.Length == 0 || indent > blockIndent)
                {
                    if (Read(line) is { Count: > 0 } inBlock) yield return (i + 1, Keys(stack), inBlock);
                    continue;
                }

                blockIndent = -1;
            }

            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

            // A list's item stands as * among the keys, and its first key is one level within it.
            var item = trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed == "-";
            if (item)
            {
                while (stack.Count > 0 && stack[^1].Indent >= indent) stack.RemoveAt(stack.Count - 1);
                stack.Add((indent, "*"));
                trimmed = trimmed[1..].TrimStart();
                indent += line.TrimStart().Length - trimmed.Length;
            }

            var match = Regex.Match(trimmed, @"^([\w-]+|""[^""]*"")\s*:(?:\s+(.*))?$");
            if (!match.Success)
            {
                if (Read(line) is { Count: > 0 } loose) yield return (i + 1, Keys(stack), loose);
                continue;
            }

            while (stack.Count > 0 && stack[^1].Indent >= indent) stack.RemoveAt(stack.Count - 1);
            var key = match.Groups[1].Value.Trim('"');
            stack.Add((indent, key));

            var value = match.Groups[2].Value.Trim();
            if (value is "|" or "|-" or "|+" or ">" or ">-" or ">+")
            {
                blockIndent = indent;
                continue;
            }

            var contexts = key == "if" && !value.Contains("${{", StringComparison.Ordinal)
                ? Context.Matches(value).Select(m => m.Value).ToList()
                : Read(value);
            if (contexts.Count > 0) yield return (i + 1, Keys(stack), contexts);
        }
    }

    // The contexts the expressions in a text read.
    private static List<string> Read(string text) =>
        [.. Regex.Matches(text, @"\$\{\{(.*?)\}\}").SelectMany(e => Context.Matches(e.Groups[1].Value).Select(m => m.Value))];

    private static List<string> Keys(List<(int Indent, string Key)> stack) => [.. stack.Select(entry => entry.Key)];

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The checkout's root was not found above the test assembly.");
    }
}
