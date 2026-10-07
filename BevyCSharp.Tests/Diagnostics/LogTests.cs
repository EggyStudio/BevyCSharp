using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers Bevy's log written from C#, its once forms, and a run ended with a failure code.</summary>
/// <remarks>
/// A line Bevy's filter shows goes to its output, which nothing here reads, so the error level is
/// what a test can see, kept by the bridge as Bevy's own errors are and laid to the app that logged
/// it. The once forms are a question of which line of code asked, answered without the engine.
/// </remarks>
[Collection("engine")]
public sealed class LogTests
{
    /// <summary>A line written at the error level is one of the engine's errors, under the target csharp.</summary>
    [Fact]
    [ExpectsError("bevy", "csharp: the tower fell over")]
    public void AnErrorWrittenFromCSharpIsOneOfTheEnginesErrors()
    {
        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Startup, _ =>
        {
            Log.Info("the tower is standing");
            Log.Warn("the tower leans");
            Log.Error("the tower fell over");
        });

        harness.Run();
    }

    /// <summary>Work given to once runs the first time its line of code asks, however often it asks, and another line's runs too.</summary>
    [Fact]
    public void OnceRunsItsWorkOnceForEachLineOfCode()
    {
        var (first, second) = (0, 0);
        for (var i = 0; i < 5; i++)
        {
            Log.Once(() => first++);
            Log.Once(() => second++);
        }

        Assert.Equal((1, 1), (first, second));
    }

    /// <summary>A run ended with a code answers it, and a code no process can end with is refused.</summary>
    [Fact]
    public void ARunEndedWithACodeAnswersIt()
    {
        var frame = 0;
        using var harness = new EngineHarness(frames: 30);
        harness.OnContext(Stage.Update, ctx =>
        {
            if (++frame == 2) ctx.Exit(7);
        });

        Assert.Equal(7, harness.App.Run());
        Assert.Throws<ArgumentOutOfRangeException>(() => App.RequestExit(256));
    }
}
