// Bevy's transparent_window example, examples/window/transparent_window.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Windowing;

// A window with no border and nothing behind the Bevy logo, so the desktop shows around it.
// Whether the desktop shows through depends on the platform's compositor.
internal static class TransparentWindow
{
    public static void Configure(Config config) => config.Transparent = true;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            if (Window.Entity() != Entity.None) Window.SetStyle(decorations: false);
            Render.SetClearColor((0f, 0f, 0f, 0f));
            Render2d.SpawnCamera2d();

            var logo = ctx.Ecs.Spawn();
            ctx.Ecs.Add(logo, Transform.Identity);
            Render2d.SetSprite(ctx.Ecs, logo, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        }, "transparent_window.Setup");
    }
}
