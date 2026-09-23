using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>Where a run is, for the computed states below.</summary>
public enum Stage3
{
    /// <summary>Nothing is going on.</summary>
    Menu,

    /// <summary>Playing.</summary>
    Playing,

    /// <summary>Playing, held.</summary>
    Paused,

    /// <summary>Watching rather than playing.</summary>
    Cutscene,
}

/// <summary>Whether the interface is up, which follows from where the run is.</summary>
[ComputedFrom(typeof(Stage3))]
public enum Hud
{
    /// <summary>Drawn.</summary>
    Shown,

    /// <summary>Drawn, with the pause overlay on it.</summary>
    Dimmed,
}

/// <summary>
/// Covers a state whose value is worked out from another rather than set.
/// </summary>
/// <remarks>
/// The thing a computed state prevents is two facts that have to agree. Whether the interface is
/// up follows from where the run is, and writing it as a plain state means every path that changes
/// one has to remember the other. This is that fact stated once.
/// </remarks>
[Collection("engine")]
public sealed class ComputedStateTests
{
    [Fact]
    public void ItFollowsItsSourceAndDisappearsWhereTheTableIsSilent()
    {
        var seen = new List<(ulong Frame, bool Exists, Hud Value)>();

        using var harness = new EngineHarness(frames: 10);

        harness.App.AddState(Stage3.Menu);
        harness.App.AddComputedState(
            (Stage3.Playing, Hud.Shown),
            (Stage3.Paused, Hud.Dimmed));

        harness.On(Stage.Update, world =>
        {
            switch (world.Resource<Time>().FrameCount)
            {
                case 1:
                    App.SetState(Stage3.Playing);
                    break;

                case 3:
                    App.SetState(Stage3.Paused);
                    break;

                case 5:
                    // Named by nothing in the table, so the state stops existing.
                    App.SetState(Stage3.Cutscene);
                    break;
            }

            seen.Add((
                world.Resource<Time>().FrameCount,
                App.TryState<Hud>(out var value),
                value));
        });

        harness.Run();

        // In the menu, which the table says nothing about.
        Assert.False(seen[0].Exists);

        var playing = seen.Single(step => step.Frame == 3);
        Assert.True(playing.Exists);
        Assert.Equal(Hud.Shown, playing.Value);

        var paused = seen.Single(step => step.Frame == 4);
        Assert.True(paused.Exists);
        Assert.Equal(Hud.Dimmed, paused.Value);

        // And the cutscene takes it away again, without anything having said so.
        Assert.False(seen.Single(step => step.Frame == 6).Exists);
    }

    /// <summary>Its edges run like any other state's.</summary>
    [Fact]
    public void ItsTransitionsRun()
    {
        var shown = 0;
        var hidden = 0;

        using var harness = new EngineHarness(frames: 8);

        harness.App.AddState(Stage3.Menu);
        harness.App.AddComputedState((Stage3.Playing, Hud.Shown));

        harness.App.AddStateSystem(
            Hud.Shown, entering: true, new SystemDescriptor(_ => shown++, "Test.Shown"));

        harness.App.AddStateSystem(
            Hud.Shown, entering: false, new SystemDescriptor(_ => hidden++, "Test.Hidden"));

        harness.On(Stage.Update, world =>
        {
            switch (world.Resource<Time>().FrameCount)
            {
                case 1:
                    App.SetState(Stage3.Playing);
                    break;

                case 4:
                    App.SetState(Stage3.Menu);
                    break;
            }
        });

        harness.Run();

        Assert.Equal(1, shown);
        Assert.Equal(1, hidden);
    }

    /// <summary>Setting one is refused, because there is nothing to set.</summary>
    [Fact]
    public void ItCannotBeSet()
    {
        using var harness = new EngineHarness(frames: 4);

        harness.App.AddState(Stage3.Menu);
        harness.App.AddComputedState((Stage3.Playing, Hud.Shown));

        harness.On(Stage.Update, world =>
        {
            if (world.Resource<Time>().FrameCount != 1) return;

            var refused = Assert.Throws<BevyNativeException>(() => App.SetState(Hud.Dimmed));
            Assert.Equal(NativeStatus.InvalidState, refused.Status);
        });

        harness.Run();
    }

    /// <summary>A table written in terms of the wrong state is refused before anything runs.</summary>
    /// <remarks>
    /// The attribute names the source and the table states the values, so the two can disagree.
    /// Saying so is worth more than computing a state from values it never sees.
    /// </remarks>
    [Fact]
    public void ATableForAnotherStateIsRefused()
    {
        using var harness = new EngineHarness(frames: 1);

        harness.App.AddState(Stage3.Menu);

        Assert.Throws<InvalidOperationException>(
            () => harness.App.AddComputedState((Session.Playing, Hud.Shown)));

        harness.Run();
    }
}
