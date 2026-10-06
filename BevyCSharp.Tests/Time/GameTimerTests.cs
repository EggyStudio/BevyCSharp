using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A behavior's timer, ticked by the frame's time on the entity it belongs to.</summary>
[Behavior]
public partial struct TickedTimer
{
    /// <summary>The timer, which the method ticks in place.</summary>
    public GameTimer Timer;

    /// <summary>How many times it has run out.</summary>
    public int Finished;

    /// <summary>Ticked each frame, counting each time it runs out.</summary>
    [OnUpdate]
    public void Tick(BehaviorContext ctx) => Finished += Timer.Tick(ctx.Time.Delta).TimesFinishedThisTick;
}

/// <summary>Bevy's timer as a value, ticked as Bevy ticks its own.</summary>
[Collection("engine")]
public sealed class GameTimerTests
{
    [Fact]
    public void OnceRunsOutOnceAndStaysAtItsDuration()
    {
        var timer = GameTimer.FromSeconds(1f, TimerMode.Once);

        Assert.False(timer.Tick(0.6f).JustFinished);
        Assert.Equal(0.6f, timer.Fraction, 5);
        Assert.Equal(0.4f, timer.Remaining, 5);

        Assert.True(timer.Tick(0.6f).JustFinished);
        Assert.True(timer.Finished);
        Assert.Equal(1f, timer.Elapsed);

        Assert.False(timer.Tick(5f).JustFinished);
        Assert.True(timer.Finished);
        Assert.Equal(1f, timer.Fraction);
    }

    [Fact]
    public void RepeatingCountsEveryTimeATickRanPastAndKeepsTheRest()
    {
        var timer = GameTimer.FromSeconds(0.25f, TimerMode.Repeating);

        Assert.Equal(2, timer.Tick(0.6f).TimesFinishedThisTick);
        Assert.Equal(0.1f, timer.Elapsed, 5);
        Assert.True(timer.Finished);

        Assert.False(timer.Tick(0.1f).JustFinished);
        Assert.False(timer.Finished, "a repeating timer is finished on the tick it ran out in alone");
    }

    [Fact]
    public void APausedTimerDoesNotMoveAndAResetOneStartsAgain()
    {
        var timer = GameTimer.FromSeconds(1f, TimerMode.Repeating);
        timer.Tick(0.5f);
        timer.Pause();
        Assert.False(timer.Tick(10f).JustFinished);
        Assert.Equal(0.5f, timer.Elapsed);

        timer.Unpause();
        Assert.True(timer.Tick(0.5f).JustFinished);

        timer.Reset();
        Assert.Equal(0f, timer.Elapsed);
        Assert.False(timer.Finished);
        Assert.Equal(1f, timer.Duration);
    }

    [Fact]
    public void ABehaviorKeepsItsTimerOnItsEntityFromFrameToFrame()
    {
        using var harness = new EngineHarness(frames: 30, discoverBehaviors: true, fps: 60);
        var entity = Entity.None;
        TickedTimer after = default;
        harness.OnContext(Stage.Startup, ctx =>
        {
            entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new TickedTimer { Timer = GameTimer.FromSeconds(0.05f, TimerMode.Repeating) });
        });
        harness.OnContext(Stage.Last, ctx => after = ctx.Ecs.GetOrDefault<TickedTimer>(entity));
        harness.Run();

        Assert.True(after.Finished > 0, "the timer ran out on its entity at least once");
        Assert.InRange(after.Timer.Elapsed, 0f, 0.05f);
        Assert.Equal(0.05f, after.Timer.Duration);
    }
}
