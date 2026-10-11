// Bevy's asset_settings example, examples/asset/asset_settings.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Assets;

// The same picture of pixels three times, scaled up: on the left as an image loads unless told
// otherwise, its pixels blurred by the linear sampler, on the right with a .meta file beside it
// that asks the loader for the nearest sampler, and above with the nearest sampler asked for where
// it is loaded, as Bevy's load_builder().with_settings does, so both of those keep their pixels
// sharp.
//
// Bevy's reads them from its example's own folder, which it makes the asset root, and this from
// that folder under the assets, where build/fetch-bevy-assets.sh puts them.
internal static class AssetSettings
{
    private const string Folder = "examples/asset/files/";

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;

        void Spawn(AssetHandle image, float x, float y)
        {
            var sprite = ecs.Spawn();
            ecs.Add(sprite, Transform.At(x, y, 0f));
            Render2d.SetSprite(ecs, sprite, image, new SpriteSettings { Size = (160f, 120f) });
        }

        Spawn(AssetServer.Load(AssetKind.Image, Folder + "bevy_pixel_dark.png"), -100f, 0f);
        Spawn(AssetServer.Load(AssetKind.Image, Folder + "bevy_pixel_dark_with_meta.png"), 100f, 0f);

        // TextureSettings' samplers are the nearest, as Bevy's ImageSampler::nearest() is.
        Spawn(AssetServer.LoadImage(Folder + "bevy_pixel_dark_with_settings.png", new TextureSettings()), 0f, 150f);

        Render2d.SpawnCamera2d();
    }, "asset_settings.Setup");
}
