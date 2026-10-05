using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows an image loaded from an asset source of its own, a folder beside the assets named
// example_files, by a path that starts with the source's name.
internal static class ExtraSource
{
    public static void Configure(Config config) =>
        config.AssetSources["example_files"] = Path.Combine(AppContext.BaseDirectory, "assets", "pixel");

    public static void Build(App app) => app.Startup(ctx =>
    {
        Render2d.SpawnCamera2d();
        var sprite = ctx.Ecs.Spawn();
        ctx.Ecs.Add(sprite, Transform.Identity);
        Render2d.SetSprite(ctx.Ecs, sprite, AssetServer.Load(AssetKind.Image, "example_files://bevy_pixel_light.png"));
    }, "extra_source.Setup");
}
