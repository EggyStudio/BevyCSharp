using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers commands queued with a delay, Bevy's <c>commands.delayed()</c>, landing once their time has passed.</summary>
/// <remarks>
/// Each frame here is a tenth of a second of the app's time, so a delay is a count of frames. A delay
/// starts as the queue it was made on is applied, at the end of the frame's systems, and a queue whose
/// time has come lands the next time the queues are applied.
/// </remarks>
[Collection("engine")]
public sealed class DelayedCommandTests
{
    /// <summary>A delayed queue lands once its time has passed, a delay of nothing the next frame, and one a landed queue made lands after its own.</summary>
    [Fact]
    public void DelayedQueuesLandOnceTheirTimeHasPassed()
    {
        var landed = new List<(string What, int Frame)>();
        var frame = 0;

        using var harness = new EngineHarness(frames: 12, frameSeconds: 0.1);
        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (frame != 1) return;

            ctx.Cmd.Delayed(0f).Run(_ => landed.Add(("now", frame)));
            ctx.Cmd.Delayed(0.25f).Run(_ => landed.Add(("later", frame)));
            ctx.Cmd.Delayed(0.25f).Run(_ => ctx.Cmd.Delayed(0.25f).Run(_ => landed.Add(("after later", frame))));
        });

        harness.Run();

        Assert.Equal([("now", 2), ("later", 4), ("after later", 7)], landed);
    }

    /// <summary>A negative delay is refused.</summary>
    [Fact]
    public void ANegativeDelayIsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new EcsCommands().Delayed(-1f));
}
