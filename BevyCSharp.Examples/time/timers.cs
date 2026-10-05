// Bevy's timers example, examples/time/timers.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Clocks;

// Illustrates timers, one on an entity that fires once after five seconds, and a twenty-second
// countdown reported every four until it is done.
internal static class Timers
{
    // Bevy's Timer, as much of it as the example uses: a duration, whether it repeats, and the
    // time run toward it.
    private sealed class Timer(float seconds, bool repeating)
    {
        public float Elapsed { get; private set; }
        public bool Finished { get; private set; }
        public bool JustFinished { get; private set; }
        public bool Paused { get; set; }
        public float Fraction => Math.Min(Elapsed / seconds, 1f);

        public Timer Tick(float delta)
        {
            JustFinished = false;
            if (Paused || (Finished && !repeating)) return this;

            Elapsed += delta;
            if (Elapsed < seconds) return this;

            (Finished, JustFinished) = (true, true);
            if (repeating) Elapsed -= seconds;
            else Elapsed = seconds;
            return this;
        }
    }

    private static Timer _onCompletion = new(5f, false);
    private static Timer _percentTrigger = new(4f, true);
    private static Timer _main = new(20f, false);

    public static void Build(App app)
    {
        (_onCompletion, _percentTrigger, _main) = (new Timer(5f, false), new Timer(4f, true), new Timer(20f, false));

        app.Update(ctx =>
        {
            _main.Tick(ctx.Time.Delta);
            if (!_percentTrigger.Tick(ctx.Time.Delta).JustFinished) return;

            if (!_main.Finished)
            {
                Console.WriteLine(FormattableString.Invariant($"Timer is {_main.Fraction * 100f:0}% complete!"));
            }
            else
            {
                _percentTrigger.Paused = true;
                Console.WriteLine("Paused percent trigger timer");
            }
        }, "timers.Countdown");

        app.Update(ctx =>
        {
            if (_onCompletion.Tick(ctx.Time.Delta).JustFinished) Console.WriteLine("Entity timer just finished");
        }, "timers.PrintWhenCompleted");
    }
}
