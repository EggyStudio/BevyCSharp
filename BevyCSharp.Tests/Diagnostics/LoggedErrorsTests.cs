using System.Reflection;
using Bevy;
using Xunit;
using Xunit.Sdk;

namespace Bevy.Tests;

/// <summary>
/// The hook that fails a test for an error the engine logged during it (N 3.7), run here around
/// errors logged in its own instance's place, so these tests judge what it would have said.
/// </summary>
/// <remarks>
/// In the engine's collection, since two of them make an app, and two native apps at once are not
/// allowed.
/// </remarks>
[Collection("engine")]
public sealed class LoggedErrorsTests
{
    private static void Plain() { }

    [ExpectsError("test", "expected")]
    private static void Expecting() { }

    private static MethodInfo Method(string name) => typeof(LoggedErrorsTests).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>What the hook says after a test of the method given, during which the action ran.</summary>
    private static string? Judge(string method, Action during)
    {
        var hook = new FailOnLoggedErrorsAttribute();
        hook.Before(Method(method));
        during();
        try
        {
            hook.After(Method(method));
            return null;
        }
        catch (XunitException failure)
        {
            return failure.Message;
        }
    }

    /// <summary>On a thread that inherits nothing of this flow, as another test's or Bevy's is.</summary>
    private static void InAnotherFlow(Action action)
    {
        Thread thread;
        using (ExecutionContext.SuppressFlow())
        {
            thread = new Thread(() => action());
            thread.Start();
        }

        thread.Join();
    }

    [Fact]
    public void AnErrorLoggedInTheTestsFlowFailsItWithItsFirstLine()
    {
        Assert.Equal(
            "N 3.7: the engine logged 1 error during this test, the first: [test] it broke",
            Judge(nameof(Plain), () => EngineLog.Error(null, "test", "it broke\n   at somewhere", new InvalidOperationException("inside"))));
    }

    [Fact]
    public void AnExpectedErrorPassesAndOneThatDoesNotComeFails()
    {
        Assert.Null(Judge(nameof(Expecting), () => EngineLog.Error(null, "test", "an expected failure")));
        Assert.Null(Judge(nameof(Expecting), () => EngineLog.Error(null, "test", "a failure", new InvalidOperationException("as expected"))));
        Assert.Equal(
            "N 3.7: the test expects an error of test saying 'expected', and none was logged.",
            Judge(nameof(Expecting), () => { }));
    }

    [Fact]
    public void AnErrorAnAppLogsFromAnyThreadIsTheTestsThatMadeItAndAnotherTestsAppsIsNot()
    {
        Assert.EndsWith("[test] from a thread of Bevy's", Judge(nameof(Plain), () =>
        {
            using var app = new App(Config.HeadlessFor(1));
            InAnotherFlow(() => EngineLog.Error(app, "test", "from a thread of Bevy's"));
        }));

        Assert.Null(Judge(nameof(Plain), () => InAnotherFlow(() =>
        {
            using var other = new App(Config.HeadlessFor(1));
            EngineLog.Error(other, "test", "from another test's app");
        })));
    }
}
