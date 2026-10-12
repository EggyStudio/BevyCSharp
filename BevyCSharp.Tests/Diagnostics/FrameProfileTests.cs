using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers measuring a frame, split into its managed systems and their calls into the bridge.</summary>
/// <remarks>In the engine collection, since it runs an app, and the counts are the bridge's own.</remarks>
[Collection("engine")]
public sealed class FrameProfileTests
{
    [Fact]
    public void AFrameIsSplitIntoItsSystemsAndTheirCrossings()
    {
        using var harness = new EngineHarness(frames: 14);

        var mover = Entity.None;
        var frame = 0;
        FrameCosts? costs = null;

        harness.OnContext(Stage.Startup, ctx =>
        {
            mover = ctx.Ecs.Spawn();
            ctx.Ecs.Add(mover, Transform.Identity);
        });

        harness.App.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            frame++;

            // A hundred calls into the bridge a frame, each a read of a component.
            for (var i = 0; i < 100; i++) ecs.GetRef<Transform>(mover).Translation.X += 1f;

            if (frame == 2) FrameProfile.Start();
            else if (frame == 12)
            {
                costs = FrameProfile.Take(ecs);
                FrameProfile.Stop();
            }
        }, "Test.Crossings"));

        harness.Run();

        Assert.NotNull(costs);
        Assert.InRange(costs!.Frames, 8, 10);
        Assert.True(costs.Crossings >= 100, $"counted {costs.Crossings} crossings a frame");
        Assert.True(costs.ManagedRuns >= 1);
        Assert.True(costs.FrameMilliseconds > 0d);
        Assert.True(costs.ScheduleMilliseconds <= costs.FrameMilliseconds + 0.01);
        Assert.Contains(costs.Systems, system => system.Name == "Test.Crossings");
        Assert.False(FrameProfile.On);
    }
}
