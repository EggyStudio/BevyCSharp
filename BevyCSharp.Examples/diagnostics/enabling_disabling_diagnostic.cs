// Bevy's enabling_disabling_diagnostic example,
// examples/diagnostics/enabling_disabling_diagnostic.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Measuring;

// Bevy's frame time diagnostics, printed once a second by Bevy's log of them, turned off after ten
// seconds and on again after ten more, so the log falls silent and starts again.
internal static class EnablingDisablingDiagnostic
{
    public static void Configure(Config config) => config.DiagnosticPlugins = DiagnosticPlugins.FrameTime | DiagnosticPlugins.Log;

    public static void Build(App app)
    {
        // Bevy's on_timer(10 s), which passes once each time the interval comes round.
        var next = 10f;
        app.On(Stage.Update, Toggle, "enabling_disabling_diagnostic.Toggle", world =>
        {
            if (world.Resource<Time>().Elapsed < next) return false;
            next += 10f;
            return true;
        });
    }

    private static void Toggle(BehaviorContext ctx)
    {
        foreach (var diagnostic in Diagnostics.All())
        {
            Log.Info($"toggling diagnostic {diagnostic.Path}");
            Diagnostics.SetEnabled(diagnostic.Path, !diagnostic.Enabled);
        }
    }
}
