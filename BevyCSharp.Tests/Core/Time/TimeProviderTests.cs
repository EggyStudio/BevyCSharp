using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the game's clock as a <see cref="TimeProvider"/>, its waits ending in the frame whose
/// clock reaches them on a set clock, its periodic timers, and its readings of the time.
/// </summary>
[Collection("engine")]
public sealed class TimeProviderTests
{
    /// <summary>
    /// A wait of half a second on a clock of a tenth of a second a frame ends in the frame whose
    /// clock reaches it, before that frame's systems, and not in the one before, so a task's wait
    /// ends in the same frame on any device.
    /// </summary>
    /// <remarks>
    /// The wait is a timer that fires once, which is what .NET's delay asks a provider for, ending
    /// a task as the delay's does, so a task awaiting it goes on as one awaiting the delay would.
    /// </remarks>
    [Fact]
    public void AWaitEndsInTheFrameWhoseClockReachesIt()
    {
        using var harness = new EngineHarness(frames: 12, frameSeconds: 0.1);
        var wait = new TaskCompletionSource();
        Task? after = null;
        var seen = new List<(double Elapsed, bool Done, bool After)>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            ctx.Time.Provider.CreateTimer(_ => wait.TrySetResult(), null, TimeSpan.FromSeconds(0.5), Timeout.InfiniteTimeSpan);
            after = AwaitThen(wait.Task);
        });
        harness.OnContext(Stage.Update, ctx => seen.Add((ctx.Time.ElapsedSeconds, wait.Task.IsCompleted, after!.IsCompleted)));

        harness.Run();

        // The wait and what awaited it are over in the frame the clock reaches half a second, and
        // neither before it.
        Assert.NotEmpty(seen);
        foreach (var (elapsed, done, then) in seen)
        {
            Assert.True(done == elapsed >= 0.5 - 1e-6, $"at {elapsed} seconds the wait was {(done ? "over" : "still waiting")}");
            Assert.True(then == done, $"at {elapsed} seconds what awaited the wait was {(then ? "done" : "not done")}");
        }
        Assert.Contains(seen, frame => frame.Done);

        static async Task AwaitThen(Task wait) => await wait;
    }

    /// <summary>
    /// A periodic timer fires each period of the game's clock, once a frame at most, a change puts
    /// its next firing off, and once disposed it fires no more.
    /// </summary>
    [Fact]
    public void APeriodicTimerFiresEachPeriodUntilItIsDisposed()
    {
        using var harness = new EngineHarness(frames: 20, frameSeconds: 0.1);
        ITimer? timer = null;
        var fired = new List<double>();
        var frames = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var time = ctx.Time;
            timer = ctx.Time.Provider.CreateTimer(_ => fired.Add(time.ElapsedSeconds), null, TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.3));
        });
        harness.OnContext(Stage.Update, _ =>
        {
            frames++;
            if (frames == 10) timer!.Change(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.3));
            if (frames == 16) timer!.Dispose();
        });

        harness.Run();

        // Every 0.3 seconds from 0.2 until the change in the tenth frame, then 0.5 seconds after
        // the change, and nothing after the sixteenth frame.
        Assert.True(fired.Count >= 4, $"the timer fired at {string.Join(", ", fired)}");
        for (var i = 1; i < fired.Count; i++)
            Assert.True(fired[i] - fired[i - 1] >= 0.3 - 1e-6, $"the timer fired at {string.Join(", ", fired)}");
        Assert.All(fired, at => Assert.True(at <= 1.6 + 1e-6, $"the timer fired at {at} seconds after it was disposed"));
    }

    /// <summary>
    /// The provider reads the game's time, its timestamps a tick a ten-millionth of a second and its
    /// moment the app's start and the game's time since, so a span measured with it is the game's.
    /// </summary>
    [Fact]
    public void ItReadsTheGamesTime()
    {
        using var harness = new EngineHarness(frames: 8, frameSeconds: 0.25);
        long first = 0, frequency = 0;
        DateTimeOffset firstMoment = default;
        TimeSpan measured = default, moved = default;
        double elapsedAtFirst = 0, elapsedAtLast = 0;
        var frames = 0;

        harness.OnContext(Stage.Update, ctx =>
        {
            var provider = ctx.Time.Provider;
            frames++;
            if (frames == 2)
            {
                (first, firstMoment, elapsedAtFirst, frequency) = (provider.GetTimestamp(), provider.GetUtcNow(), ctx.Time.ElapsedSeconds, provider.TimestampFrequency);
            }
            else if (frames == 6)
            {
                (measured, moved, elapsedAtLast) = (provider.GetElapsedTime(first), provider.GetUtcNow() - firstMoment, ctx.Time.ElapsedSeconds);
            }
        });

        harness.Run();

        Assert.Equal(TimeSpan.TicksPerSecond, frequency);
        Assert.Equal(elapsedAtLast - elapsedAtFirst, measured.TotalSeconds, 6);
        Assert.Equal(elapsedAtLast - elapsedAtFirst, moved.TotalSeconds, 6);
        Assert.True(measured.TotalSeconds >= 0.9, $"four frames of a quarter of a second measured {measured.TotalSeconds} seconds");
    }
}
