using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Displays a single sprite, flipped across its vertical axis.
internal static class SpriteFlipping
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        Render2d.SpawnCamera2d();
        var bird = ctx.Ecs.Spawn();
        ctx.Ecs.Add(bird, Transform.Identity);
        Render2d.SetSprite(ctx.Ecs, bird, AssetServer.Load(AssetKind.Image, "branding/bevy_bird_dark.png"), new SpriteSettings { FlipX = true, FlipY = false });
    }, "sprite_flipping.Setup");
}
