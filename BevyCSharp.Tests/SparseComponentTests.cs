using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>A sparse tag a generated filter names.</summary>
public struct Charging : ISparseComponent;

/// <summary>A behavior whose tick is filtered to entities carrying a sparse tag.</summary>
/// <remarks>
/// The generated runner works in component ids and passes them to <c>Chunks</c>, so a sparse tag
/// reaches the same per-entity path a hand-written filter does. This is the route a game takes.
/// </remarks>
[Behavior]
public partial struct SparseFiltered
{
    /// <summary>How many times this entity's tick has run.</summary>
    public int Ticks;

    [OnUpdate]
    [With(typeof(Charging))]
    public void Tick(BehaviorContext ctx) => Ticks++;
}

/// <summary>
/// Covers components Bevy keeps in a sparse set rather than a table.
/// </summary>
/// <remarks>
/// The point of the storage is a tag that is added and removed far more often than it is read,
/// so what has to work is filtering on one, and what cannot work is iterating it.
/// </remarks>
[Collection("engine")]
public sealed class SparseComponentTests
{
    /// <summary>A table-stored component, so it can be the one a query iterates.</summary>
    private struct Position
    {
        public float X;
    }

    /// <summary>The tag under test: cheap to add and remove, never iterated.</summary>
    private struct Stunned : ISparseComponent;

    /// <summary>A second one, carried by a contiguous stretch rather than every other row.</summary>
    private struct Burning : ISparseComponent;

    [Fact]
    public void SparseComponentsBehaveLikeAnyOtherOnASingleEntity()
    {
        // Everything that works one entity at a time goes through Bevy's entity API, which does
        // not care which storage a component lives in.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            Assert.False(ctx.Ecs.Has<Stunned>(entity));

            ctx.Ecs.Add(entity, new Stunned());
            Assert.True(ctx.Ecs.Has<Stunned>(entity));
            Assert.Equal(1, ctx.Ecs.Count<Stunned>());

            Assert.True(ctx.Ecs.Remove<Stunned>(entity));
            Assert.False(ctx.Ecs.Has<Stunned>(entity));
            Assert.Equal(0, ctx.Ecs.Count<Stunned>());
        });

        harness.Run();
    }

    [Fact]
    public void IteratingASparseComponentIsRefusedWithAnExplanation()
    {
        // Bevy keeps the values in a dense column but exposes no way to reach it, so there is
        // nothing for a chunk to point at. Better to say so than to return nothing and let a
        // system quietly do no work.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            ctx.Ecs.Add(ctx.Ecs.Spawn(), new Stunned());

            var query = Assert.Throws<BevyNativeException>(() => ctx.Ecs.Query<Stunned>());
            Assert.Equal(NativeStatus.Unsupported, query.Status);
            Assert.Contains("With or Without filter", query.Message);

            var entities = Assert.Throws<BevyNativeException>(() => ctx.Ecs.EntitiesWith<Stunned>());
            Assert.Equal(NativeStatus.Unsupported, entities.Status);
        });

        harness.Run();
    }

    [Fact]
    public void ASparseFilterSelectsTheRightEntities()
    {
        // The gap this closes. A table-stored filter is answered once per table; a sparse one
        // has to be answered per entity, and the two have to compose.
        using var harness = new EngineHarness(frames: 3);
        var withStunned = new List<float>();
        var withoutStunned = new List<float>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            // Alternating, so the matching rows are not one contiguous stretch of the table and
            // the query has to break it into several runs.
            for (var i = 0; i < 10; i++)
            {
                var entity = ctx.Ecs.Spawn();
                ctx.Ecs.Add(entity, new Position { X = i });
                if (i % 2 == 0) ctx.Ecs.Add(entity, new Stunned());
            }
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            // Update runs once per frame, so each pass starts from empty rather than appending
            // the same rows again.
            withStunned.Clear();
            withoutStunned.Clear();
            var stunned = EcsWorld.ComponentId<Stunned>();

            using (var chunks = ctx.Ecs.Chunks<Position>(with: [stunned], markChanged: false))
                for (var c = 0; c < chunks.Count; c++)
                    foreach (var position in chunks[c].Components<Position>())
                        withStunned.Add(position.X);

            using (var chunks = ctx.Ecs.Chunks<Position>(without: [stunned], markChanged: false))
                for (var c = 0; c < chunks.Count; c++)
                    foreach (var position in chunks[c].Components<Position>())
                        withoutStunned.Add(position.X);
        });

        harness.Run();

        Assert.Equal([0f, 2f, 4f, 6f, 8f], withStunned.Order());
        Assert.Equal([1f, 3f, 5f, 7f, 9f], withoutStunned.Order());
    }

    [Fact]
    public void ASparseFilterSplitsATableIntoContiguousRuns()
    {
        // What a per-entity filter costs: the rows still come back, but a table that alternates
        // yields one chunk per matching row rather than one chunk for the table. Asserting it
        // here pins down the contract, because a caller holding a pointer and a length has to be
        // able to trust that every row in the chunk matched.
        using var harness = new EngineHarness(frames: 3);
        var alternatingChunks = 0;
        var contiguousChunks = 0;
        var rows = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < 8; i++)
            {
                var entity = ctx.Ecs.Spawn();
                ctx.Ecs.Add(entity, new Position { X = i });
                if (i % 2 == 0) ctx.Ecs.Add(entity, new Stunned());

                // The first four in a row, so this filter has one unbroken stretch.
                if (i < 4) ctx.Ecs.Add(entity, new Burning());
            }
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            using (var chunks = ctx.Ecs.Chunks<Position>(
                       with: [EcsWorld.ComponentId<Stunned>()], markChanged: false))
            {
                alternatingChunks = chunks.Count;
                rows = chunks.TotalLength;
            }

            using (var chunks = ctx.Ecs.Chunks<Position>(
                       with: [EcsWorld.ComponentId<Burning>()], markChanged: false))
                contiguousChunks = chunks.Count;
        });

        harness.Run();

        Assert.Equal(4, alternatingChunks);   // every other row, so four runs of one
        Assert.Equal(4, rows);
        Assert.Equal(1, contiguousChunks);    // one unbroken stretch, so one run
    }

    [Fact]
    public void SparseAndTableFiltersCompose()
    {
        // The two are answered in different places, one per table and one per entity, so this
        // checks they narrow the same result rather than one overriding the other.
        using var harness = new EngineHarness(frames: 3);
        var matched = new List<float>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < 8; i++)
            {
                var entity = ctx.Ecs.Spawn();
                ctx.Ecs.Add(entity, new Position { X = i });
                if (i % 2 == 0) ctx.Ecs.Add(entity, new Stunned());

                // Table-stored, and carried by the second half, which moves those entities into
                // a different table from the first half.
                if (i >= 4) ctx.Ecs.Add(entity, Transform.Identity);
            }
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            matched.Clear();

            using var chunks = ctx.Ecs.Chunks<Position>(
                with: [EcsWorld.ComponentId<Stunned>(), NativeComponents.Transform],
                markChanged: false);

            for (var c = 0; c < chunks.Count; c++)
                foreach (var position in chunks[c].Components<Position>())
                    matched.Add(position.X);
        });

        harness.Run();

        // Even, and in the half carrying a Transform.
        Assert.Equal([4f, 6f], matched.Order());
    }

    [Fact]
    public void AGeneratedFilterCanNameASparseTag()
    {
        using var harness = new EngineHarness(frames: 4, discoverBehaviors: true);
        var ticksByEntity = new Dictionary<Entity, int>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            // Alternating again, so the runner has to walk runs rather than whole tables.
            for (var i = 0; i < 6; i++)
            {
                var entity = ctx.Ecs.Spawn();
                ctx.Ecs.Add(entity, new SparseFiltered());
                if (i % 2 == 0) ctx.Ecs.Add(entity, new Charging());
            }
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            ticksByEntity.Clear();
            foreach (var row in ctx.Ecs.Query<SparseFiltered>(markChanged: false))
                ticksByEntity[row.Entity] = row.Component.Ticks;
        });

        harness.Run();

        Assert.Equal(6, ticksByEntity.Count);
        Assert.Equal(3, ticksByEntity.Values.Count(t => t > 0));
    }
}
