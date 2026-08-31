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
}
