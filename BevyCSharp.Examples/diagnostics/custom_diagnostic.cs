// Bevy's custom_diagnostic example, examples/diagnostics/custom_diagnostic.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Measuring;

// A diagnostic of the game's own, measured by a system every frame, which Bevy's log of the
// diagnostics store prints once a second with its suffix after the number.
internal static class CustomDiagnostic
{
    // Every diagnostic has a path of its own.
    private const string SystemIterationCount = "system_iteration_count";

    // Bevy's log of the store, which only shows the diagnostic and could be left out.
    public static void Configure(Config config) => config.DiagnosticPlugins = DiagnosticPlugins.Log;

    public static void Build(App app)
    {
        // Registered before it is measured, since the store keeps only the measurements of a path
        // it has.
        app.Startup(_ => Diagnostics.Register(SystemIterationCount, " iterations"), "custom_diagnostic.Register");

        // A measurement of ten each time the system runs.
        app.Update(_ => Diagnostics.Measure(SystemIterationCount, 10.0), "custom_diagnostic.MySystem");
    }
}
