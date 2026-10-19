using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a set clock held still, while an asset the app asked for is loading and until the hold
/// between commands lets it go, and the count of the frames it moved in.
/// </summary>
[Collection("engine")]
public sealed class ClockHoldTests
{
    /// <summary>
    /// Held for loads, a set clock stands still in each frame after one that found an asset
    /// loading, and moves a step in each frame after one that found none, so the game's time goes on
    /// when the load ends.
    /// </summary>
    /// <remarks>
    /// Bevy moves a load on before the frame's systems, so a system finds the load as the engine
    /// finds it before the next frame's clock moves. The image is asked for in the third frame's
    /// systems, since one this small asked for at startup has loaded before the first frame's
    /// systems run, and an asset is loading from the moment it is asked for, so the frame after
    /// stands still however fast the load is.
    /// </remarks>
    [Fact]
    public void AClockHeldForLoadsStandsStillUntilTheyEnd()
    {
        using var harness = new EngineHarness(frames: 30, frameSeconds: 0.1);
        var image = AssetHandle.None;
        var seen = new List<(double Elapsed, ulong Clock, bool Loading)>();

        harness.OnContext(Stage.Startup, ctx => ctx.Time.Holds = ClockHolds.Loads);
        harness.OnContext(Stage.Update, ctx =>
        {
            if (ctx.Time.FrameCount == 3) image = AssetServer.Load(AssetKind.Image, "textures/checker.png");
            var loading = image.IsValid && AssetServer.StateWithDependenciesOf(image) == AssetLoadState.Loading;
            seen.Add((ctx.Time.ElapsedSeconds, ctx.Time.ClockFrames, loading));
        });

        harness.Run();

        Assert.True(seen.Count > 5 && seen[3].Loading, "the image was not loading in the frame it was asked for");
        Assert.False(seen[^1].Loading, "the image never loaded");
        for (var i = 1; i < seen.Count; i++)
        {
            var moved = seen[i].Elapsed - seen[i - 1].Elapsed;
            Assert.Equal(seen[i - 1].Loading ? 0.0 : 0.1, moved, 9);
            Assert.Equal((ulong)Math.Round(seen[i].Elapsed / 0.1), seen[i].Clock);
        }
    }

    /// <summary>
    /// Held still, as the server holds it between commands, a set clock passes no time to the game
    /// and runs no fixed step until it is let go, and the frames it stood in are left out of its
    /// count.
    /// </summary>
    [Fact]
    public void AClockHeldStillPassesNoTimeUntilLetGo()
    {
        using var harness = new EngineHarness(frames: 12, frameSeconds: 0.1, fixedHz: 20);
        var seen = new List<(ulong Frame, double Elapsed, ulong Clock, int Steps)>();
        var steps = 0;

        harness.OnContext(Stage.Startup, ctx => ctx.Time.Holds = ClockHolds.Commands);
        harness.On(Stage.FixedUpdate, _ => steps++);
        harness.OnContext(Stage.Update, ctx =>
        {
            var frame = ctx.Time.FrameCount;
            seen.Add((frame, ctx.Time.ElapsedSeconds, ctx.Time.ClockFrames, steps));
            ctx.Time.HoldStill(frame is >= 3 and < 7);
        });

        harness.Run();

        // Held in the frames after the third to the sixth, so the fourth to the seventh stand still.
        Assert.True(seen.Count >= 10, $"only {seen.Count} frames ran");
        var still = 0ul;
        for (var i = 1; i < seen.Count; i++)
        {
            var held = seen[i].Frame is >= 4 and <= 7;
            if (held) still++;

            Assert.Equal(held ? 0.0 : 0.1, seen[i].Elapsed - seen[i - 1].Elapsed, 9);
            Assert.Equal(seen[i].Frame - still, seen[i].Clock);
            if (held) Assert.Equal(seen[i - 1].Steps, seen[i].Steps);
        }

        Assert.Equal(4ul, still);
    }

    /// <summary>
    /// The machine's clock is not held, since it reads the time since the last frame it counted
    /// and would hand the frame after a hold the whole of it, so every frame after the first moves
    /// and is counted.
    /// </summary>
    [Fact]
    public void TheMachinesClockIsNotHeld()
    {
        using var harness = new EngineHarness(frames: 6);
        var seen = new List<(ulong Frame, double Delta, ulong Clock)>();

        harness.OnContext(Stage.Startup, ctx => ctx.Time.Holds = ClockHolds.Loads | ClockHolds.Commands);
        harness.OnContext(Stage.Update, ctx =>
        {
            ctx.Time.HoldStill(true);
            seen.Add((ctx.Time.FrameCount, ctx.Time.RawDeltaSeconds, ctx.Time.ClockFrames));
        });

        harness.Run();

        Assert.True(seen.Count >= 4, $"only {seen.Count} frames ran");
        Assert.All(seen.Skip(1), frame =>
        {
            Assert.True(frame.Delta > 0, $"frame {frame.Frame} passed no time on the machine's clock");
            Assert.Equal(frame.Frame, frame.Clock);
        });
    }
}
