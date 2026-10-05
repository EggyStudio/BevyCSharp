// Bevy's persisting_window_settings example, examples/window/persisting_window_settings.rs at
// v0.19.1, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Windowing;

// A window that opens where it was left, at the size it was left at and fullscreen if it was, from
// one run to the next.
//
// Bevy keeps those in a settings group of the example's own, which its settings plugin writes to a
// file. Config.RememberWindow keeps the same three in the game's persistent settings and writes
// them as they change.
internal static class PersistingWindowSettings
{
    public static void Configure(Config config)
    {
        config.Title = "Settings Window";
        config.RememberWindow = true;
    }

    public static void Build(App app)
    {
        app.Startup(_ =>
        {
            Render2d.SpawnCamera2d();
            Ui.SpawnNode(new UiSettings
            {
                Width = Length.Percent(100f),
                Height = Length.Percent(100f),
                Direction = UiDirection.Column,
                Align = UiAlign.Center,
                Justify = UiJustify.Center,
            });
        }, "persisting_window_settings.Setup");
    }
}
