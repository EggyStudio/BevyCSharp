using System.Collections.Concurrent;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A behaviour with enough entities to cross the parallel threshold.
/// </summary>
/// <remarks>
/// The count matters: <see cref="BehaviorRunners.DefaultParallelThreshold"/> decides whether
/// iteration is split across the thread pool, and the two paths are different code. This
/// behaviour is deliberately spawned above the threshold so the parallel path is the one under
/// test.
/// </remarks>
[Behavior]
public partial struct Particle
{
    /// <summary>Position along one axis.</summary>
    public float X;

    /// <summary>Speed along one axis.</summary>
    public float Velocity;

    /// <summary>Number of ticks this entity has seen.</summary>
    public int Ticks;

    /// <summary>The entity index this tick observed, used to check the binding is right.</summary>
    public uint SeenEntityIndex;

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        X += Velocity;
        Ticks++;
        SeenEntityIndex = ctx.Entity.Index;
        ParticleProbe.Threads.TryAdd(Environment.CurrentManagedThreadId, 0);
    }
}

/// <summary>Records which threads ran <see cref="Particle.Tick"/>.</summary>
public static class ParticleProbe
{
    /// <summary>Thread ids observed inside the behaviour method.</summary>
    public static readonly ConcurrentDictionary<int, byte> Threads = new();
}

/// <summary>Exercises the iteration paths at a scale where the parallel split kicks in.</summary>
[Collection("engine")]
public sealed class ScaleTests
{
    private const int ParticleCount = BehaviorRunners.DefaultParallelThreshold * 2;

    [Fact]
    public void EveryEntityIsVisitedExactlyOncePerFrameAtScale()
    {
        ParticleProbe.Threads.Clear();

        using var harness = new EngineHarness(frames: 4, discoverBehaviors: true);
        var ticks = new List<int>();
        var positions = new List<float>();
        var entityBindingsCorrect = true;

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < ParticleCount; i++)
                ctx.Ecs.Add(ctx.Ecs.Spawn(), new Particle { Velocity = 1f });
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            if (ctx.Time.FrameCount != 3) return;

            foreach (var row in ctx.Ecs.Query<Particle>(markChanged: false))
            {
                ticks.Add(row.Component.Ticks);
                positions.Add(row.Component.X);

                // Each invocation must have been handed its own entity, not a neighbour's.
                if (row.Component.SeenEntityIndex != row.Entity.Index)
                    entityBindingsCorrect = false;
            }
        });

        harness.Run();

        Assert.Equal(ParticleCount, ticks.Count);
        Assert.True(entityBindingsCorrect, "A behaviour saw the wrong entity for its component.");

        // Every entity must have been visited the same number of times: no double-counting
        // across partition boundaries, and nothing skipped.
        Assert.All(ticks, t => Assert.Equal(ticks[0], t));
        Assert.True(ticks[0] >= 3, $"expected at least 3 ticks, saw {ticks[0]}");

        // Position is written through the chunk pointer, so it proves the writes landed.
        Assert.All(positions, x => Assert.Equal(ticks[0], x));
    }

    [Fact]
    public void LargeIterationsAreActuallySplitAcrossThreads()
    {
        ParticleProbe.Threads.Clear();

        using var harness = new EngineHarness(frames: 3, discoverBehaviors: true);

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < ParticleCount; i++)
                ctx.Ecs.Add(ctx.Ecs.Spawn(), new Particle { Velocity = 1f });
        });

        harness.Run();

        // On a single-core machine the pool may legitimately use one thread, so this asserts
        // the weaker fact that the parallel path ran at all rather than a specific thread count.
        Assert.NotEmpty(ParticleProbe.Threads);
        if (Environment.ProcessorCount > 2)
            Assert.True(ParticleProbe.Threads.Count > 1,
                $"expected the work to be split, saw {ParticleProbe.Threads.Count} thread(s)");
    }

    [Fact]
    public void SpawningAndDespawningManyEntitiesStaysConsistent()
    {
        using var harness = new EngineHarness(frames: 6);
        var counts = new List<int>();

        harness.OnContext(Stage.Update, ctx =>
        {
            // Add a batch every frame and remove the previous one, so archetypes churn.
            ctx.Cmd.SpawnBatch(500, (entity, ecs) => ecs.Add(entity, new Health { Value = 1 }));
        });

        harness.OnContext(Stage.PostUpdate, ctx =>
        {
            var toRemove = new List<Entity>();
            foreach (var row in ctx.Ecs.Query<Health>(markChanged: false))
            {
                if (row.Component.Value > 0) toRemove.Add(row.Entity);
                if (toRemove.Count >= 250) break;
            }

            foreach (var entity in toRemove) ctx.Cmd.Despawn(entity);
        });

        harness.OnContext(Stage.Last, ctx => counts.Add(ctx.Ecs.Count<Health>()));

        harness.Run();

        Assert.Equal(6, counts.Count);

        // Each frame adds 500 and removes 250, so the population grows by 250 a frame.
        for (var i = 1; i < counts.Count; i++)
            Assert.Equal(counts[i - 1] + 250, counts[i]);
    }

    [Fact]
    public void ManyArchetypesAreAllVisited()
    {
        using var harness = new EngineHarness(frames: 3);
        var total = 0;
        var poisonedOnly = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            // Four distinct archetypes, so the query has to walk several tables.
            for (var i = 0; i < 100; i++)
            {
                var entity = ctx.Ecs.Spawn();
                ctx.Ecs.Add(entity, new Health { Value = i });

                if (i % 2 == 0) ctx.Ecs.Add(entity, new Poisoned());
                if (i % 3 == 0) ctx.Ecs.Add(entity, new Shielded());
            }
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            total = ctx.Ecs.Count<Health>();

            Span<int> poison = [EcsWorld.ComponentId<Poisoned>()];
            using var chunks = ctx.Ecs.Chunks<Health>(poison, default, markChanged: false);
            poisonedOnly = chunks.TotalLength;

            Assert.True(chunks.Count >= 2, "expected the poisoned entities to span several tables");
        });

        harness.Run();

        Assert.Equal(100, total);
        Assert.Equal(50, poisonedOnly);
    }
}
