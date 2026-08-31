using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A behavior driven by the fixed timestep rather than by the frame.</summary>
[Behavior]
public partial struct FixedStepper
{
    /// <summary>How many fixed steps this entity has seen.</summary>
    public int Steps;

    /// <summary>Seconds accounted for, one fixed slice at a time.</summary>
    public float Simulated;

    [OnFixedUpdate]
    public void Step(BehaviorContext ctx)
    {
        Steps++;
        Simulated += ctx.Time.FixedDelta;
    }
}

/// <summary>
/// Covers Bevy's fixed timestep: a schedule that runs on simulated time rather than per frame.
/// </summary>
/// <remarks>
/// The assertions are about the relationship between fixed steps and frames rather than about
/// exact counts, because how many steps a run produces depends on how long the run really took.
/// The rates are chosen far enough from the frame rate that the relationship holds with room to
/// spare on a slow machine.
/// </remarks>
[Collection("engine")]
public sealed class FixedUpdateTests
{
    [Fact]
    public void FixedDeltaReportsTheConfiguredRate()
    {
        // A constant, not a reading: this is the slice each step covers, which is what makes a
        // fixed-step simulation reproduce itself on a different machine.
        using var harness = new EngineHarness(frames: 3, fixedHz: 50);
        var fixedDelta = 0f;

        harness.OnContext(Stage.Update, ctx => fixedDelta = ctx.Time.FixedDelta);
        harness.Run();

        Assert.Equal(0.02f, fixedDelta, 5);
    }

    [Fact]
    public void FixedUpdateRunsMoreOftenThanTheFrameWhenTheRateIsHigh()
    {
        using var harness = new EngineHarness(frames: 6, fps: 60, fixedHz: 500);
        var steps = 0;
        var frames = 0;

        harness.On(Stage.FixedUpdate, _ => steps++);
        harness.On(Stage.Update, _ => frames++);
        harness.Run();

        Assert.True(steps > frames, $"expected more fixed steps than frames, got {steps} vs {frames}");
    }

    [Fact]
    public void FixedUpdateRunsLessOftenThanTheFrameWhenTheRateIsLow()
    {
        // With the test above, this is the whole claim: the two are independent in both
        // directions, not merely at different rates.
        using var harness = new EngineHarness(frames: 6, fps: 60, fixedHz: 2);
        var steps = 0;
        var frames = 0;

        harness.On(Stage.FixedUpdate, _ => steps++);
        harness.On(Stage.Update, _ => frames++);
        harness.Run();

        Assert.True(steps < frames, $"expected fewer fixed steps than frames, got {steps} vs {frames}");
    }

    [Fact]
    public void SimulatedTimeNeverRunsAheadOfRealTime()
    {
        // What the fixed loop is actually doing: spending accumulated time, a slice at a time.
        // It may lag real time by up to one slice, but it must never invent time it has not been
        // given, which is the invariant a physics step depends on.
        using var harness = new EngineHarness(frames: 8, fps: 60, fixedHz: 120);
        var steps = 0;
        var spent = 0;
        var fixedDelta = 0.0;
        var elapsed = 0.0;

        harness.On(Stage.FixedUpdate, _ => steps++);

        // Both readings are taken at the top of the frame, before that frame's fixed steps have
        // run, so the steps counted are exactly those driven by time the snapshot already
        // includes. Reading them from different points in the frame would compare a step count
        // against a clock that does not cover it, which on a machine with uneven frame times is
        // a different answer every run.
        harness.OnContext(Stage.First, ctx =>
        {
            spent = steps;
            fixedDelta = ctx.Time.FixedDeltaSeconds;
            elapsed = ctx.Time.ElapsedSeconds;
        });
        harness.Run();

        Assert.True(steps > 0, "the fixed schedule never ran");

        // One slice of slack, because the loop spends whole slices and keeps the remainder.
        Assert.True(
            spent * fixedDelta <= elapsed + fixedDelta,
            $"{spent} steps of {fixedDelta}s exceeds the {elapsed}s the run had to spend");
    }

    [Fact]
    public void TheAttributeRoutesABehaviorOntoTheFixedSchedule()
    {
        using var harness = new EngineHarness(frames: 6, discoverBehaviors: true, fps: 60, fixedHz: 500);
        var steps = 0;
        var simulated = 0f;

        harness.OnContext(Stage.Startup, ctx => ctx.Ecs.Add(ctx.Ecs.Spawn(), new FixedStepper()));

        harness.OnContext(Stage.Last, ctx =>
        {
            foreach (var row in ctx.Ecs.Query<FixedStepper>(markChanged: false))
            {
                steps = row.Component.Steps;
                simulated = row.Component.Simulated;
            }
        });

        harness.Run();

        Assert.True(steps > 0, "the behavior's fixed method never ran");

        Assert.Equal(steps * (1f / 500f), simulated, 4);
    }
}
