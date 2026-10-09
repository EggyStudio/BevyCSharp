// Bevy's timers example, examples/time/timers.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Clocks;

// Illustrates timers, one on an entity that fires once after five seconds, and a twenty-second
// countdown reported every four until it is done.
internal static class Timers
{
    // Bevy's Countdown resource, its trigger every four seconds and its main timer of twenty.
    private static GameTimer _percentTrigger, _mainTimer;

    public static void Build(App app)
    {
        (_percentTrigger, _mainTimer) = (GameTimer.FromSeconds(4f, TimerMode.Repeating), GameTimer.FromSeconds(20f, TimerMode.Once));

        app.Startup(ctx => ctx.Ecs.Add(ctx.Ecs.Spawn(), new PrintOnCompletionTimer { Timer = GameTimer.FromSeconds(5f, TimerMode.Once) }), "timers.Setup");

        app.Update(ctx =>
        {
            _mainTimer.Tick(ctx.Time.Delta);
            if (!_percentTrigger.Tick(ctx.Time.Delta).JustFinished) return;

            if (!_mainTimer.Finished)
            {
                Console.WriteLine(FormattableString.Invariant($"Timer is {_mainTimer.Fraction * 100f:0}% complete!"));
            }
            else
            {
                _percentTrigger.Pause();
                Console.WriteLine("Paused percent trigger timer");
            }
        }, "timers.Countdown");
    }
}

/// <summary>A timer on an entity, which says so once it has run out.</summary>
[Behavior]
public partial struct PrintOnCompletionTimer
{
    /// <summary>The timer, five seconds once.</summary>
    public GameTimer Timer;

    /// <summary>Ticked by the frame's time, saying so on the frame it runs out, as Bevy's <c>print_when_completed</c> does.</summary>
    [OnUpdate]
    public void PrintWhenCompleted(BehaviorContext ctx)
    {
        if (Timer.Tick(ctx.Time.Delta).JustFinished) Console.WriteLine("Entity timer just finished");
    }
}
