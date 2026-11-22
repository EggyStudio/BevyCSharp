using Bevy;
using Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what the generator says a command takes.
/// </summary>
/// <remarks>
/// The schema lets something that has never seen a command compose a call to it, which is the
/// difference between a command line a person reads the source to use and one a program can
/// discover. The usage string is for a person; this is the same thing for a caller.
/// </remarks>
public sealed class CliCommandSchemaTests
{
    [Fact]
    public void ACommandWithNoArgumentsTakesNothing()
    {
        var command = Find("tests.nothing");

        Assert.Empty(command.Arguments);
        Assert.Equal("tests.nothing", command.Usage);
    }

    [Fact]
    public void EveryParameterIsNamedAndTyped()
    {
        var command = Find("tests.several");

        Assert.Collection(
            command.Arguments,
            first =>
            {
                Assert.Equal("which", first.Name);
                Assert.Equal("text", first.Kind);
            },
            second =>
            {
                Assert.Equal("count", second.Name);
                Assert.Equal("whole", second.Kind);
            },
            third =>
            {
                Assert.Equal("scale", third.Name);
                Assert.Equal("single", third.Kind);
            },
            fourth =>
            {
                Assert.Equal("on", fourth.Name);
                Assert.Equal("flag", fourth.Kind);
            });

        Assert.All(command.Arguments, parameter => Assert.False(parameter.TakesLine));
        Assert.Equal("tests.several <which> <count> <scale> <on>", command.Usage);
    }

    /// <summary>The one case where a parameter is the rest of the line rather than a word.</summary>
    [Fact]
    public void ASingleStringTakesTheWholeLine()
    {
        var parameter = Assert.Single(Find("tests.line").Arguments);

        Assert.Equal("text", parameter.Kind);
        Assert.True(parameter.TakesLine);
    }

    [Fact]
    public void AndTheWholeLineIsWhatItReceives()
    {
        Assert.Equal("a  b \"c\"", ConsoleHost.Run("tests.line a  b \"c\"").Text);
        Assert.Equal("a b", ConsoleHost.Run("tests.line \"a b\"").Text);
    }

    [Fact]
    public void WordsAreReadIntoTheTypesDeclared() =>
        Assert.Equal("cube 3 1.5 True", ConsoleHost.Run("tests.several cube 3 1.5 on").Text);

    [Fact]
    public void AWordThatWillNotGoIsAnsweredWithASentence()
    {
        var answer = ConsoleHost.Run("tests.several cube many 1.5 on");
        Assert.Equal("not a whole: many", answer.Text);
        Assert.Equal(Answer.BadArgument, answer.Code);
    }

    /// <summary>The command of that name, or a failure that says which one was missing.</summary>
    private static Command Find(string name) =>
        ConsoleHost.Commands.FindCommand(name) ?? throw new Xunit.Sdk.XunitException(
            $"{name} was not registered; the generator should have emitted it.");
}

/// <summary>Commands that exist only to be looked at by the tests above.</summary>
internal static class SchemaProbeCommands
{
    [Command("tests.nothing", "Takes nothing")]
    internal static string Nothing() => "nothing";

    [Command("tests.several", "Takes one of each")]
    internal static string Several(string which, int count, float scale, bool on) =>
        $"{which} {count} {scale.ToString(System.Globalization.CultureInfo.InvariantCulture)} {on}";

    [Command("tests.line", "Takes the rest of the line")]
    internal static string Line(string text) => text;
}
