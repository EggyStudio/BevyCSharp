using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a clock that advances a set amount a frame, in place of the machine's.
/// </summary>
/// <remarks>
/// None of these pace their frames, so a frame takes the machine a fraction of a millisecond, and
/// every number read is the set clock's and not the machine's.
/// </remarks>
[Collection("engine")]
public sealed class ClockTests
{
    /// <summary>
    /// Sixty frames of a sixtieth come to a second exactly, each frame reading a sixtieth, and the
    /// fixed steps spend that second as Bevy's sixty-four a second do.
    /// </summary>
    [Fact]
    public void SixtyFramesOfASixtiethAreASecondHoweverLongTheyTook()
    {
        var steps = 0;
        var frame = 0;
        var elapsed = 0.0;
        var deltas = new List<double>();
        var set = 0.0;

        // The first frame reads no time gone, as Bevy's always does, so sixty-one frames.
        using var harness = new EngineHarness(frames: 61, frameSeconds: 1.0 / 60.0);
        harness.On(Stage.FixedUpdate, _ => steps++);
        harness.OnContext(Stage.Update, ctx =>
        {
            if (frame++ > 0) deltas.Add(ctx.Time.DeltaSeconds);
            elapsed = ctx.Time.ElapsedSeconds;
            set = ctx.Time.FrameSeconds;
        });

        harness.Run();

        Assert.Equal(1.0, elapsed, 6);
        Assert.All(deltas, delta => Assert.Equal(1.0 / 60.0, delta, 9));
        Assert.Equal(64, steps);
        Assert.Equal(1.0 / 60.0, set, 9);
    }

    /// <summary>A frame set longer for one frame is that long, and the next is back to the set length.</summary>
    [Fact]
    public void AFrameMadeSlowOnPurposeIsAsSlowAsItWasMade()
    {
        var frame = 0;
        var deltas = new Dictionary<int, double>();

        using var harness = new EngineHarness(frames: 8, frameSeconds: 1.0 / 60.0);
        harness.OnContext(Stage.Update, ctx =>
        {
            deltas[frame] = ctx.Time.DeltaSeconds;

            // Asked in one frame's update, so the next frame is the slow one.
            if (frame == 3) ctx.Time.FrameSeconds = 0.1;
            if (frame == 4) ctx.Time.FrameSeconds = 1.0 / 60.0;
            frame++;
        });

        harness.Run();

        Assert.Equal(1.0 / 60.0, deltas[3], 9);
        Assert.Equal(0.1, deltas[4], 9);
        Assert.Equal(1.0 / 60.0, deltas[5], 9);
    }

    /// <summary>
    /// The machine's clock let run again goes on from where the set clock had reached, rather than
    /// standing until the machine catches up with frames that ran ahead of it.
    /// </summary>
    [Fact]
    public void TheMachinesClockLetRunAgainGoesOnFromWhereTheSetClockWas()
    {
        var frame = 0;
        var atRelease = 0.0;
        var after = new List<double>();
        var final = 0.0;

        // A hundred and twenty frames of a sixtieth are two seconds of the game, which unpaced take
        // the machine far less, so the set clock is well ahead of the machine's when it is let go.
        using var harness = new EngineHarness(frames: 140, frameSeconds: 1.0 / 60.0);
        harness.OnContext(Stage.Update, ctx =>
        {
            if (frame == 120)
            {
                atRelease = ctx.Time.ElapsedSeconds;
                ctx.Time.FrameSeconds = 0;
            }
            else if (frame > 120)
            {
                after.Add(ctx.Time.DeltaSeconds);
                final = ctx.Time.ElapsedSeconds;
            }

            frame++;
        });

        harness.Run();

        Assert.True(atRelease > 1.9, $"the set clock had reached only {atRelease} seconds");
        Assert.True(after.Count(delta => delta > 0) > after.Count / 2, $"the machine's clock read no time in most frames after: {string.Join(", ", after)}");
        Assert.All(after, delta => Assert.True(delta < 0.25, $"a frame after read {delta} seconds"));
        Assert.True(final >= atRelease, $"the clock went back from {atRelease} to {final}");
    }

    /// <summary>The command sets the clock, says what it is, and refuses what is no length of time.</summary>
    [Fact]
    public void TheCommandSetsTheClockAndLetsItGo()
    {
        var answers = new List<string>();
        var frame = 0;
        var deltas = new Dictionary<int, double>();

        using var harness = new EngineHarness(frames: 6);
        harness.On(Stage.Update, world =>
        {
            deltas[frame] = world.Resource<Time>().DeltaSeconds;
            using (ConsoleHost.Lend(world))
            {
                if (frame == 1)
                {
                    answers.Add(ConsoleCommands.Run("app.frametime 0.025"));
                    answers.Add(ConsoleCommands.Run("app.frametime"));
                    answers.Add(ConsoleCommands.Run("app.frametime -1"));
                }

                if (frame == 3) answers.Add(ConsoleCommands.Run("app.frametime off"));
            }

            frame++;
        });

        harness.Run();

        Assert.Equal(["each frame is 0.025 seconds", "each frame is 0.025 seconds", "'-1' is not a length of time", "the machine's clock"], answers);
        Assert.Equal(0.025, deltas[2], 9);
        Assert.Equal(0.025, deltas[3], 9);
    }
}
