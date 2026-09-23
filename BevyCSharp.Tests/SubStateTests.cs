using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>The state a game is in, for the tests below.</summary>
public enum Session
{
    /// <summary>Not playing.</summary>
    Menu,

    /// <summary>Playing.</summary>
    Playing,
}

/// <summary>A state that only means anything during a run.</summary>
[SubStateOf(typeof(Session), Session.Playing)]
public enum Paused
{
    /// <summary>The run is going.</summary>
    No,

    /// <summary>It is held.</summary>
    Yes,
}

/// <summary>A second state under the same parent, for the test that two can share one.</summary>
[SubStateOf(typeof(Session), Session.Playing)]
public enum Difficulty
{
    /// <summary>The usual.</summary>
    Normal,

    /// <summary>Harder.</summary>
    Brutal,
}

/// <summary>
/// Covers a state that exists only while another holds a value.
/// </summary>
/// <remarks>
/// The thing a sub-state is for is not being asked about when it does not apply. A pause outside a
/// run is not "off", it is nothing, and the difference shows the moment a system asks. A plain
/// state would answer with a value that means nothing, and this answers that there is no state.
/// </remarks>
[Collection("engine")]
public sealed class SubStateTests
{
    [Fact]
    public void ASubStateExistsOnlyWhileItsParentHoldsItsValue()
    {
        var existed = new List<(ulong Frame, bool Exists, Paused Value)>();

        using var harness = new EngineHarness(frames: 8);

        harness.App.AddState(Session.Menu);
        harness.App.AddSubState(Paused.No);

        harness.On(Stage.Update, world =>
        {
            var frame = world.Resource<Time>().FrameCount;

            // Into the run, then pause it, then out of the run again.
            if (frame == 1) App.SetState(Session.Playing);
            if (frame == 3) App.SetState(Paused.Yes);
            if (frame == 5) App.SetState(Session.Menu);

            existed.Add((frame, App.TryState<Paused>(out var value), value));
        });

        harness.Run();

        // Before the run starts there is no pause at all.
        Assert.False(existed[0].Exists);

        var duringRun = existed.Single(step => step.Frame == 3);
        Assert.True(duringRun.Exists);
        Assert.Equal(Paused.No, duringRun.Value);

        var afterPausing = existed.Single(step => step.Frame == 4);
        Assert.True(afterPausing.Exists);
        Assert.Equal(Paused.Yes, afterPausing.Value);

        // And leaving the run takes the pause with it, rather than leaving it held.
        Assert.False(existed.Single(step => step.Frame == 6).Exists);
    }

    /// <summary>Coming back into the parent value starts the sub-state over.</summary>
    /// <remarks>
    /// A pause left on when a run ended would otherwise be on when the next run began, which is
    /// the bug this shape of state exists to prevent.
    /// </remarks>
    [Fact]
    public void ItStartsAgainWhereItWasTold()
    {
        Paused? second = null;

        using var harness = new EngineHarness(frames: 10);

        harness.App.AddState(Session.Menu);
        harness.App.AddSubState(Paused.No);

        harness.On(Stage.Update, world =>
        {
            switch (world.Resource<Time>().FrameCount)
            {
                case 1:
                    App.SetState(Session.Playing);
                    break;

                case 3:
                    App.SetState(Paused.Yes);
                    break;

                case 5:
                    App.SetState(Session.Menu);
                    break;

                case 7:
                    App.SetState(Session.Playing);
                    break;

                case 9:
                    if (App.TryState<Paused>(out var value)) second = value;
                    break;
            }
        });

        harness.Run();

        Assert.Equal(Paused.No, second);
    }

    /// <summary>Its edges are the edges a plain state has.</summary>
    [Fact]
    public void ItsTransitionsRun()
    {
        var entered = 0;
        var left = 0;

        using var harness = new EngineHarness(frames: 8);

        harness.App.AddState(Session.Menu);
        harness.App.AddSubState(Paused.No);

        harness.App.AddStateSystem(
            Paused.Yes, entering: true, new SystemDescriptor(_ => entered++, "Test.Paused"));

        harness.App.AddStateSystem(
            Paused.Yes, entering: false, new SystemDescriptor(_ => left++, "Test.Unpaused"));

        harness.On(Stage.Update, world =>
        {
            switch (world.Resource<Time>().FrameCount)
            {
                case 1:
                    App.SetState(Session.Playing);
                    break;

                case 3:
                    App.SetState(Paused.Yes);
                    break;

                case 5:
                    App.SetState(Paused.No);
                    break;
            }
        });

        harness.Run();

        Assert.Equal(1, entered);
        Assert.Equal(1, left);
    }

    /// <summary>
    /// A behavior scoped to a sub-state runs while it holds that value and at no other time.
    /// </summary>
    /// <remarks>
    /// The attributes are what a game actually writes, so this is the shape that matters:
    /// <c>[InState]</c> and <c>[OnEnter]</c> know nothing about sub-states and should not have to,
    /// because which state a sub-state belongs to is written on the enum.
    /// </remarks>
    [Fact]
    public void ABehaviorScopedToASubStateRunsInsideItOnly()
    {
        Held.Ran = 0;
        Held.Entered = 0;

        using var harness = new EngineHarness(frames: 10, discoverBehaviors: true);

        harness.App.AddState(Session.Menu);
        harness.App.AddSubState(Paused.No);

        harness.On(Stage.Update, world =>
        {
            switch (world.Resource<Time>().FrameCount)
            {
                case 1:
                    App.SetState(Session.Playing);
                    break;

                case 3:
                    App.SetState(Paused.Yes);
                    break;

                case 6:
                    App.SetState(Paused.No);
                    break;
            }
        });

        harness.Run();

        Assert.Equal(1, Held.Entered);

        // Frames 4, 5 and 6, which is every frame the pause was held and none of the ones before
        // the run started or after it was let go.
        Assert.InRange(Held.Ran, 2, 4);
    }

    /// <summary>Two sub-states can hang from one parent, and each keeps its own value.</summary>
    /// <remarks>
    /// A run that can be paused and can be played at a difficulty wants both, and neither is a
    /// value of the other. What is checked is that the second one is not silently given the first
    /// one's slot, which would show as the two reading the same number.
    /// </remarks>
    [Fact]
    public void TwoSubStatesCanShareAParent()
    {
        var seen = new List<(bool Paused, Paused Pause, bool Hard, Difficulty Level)>();

        using var harness = new EngineHarness(frames: 8);

        harness.App.AddState(Session.Menu);
        harness.App.AddSubState(Paused.No);
        harness.App.AddSubState(Difficulty.Normal);

        harness.On(Stage.Update, world =>
        {
            var frame = world.Resource<Time>().FrameCount;

            if (frame == 1) App.SetState(Session.Playing);
            if (frame == 3) App.SetState(Difficulty.Brutal);
            if (frame == 5) App.SetState(Session.Menu);

            seen.Add((
                App.TryState<Paused>(out var pause),
                pause,
                App.TryState<Difficulty>(out var level),
                level));
        });

        harness.Run();

        // Outside the run neither exists, and inside it both do, each at its own value.
        Assert.All(seen, row => Assert.Equal(row.Paused, row.Hard));

        var during = seen.Where(row => row.Paused).ToList();
        Assert.NotEmpty(during);

        // The pause was never asked to change, so it stays where it started while the difficulty
        // moves, which is what two slots rather than one looks like.
        Assert.All(during, row => Assert.Equal(Paused.No, row.Pause));
        Assert.Contains(during, row => row.Level == Difficulty.Brutal);
    }

    /// <summary>A sub-state added before its parent has nothing to hang from.</summary>
    [Fact]
    public void ItRefusesToBeAddedBeforeItsParent()
    {
        using var harness = new EngineHarness(frames: 1);

        var refused = Assert.Throws<BevyNativeException>(
            () => harness.App.AddSubState(Paused.No));

        Assert.Equal(NativeStatus.InvalidState, refused.Status);

        harness.Run();
    }
}

/// <summary>A behavior that only runs while the run is paused.</summary>
[Behavior]
public partial struct Held
{
    /// <summary>How many frames it ran for.</summary>
    public static int Ran;

    /// <summary>How many times the pause was entered.</summary>
    public static int Entered;

    /// <summary>Counts the frames the pause is held.</summary>
    [OnUpdate]
    [InState(Paused.Yes)]
    public static void Tick(BehaviorContext ctx) => Ran++;

    /// <summary>Counts the transitions into it.</summary>
    [OnEnter(Paused.Yes)]
    public static void Enter(BehaviorContext ctx) => Entered++;
}
