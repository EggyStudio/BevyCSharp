// Bevy's fps_overlay example, examples/dev_tools/fps_overlay.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.DevTool;

// Bevy's FPS overlay at the window's top left, in large green text over its frame time graph, and
// the keys that change it, 1 its color, 2 and 3 its size, 4 whether it shows and 5 whether the
// graph does.
internal static class FpsOverlay
{
    private static readonly Color Red = Color.FromSrgb(1f, 0f, 0f), Green = Color.FromSrgb(0f, 1f, 0f);

    // Bevy's FpsOverlayPlugin.
    public static void Configure(Config config) => config.DevTools = DevTools.FpsOverlay;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            // A camera, 2D or 3D, for the overlay to be drawn by.
            Render2d.SpawnCamera2d();
            Ui.SpawnText(
                "Press 1 to toggle the overlay color.\n"
                + "Press 2 to decrease the overlay size.\n"
                + "Press 3 to increase the overlay size.\n"
                + "Press 4 to toggle the text visibility.\n"
                + "Press 5 to toggle the frame time graph.",
                new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });

            // Bevy's plugin is given these as it is added. Here they are written over the resource
            // it adds with its defaults, which the overlay follows as the resource changes, and the
            // interval at which its text changes is Bevy's default of a tenth of a second.
            if (ctx.Ecs.Resource<FpsOverlayConfigRef>() is not { } overlay) return;
            overlay.TextConfigFontSize = new FontSize.Px(42f);
            overlay.TextColor = Green;
            overlay.Enabled = true;
            overlay.FrameTimeGraphConfigEnabled = true;
            overlay.FrameTimeGraphConfigMinFps = 30f;
            overlay.FrameTimeGraphConfigTargetFps = 144f;
        }, "fps_overlay.Setup");

        app.Update(CustomizeConfig, "fps_overlay.CustomizeConfig");
    }

    // Bevy's customize_config, each change written to the resource the overlay follows.
    private static void CustomizeConfig(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (ctx.Ecs.Resource<FpsOverlayConfigRef>() is not { } overlay) return;

        if (input.KeyPressed(Key.Digit1)) overlay.TextColor = overlay.TextColor == Green ? Red : Green;

        // Only a size in pixels is changed.
        if (overlay.TextConfigFontSize is FontSize.Px(var size))
        {
            var changed = size;
            if (input.KeyPressed(Key.Digit2)) changed = MathF.Max(changed - 2f, 2f);
            if (input.KeyPressed(Key.Digit3)) changed += 2f;
            if (changed != size) overlay.TextConfigFontSize = new FontSize.Px(changed);
        }

        if (input.KeyPressed(Key.Digit4)) overlay.Enabled = !overlay.Enabled;
        if (input.KeyReleased(Key.Digit5)) overlay.FrameTimeGraphConfigEnabled = !overlay.FrameTimeGraphConfigEnabled;
    }
}
