// Bevy's logs example, examples/app/logs.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Application;

// This example illustrates how to use logs in Bevy. A line is written at each of Bevy's five levels
// every frame, the trace and debug ones left out by Bevy's default filter, a line at each level once,
// and a costly sum worked out once. P ends the run. Bevy's panics there, and a C# system that throws
// is logged and the app runs on, so this one ends the run with Rust's panic code, 101, which is how a
// C# game says it could not go on.
internal static class Logs
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Render2d.SpawnCamera2d();
            Ui.SpawnText("Press P to panic", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        }, "logs.Setup");

        app.Update(LogSystem, "logs.LogSystem");
        app.Update(LogOnceSystem, "logs.LogOnceSystem");
        app.Update(PanicOnP, "logs.PanicOnP");
    }

    private static void PanicOnP(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.P)) return;
        Log.Error("P pressed, panicking");
        ctx.Exit(101);
    }

    // Bevy's log_system, a line at each level from the least important to the most. Trace and debug
    // are left out unless the filter asks for them, as RUST_LOG=trace or RUST_LOG=csharp=debug does.
    private static void LogSystem(BehaviorContext ctx)
    {
        Log.Trace("very noisy");
        Log.Debug("helpful for debugging");
        Log.Info("helpful information that is worth printing by default");
        Log.Warn("something bad happened that isn't a failure, but thats worth calling out");
        Log.Error("something failed");
    }

    // Bevy's log_once_system, the Once forms for a system that runs every frame and has something to
    // say a single time.
    private static void LogOnceSystem(BehaviorContext ctx)
    {
        Log.TraceOnce("one time noisy message");
        Log.DebugOnce("one time debug message");
        Log.InfoOnce("some info which is printed only once");
        Log.WarnOnce("some warning we wish to call out only once");
        Log.ErrorOnce("some error we wish to report only once");

        // Once a line of code, so the loop writes its first pass alone.
        for (var i = 0; i < 10; i++) Log.InfoOnce($"logs once per call site, so this works just fine: {i}");

        // Once for anything, here something costly a running system needs a single time.
        Log.Once(() =>
        {
            Log.Info("doing expensive things");
            ulong a = 0;
            for (ulong i = 0; i < 100_000_000; i++) a += i;
            Log.Info($"result of some expensive one time calculation: {a}");
        });
    }
}
