using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers <see cref="EcsWorld.Removed{T}"/>, the entities that lost a component since the running
/// system last asked, against a real Bevy world.
/// </summary>
[Collection("engine")]
public sealed class RemovedTests
{
    /// <summary>
    /// A removal and a despawn reach a system once each, and a second system asking about the same
    /// component sees them as well, each keeping its own place.
    /// </summary>
    [Fact]
    public void EachSystemSeesARemovalAndADespawnOnce()
    {
        using var harness = new EngineHarness(frames: 6);
        Entity kept = Entity.None, stripped = Entity.None, gone = Entity.None;
        var frame = 0;
        var first = new List<List<Entity>>();
        var second = new List<List<Entity>>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            (kept, stripped, gone) = (ctx.Ecs.Spawn(), ctx.Ecs.Spawn(), ctx.Ecs.Spawn());
            foreach (var entity in new[] { kept, stripped, gone }) ctx.Ecs.Add(entity, new Health { Value = 3 });
        });

        // The removals happen in the second frame, between what the readers read.
        harness.OnContext(Stage.PreUpdate, ctx =>
        {
            if (++frame != 2) return;
            ctx.Ecs.Remove<Health>(stripped);
            ctx.Ecs.Despawn(gone);
        });

        harness.OnContext(Stage.Update, ctx => first.Add(ctx.Ecs.Removed<Health>()));

        // The second reader asks only from the third frame on, so the removals are older than its
        // first question and still held.
        harness.OnContext(Stage.PostUpdate, ctx =>
        {
            if (frame >= 3) second.Add(ctx.Ecs.Removed<Health>());
        });

        harness.Run();

        Assert.Equal([stripped, gone], first.SelectMany(seen => seen));
        Assert.Equal([stripped, gone], first[1]);
        Assert.Equal([stripped, gone], second.SelectMany(seen => seen));
    }

    /// <summary>Asked outside a system, there is no last run to count from, which is said.</summary>
    [Fact]
    public void OutsideASystemItIsRefused()
    {
        using var harness = new EngineHarness(frames: 1);
        harness.Run();

        Assert.Throws<InvalidOperationException>(() => harness.App.World.Resource<EcsWorld>().Removed<Health>());
    }
}
