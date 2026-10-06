using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A component the observer tests watch come and go.</summary>
public struct Watched
{
    public int Value;
}

/// <summary>An event a game declares, with no entity.</summary>
public readonly record struct Explosion(float Radius);

/// <summary>An event that happens to one entity and stays there.</summary>
public readonly record struct Struck(Entity Entity, int Damage) : IEntityEvent;

/// <summary>An event that goes on up from the entity to its parents, as an attack on armor does.</summary>
public record struct Attack(Entity Entity, int Damage) : IPropagatingEvent;

/// <summary>
/// Covers observers, of a game's own events, triggered from C#, and of changes to a C# component,
/// which Bevy reports through the bridge.
/// </summary>
/// <remarks>
/// A lifecycle observer runs from a callback out of Bevy, where an exception cannot cross back, so
/// those tests record what their observers saw and assert it outside the observer.
/// </remarks>
[Collection("engine")]
public sealed class ObserverTests
{
    [Fact]
    public void AnEventRunsItsObserversInTheOrderTheyWereAddedAndStopsWhenOneIsDisposed()
    {
        using var harness = new EngineHarness(frames: 2);
        var seen = new List<string>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            var first = ctx.Ecs.Observe<Explosion>(on => seen.Add($"first {on.Event.Radius}"));
            ctx.Ecs.Observe<Explosion>(on => seen.Add($"second {on.Event.Radius}"));
            ctx.Ecs.Observe<Struck>(_ => seen.Add("not an explosion"));

            ctx.Ecs.Trigger(new Explosion(2f));
            first.Dispose();
            ctx.Ecs.Trigger(new Explosion(3f));
        });

        harness.Run();
        Assert.Equal(["first 2", "second 2", "second 3"], seen);
    }

    [Fact]
    public void AnEntityEventReachesTheGlobalObserversThenItsEntitysButNotAnothersEntitys()
    {
        using var harness = new EngineHarness(frames: 2);
        var seen = new List<string>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            var target = ctx.Ecs.Spawn();
            var bystander = ctx.Ecs.Spawn();
            ctx.Ecs.Observe<Struck>(target, on => seen.Add($"target {on.Event.Damage}"));
            ctx.Ecs.Observe<Struck>(bystander, _ => seen.Add("bystander"));
            ctx.Ecs.Observe<Struck>(on => seen.Add($"anyone {on.Entity == target}"));

            ctx.Ecs.Trigger(new Struck(target, 5));
        });

        harness.Run();
        Assert.Equal(["anyone True", "target 5"], seen);
    }

    /// <summary>
    /// A propagating event climbs the hierarchy, an observer's change to it reaches the ones above,
    /// and an observer that stops it keeps it from its parent.
    /// </summary>
    /// <remarks>Bevy's <c>observer_propagation</c>, armor on a body on a root, in miniature.</remarks>
    [Fact]
    public void APropagatingEventClimbsToTheParentsCarryingChangesUntilStopped()
    {
        using var harness = new EngineHarness(frames: 2);
        var seen = new List<string>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            var root = ctx.Ecs.Spawn();
            var body = ctx.Ecs.Spawn();
            var armor = ctx.Ecs.Spawn();
            ctx.Ecs.SetParent(body, root);
            ctx.Ecs.SetParent(armor, body);

            ctx.Ecs.Observe<Attack>(armor, on =>
            {
                seen.Add($"armor {on.Event.Damage}");
                on.Event.Damage -= 3;
                if (on.Event.Damage <= 0) on.Propagate(false);
            });
            ctx.Ecs.Observe<Attack>(body, on => seen.Add($"body {on.Event.Damage}"));
            ctx.Ecs.Observe<Attack>(root, on => seen.Add($"root {on.Event.Damage} at {(on.Entity == root ? "root" : "?")}"));

            ctx.Ecs.Trigger(new Attack(armor, 10));
            ctx.Ecs.Trigger(new Attack(armor, 2));
        });

        harness.Run();
        Assert.Equal(["armor 10", "body 7", "root 7 at root", "armor 2"], seen);
    }

    /// <summary>
    /// Bevy reports a C# component's addition, replacement and removal before the call that made
    /// each returns, and a removal is handed the value that went, after the component has gone.
    /// </summary>
    [Fact]
    public void AComponentsComingAndGoingIsObservedWithItsValues()
    {
        using var harness = new EngineHarness(frames: 2);
        var seen = new List<string>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            ctx.Ecs.Observe<Add<Watched>>(on => seen.Add($"add {on.Event.Value.Value}"));
            ctx.Ecs.Observe<Insert<Watched>>(on => seen.Add($"insert {on.Event.Value.Value}"));
            ctx.Ecs.Observe<Discard<Watched>>(on => seen.Add($"discard {on.Event.Value.Value}"));
            ctx.Ecs.Observe<Remove<Watched>>(on => seen.Add(
                $"remove {on.Event.Value.Value} {(on.Ecs.Has<Watched>(on.Event.Entity) ? "still there" : "gone")}"));

            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Watched { Value = 1 });
            Assert.Equal(["add 1", "insert 1"], seen);

            ctx.Ecs.Add(entity, new Watched { Value = 2 });
            ctx.Ecs.Remove<Watched>(entity);
            Assert.Equal(["add 1", "insert 1", "discard 1", "insert 2", "discard 2", "remove 2 gone"], seen);
        });

        harness.Run();
    }

    /// <summary>
    /// Despawning an entity reports its component's removal and despawn, and an observer of one
    /// entity hears only that entity's changes.
    /// </summary>
    [Fact]
    public void DespawningIsObservedAndAnEntitysObserverHearsOnlyIt()
    {
        using var harness = new EngineHarness(frames: 3);
        var seen = new List<string>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            var mine = ctx.Ecs.Spawn();
            var other = ctx.Ecs.Spawn();
            ctx.Ecs.Add(mine, new Watched { Value = 7 });
            ctx.Ecs.Add(other, new Watched { Value = 8 });

            ctx.Ecs.Observe<Remove<Watched>>(mine, on => seen.Add($"mine removed {on.Event.Value.Value}"));
            ctx.Ecs.Observe<Despawn<Watched>>(on => seen.Add($"despawned {on.Event.Value.Value}"));

            ctx.Ecs.Despawn(other);
            ctx.Ecs.Despawn(mine);
        });

        harness.Run();
        Assert.Equal(["despawned 8", "despawned 7", "mine removed 7"], seen);
    }

    [Fact]
    public void AnObserverAddedToTheAppBeforeItRunsHearsTheFirstFrame()
    {
        using var app = new App(Config.HeadlessFor(2));
        app.AddPlugin(new EnginePlugin());
        var added = 0;

        app.AddObserver<Add<Watched>>(_ => added++);
        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            ecs.Add(ecs.Spawn(), new Watched { Value = 1 });
        }, "Spawner"));

        Assert.Equal(0, app.Run());
        Assert.Equal(1, added);
    }
}
