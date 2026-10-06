using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers what an app is told when a system is scoped to a state it never added.</summary>
/// <remarks>
/// The systems are the three <see cref="StateTests"/> declares scoped to <see cref="Screen.Playing"/>,
/// which every app that discovers the tests' behaviors runs, and most such apps add no screen,
/// since they test something else.
/// </remarks>
[Collection("engine")]
public sealed class StateScopeReportTests
{
    /// <summary>
    /// An app holding several systems scoped to a state it never added is told so once, naming the
    /// state, rather than once a system, and the next app is told again, since it is another app
    /// with the same mistake.
    /// </summary>
    [Fact]
    public void AStateNeverAddedIsReportedOnceAnApp()
    {
        var first = Reports();
        var second = Reports();

        Assert.Equal(1, Count(first));
        Assert.Equal(1, Count(second));
        Assert.Contains("every other system scoped to Screen will never run", first);
    }

    private static int Count(string text) =>
        text.Split('\n').Count(line => line.Contains("is scoped to Screen.", StringComparison.Ordinal));

    /// <summary>What an app with every test behavior and no screen writes to the error stream.</summary>
    private static string Reports()
    {
        var error = Console.Error;
        using var captured = new StringWriter();
        Console.SetError(captured);
        try
        {
            using var harness = new EngineHarness(frames: 3, discoverBehaviors: true);
            harness.Run();
        }
        finally
        {
            Console.SetError(error);
        }

        return captured.ToString();
    }
}
