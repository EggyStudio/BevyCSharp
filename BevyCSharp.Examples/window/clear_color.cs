// Bevy's clear_color example, examples/window/clear_color.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Windowing;

// The color the window is cleared to each frame, a pale blue that Space turns purple.
internal static class ClearColor
{
    public static void Build(App app)
    {
        app.Startup(_ =>
        {
            Render.SetClearColor(Color.FromSrgb(0.5f, 0.5f, 0.9f));
            Render2d.SpawnCamera2d();
        }, "clear_color.Setup");

        app.Update(ctx =>
        {
            if (ctx.Input.KeyPressed(Key.Space)) Render.SetClearColor(Color.FromSrgb8(128, 0, 128));
        }, "clear_color.ChangeClearColor");
    }
}
