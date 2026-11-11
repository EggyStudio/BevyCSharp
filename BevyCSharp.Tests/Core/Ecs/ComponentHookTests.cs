using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>A component the hook tests put on and take off.</summary>
public struct Hooked
{
    /// <summary>The value each hook reads.</summary>
    public uint Value;
}

/// <summary>A second, for the hook given after an entity carries it.</summary>
public struct HookedLate
{
    /// <summary>Unread.</summary>
    public uint Value;
}

/// <summary>Covers a game's component hooks, Bevy's add, insert, discard and remove, and what one may reach.</summary>
/// <remarks>
/// A hook runs inside Bevy while Bevy holds the world, so besides the order Bevy runs them in and
/// the value each sees, these hold that a hook queues what it changes and that a call into Bevy's
/// world from one is refused rather than made beside Bevy's own.
/// </remarks>
[Collection("engine")]
public sealed class ComponentHookTests
{
    /// <summary>Each hook runs in Bevy's order with the value it sees, and a hook's queued despawn lands.</summary>
    [Fact]
    public void EachHookRunsInBevysOrderWithItsValue()
    {
        var heard = new List<(string Kind, uint Value)>();
        var frame = 0;
        var entity = Entity.None;
        bool? aliveAfter = null;
        BevyNativeException? refused = null;

        using var harness = new EngineHarness(frames: 8);
        harness.App
            .OnAdd((HookContext ctx, in Hooked hooked) =>
            {
                heard.Add(("add", hooked.Value));
                refused = Assert.Throws<BevyNativeException>(() => ctx.Res<EcsWorld>().Spawn());
            })
            .OnInsert((HookContext _, in Hooked hooked) => heard.Add(("insert", hooked.Value)))
            .OnDiscard((HookContext _, in Hooked hooked) => heard.Add(("discard", hooked.Value)))
            .OnRemove((HookContext ctx, in Hooked hooked) =>
            {
                heard.Add(("remove", hooked.Value));
                ctx.Cmd.Despawn(ctx.Entity);
            });

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (frame == 2)
            {
                entity = ctx.Ecs.Spawn();
                ctx.Ecs.Add(entity, new Hooked { Value = 3 });
                ctx.Ecs.Add(entity, new Hooked { Value = 5 });
                ctx.Ecs.Remove<Hooked>(entity);
            }

            if (frame == 5) aliveAfter = ctx.Ecs.IsAlive(entity);
        });

        harness.Run();

        Assert.Equal([("add", 3u), ("insert", 3u), ("discard", 3u), ("insert", 5u), ("discard", 5u), ("remove", 5u)], heard);
        Assert.Equal(NativeStatus.NoWorld, refused?.Status);
        Assert.False(aliveAfter);
    }

    /// <summary>A hook given once an entity carries the component is refused, since Bevy takes hooks before that.</summary>
    [Fact]
    public void AHookGivenOnceAnEntityCarriesTheComponentIsRefused()
    {
        BevyNativeException? refused = null;
        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Startup, ctx =>
        {
            ctx.Ecs.Add(ctx.Ecs.Spawn(), new HookedLate { Value = 1 });
            refused = Assert.Throws<BevyNativeException>(() => harness.App.OnAdd((HookContext _, in HookedLate _) => { }));
        });

        harness.Run();
        Assert.Equal(NativeStatus.InvalidState, refused?.Status);
    }
}
