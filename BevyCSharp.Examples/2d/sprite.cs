// Bevy's sprite example, examples/2d/sprite.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Displays a single sprite.
internal static class SpriteExample
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        Render2d.SpawnCamera2d();
        var bird = ctx.Ecs.Spawn();
        ctx.Ecs.Add(bird, Transform.Identity);
        Render2d.SetSprite(ctx.Ecs, bird, AssetServer.Load(AssetKind.Image, "branding/bevy_bird_dark.png"));
    }, "sprite.Setup");
}
