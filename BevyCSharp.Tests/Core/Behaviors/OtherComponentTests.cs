using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Moves its entity's transform by its own velocity, through the transform it is handed.</summary>
[Behavior]
public partial struct Drifting
{
    public float Speed;

    [OnUpdate]
    public void Drift(BehaviorContext ctx, ref Transform transform) =>
        transform.Translation = transform.Translation + new Vec3(Speed, 0f, 0f);
}

/// <summary>Reads its entity's transform and counts in another component, two others at once.</summary>
[Behavior]
public partial struct Watching
{
    public float Seen;

    [OnUpdate]
    public void Watch(BehaviorContext ctx, in Transform transform, ref Health health)
    {
        Seen = transform.Translation.X;
        health.Value++;
    }
}

/// <summary>
/// Covers a behavior method taking its entity's other components, handed from the same storage
/// as its own rather than reached through the world an entity at a time.
/// </summary>
/// <remarks>In the engine collection, since each runs an app with the test assembly's behaviors.</remarks>
[Collection("engine")]
public sealed class OtherComponentTests
{
    [Fact]
    public void EachEntityMovesItsOwnTransformAndTheWholeSetCrossesAFewTimes()
    {
        // Past the count at which a method per entity is spread across threads, so the pairing
        // is checked on that path too.
        const int Count = 5000;

        using var harness = new EngineHarness(frames: 6, discoverBehaviors: true);
        var movers = new Entity[Count];
        var frame = 0;
        FrameCosts? costs = null;
        var wrong = new List<string>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < Count; i++)
            {
                movers[i] = ctx.Ecs.Spawn();
                ctx.Ecs.Add(movers[i], Transform.Identity);
                ctx.Ecs.Add(movers[i], new Drifting { Speed = i + 1 });
            }
        });

        harness.App.AddSystem(Stage.First, new SystemDescriptor(world =>
        {
            frame++;
            if (frame == 2) FrameProfile.Start();
            if (frame != 5) return;

            var ecs = world.Resource<EcsWorld>();
            costs = FrameProfile.Take(ecs);
            FrameProfile.Stop();

            // Every one moved by its own speed the same number of times, so a transform handed to
            // the wrong entity's method would show.
            var runs = ecs.GetOrDefault<Transform>(movers[0]).Translation.X;
            for (var i = 0; i < Count; i++)
            {
                var x = ecs.GetOrDefault<Transform>(movers[i]).Translation.X;
                if (MathF.Abs(x - runs * (i + 1)) > 0.01f * (i + 1)) wrong.Add($"#{i} at {x}, not {runs * (i + 1)}");
            }
        }, "Test.Check"));

        harness.Run();

        Assert.Empty(wrong);
        Assert.NotNull(costs);

        // The test's own checks cross an entity at a time in another frame; the frames measured
        // cross a few times for the whole set, not once for each of five thousand.
        Assert.True(costs!.Crossings < 200, $"{costs.Crossings} crossings a frame");
    }

    [Fact]
    public void AComponentTakenInIsReadAndNotMarkedChangedAndTwoAreHandedAtOnce()
    {
        using var harness = new EngineHarness(frames: 6, discoverBehaviors: true);
        var watcher = Entity.None;
        var frame = 0;
        uint tick = 0;
        List<Entity>? changed = null;
        var seen = 0f;
        var counted = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            watcher = ctx.Ecs.Spawn();
            ctx.Ecs.Add(watcher, Transform.At(7f, 0f, 0f));
            ctx.Ecs.Add(watcher, new Health());
            ctx.Ecs.Add(watcher, new Watching());
        });

        harness.App.AddSystem(Stage.First, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            frame++;

            if (frame == 2) ecs.ChangedSince<Transform>(ref tick);
            if (frame == 5) changed = ecs.ChangedSince<Transform>(ref tick);
            if (frame == 5)
            {
                seen = ecs.GetOrDefault<Watching>(watcher).Seen;
                counted = ecs.GetOrDefault<Health>(watcher).Value;
            }
        }, "Test.Watch"));

        harness.Run();

        Assert.Equal(7f, seen);
        Assert.True(counted >= 3, $"counted {counted}");
        Assert.DoesNotContain(watcher, changed!);
    }
}
