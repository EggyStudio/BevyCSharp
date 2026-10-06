using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>Which floor of a tower the run is on, for the joint states below.</summary>
public enum Floor
{
    /// <summary>Any floor but the top.</summary>
    Lower,

    /// <summary>The top, where the boss is.</summary>
    Top,
}

/// <summary>Whether play is held, for the joint states below.</summary>
public enum Hold
{
    /// <summary>Playing.</summary>
    Off,

    /// <summary>Held.</summary>
    On,
}

/// <summary>Whether the run is being watched rather than played, for a joint of three.</summary>
public enum Watch
{
    /// <summary>Played.</summary>
    Playing,

    /// <summary>Watched.</summary>
    Watching,
}

/// <summary>What plays, which follows from the floor and whether play is held, together.</summary>
[ComputedFrom(typeof(Floor), typeof(Hold))]
public enum Theme
{
    /// <summary>Below the top, playing.</summary>
    Calm,

    /// <summary>At the top, playing.</summary>
    Boss,

    /// <summary>Held, wherever the run is.</summary>
    Quiet,
}

/// <summary>Whether the interface shows, which follows from three states at once.</summary>
[ComputedFrom(typeof(Floor), typeof(Hold), typeof(Watch))]
public enum Overlay
{
    /// <summary>Shown.</summary>
    Shown,
}

/// <summary>Whether the run is at the top, computed from the floor by a rule, for a joint to read.</summary>
[ComputedFrom(typeof(Floor))]
public enum AtTop
{
    /// <summary>At the top.</summary>
    Present,
}

/// <summary>Whether play is held, computed from the hold by a table, for a joint to read.</summary>
[ComputedFrom(typeof(Hold))]
public enum Holding
{
    /// <summary>Held.</summary>
    Yes,
}

/// <summary>What the top shows, worked out from a computed state and a state.</summary>
[ComputedFrom(typeof(AtTop), typeof(Hold))]
public enum Banner
{
    /// <summary>At the top, playing.</summary>
    Boss,

    /// <summary>At the top, held.</summary>
    Held,
}

/// <summary>Where a held run is, worked out from a state and a computed state.</summary>
[ComputedFrom(typeof(Floor), typeof(Holding))]
public enum Hint
{
    /// <summary>Held below the top.</summary>
    Below,

    /// <summary>Held at the top.</summary>
    AtTheTop,
}

/// <summary>The boss fight, which exists only at the top while play is not held.</summary>
[SubStateOf(typeof(Floor), Floor.Top)]
[SubStateOf(typeof(Hold), Hold.Off)]
public enum Fight
{
    /// <summary>The boss has not moved yet.</summary>
    Waiting,

    /// <summary>The boss is fighting.</summary>
    Fighting,
}

/// <summary>
/// Covers a state worked out from several others at once by a rule of the game's own, and a
/// sub-state living inside several states at once.
/// </summary>
/// <remarks>
/// A fact that follows from two facts is the case a computed state over one cannot state, so a
/// game either keeps a third state in step by hand or reads both every frame. These are that fact
/// stated once, over two states and over three.
/// </remarks>
[Collection("engine")]
public sealed class JointStateTests
{
    /// <summary>
    /// It follows both sources, enters a value once however often an unrelated change works it out
    /// again, and refuses to be set.
    /// </summary>
    [Fact]
    public void ItFollowsBothOfItsSourcesAndEntersAValueOnce()
    {
        var seen = new Dictionary<ulong, Theme?>();
        var bossEntered = 0;
        var refused = 0;

        using var harness = new EngineHarness(frames: 12);

        harness.App.AddState(Floor.Lower);
        harness.App.AddState(Hold.Off);
        harness.App.AddComputedState<Theme, Floor, Hold>((floor, hold) =>
            hold == Hold.On ? Theme.Quiet : floor == Floor.Top ? Theme.Boss : Theme.Calm);

        harness.App.AddStateSystem(Theme.Boss, entering: true, new SystemDescriptor(_ => bossEntered++, "Test.OnEnterBoss"));

        harness.On(Stage.Update, world =>
        {
            var frame = world.Resource<Time>().FrameCount;

            switch (frame)
            {
                case 2:
                    App.SetState(Floor.Top);
                    break;

                case 4:
                    App.SetState(Hold.On);
                    break;

                case 6:
                    App.SetState(Hold.Off);
                    break;

                case 8:
                    // The same floor again, which works the theme out again to the same value.
                    App.SetState(Floor.Top);

                    try
                    {
                        App.SetState(Theme.Calm);
                    }
                    catch (BevyNativeException)
                    {
                        refused++;
                    }

                    break;
            }

            seen[frame] = App.TryState<Theme>(out var theme) ? theme : null;
        });

        harness.Run();

        Assert.Equal(Theme.Calm, seen[1]);
        Assert.Equal(Theme.Boss, seen[3]);
        Assert.Equal(Theme.Quiet, seen[5]);
        Assert.Equal(Theme.Boss, seen[7]);
        Assert.Equal(Theme.Boss, seen[10]);

        // Entered at the top and again after the hold, and not when the same floor was set again.
        Assert.Equal(2, bossEntered);
        Assert.Equal(1, refused);
    }

    /// <summary>
    /// Over three sources, it exists only while every one of them holds a state, and a rule that
    /// answers nothing leaves it absent.
    /// </summary>
    [Fact]
    public void OverThreeItExistsWhileEverySourceDoesAndTheRuleSaysSo()
    {
        var seen = new Dictionary<ulong, bool>();

        using var harness = new EngineHarness(frames: 8);

        harness.App.AddState(Floor.Lower);
        harness.App.AddState(Hold.Off);
        harness.App.AddState(Watch.Playing);
        harness.App.AddComputedState<Overlay, Floor, Hold, Watch>((floor, hold, watch) =>
            watch == Watch.Watching || hold == Hold.On ? null : Overlay.Shown);

        harness.On(Stage.Update, world =>
        {
            var frame = world.Resource<Time>().FrameCount;

            if (frame == 2) App.SetState(Watch.Watching);
            if (frame == 4) App.SetState(Watch.Playing);

            seen[frame] = App.TryState<Overlay>(out _);
        });

        harness.Run();

        Assert.True(seen[1]);
        Assert.False(seen[3]);
        Assert.True(seen[5]);
    }

    /// <summary>
    /// A joint reads a state computed from another through that state's value, by a rule and by a
    /// table alike, and does not exist while the computed one does not.
    /// </summary>
    [Fact]
    public void AJointReadsAComputedStateThroughItsSource()
    {
        var seen = new Dictionary<ulong, (Banner?, Hint?)>();

        using var harness = new EngineHarness(frames: 10);

        harness.App.AddState(Floor.Lower);
        harness.App.AddState(Hold.Off);
        harness.App.AddComputedState<AtTop, Floor>(floor => floor == Floor.Top ? AtTop.Present : null);
        harness.App.AddComputedState((Hold.On, Holding.Yes));
        harness.App.AddComputedState<Banner, AtTop, Hold>((_, hold) => hold == Hold.On ? Banner.Held : Banner.Boss);
        harness.App.AddComputedState<Hint, Floor, Holding>((floor, _) => floor == Floor.Top ? Hint.AtTheTop : Hint.Below);

        harness.On(Stage.Update, world =>
        {
            var frame = world.Resource<Time>().FrameCount;

            if (frame == 2) App.SetState(Floor.Top);
            if (frame == 4) App.SetState(Hold.On);
            if (frame == 6) App.SetState(Floor.Lower);

            seen[frame] = (App.TryState<Banner>(out var banner) ? banner : null, App.TryState<Hint>(out var hint) ? hint : null);
        });

        harness.Run();

        Assert.Equal((null, null), seen[1]);
        Assert.Equal((Banner.Boss, null), seen[3]);
        Assert.Equal((Banner.Held, Hint.AtTheTop), seen[5]);
        Assert.Equal((null, Hint.Below), seen[7]);
    }

    /// <summary>A joint reading a computed state not yet added is refused before it runs, since there is nothing to read it through.</summary>
    [Fact]
    public void AJointReadingAComputedStateNeedsItAddedFirst()
    {
        using var harness = new EngineHarness(frames: 1);

        harness.App.AddState(Floor.Lower);
        harness.App.AddState(Hold.Off);

        Assert.Throws<InvalidOperationException>(() =>
            harness.App.AddComputedState<Banner, AtTop, Hold>((_, _) => Banner.Boss));
    }

    /// <summary>A rule written over other states than the enum names is refused before it runs.</summary>
    [Fact]
    public void ARuleOverOtherStatesThanTheEnumNamesIsRefused()
    {
        using var harness = new EngineHarness(frames: 1);

        harness.App.AddState(Floor.Lower);
        harness.App.AddState(Hold.Off);

        Assert.Throws<InvalidOperationException>(() =>
            harness.App.AddComputedState<Theme, Hold, Floor>((hold, floor) => Theme.Calm));

        Assert.Throws<InvalidOperationException>(() =>
            harness.App.AddComputedState<Theme, Floor>(floor => Theme.Calm));
    }

    /// <summary>
    /// A sub-state of two states exists only while both hold their values, is set like any other
    /// while it does, keeps its value through a change to a state it does not name, and starts
    /// over each time it comes back.
    /// </summary>
    [Fact]
    public void ASubStateOfTwoLivesWhileBothHoldTheirValues()
    {
        var seen = new Dictionary<ulong, Fight?>();

        using var harness = new EngineHarness(frames: 14);

        harness.App.AddState(Floor.Lower);
        harness.App.AddState(Hold.Off);
        harness.App.AddState(Watch.Playing);
        harness.App.AddSubState(Fight.Waiting);

        harness.On(Stage.Update, world =>
        {
            var frame = world.Resource<Time>().FrameCount;

            switch (frame)
            {
                case 2:
                    App.SetState(Floor.Top);
                    break;

                case 4:
                    App.SetState(Fight.Fighting);
                    break;

                case 6:
                    // Named by nothing the fight lives inside, so it stays as it was.
                    App.SetState(Watch.Watching);
                    break;

                case 8:
                    App.SetState(Hold.On);
                    break;

                case 10:
                    App.SetState(Hold.Off);
                    break;
            }

            seen[frame] = App.TryState<Fight>(out var fight) ? fight : null;
        });

        harness.Run();

        Assert.Null(seen[1]);
        Assert.Equal(Fight.Waiting, seen[3]);
        Assert.Equal(Fight.Fighting, seen[5]);
        Assert.Equal(Fight.Fighting, seen[7]);
        Assert.Null(seen[9]);
        Assert.Equal(Fight.Waiting, seen[11]);
    }

    /// <summary>A sub-state of two is refused while one of the states it lives inside is not added.</summary>
    [Fact]
    public void ASubStateOfTwoNeedsBothAdded()
    {
        using var harness = new EngineHarness(frames: 1);

        harness.App.AddState(Floor.Lower);

        Assert.Throws<BevyNativeException>(() => harness.App.AddSubState(Fight.Waiting));
    }
}
