using System.Text.RegularExpressions;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Every expression in the workflows names only the contexts GitHub gives the place it stands in,
/// so a file GitHub refuses is seen here before a push rather than as a run with no jobs after it.
/// </summary>
/// <remarks>
/// <para>
/// A job's <c>env</c> that read <c>runner.temp</c>, which GitHub gives to steps alone, had both
/// workflows refused on the push of <c>a31e3b3</c>, each ending within a minute with no job and no
/// word of why (REVIEW.md, Verdict 4). The file parsed as YAML, and nothing that runs here reads
/// it as GitHub does.
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
