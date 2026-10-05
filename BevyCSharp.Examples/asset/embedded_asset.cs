// Bevy's embedded_asset example, examples/asset/embedded_asset.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows an image carried inside the program rather than in the asset folder beside it, loaded by
// the path it has under the assets as any other.
//
// Bevy embeds a file in its binary with embedded_asset! and loads it from its embedded:// source.
// A C# game carries a file as a resource of its assembly named under assets/, which the examples'
// project does for this picture, and the asset server finds it there when no file on disk answers.
internal static class EmbeddedAsset
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        Render2d.SpawnCamera2d();

        const string Path = "embedded_asset/files/bevy_pixel_light.png";
        Console.WriteLine($"{Path} is carried by the program: {AssetFiles.IsCarried(Path)}");

        var sprite = ctx.Ecs.Spawn();
        ctx.Ecs.Add(sprite, Transform.Identity);
        Render2d.SetSprite(ctx.Ecs, sprite, AssetServer.Load(AssetKind.Image, Path));
    }, "embedded_asset.Setup");
}
