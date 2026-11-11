using System.Runtime.CompilerServices;
using Xunit;

namespace Bevy.Tests;

/// <summary>A mark a behavior puts on its own entity, which moves the entity to another table.</summary>
[Behavior]
public partial struct RowMark
{
    /// <summary>A value, so the mark is not empty.</summary>
    public int Value;
}

/// <summary>Counts its frames and marks its own entity in the first, through the world rather than a queue.</summary>
[Behavior]
public partial struct SelfMarker
{
    /// <summary>The frames it ran in.</summary>
    public int Ticks;

    /// <summary>Whether it has marked its entity.</summary>
    public bool Marked;

    /// <summary>Counts the frame, and marks the entity the first time.</summary>
    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Ticks++;
        if (Marked) return;

        ctx.Ecs.Add(ctx.Entity, new RowMark { Value = 1 });
        Marked = true;
    }
}

/// <summary>As <see cref="SelfMarker"/>, moving its entity's transform as well, taken by reference.</summary>
[Behavior]
public partial struct SelfMover
{
    /// <summary>The frames it ran in.</summary>
    public int Ticks;

    /// <summary>Whether it has marked its entity.</summary>
    public bool Marked;

    /// <summary>Counts the frame and moves the transform a unit, marking the entity the first time between the two.</summary>
    [OnUpdate]
    public void Tick(BehaviorContext ctx, ref Transform transform)
    {
        Ticks++;
        if (!Marked)
        {
            ctx.Ecs.Add(ctx.Entity, new RowMark { Value = 1 });
            Marked = true;
        }

        transform.Translation.X += 1f;
    }
}

/// <summary>Counts its frames, and where it grows, fills its own table until the table's columns move.</summary>
[Behavior]
public partial struct Grower
{
    /// <summary>The frames it ran in.</summary>
    public int Ticks;

    /// <summary>Whether this one fills the table.</summary>
    public bool Grows;

    /// <summary>Whether it has.</summary>
    public bool Grew;

    /// <summary>Counts the frame, and fills the table the first time where it grows.</summary>
    [OnUpdate]
    public unsafe void Tick(BehaviorContext ctx)
    {
        Ticks++;
        if (!Grows || Grew) return;

        var before = (nint)Unsafe.AsPointer(ref ctx.Ecs.GetRef<Grower>(ctx.Entity));
        for (var i = 0; i < RowMoveTests.Filled; i++) ctx.Ecs.Add(ctx.Ecs.Spawn(), new Grower());
        GrowerProbe.Moved = before != (nint)Unsafe.AsPointer(ref ctx.Ecs.GetRef<Grower>(ctx.Entity));
        Grew = true;
    }
}

/// <summary>Whether the grower's own component moved when it filled its table.</summary>
public static class GrowerProbe
{
    /// <summary>Whether it moved.</summary>
    public static bool Moved;
}

/// <summary>Plays a graph on its own entity once, which puts Bevy's player and graph handle there.</summary>
[Behavior]
public partial struct GraphStarter
{
    /// <summary>Whether it has played.</summary>
    public bool Started;

    /// <summary>Plays the graph the first time, and counts the play.</summary>
    [OnUpdate]
    public void Start(BehaviorContext ctx)
    {
        if (Started) return;

        Animation.PlayGraph(ctx.Entity, GraphStarts.Graph, GraphStarts.Node, repeat: true);
        Started = true;
        GraphStarts.Plays[ctx.Entity] = GraphStarts.Plays.GetValueOrDefault(ctx.Entity) + 1;
    }
}

/// <summary>The graph <see cref="GraphStarter"/> plays, and how many times each entity played it.</summary>
public static class GraphStarts
{
    /// <summary>The graph.</summary>
    public static AssetHandle Graph;

    /// <summary>Its clip's node.</summary>
    public static uint Node;

    /// <summary>Each entity's count of plays.</summary>
    public static readonly Dictionary<Entity, int> Plays = [];
}

/// <summary>
/// Covers a behavior's method making a call that moves its own entity's row, which the runner
/// notices, putting what the method wrote where the entity went and going on with the rest.
/// </summary>
/// <remarks>
/// A method is handed a copy of its components and the copy written back, and a call that moved
/// the row sends it to the entity instead. A method holding its row by reference would write after
/// such a call into whichever entity took the row, since a call that puts a table component on its
/// entity moves the entity to another table and the old table's last row into its place, as the
/// foxes of many_foxes did. Eight entities share a table, so a row that moves is followed by others.
/// </remarks>
[Collection("engine")]
public sealed class RowMoveTests
{
    /// <summary>How many entities the grower adds to its table, past any room a table of eight keeps.</summary>
    internal const int Filled = 4096;

    /// <summary>
    /// A method that puts a component on its own entity keeps what it wrote after the call, and
    /// each entity runs once a frame though it moved to a table the walk reaches later.
    /// </summary>
    [Fact]
    public void AMethodMarkingItsOwnEntityKeepsWhatItWroteAndRunsOnceAFrame()
    {
        using var harness = new EngineHarness(frames: 4, discoverBehaviors: true);
        var seen = new List<(int Ticks, bool Marked, bool Has)>();
        var frames = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < 8; i++) ctx.Ecs.Add(ctx.Ecs.Spawn(), new SelfMarker());
        });
        harness.OnContext(Stage.Last, ctx =>
        {
            frames++;
            seen.Clear();
            foreach (var row in ctx.Ecs.Query<SelfMarker>(markChanged: false))
                seen.Add((row.Component.Ticks, row.Component.Marked, ctx.Ecs.Has<RowMark>(row.Entity)));
        });

        harness.Run();

        Assert.Equal(8, seen.Count);
        Assert.All(seen, entity =>
        {
            Assert.Equal(frames, entity.Ticks);
            Assert.True(entity.Marked, "the method's write after the call landed elsewhere");
            Assert.True(entity.Has);
        });
    }

    /// <summary>
    /// A method taking its transform by reference as well keeps both what it wrote to itself and
    /// what it wrote to the transform after the call moved them, and runs once a frame.
    /// </summary>
    [Fact]
    public void AMethodTakingAnotherComponentKeepsBothWritesAfterItsEntityMoves()
    {
        using var harness = new EngineHarness(frames: 4, discoverBehaviors: true);
        var seen = new List<(int Ticks, bool Marked, float X)>();
        var frames = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < 8; i++)
            {
                var entity = ctx.Ecs.Spawn();
                ctx.Ecs.Add(entity, new SelfMover());
                ctx.Ecs.Add(entity, Transform.Identity);
            }
        });
        harness.OnContext(Stage.Last, ctx =>
        {
            frames++;
            seen.Clear();
            foreach (var row in ctx.Ecs.Query<SelfMover>(markChanged: false))
                seen.Add((row.Component.Ticks, row.Component.Marked, ctx.Ecs.GetOrDefault<Transform>(row.Entity).Translation.X));
        });

        harness.Run();

        Assert.Equal(8, seen.Count);
        Assert.All(seen, entity =>
        {
            Assert.Equal(frames, entity.Ticks);
            Assert.True(entity.Marked, "the method's write to itself after the call landed elsewhere");
            Assert.Equal(frames, entity.X);
        });
    }

    /// <summary>
    /// A method that adds entities to its own table until the table's columns grow into new
    /// memory, which moves its own component without moving its entity, keeps what it wrote after,
    /// and the walk goes on through the rows listed again rather than the memory let go of.
    /// </summary>
    [Fact]
    public void AMethodGrowingItsOwnTableKeepsWhatItWrote()
    {
        GrowerProbe.Moved = false;
        using var harness = new EngineHarness(frames: 4, discoverBehaviors: true);
        var first = new List<Entity>();
        var seen = new Dictionary<Entity, (int Ticks, bool Grew)>();
        var frames = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < 8; i++)
            {
                var entity = ctx.Ecs.Spawn();
                ctx.Ecs.Add(entity, new Grower { Grows = i == 3 });
                first.Add(entity);
            }
        });
        harness.OnContext(Stage.Last, ctx =>
        {
            frames++;
            foreach (var entity in first)
                seen[entity] = ctx.Ecs.TryGet<Grower>(entity, out var grower) ? (grower.Ticks, grower.Grew) : (-1, false);
        });

        harness.Run();

        Assert.True(GrowerProbe.Moved, $"adding {Filled} entities left the grower's component where it was, so nothing here grew");
        Assert.All(first, entity => Assert.Equal(frames, seen[entity].Ticks));
        Assert.True(seen[first[3]].Grew, "the grower's write after its table grew landed elsewhere");
    }

    /// <summary>
    /// A method playing a graph on its own entity, which puts Bevy's animation player and graph
    /// handle there, Bevy's own types whose storage is the table, plays once and keeps what it
    /// wrote after the call.
    /// </summary>
    [SkippableFact]
    public void AMethodPlayingAGraphOnItsOwnEntityKeepsWhatItWrote()
    {
        Needs.Renderer();

        GraphStarts.Plays.Clear();
        var entities = new List<Entity>();
        var started = 0;

        var run = new PictureRun
        {
            Build = app =>
            {
                app.AddPlugin(EngineHarness.Behaviors());
                app.AddState(Screen.Menu);
            },
            Scene = ecs =>
            {
                (GraphStarts.Graph, GraphStarts.Node) = Animation.GraphFromClip(Animation.CreateClip());
                for (var i = 0; i < 8; i++)
                {
                    var entity = ecs.Spawn();
                    ecs.Add(entity, Transform.Identity);
                    ecs.Add(entity, new GraphStarter());
                    entities.Add(entity);
                }
            },
        };

        run.Until("every graph plays", _ => GraphStarts.Plays.Count == entities.Count)
            .Wait(5)
            .Do("reading the entities", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                started = entities.Count(entity => ecs.TryGet<GraphStarter>(entity, out var starter) && starter.Started);
            })
            .Go();

        Assert.Equal(entities.Count, started);
        Assert.All(entities, entity => Assert.Equal(1, GraphStarts.Plays[entity]));
    }
}
