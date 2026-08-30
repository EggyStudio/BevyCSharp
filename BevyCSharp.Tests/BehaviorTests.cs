using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A behavior with per-entity state, driven by an instance method.</summary>
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

/// <summary>A behavior whose instance method is filtered to a subset of entities.</summary>
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

/// <summary>A behavior with only static methods, used to check scheduling order.</summary>
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

/// <summary>A behavior gated by a static bool, to exercise <c>[RunIf]</c>.</summary>
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

/// <summary>A behavior whose toggle shortcut needs two modifiers at once.</summary>
[Behavior]
public partial struct ChordHud
{
    [OnRender]
    [ToggleKey(Key.F3, KeyModifier.Ctrl | KeyModifier.Shift, DefaultEnabled = false)]
    public static void Draw(BehaviorContext ctx) { }
}

/// <summary>
/// Covers the generated behavior plumbing: registration, per-entity dispatch, filters,
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
        StageRecorder.Recording = true;

        using (var harness = new EngineHarness(frames: 3, discoverBehaviors: true))
        {
            harness.Run();
        }

        StageRecorder.Recording = false;

        // Recording runs for the whole app rather than being switched on mid-frame: two systems
        // in the same stage have no ordering between them, so anything that tried to start
        // recording from inside a stage would race whichever system shares it. Instead, take a
        // whole frame out of the middle by anchoring on the first First.
        var firstFrame = StageRecorder.Observed.IndexOf(Stage.First);
        Assert.True(firstFrame >= 0, "the recorder never ran");

        Assert.Equal(
            [Stage.First, Stage.PreUpdate, Stage.Update, Stage.PostUpdate, Stage.Render, Stage.Last],
            StageRecorder.Observed.Skip(firstFrame).Take(6));
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
    public void CombinedToggleModifiersSurviveTheGenerator()
    {
        using var app = new App(Config.HeadlessFor(1));
        app.AddPlugin(new EnginePlugin());
        app.AddPlugin(new BehaviorsPlugin());

        var condition = Assert
            .Single(app.SystemsIn(Stage.Render), s => s.Name == "ChordHud.Draw")
            .RunCondition;
        Assert.NotNull(condition);

        // Declared DefaultEnabled = false, so it starts off.
        app.World.InsertResource(KeysHeld(pressed: Key.F3));
        Assert.False(condition!(app.World));

        // Ctrl alone must not flip it. Had the generator kept only the first flag of
        // Ctrl | Shift, this would turn the system on and the test would fail here.
        app.World.InsertResource(KeysHeld(Key.F3, Key.ControlLeft));
        Assert.False(condition(app.World));

        // Both modifiers together do flip it.
        app.World.InsertResource(KeysHeld(Key.F3, Key.ControlLeft, Key.ShiftLeft));
        Assert.True(condition(app.World));
    }

    /// <summary>
    /// Builds an <see cref="Input"/> with <paramref name="pressed"/> going down this frame while
    /// <paramref name="held"/> are already down.
    /// </summary>
    /// <remarks>
    /// A headless run has no keyboard, so this writes the same native snapshot struct the engine
    /// fills each frame, keeping the test on the real code path rather than a parallel fake.
    /// </remarks>
    private static unsafe Input KeysHeld(Key pressed, params Key[] held)
    {
        var snapshot = default(Interop.NativeInput);
        SetBit(snapshot.KeysPressed, pressed);
        SetBit(snapshot.KeysDown, pressed);
        foreach (var key in held) SetBit(snapshot.KeysDown, key);

        var input = new Input();
        input.Update(snapshot);
        return input;
    }

    private static unsafe void SetBit(ulong* bits, Key key) =>
        bits[(int)key / 64] |= 1UL << ((int)key % 64);

    [Fact]
    public void BehaviorsPluginFindsTheGeneratedRegistration()
    {
        using var app = new App(Config.HeadlessFor(1));
        var plugin = new BehaviorsPlugin();
        app.AddPlugin(new EnginePlugin());
        app.AddPlugin(plugin);

        // One generated registration method per assembly that declares behaviors.
        Assert.True(plugin.RegistrationsFound >= 1);
        Assert.Contains(app.SystemsIn(Stage.Update), s => s.Name == "Counter.Tick");
    }
}
