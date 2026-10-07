// Bevy's window_fallthrough example, examples/ui/window_fallthrough.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// This example illustrates how to have a mouse's clicks, wheel and movement fall through the
// transparent window to a window below. P turns the pass-through on and off. The window stays above
// every other, and is drawn with nothing behind its text so the effect shows. Falling through is not
// supported on X11 or the web, where the window keeps its pointer either way.
internal static class WindowFallthrough
{
    public static void Configure(Config config) => config.Transparent = true;

    public static void Build(App app)
    {
        app.Startup(_ =>
        {
            // Bevy's ClearColor of nothing, which a transparent window shows the desktop through.
            Render.SetClearColor((0f, 0f, 0f, 0f));
            if (Window.Entity() != Entity.None) Window.SetStyle(decorations: true, alwaysOnTop: true);

            Render2d.SpawnCamera2d();
            Ui.SpawnText(
                "Hit 'P' then scroll/click around!",
                new UiSettings { Absolute = true, Bottom = Length.Px(5f), Right = Length.Px(10f) },
                new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 83f });
        }, "window_fallthrough.Setup");

        app.Update(ToggleMousePassthrough, "window_fallthrough.ToggleMousePassthrough");
    }

    // Bevy's toggle_mouse_passthrough, the window's hit test turned over. An offscreen run has no
    // window, and nothing to turn.
    private static void ToggleMousePassthrough(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.P) || Window.Entity() is var window && window == Entity.None) return;

        var cursor = ctx.Ecs.Wrap<CursorOptionsRef>(window);
        cursor.HitTest = !cursor.HitTest;
    }
}
