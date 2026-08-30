using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A behaviour with per-entity state, driven by an instance method.</summary>
[Behavior]
public partial struct Counter
{
    /// <summary>How many times this entity's tick has run.</summary>
    public int Ticks;

    /// <summary>Amount added on each tick, so entities can be told apart.</summary>
    public int Step;

    /// <summary>Running total.</summary>
    public int Total;

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Ticks++;
        Total += Step;
    }
}

/// <summary>A behaviour whose instance method is filtered to a subset of entities.</summary>
[Behavior]
public partial struct FilteredCounter
{
    /// <summary>How many times this entity's tick has run.</summary>
    public int Ticks;

    [OnUpdate]
    [With(typeof(Poisoned))]
    [Without(typeof(Shielded))]
    public void Tick(BehaviorContext ctx) => Ticks++;
}

/// <summary>A behaviour with only static methods, used to check scheduling order.</summary>
[Behavior]
public partial struct StageRecorder
{
    /// <summary>The stages observed, in the order they ran, across all frames.</summary>
    public static readonly List<Stage> Observed = [];

    /// <summary>Set to stop recording once one full frame has been captured.</summary>
    public static bool Recording;

    [OnFirst]
    public static void First(BehaviorContext ctx) => Record(Stage.First);

    [OnPreUpdate]
    public static void PreUpdate(BehaviorContext ctx) => Record(Stage.PreUpdate);

    [OnUpdate]
    public static void Update(BehaviorContext ctx) => Record(Stage.Update);

    [OnPostUpdate]
    public static void PostUpdate(BehaviorContext ctx) => Record(Stage.PostUpdate);

    [OnRender]
    public static void Render(BehaviorContext ctx) => Record(Stage.Render);

    [OnLast]
    public static void Last(BehaviorContext ctx) => Record(Stage.Last);

    private static void Record(Stage stage)
    {
        if (Recording) Observed.Add(stage);
    }
}

/// <summary>A behaviour gated by a static bool, to exercise <c>[RunIf]</c>.</summary>
[Behavior]
public partial struct Gated
{
    /// <summary>Whether the system should run.</summary>
    public static bool Enabled;

    /// <summary>How many times it ran.</summary>
    public static int Runs;

    [OnUpdate]
    [RunIf(nameof(Enabled))]
    public static void Tick(BehaviorContext ctx) => Runs++;
}

/// <summary>
/// Covers the generated behaviour plumbing: registration, per-entity dispatch, filters,
/// conditions and stage ordering.
/// </summary>
[Collection("engine")]
public sealed class BehaviorTests
{
    [Fact]
    public void InstanceMethodsRunPerEntityAndPersistState()
    {
        using var harness = new EngineHarness(frames: 5, discoverBehaviors: true);
        var totals = new List<int>();
        var ticks = new List<int>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            ctx.Ecs.Add(ctx.Ecs.Spawn(), new Counter { Step = 1 });
            ctx.Ecs.Add(ctx.Ecs.Spawn(), new Counter { Step = 10 });
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            totals.Clear();
            ticks.Clear();
            foreach (var row in ctx.Ecs.Query<Counter>(markChanged: false))
            {
                totals.Add(row.Component.Total);
                ticks.Add(row.Component.Ticks);
            }
        });

        harness.Run();

        // Startup runs before the first frame, so Update runs once per frame thereafter.
        Assert.Equal(2, ticks.Count);
        Assert.All(ticks, t => Assert.Equal(ticks[0], t));
        Assert.True(ticks[0] >= 4, $"expected at least 4 ticks, saw {ticks[0]}");

        totals.Sort();
        Assert.Equal(ticks[0] * 1, totals[0]);
        Assert.Equal(ticks[0] * 10, totals[1]);
    }

    [Fact]
    public void WithAndWithoutFiltersSelectTheRightEntities()
    {
        using var harness = new EngineHarness(frames: 4, discoverBehaviors: true);
        var ticksByEntity = new Dictionary<Entity, int>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            // Not poisoned: filtered out by [With].
            var plain = ctx.Ecs.Spawn();
            ctx.Ecs.Add(plain, new FilteredCounter());

            // Poisoned: matches.
            var poisoned = ctx.Ecs.Spawn();
            ctx.Ecs.Add(poisoned, new FilteredCounter());
            ctx.Ecs.Add(poisoned, new Poisoned());

            // Poisoned and shielded: filtered out by [Without].
            var shielded = ctx.Ecs.Spawn();
            ctx.Ecs.Add(shielded, new FilteredCounter());
            ctx.Ecs.Add(shielded, new Poisoned());
            ctx.Ecs.Add(shielded, new Shielded());
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            ticksByEntity.Clear();
            foreach (var row in ctx.Ecs.Query<FilteredCounter>(markChanged: false))
                ticksByEntity[row.Entity] = row.Component.Ticks;
        });

        harness.Run();

        Assert.Equal(3, ticksByEntity.Count);

        var ticked = ticksByEntity.Values.Count(t => t > 0);
        Assert.Equal(1, ticked);
    }

    [Fact]
    public void StagesRunInDeclaredOrderWithinAFrame()
    {
        StageRecorder.Observed.Clear();
        StageRecorder.Recording = false;

        using var harness = new EngineHarness(frames: 3, discoverBehaviors: true);

        // Record exactly one frame. Recording is switched on at the *end* of frame 0 rather
        // than at the top of frame 1, because two systems in the same stage have no ordering
        // between them - starting from Stage.First could race StageRecorder's own First.
        harness.OnContext(Stage.Cleanup, _ => { }, "Test.KeepAlive");

        harness.OnContext(Stage.Last, ctx =>
        {
            if (ctx.Time.FrameCount == 0) StageRecorder.Recording = true;
            else if (StageRecorder.Observed.Count >= 6) StageRecorder.Recording = false;
        }, "Test.ToggleRecording");

        harness.Run();

        Assert.Equal(
            [Stage.First, Stage.PreUpdate, Stage.Update, Stage.PostUpdate, Stage.Render, Stage.Last],
            StageRecorder.Observed.Take(6));
    }

    [Fact]
    public void RunIfSkipsTheSystemWhileTheConditionIsFalse()
    {
        Gated.Runs = 0;
        Gated.Enabled = false;

        using (var harness = new EngineHarness(frames: 4, discoverBehaviors: true))
        {
            harness.Run();
        }

        Assert.Equal(0, Gated.Runs);

        Gated.Runs = 0;
        Gated.Enabled = true;

        using (var harness = new EngineHarness(frames: 4, discoverBehaviors: true))
        {
            harness.Run();
        }

        Assert.True(Gated.Runs >= 3, $"expected the gated system to run, saw {Gated.Runs}");
    }

    [Fact]
    public void BehaviorsPluginFindsTheGeneratedRegistration()
    {
        using var app = new App(Config.HeadlessFor(1));
        var plugin = new BehaviorsPlugin();
        app.AddPlugin(new EnginePlugin());
        app.AddPlugin(plugin);

        // One generated registration method per assembly that declares behaviours.
        Assert.True(plugin.RegistrationsFound >= 1);
        Assert.Contains(app.SystemsIn(Stage.Update), s => s.Name == "Counter.Tick");
    }
}
