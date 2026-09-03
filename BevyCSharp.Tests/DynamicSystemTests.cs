using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers adding and retiring systems while the loop is running.
/// </summary>
/// <remarks>
/// What script hot reload rests on. A schedule cannot be added to once Bevy owns it, so a system
/// that arrives late runs through a dispatcher put in each stage beforehand. These check that it
/// arrives, that it stops when its generation is retired, and that asking without the dispatchers
/// still fails rather than quietly doing nothing.
/// </remarks>
[Collection("engine")]
public sealed class DynamicSystemTests
{
    [Fact]
    public void ASystemAddedWhileRunningStillRuns()
    {
        using var harness = new EngineHarness(frames: 10);
        harness.App.EnableDynamicSystems();

        var ran = 0;
        var added = false;

        harness.OnContext(Stage.Update, _ =>
        {
            if (added) return;
            added = true;

            harness.App.AddSystem(Stage.Update, _ => ran++);
        });

        harness.Run();

        Assert.True(added, "the system that adds the other one never ran");
        Assert.True(ran > 0, "the system added while running never ran");
    }

    [Fact]
    public void RetiringAGenerationStopsIt()
    {
        using var harness = new EngineHarness(frames: 12);
        harness.App.EnableDynamicSystems();

        var ran = 0;
        var stoppedAt = -1;

        harness.OnContext(Stage.Update, ctx =>
        {
            switch (ctx.Time.FrameCount)
            {
                case 2:
                    // Tagged, which is how one generation of scripts is told from the next.
                    using (new SystemRegistrationSourceScope("Scripts.Generation1"))
                        harness.App.AddSystem(Stage.Update, _ => ran++);
                    break;

                case 6:
                    harness.App.RemoveSystemsBySource("Scripts.Generation1");
                    stoppedAt = ran;
                    break;
            }
        });

        harness.Run();

        Assert.True(ran > 0, "the added system never ran");
        Assert.True(stoppedAt >= 0, "the generation was never retired");
        Assert.Equal(stoppedAt, ran);
    }

    [Fact]
    public void AddingWhileRunningIsRefusedWithoutTheDispatchers()
    {
        // The default, and the one that catches a system registered from the wrong place: without
        // somewhere to put it, a late system would never run and nothing would say so.
        using var harness = new EngineHarness(frames: 4);

        var refused = false;

        harness.OnContext(Stage.Update, _ =>
        {
            if (refused) return;

            try
            {
                harness.App.AddSystem(Stage.Update, _ => { });
            }
            catch (InvalidOperationException e) when (e.Message.Contains("EnableDynamicSystems"))
            {
                refused = true;
            }
        });

        harness.Run();
        Assert.True(refused, "adding a system while running was allowed without the dispatchers");
    }
}
