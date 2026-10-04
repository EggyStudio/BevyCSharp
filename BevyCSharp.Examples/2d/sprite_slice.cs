using System.Text.Json.Nodes;
using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Shows nine-slice sprites, two squares each drawn six ways: as they are, stretched, sliced so the
// corners keep their size, sliced with the middle and the sides tiled, wider, and with the corners
// held small.
internal static class SpriteSlice
{
    private const string SpriteType = "bevy_sprite::sprite::Sprite";
    private const string Text2d = "bevy_sprite::text2d::Text2d";
    private const string TextFont = "bevy_text::text::TextFont";
    private const string TextLayout = "bevy_text::text::TextLayout";
    private const string Anchor = "bevy_sprite::sprite::Anchor";

    // A label, a size, whether it is sliced, and the stretch of its middle and sides where they
    // tile, zero for stretched, and how far its corners may grow.
    private static readonly (string Label, (float W, float H) Size, bool Sliced, float Center, float Sides, float Corners)[] Cases =
    [
        ("Original", (100f, 100f), false, 0f, 0f, 1f),
        ("Stretched", (100f, 200f), false, 0f, 0f, 1f),
        ("With Slicing", (100f, 200f), true, 0f, 0f, 1f),
        ("With Tiling", (100f, 200f), true, 0.5f, 0.2f, 1f),
        ("With Tiling", (300f, 200f), true, 0.2f, 0.3f, 1f),
        ("With Corners Constrained", (300f, 200f), true, 0.1f, 0.2f, 0.2f),
    ];

    public static void Build(App app) => app.Startup(ctx =>
    {
        Render2d.SpawnCamera2d();
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        Spawn(ctx.Ecs, AssetServer.Load(AssetKind.Image, "textures/slice_square.png"), new Vec3(-600f, 150f, 0f), 200f, font);
        Spawn(ctx.Ecs, AssetServer.Load(AssetKind.Image, "textures/slice_square_2.png"), new Vec3(-600f, -150f, 0f), 80f, font);
    }, "sprite_slice.Setup");

    private static void Spawn(EcsWorld ecs, AssetHandle image, Vec3 position, float border, AssetHandle font)
    {
        const float Gap = 40f;
        foreach (var (label, size, sliced, center, sides, corners) in Cases)
        {
            position += new Vec3(0.5f * size.W, 0f, 0f);
            var sprite = ecs.Spawn();
            ecs.Add(sprite, new Transform(position));
            Render2d.SetSprite(ecs, sprite, image, new SpriteSettings
            {
                Size = size,
                Mode = sliced ? SpriteImageMode.Sliced : SpriteImageMode.Auto,
                SliceBorder = (border, border, border, border),
                CornerScale = corners,
            });
            if (sliced) ScaleModes(ecs, sprite, center, sides);

            // The label under the sprite, hung by its top middle.
            var text = ecs.Spawn();
            ecs.Add(text, Transform.At(0f, -0.5f * size.H - 10f, 0f));
            ecs.InsertReflected(text, Text2d, System.Text.Json.JsonSerializer.Serialize(label));
            ecs.InsertReflected(text, TextFont);
            ecs.SetReflectedAsset(text, TextFont, ".font.0", font);
            ecs.InsertReflected(text, TextLayout);
            ecs.SetVariant(text, TextLayout, ".justify", "Center");
            ecs.InsertReflected(text, Anchor);
            ecs.SetReflected(text, Anchor, ".0", "[0.0,0.5]");
            ecs.SetParent(text, sprite);

            position += new Vec3(0.5f * size.W + Gap, 0f, 0f);
        }
    }

    // The middle and the sides each stretched or tiled at their own rate, which Bevy's slicer
    // holds apart and the sprite's settings give as one, so the two are written into it here.
    private static void ScaleModes(EcsWorld ecs, Entity sprite, float center, float sides)
    {
        if (ecs.GetReflected(sprite, SpriteType, ".image_mode") is not { } json || JsonNode.Parse(json)?["Sliced"] is not JsonObject slicer) return;

        slicer["center_scale_mode"] = Mode(center);
        slicer["sides_scale_mode"] = Mode(sides);
        ecs.SetReflected(sprite, SpriteType, ".image_mode", new JsonObject { ["Sliced"] = slicer.DeepClone() }.ToJsonString());

        static JsonNode Mode(float stretch) => stretch > 0f
            ? new JsonObject { ["Tile"] = new JsonObject { ["stretch_value"] = stretch } }
            : JsonValue.Create("Stretch");
    }
}
