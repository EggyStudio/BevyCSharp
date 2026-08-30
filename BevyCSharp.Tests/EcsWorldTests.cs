using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>Components used only by these tests.</summary>
public struct Health
{
    public int Value;
}

public struct Armour
{
    public float Rating;
}

public struct Poisoned;

public struct Shielded;

/// <summary>
/// Covers the ECS surface against a real Bevy world.
/// </summary>
/// <remarks>
/// These are not mocked. Each test spins up an actual headless Bevy app, so a regression in the
/// native bridge, the component-layout handshake or the chunk pointers fails here rather than
/// somewhere far downstream.
/// </remarks>
[Collection("engine")]
public sealed class EcsWorldTests
{
    [Fact]
    public void SpawnInsertReadRemoveRoundTrips()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            Assert.True(ctx.Ecs.IsAlive(entity));
            Assert.False(ctx.Ecs.Has<Health>(entity));

            ctx.Ecs.Add(entity, new Health { Value = 42 });
            Assert.True(ctx.Ecs.Has<Health>(entity));
            Assert.True(ctx.Ecs.TryGet<Health>(entity, out var health));
            Assert.Equal(42, health.Value);

            ctx.Ecs.Remove<Health>(entity);
            Assert.False(ctx.Ecs.Has<Health>(entity));

            Assert.True(ctx.Ecs.Despawn(entity));
            Assert.False(ctx.Ecs.IsAlive(entity));
        });

        harness.Run();
    }

    [Fact]
    public void WritesThroughGetRefLandInBevyStorage()
    {
        using var harness = new EngineHarness(frames: 2);
        var entity = Entity.None;

        harness.OnContext(Stage.Startup, ctx =>
        {
            entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Health { Value = 1 });

            ref var health = ref ctx.Ecs.GetRef<Health>(entity);
            health.Value = 99;
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!ctx.Ecs.TryGet<Health>(entity, out var health)) return;
            Assert.Equal(99, health.Value);
        });

        harness.Run();
    }

    [Fact]
    public void QueryYieldsReferencesThatMutateInPlace()
    {
        using var harness = new EngineHarness(frames: 3);
        var finalTotal = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < 10; i++)
                ctx.Ecs.Add(ctx.Ecs.Spawn(), new Health { Value = i });
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            foreach (var row in ctx.Ecs.Query<Health>())
                row.Component.Value += 1;
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            finalTotal = 0;
            foreach (var row in ctx.Ecs.Query<Health>(markChanged: false))
                finalTotal += row.Component.Value;
        });

        harness.Run();

        // 0..9 sums to 45; three Update runs add 10 each time.
        Assert.Equal(45 + 30, finalTotal);
    }

    [Fact]
    public void CountReflectsArchetypeMembership()
    {
        using var harness = new EngineHarness(frames: 2);
        var counted = -1;

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < 7; i++) ctx.Ecs.Add(ctx.Ecs.Spawn(), new Health());
            for (var i = 0; i < 3; i++) ctx.Ecs.Add(ctx.Ecs.Spawn(), new Armour());
        });

        harness.OnContext(Stage.Update, ctx => counted = ctx.Ecs.Count<Health>());

        harness.Run();
        Assert.Equal(7, counted);
    }

    [Fact]
    public void StaleHandleIsDetectedByGeneration()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var first = ctx.Ecs.Spawn();
            ctx.Ecs.Despawn(first);

            // Bevy reuses the index but bumps the generation, so the old handle stays invalid
            // even though something else now occupies the slot.
            var second = ctx.Ecs.Spawn();
            Assert.False(ctx.Ecs.IsAlive(first));
            Assert.True(ctx.Ecs.IsAlive(second));
        });

        harness.Run();
    }

    [Fact]
    public void FiltersAreAppliedPerArchetype()
    {
        using var harness = new EngineHarness(frames: 2);
        var withPoison = 0;
        var withoutShield = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            // Health only.
            ctx.Ecs.Add(ctx.Ecs.Spawn(), new Health());

            // Health + Poisoned.
            var poisoned = ctx.Ecs.Spawn();
            ctx.Ecs.Add(poisoned, new Health());
            ctx.Ecs.Add(poisoned, new Poisoned());

            // Health + Poisoned + Shielded.
            var both = ctx.Ecs.Spawn();
            ctx.Ecs.Add(both, new Health());
            ctx.Ecs.Add(both, new Poisoned());
            ctx.Ecs.Add(both, new Shielded());
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            Span<int> poison = [EcsWorld.ComponentId<Poisoned>()];
            Span<int> shield = [EcsWorld.ComponentId<Shielded>()];

            using (var chunks = ctx.Ecs.Chunks<Health>(poison, default, markChanged: false))
                withPoison = chunks.TotalLength;

            using (var chunks = ctx.Ecs.Chunks<Health>(poison, shield, markChanged: false))
                withoutShield = chunks.TotalLength;
        });

        harness.Run();

        Assert.Equal(2, withPoison);
        Assert.Equal(1, withoutShield);
    }

    [Fact]
    public void EcsCallsFromAWorkerThreadAreRejectedRatherThanRacing()
    {
        using var harness = new EngineHarness(frames: 2);
        BevyNativeException? captured = null;

        harness.OnContext(Stage.Update, ctx =>
        {
            // A dedicated thread rather than the thread pool: `Task.Wait` is allowed to inline
            // a queued task onto the waiting thread, which would run this on the main thread
            // and quietly test nothing.
            var worker = new Thread(() =>
            {
                try
                {
                    ctx.Ecs.Spawn();
                }
                catch (BevyNativeException ex)
                {
                    captured = ex;
                }
            });

            worker.Start();
            worker.Join();
        });

        harness.Run();

        Assert.NotNull(captured);
        Assert.Equal(NativeStatus.NoWorld, captured!.Status);
    }

    [Fact]
    public void EntityIndexAndGenerationAreDecodedByTheEngine()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var first = ctx.Ecs.Spawn();
            var second = ctx.Ecs.Spawn();

            // Index must be a small slot number, not the handle's raw bits. Bevy packs the
            // index through a niche-optimised type, so masking the handle yields something near
            // uint.MaxValue, which is exactly the bug this guards against.
            Assert.True(first.Index < 1024, $"index looks like raw bits: {first.Index}");
            Assert.NotEqual((uint)(first.Bits & 0xFFFF_FFFF), first.Index);

            // Distinct entities have distinct handles and distinct slots.
            Assert.NotEqual(first, second);
            Assert.NotEqual(first.Index, second.Index);

            // Generation starts at zero and is readable.
            Assert.Equal(0u, first.Generation);
            Assert.Equal(0u, second.Generation);
        });

        harness.Run();
    }

    [Fact]
    public void ComponentIdsAreStableWithinAnAppAndDistinctBetweenTypes()
    {
        using var harness = new EngineHarness(frames: 2);
        var healthId = -1;
        var armourId = -1;

        harness.OnContext(Stage.Startup, _ =>
        {
            healthId = EcsWorld.ComponentId<Health>();
            armourId = EcsWorld.ComponentId<Armour>();
        });

        harness.OnContext(Stage.Update, _ =>
        {
            Assert.Equal(healthId, EcsWorld.ComponentId<Health>());
            Assert.NotEqual(healthId, armourId);
        });

        harness.Run();
        Assert.True(healthId >= 0);
    }
}
