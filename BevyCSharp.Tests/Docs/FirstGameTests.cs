using System.Text.RegularExpressions;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// <c>docs/first-game.md</c> against the game it makes, <c>games/FirstGame</c>, whose every step is
/// a whole program under <c>steps/</c> that <c>build/first-game.sh</c> builds and runs. Each block of
/// code the page marks with a step is in that step's program as the page shows it, line for line,
/// so the page cannot show code the build does not.
/// </summary>
/// <remarks>
/// Taken from 3DEngine's test of its own first game (its <c>d5d2578d</c>).
/// </remarks>
public sealed partial class FirstGameTests
{
    private static string Root => CheatsheetTests.RepositoryRoot();

    private static string Game => Path.Combine(Root, "games", "FirstGame");

    [GeneratedRegex(@"<!-- step (?<step>\d\d) -->\s*```csharp\n(?<code>.*?)\n```", RegexOptions.Singleline)]
    private static partial Regex MarkedBlock();

    private static List<(string Step, string Code)> Blocks() =>
        [.. MarkedBlock().Matches(File.ReadAllText(Path.Combine(Root, "docs", "first-game.md")).ReplaceLineEndings("\n"))
            .Select(match => (match.Groups["step"].Value, match.Groups["code"].Value))];

    private static string[] Lines(string text) => [.. text.ReplaceLineEndings("\n").Split('\n').Select(line => line.TrimEnd())];

    [Fact]
    public void EveryBlockThePageMarksWithAStepIsInThatStepsProgram()
    {
        var blocks = Blocks();
        var steps = Directory.GetFiles(Path.Combine(Game, "steps"), "*.cs").Select(Path.GetFileNameWithoutExtension).Order().ToList();

        Assert.True(steps.Count >= 10, $"the game is told in {steps.Count} steps");
        Assert.Equal(steps, blocks.Select(block => block.Step).Distinct().Order());

        foreach (var (step, code) in blocks)
        {
            var program = Lines(File.ReadAllText(Path.Combine(Game, "steps", step + ".cs")));
            var shown = Lines(code);
            var found = Enumerable.Range(0, program.Length - shown.Length + 1).Any(at => program.Skip(at).Take(shown.Length).SequenceEqual(shown));
            Assert.True(found, $"the page's block for step {step}, starting \"{shown[0].Trim()}\", is not in steps/{step}.cs as shown");
        }
    }

    [Fact]
    public void TheLastStepIsTheFinishedGame()
    {
        var last = Directory.GetFiles(Path.Combine(Game, "steps"), "*.cs").Order(StringComparer.Ordinal).Last();

        Assert.Equal(File.ReadAllText(Path.Combine(Game, "Program.cs")), File.ReadAllText(last));
    }

    [Fact]
    public void EveryStepHasItsPicture()
    {
        foreach (var step in Directory.GetFiles(Path.Combine(Game, "steps"), "*.cs").Select(Path.GetFileNameWithoutExtension))
            Assert.True(File.Exists(Path.Combine(Root, ".github", "assets", "first-game", step + ".webp")), $"step {step} has no picture");
    }
}
