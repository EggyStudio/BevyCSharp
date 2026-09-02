using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>The state a game is in: what a menu, a run and a pause screen are told apart by.</summary>
public enum Screen
{
    /// <summary>The title screen.</summary>
    Menu = 0,

    /// <summary>A run in progress.</summary>
    Playing = 1,

    /// <summary>Paused mid-run.</summary>
    Paused = 2,
}

/// <summary>A second, independent state machine, to prove the slots do not collide.</summary>
public enum Connection
{
    /// <summary>Not connected.</summary>
    Offline = 0,

    /// <summary>Connected.</summary>
    Online = 1,
}

/// <summary>A state over a narrow signed enum, with a member below zero.</summary>
public enum Countdown : sbyte
{
    /// <summary>Overrun, which is what makes this signed.</summary>
    Late = -1,

    /// <summary>On time.</summary>
    OnTime = 0,
}

/// <summary>A behavior whose tick only runs while a run is in progress.</summary>
[Behavior]
public partial struct PlayingOnly
{
    /// <summary>Ticks observed while playing.</summary>
    public static int Ticks;

    [OnUpdate]
    [InState(Screen.Playing)]
    public static void Tick(BehaviorContext ctx) => Ticks++;
}

/// <summary>A behavior that builds and tears down as a state is entered and left.</summary>
[Behavior]
public partial struct ScreenLifecycle
{
    /// <summary>How many times the screen has been built.</summary>
    public static int Entered;

    /// <summary>How many times it has been taken away.</summary>
    public static int Exited;

    /// <summary>Frames the tick ran, to show a transition is not a stage.</summary>
    public static int Ticks;

    [OnEnter(Screen.Playing)]
    public static void Build(BehaviorContext ctx) => Entered++;

    [OnExit(Screen.Playing)]
    public static void TearDown(BehaviorContext ctx) => Exited++;

    [OnUpdate]
    [InState(Screen.Playing)]
    public static void Tick(BehaviorContext ctx) => Ticks++;
}

/// <summary>A behavior carrying both an <c>[InState]</c> and a <c>[RunIf]</c>.</summary>
[Behavior]
public partial struct PlayingAndEnabled
{
    /// <summary>The second gate, independent of the state.</summary>
    public static bool Enabled;

    /// <summary>Ticks observed with both gates open.</summary>
    public static int Ticks;

    [OnUpdate]
    [InState(Screen.Playing)]
    [RunIf(nameof(Enabled))]
    public static void Tick(BehaviorContext ctx) => Ticks++;
}

/// <summary>
/// Covers Bevy's app states: the menu/playing/paused axis systems are scoped to.
/// </summary>
[Collection("engine")]
public sealed class StateTests
{
    [Fact]
    public void AStateStartsAtItsInitialValue()
    {
        using var harness = new EngineHarness(frames: 3);
        harness.App.AddState(Screen.Menu);

        var seen = Screen.Playing;
        harness.OnContext(Stage.Update, ctx => seen = ctx.State<Screen>());
        harness.Run();

        Assert.Equal(Screen.Menu, seen);
    }

    [Fact]
    public void ATransitionIsAppliedButNotBeforeTheFrameIsOver()
    {
        // Queued rather than immediate, which is what lets every system in a frame agree on
        // which state it is in instead of some seeing the change halfway through.
        using var harness = new EngineHarness(frames: 4);
        harness.App.AddState(Screen.Menu);

        var atRequest = Screen.Playing;
        var sameFrame = Screen.Playing;
        var afterwards = Screen.Menu;
        var requested = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!requested)
            {
                atRequest = ctx.State<Screen>();
                ctx.SetState(Screen.Playing);
                sameFrame = ctx.State<Screen>();
                requested = true;
                return;
            }

            afterwards = ctx.State<Screen>();
        });

        harness.Run();

        Assert.Equal(Screen.Menu, atRequest);
        Assert.Equal(Screen.Menu, sameFrame);
        Assert.Equal(Screen.Playing, afterwards);
    }

    [Fact]
    public void EntitiesScopedToAStateGoWhenItDoes()
    {
        // What removes a level without a teardown system listing everything in it.
        using var harness = new EngineHarness(frames: 8);
        harness.App.AddState(Screen.Menu);

        var scoped = Entity.None;
        var child = Entity.None;
        var unscoped = Entity.None;
        var aliveInPlaying = false;
        var aliveAfterLeaving = true;
        var childAfterLeaving = true;
        var unscopedSurvived = false;
        var moved = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            switch (ctx.Time.FrameCount)
            {
                case 1:
                    ctx.SetState(Screen.Playing);
                    return;

                case 2:
                    scoped = ctx.Ecs.Spawn();
                    ctx.Ecs.DespawnOnExit(scoped, Screen.Playing);

                    // The despawn is Bevy's own, so it reaches children as well.
                    child = ctx.Ecs.Spawn();
                    ctx.Ecs.SetParent(child, scoped);

                    unscoped = ctx.Ecs.Spawn();
                    aliveInPlaying = ctx.Ecs.IsAlive(scoped);
                    return;

                case 3:
                    ctx.SetState(Screen.Menu);
                    moved = true;
                    return;
            }

            if (!moved) return;

            aliveAfterLeaving = ctx.Ecs.IsAlive(scoped);
            childAfterLeaving = ctx.Ecs.IsAlive(child);
            unscopedSurvived = ctx.Ecs.IsAlive(unscoped);
        });

        harness.Run();

        Assert.True(aliveInPlaying, "the entity did not survive the state it belongs to");
        Assert.False(aliveAfterLeaving, "leaving the state left the entity behind");
        Assert.False(childAfterLeaving, "the child outlived the parent it hung from");
        Assert.True(unscopedSurvived, "an entity that belongs to no state was taken as well");
    }

    [Fact]
    public void ScopingNeedsAnEntityAndAStateThatExist()
    {
        using var harness = new EngineHarness(frames: 3);
        harness.App.AddState(Screen.Menu);

        harness.OnContext(Stage.Update, ctx =>
        {
            var gone = Assert.Throws<BevyNativeException>(
                () => ctx.Ecs.DespawnOnExit(Entity.None, Screen.Playing));
            Assert.Equal(NativeStatus.NoEntity, gone.Status);
        });

        harness.Run();
    }

    [Fact]
    public void TwoStateMachinesAreIndependent()
    {
        // Each enum claims its own slot, and Bevy keys everything on the slot's type, so moving
        // one leaves the other alone.
        using var harness = new EngineHarness(frames: 4);
        harness.App.AddState(Screen.Menu).AddState(Connection.Offline);

        var screen = Screen.Playing;
        var connection = Connection.Online;
        var moved = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!moved)
            {
                ctx.SetState(Connection.Online);
                moved = true;
                return;
            }

            screen = ctx.State<Screen>();
            connection = ctx.State<Connection>();
        });

        harness.Run();

        Assert.Equal(Screen.Menu, screen);
        Assert.Equal(Connection.Online, connection);
    }

    [Fact]
    public void ReadingAStateThatWasNeverAddedSaysSo()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            var ex = Assert.Throws<InvalidOperationException>(() => StateRegistry.Current<Screen>());
            Assert.Contains("AddState", ex.Message);
        });

        harness.Run();
    }

    [Fact]
    public void InStateScopesABehaviorToOneState()
    {
        // The route a game takes. The behavior is registered unconditionally; the state decides
        // whether its tick runs on any given frame.
        PlayingOnly.Ticks = 0;

        using var harness = new EngineHarness(frames: 6, discoverBehaviors: true);
        harness.App.AddState(Screen.Menu);

        var framesInMenu = 0;
        var switched = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (switched) return;

            // Two frames in the menu, where the tick must not run, then a transition.
            if (++framesInMenu < 2) return;

            ctx.SetState(Screen.Playing);
            switched = true;
        });

        harness.Run();

        // It ran, but not on the frames spent in the menu.
        Assert.True(PlayingOnly.Ticks > 0, "the scoped behavior never ran");
        Assert.True(PlayingOnly.Ticks < 6, $"it ran on every frame ({PlayingOnly.Ticks}), so the state was ignored");
    }

    [Fact]
    public void StatesAreReportedAsAFiniteResource()
    {
        // The slots exist as Rust types, so there is a fixed number of them, and running out has
        // to say so rather than silently reusing one.
        Assert.True(StateRegistry.SlotCount >= 4);
    }

    [Fact]
    public void AStateRestrictionAndARunConditionMustBothPass()
    {
        // Two run conditions on one system, from two different attributes. They accumulate, so
        // neither can quietly discard the other: the state being right is not enough if the
        // other gate is shut.
        PlayingAndEnabled.Ticks = 0;
        PlayingAndEnabled.Enabled = false;

        using (var shut = new EngineHarness(frames: 4, discoverBehaviors: true))
        {
            shut.App.AddState(Screen.Playing);
            shut.Run();
        }

        Assert.Equal(0, PlayingAndEnabled.Ticks);

        PlayingAndEnabled.Enabled = true;

        using (var open = new EngineHarness(frames: 4, discoverBehaviors: true))
        {
            open.App.AddState(Screen.Playing);
            open.Run();
        }

        Assert.True(PlayingAndEnabled.Ticks > 0, "both gates were open and it still did not run");
    }

    [Fact]
    public void ANegativeMemberOfANarrowEnumSurvivesTheRoundTrip()
    {
        // A slot holds an int, and a byte-backed -1 has to arrive as -1 rather than as 255,
        // which is what reinterpreting the bytes rather than converting them would produce.
        using var harness = new EngineHarness(frames: 4);
        harness.App.AddState(Countdown.OnTime);

        var seen = Countdown.OnTime;
        var moved = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!moved)
            {
                ctx.SetState(Countdown.Late);
                moved = true;
                return;
            }

            seen = ctx.State<Countdown>();
        });

        harness.Run();

        Assert.Equal(Countdown.Late, seen);
    }

    [Fact]
    public void AScopedBehaviorInAnAppWithoutThatStateDoesNotRun()
    {
        // A state that was never added reads as "not in it". The alternative, throwing, happens
        // once per system per frame and buries the run in identical stack traces.
        PlayingOnly.Ticks = 0;

        using var harness = new EngineHarness(frames: 5, discoverBehaviors: true);
        harness.Run();

        Assert.Equal(0, PlayingOnly.Ticks);
    }

    [Fact]
    public void ATransitionRunsOnceRatherThanEveryFrame()
    {
        // The distinction that makes these worth having: a stage runs every frame the state is
        // held, an edge runs once as it changes. A screen is built by one and driven by the
        // other.
        ScreenLifecycle.Entered = 0;
        ScreenLifecycle.Exited = 0;
        ScreenLifecycle.Ticks = 0;

        using var harness = new EngineHarness(frames: 8, discoverBehaviors: true);
        harness.App.AddState(Screen.Menu);

        var frame = 0;

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;

            // Into the screen, then back out of it.
            if (frame == 2) ctx.SetState(Screen.Playing);
            if (frame == 5) ctx.SetState(Screen.Menu);
        });

        harness.Run();

        Assert.Equal(1, ScreenLifecycle.Entered);
        Assert.Equal(1, ScreenLifecycle.Exited);

        // The tick ran while the screen was held, which is more than once and fewer than every
        // frame of the run.
        Assert.True(ScreenLifecycle.Ticks > 1, $"the tick ran {ScreenLifecycle.Ticks} times");
        Assert.True(ScreenLifecycle.Ticks < 8, $"the tick ran {ScreenLifecycle.Ticks} times");
    }

    [Fact]
    public void ATransitionThatNeverHappensNeverRuns()
    {
        ScreenLifecycle.Entered = 0;
        ScreenLifecycle.Exited = 0;

        using var harness = new EngineHarness(frames: 5, discoverBehaviors: true);
        harness.App.AddState(Screen.Menu);
        harness.Run();

        Assert.Equal(0, ScreenLifecycle.Entered);
        Assert.Equal(0, ScreenLifecycle.Exited);
    }

    [Fact]
    public void ATransitionSystemCanBeRegisteredByHand()
    {
        // The route the attributes lower to, usable directly for a system that is not a behavior.
        using var harness = new EngineHarness(frames: 6);
        harness.App.AddState(Screen.Menu);

        var entered = 0;

        harness.App.AddStateSystem(Screen.Paused, entering: true, new SystemDescriptor(
            _ => entered++, "Test.OnEnterPaused"));

        var frame = 0;
        harness.OnContext(Stage.Update, ctx =>
        {
            if (++frame == 2) ctx.SetState(Screen.Paused);
        });

        harness.Run();

        Assert.Equal(1, entered);
    }
}
