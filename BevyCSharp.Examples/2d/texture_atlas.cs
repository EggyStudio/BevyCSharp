// Bevy's texture_atlas example, examples/2d/texture_atlas.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;
using Justify = Bevy.Reflected.TextLayoutRef.JustifyVariant;

namespace BevyCSharp.Examples.TwoD;

// Packs a folder of sprites into atlases as the app runs, with and without padding between them
// and sampled linearly or by nearest pixel, and draws a vendor from each, which shows how a sprite
// sampled between pixels takes color from its neighbor's edge unless the atlas pads them apart.
internal static class TextureAtlas
{
    private const string VendorPath = "textures/rpg/chars/vendor/generic-rpg-vendor.png";

    private static AssetHandle _folder;
    private static bool _done;

    public static void Build(App app)
    {
        (_folder, _done) = (AssetHandle.None, false);

        // Bevy's load_textures, every sprite of the folder loaded at once.
        app.Startup(_ => _folder = AssetServer.LoadFolder("textures/rpg"), "texture_atlas.LoadTextures");

        // Bevy's check_textures and setup, the atlases packed once the whole folder has loaded.
        app.Update(ctx =>
        {
            if (_done || !AssetServer.TryReadFolder(_folder, out var sprites)) return;
            _done = true;
            Setup(ctx.Ecs, sprites);
        }, "texture_atlas.Setup");
    }

    private static void Setup(EcsWorld ecs, AssetHandle[] sprites)
    {
        // Everything the folder holds is an image here, which Bevy checks one by one.
        var vendor = Array.FindIndex(sprites, sprite => AssetServer.PathOf(sprite) == VendorPath);

        var linear = Pack(sprites, (0, 0), TextureFilter.Linear);
        var nearest = Pack(sprites, (0, 0), TextureFilter.Nearest);
        var linearPadded = Pack(sprites, (6, 6), TextureFilter.Linear);
        var nearestPadded = Pack(sprites, (6, 6), TextureFilter.Nearest);

        Render2d.SpawnCamera2d();

        // The whole of the unpadded atlas to the left, and of the padded one to the right.
        Sprite(ecs, linear.Image, new Vec3(-250f, -160f, 0f), 0.5f);
        Sprite(ecs, linearPadded.Image, new Vec3(250f, -160f, 0f), 0.5f);

        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        Label(ecs, font, 42f, new Vec3(-250f, 250f, 0f), "No padding");
        Label(ecs, font, 42f, new Vec3(250f, 250f, 0f), "Padding");

        // The vendor drawn from each atlas, with what it is sampled by above it.
        const float BaseY = 80f;
        foreach (var (sampling, atlas, x) in new[] { ("Linear", linear, -350f), ("Nearest", nearest, -150f), ("Linear", linearPadded, 150f), ("Nearest", nearestPadded, 350f) })
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, new Transform(new Vec3(x, BaseY, 0f), Quat.Identity, new Vec3(3f, 3f, 3f)));
            Render2d.SetSprite(ecs, entity, atlas.Image, new SpriteSettings { Atlas = atlas.Layout, Frame = atlas.Indices[vendor] });
            Label(ecs, font, 25f, new Vec3(x, BaseY + 110f, 0f), sampling);
        }
    }

    // Bevy's create_texture_atlas, the folder's sprites packed with a padding and the atlas
    // sampled one way.
    private static PackedAtlas Pack(AssetHandle[] sprites, (uint X, uint Y) padding, TextureFilter sampling)
    {
        var atlas = Render2d.PackAtlas(sprites, padding)
            ?? throw new InvalidOperationException("The folder was read as loaded, so every sprite in it is.");
        Render.SetSampler(atlas.Image, new TextureSettings { MagFilter = sampling, MinFilter = sampling, MipmapFilter = sampling });
        return atlas;
    }

    private static void Sprite(EcsWorld ecs, AssetHandle image, Vec3 at, float scale)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, new Transform(at, Quat.Identity, new Vec3(scale, scale, scale)));
        Render2d.SetSprite(ecs, entity, image);
    }

    private static void Label(EcsWorld ecs, AssetHandle font, float size, Vec3 at, string text)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, new Transform(at));
        ecs.Insert<Text2dRef>(entity).Value = text;
        var style = ecs.Insert<TextFontRef>(entity);
        style.Font = new FontSource.Handle(font);
        style.FontSize = new FontSize.Px(size);
        ecs.Insert<TextLayoutRef>(entity).Justify = Justify.Center;
    }
}
