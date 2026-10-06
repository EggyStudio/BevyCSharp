using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A system said in one call, as Bevy's <c>add_systems</c> says it, at startup, every frame, or in
/// a stage of its own while a condition passes.
/// </summary>
[Collection("engine")]
public sealed class AppStageTests
{
    [Fact]
    public void StartupRunsOnceAndUpdateEveryFrameEachHandedTheFramesContext()
    {
        using var harness = new EngineHarness(frames: 5);
        var started = 0;
        var updated = new List<ulong>();
        harness.App
            .Startup(_ => started++, "Tests.Setup")
            .Update(ctx => updated.Add(ctx.Time.FrameCount), "Tests.Each");

        harness.Run();

        Assert.Equal(1, started);
        Assert.Equal(5, updated.Count);
        Assert.Equal(updated.Order(), updated);
        Assert.Contains(harness.App.SystemsIn(Stage.Startup), system => system.Name == "Tests.Setup");
        Assert.Contains(harness.App.SystemsIn(Stage.Update), system => system.Name == "Tests.Each");
    }

    [Fact]
    public void OnRunsInItsStageWhileItsConditionPasses()
    {
        using var harness = new EngineHarness(frames: 6);
        var frames = 0;
        var ran = new List<int>();
        harness.App
            .Update(_ => frames++, "Tests.Count")
            .On(Stage.PostUpdate, _ => ran.Add(frames), "Tests.Even", runIf: _ => frames % 2 == 0);

        harness.Run();

        Assert.Equal([2, 4, 6], ran);
        Assert.Contains(harness.App.SystemsIn(Stage.PostUpdate), system => system.Name == "Tests.Even");
    }
}
