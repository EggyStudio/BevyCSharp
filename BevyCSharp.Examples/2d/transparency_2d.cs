// Bevy's transparency_2d example, examples/2d/transparency_2d.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Demonstrates transparency in 2D, three logos overlapping, opaque, then blue at seventy percent
// and green at thirty, each a little nearer than the last.
internal static class Transparency2d
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var logo = AssetServer.Load(AssetKind.Image, "branding/icon.png");

        foreach (var (at, color) in new[]
        {
            (new Vec3(-100f, 0f, 0f), (1f, 1f, 1f, 1f)),
            (new Vec3(0f, 0f, 0.1f), Color.FromSrgb(0f, 0f, 1f, 0.7f)),
            (new Vec3(100f, 0f, 0.2f), Color.FromSrgb(0f, 1f, 0f, 0.3f)),
        })
        {
            var sprite = ecs.Spawn();
            ecs.Add(sprite, new Transform(at));
            Render2d.SetSprite(ecs, sprite, logo, new SpriteSettings { Color = color });
        }
    }, "transparency_2d.Setup");
}
