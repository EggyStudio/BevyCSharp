using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.StressTests;

// What Bevy's stress tests have in common. Each opens a window of 1920 by 1080 at a scale factor of
// one with no vertical sync, so a frame runs as fast as it can, and most warn that they are stress
// tests and log how fast their frames run once a second, as Bevy's FrameTimeDiagnosticsPlugin
// measures and its LogDiagnosticsPlugin writes.
internal static class StressTest
{
    // Bevy's frame time diagnostics keep the last 120 frames and smooth the latest by an average
    // that gives each new frame two parts in 21.
    private const int History = 120;
    private const double Smoothing = 2.0 / 21.0;

    private static readonly Queue<(double Fps, double Milliseconds)> Frames = new();
    private static double _smoothedFps, _smoothedMilliseconds, _sinceLog;
    private static ulong _count;

    // The window Bevy's stress tests ask for, unpaced. An offscreen run draws an image of the same
    // size, as fast as it can.
    public static void Configure(Config config)
    {
        (config.Width, config.Height) = (1920, 1080);
        config.Vsync = false;
        config.HeadlessFps = 0;
    }

    // The window's scale factor held at one, so the window is 1920 by 1080 pixels on a display that
    // scales, and the log once a second where the test logs, as most of Bevy's do.
    public static void Add(App app, bool log = true)
    {
        Frames.Clear();
        (_smoothedFps, _smoothedMilliseconds, _sinceLog, _count) = (0, 0, 0, 0);

        app.Startup(ctx =>
        {
            var window = Window.Entity();
            if (window != Entity.None) ctx.Ecs.Wrap<WindowRef>(window).ResolutionScaleFactorOverride = 1f;
        }, "stress_test.ScaleFactor");

        if (log) app.Update(Log, "stress_test.Log");
    }

    // Bevy's warning_string.txt, which most of its stress tests write when they start.
    public static void Warn() => Console.Error.WriteLine(
        "This is a stress test used to push Bevy to its limit and debug performance issues. It is not representative of an actual game. It must be built in Release or it will be very slow.");

    // Each frame's rate and time, as Bevy's diagnostics take them from the real clock, which a
    // pause or a slowed clock does not change.
    private static void Log(BehaviorContext ctx)
    {
        var seconds = ctx.Time.RawDeltaSeconds;
        if (seconds <= 0) return;

        var (fps, milliseconds) = (1.0 / seconds, seconds * 1000.0);
        Frames.Enqueue((fps, milliseconds));
        if (Frames.Count > History) Frames.Dequeue();
        (_smoothedFps, _smoothedMilliseconds) = _count++ == 0
            ? (fps, milliseconds)
            : (_smoothedFps + (fps - _smoothedFps) * Smoothing, _smoothedMilliseconds + (milliseconds - _smoothedMilliseconds) * Smoothing);

        _sinceLog += seconds;
        if (_sinceLog < 1.0) return;
        _sinceLog = 0;

        Console.WriteLine(FormattableString.Invariant($"fps        : {_smoothedFps,10:F2}  (avg {Frames.Average(frame => frame.Fps):F2})"));
        Console.WriteLine(FormattableString.Invariant($"frame_time : {_smoothedMilliseconds,10:F2}ms (avg {Frames.Average(frame => frame.Milliseconds):F2}ms)"));
        Console.WriteLine(FormattableString.Invariant($"frame_count: {_count,10}"));
    }
}
