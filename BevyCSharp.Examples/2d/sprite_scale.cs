using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Shows the ways a sprite's picture meets a size it does not fit, stretched, filled from the
// middle, the start or the end, and fitted the same three ways, for still pictures and for frames
// of a running character.
internal static class SpriteScale
{
    private const string Text2d = "bevy_sprite::text2d::Text2d";
    private const string TextFont = "bevy_text::text::TextFont";
    private const string TextLayout = "bevy_text::text::TextLayout";
    private const string Anchor = "bevy_sprite::sprite::Anchor";

    private static readonly (float W, float H, string Text, float X, float Y, bool Banner, SpriteScaling? Scaling)[] Pictures =
    [
        (100f, 225f, "Stretched", -570f, 230f, false, null),
        (100f, 225f, "Fill Center", -450f, 230f, false, SpriteScaling.FillCenter),
        (100f, 225f, "Fill Start", -330f, 230f, false, SpriteScaling.FillStart),
        (100f, 225f, "Fill End", -210f, 230f, false, SpriteScaling.FillEnd),
        (300f, 100f, "Fill Start Horizontal", 10f, 290f, false, SpriteScaling.FillStart),
        (300f, 100f, "Fill End Horizontal", 10f, 155f, false, SpriteScaling.FillEnd),
        (200f, 200f, "Fill Center", 280f, 230f, true, SpriteScaling.FillCenter),
        (200f, 100f, "Fill Center", 500f, 230f, false, SpriteScaling.FillCenter),
        (100f, 100f, "Stretched", -570f, -40f, true, null),
        (200f, 200f, "Fit Center", -400f, -40f, true, SpriteScaling.FitCenter),
        (200f, 200f, "Fit Start", -180f, -40f, true, SpriteScaling.FitStart),
        (200f, 200f, "Fit End", 40f, -40f, true, SpriteScaling.FitEnd),
        (100f, 200f, "Fit Center", 210f, -40f, true, SpriteScaling.FitCenter),
    ];

    private static readonly (float W, float H, string Text, float X, float Y, SpriteScaling? Scaling)[] Sheets =
    [
        (120f, 50f, "Stretched", -570f, -200f, null),
        (120f, 50f, "Fill Center", -570f, -300f, SpriteScaling.FillCenter),
        (120f, 50f, "Fill Start", -430f, -200f, SpriteScaling.FillStart),
        (120f, 50f, "Fill End", -430f, -300f, SpriteScaling.FillEnd),
        (50f, 120f, "Fill Center", -300f, -250f, SpriteScaling.FillCenter),
        (50f, 120f, "Fill Start", -190f, -250f, SpriteScaling.FillStart),
        (50f, 120f, "Fill End", -90f, -250f, SpriteScaling.FillEnd),
        (120f, 50f, "Fit Center", 20f, -200f, SpriteScaling.FitCenter),
        (120f, 50f, "Fit Start", 20f, -300f, SpriteScaling.FitStart),
        (120f, 50f, "Fit End", 160f, -200f, SpriteScaling.FitEnd),
    ];

    private static readonly List<(Entity Sprite, SpriteSettings Settings)> Running = [];
    private static AssetHandle _gabe;
    private static uint _frame;
    private static float _timer;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Running.Clear();
            (_frame, _timer) = (0, 0f);
            Render2d.SpawnCamera2d();

            var square = AssetServer.Load(AssetKind.Image, "textures/slice_square_2.png");
            var banner = AssetServer.Load(AssetKind.Image, "branding/banner.png");
            foreach (var (w, h, text, x, y, isBanner, scaling) in Pictures)
                Labeled(ecs, isBanner ? banner : square, Settings(w, h, scaling), text, x, y, h);

            _gabe = AssetServer.Load(AssetKind.Image, "textures/rpg/chars/gabe/gabe-idle-run.png");
            var layout = Render2d.CreateAtlas(24, 24, 7, 1);
            foreach (var (w, h, text, x, y, scaling) in Sheets)
            {
                var settings = Settings(w, h, scaling);
                settings.Atlas = layout;
                Running.Add((Labeled(ecs, _gabe, settings, text, x, y, h), settings));
            }
        }, "sprite_scale.Setup");

        // Every running character a frame on each tenth of a second, the seven in turn.
        app.Update(ctx =>
        {
            _timer += ctx.Time.Delta;
            if (_timer < 0.1f) return;
            _timer -= 0.1f;
            _frame = _frame == 6 ? 0 : _frame + 1;
            foreach (var (sprite, settings) in Running)
            {
                settings.Frame = _frame;
                Render2d.SetSprite(ctx.Ecs, sprite, _gabe, settings);
            }
        }, "sprite_scale.AnimateSprite");
    }

    private static SpriteSettings Settings(float width, float height, SpriteScaling? scaling) => new()
    {
        Size = (width, height),
        Mode = scaling is null ? SpriteImageMode.Auto : SpriteImageMode.Scaled,
        Scaling = scaling ?? SpriteScaling.FitCenter,
    };

    // A sprite with its label in small text under it, hung by the label's top middle.
    private static Entity Labeled(EcsWorld ecs, AssetHandle image, SpriteSettings settings, string text, float x, float y, float height)
    {
        var sprite = ecs.Spawn();
        ecs.Add(sprite, Transform.At(x, y, 0f));
        Render2d.SetSprite(ecs, sprite, image, settings);

        var label = ecs.Spawn();
        ecs.Add(label, Transform.At(0f, -0.5f * height - 10f, 0f));
        ecs.InsertReflected(label, Text2d, System.Text.Json.JsonSerializer.Serialize(text));
        ecs.InsertReflected(label, TextFont);
        ecs.SetReflected(label, TextFont, ".font_size", "{\"Px\":15.0}");
        ecs.InsertReflected(label, TextLayout);
        ecs.SetVariant(label, TextLayout, ".justify", "Center");
        ecs.InsertReflected(label, Anchor);
        ecs.SetReflected(label, Anchor, ".0", "[0.0,0.5]");
        ecs.SetParent(label, sprite);
        return sprite;
    }
}
