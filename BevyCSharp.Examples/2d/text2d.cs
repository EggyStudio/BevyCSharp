using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Shows text drawn in the world rather than on the interface, moved, turned and scaled, wrapped in
// boxes two ways, left unsmoothed, and anchored by each of its corners to one point. Bevy's
// underlines the first box's text, which is not reachable here, so this is written in part.
internal static class Text2dExample
{
    private const string Text2d = "bevy_sprite::text2d::Text2d";
    private const string TextSpan = "bevy_text::text::TextSpan";
    private const string TextFont = "bevy_text::text::TextFont";
    private const string TextLayout = "bevy_text::text::TextLayout";
    private const string TextBounds = "bevy_text::bounds::TextBounds";
    private const string TextColor = "bevy_text::text::TextColor";
    private const string TextBackground = "bevy_text::text::TextBackgroundColor";
    private const string Shadow = "bevy_sprite::text2d::Text2dShadow";
    private const string Anchor = "bevy_sprite::sprite::Anchor";

    private static Entity _translated, _rotated, _scaled;
    private static AssetHandle _font, _white;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render2d.SpawnCamera2d();
            _font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
            _white = Render.CreateImage([255, 255, 255, 255], 1, 1);
            var black = Color.FromSrgb(0f, 0f, 0f, 0.5f);

            _translated = Text(ecs, " translation ", 50f, "Center", background: black, shadow: true);
            _rotated = Text(ecs, " rotation ", 50f, "Center", background: black, shadow: true);
            _scaled = Text(ecs, " scale ", 50f, "Center", background: black, shadow: true);
            ecs.Set(_scaled, Transform.At(400f, 0f, 0f));

            // Two boxes the text wraps inside, at word boundaries and at any character.
            var boxColor = Color.FromSrgb(0.25f, 0.25f, 0.55f);
            var shadowColor = Darker(boxColor, 0.05f);
            foreach (var (x, label, linebreak) in new[] { (0f, "Unicode linebreaks", "WordBoundary"), (320f, "AnyCharacter linebreaks", "AnyCharacter") })
            {
                var box = Square(ecs, new Vec3(x, -250f, 0f), boxColor, (300f, 200f));
                var text = Text(ecs, $"this text wraps in the box\n({label})", 35f, "Left", linebreak: linebreak);
                ecs.SetVariant(text, TextBounds, ".width", "Some");
                ecs.SetReflected(text, TextBounds, ".width.0", "300.0");
                ecs.SetVariant(text, TextBounds, ".height", "Some");
                ecs.SetReflected(text, TextBounds, ".height.0", "200.0");
                ecs.InsertReflected(text, Shadow);
                ecs.SetReflectedColor(text, Shadow, ".color", shadowColor);
                ecs.Set(text, Transform.At(0f, 0f, 1f));
                ecs.SetParent(text, box);
            }

            var unsmoothed = Text(ecs, "This text has\nFontSmoothing::None\nAnd Justify::Center", 35f, "Center", shadow: true);
            ecs.SetVariant(unsmoothed, TextFont, ".font_smoothing", "None");
            ecs.Set(unsmoothed, Transform.At(-400f, -250f, 0f));

            // Four labels hung from one small square, each by a different corner.
            var point = Square(ecs, new Vec3(0f, 250f, 0f), Color.FromSrgb(224f / 255f, 1f, 1f), (10f, 10f));
            foreach (var (name, anchor, color) in new[]
            {
                ("TOP_LEFT", "[-0.5,0.5]", Color.FromSrgb(1f, 160f / 255f, 122f / 255f)),
                ("TOP_RIGHT", "[0.5,0.5]", Color.FromSrgb(144f / 255f, 238f / 255f, 144f / 255f)),
                ("BOTTOM_RIGHT", "[0.5,-0.5]", Color.FromSrgb(173f / 255f, 216f / 255f, 230f / 255f)),
                ("BOTTOM_LEFT", "[-0.5,-0.5]", Color.FromSrgb(1f, 1f, 224f / 255f)),
            })
            {
                var label = Text(ecs, " Anchor", 35f, "Left", background: Darker(Color.White, 0.8f));
                ecs.InsertReflected(label, Anchor);
                ecs.SetReflected(label, Anchor, ".0", anchor);
                ecs.Set(label, Transform.At(0f, 0f, -1f));
                ecs.SetParent(label, point);
                Span(ecs, label, "::", Color.FromSrgb(211f / 255f, 211f / 255f, 211f / 255f), Color.FromSrgb(0f, 0f, 139f / 255f));
                Span(ecs, label, $"{name} ", color, Darker(color, 0.3f));
            }
        }, "text2d.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var t = ctx.Time.Elapsed;
            ecs.Set(_translated, Transform.At(100f * MathF.Sin(t) - 400f, 100f * MathF.Cos(t), 0f));
            ecs.Set(_rotated, new Transform(Vec3.Zero, Quat.FromRotationZ(MathF.Cos(t)), Vec3.One));
            var scale = (MathF.Sin(t) + 1.1f) * 2f;
            ecs.Set(_scaled, new Transform(new Vec3(400f, 0f, 0f), Quat.Identity, new Vec3(scale, scale, 1f)));
        }, "text2d.Animate");
    }

    // A run of text in the world, in Fira Sans at a size, justified, and given a background and a
    // shadow where asked.
    private static Entity Text(EcsWorld ecs, string text, float size, string justify, Color? background = null, bool shadow = false, string linebreak = "WordBoundary")
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, Transform.Identity);
        ecs.InsertReflected(entity, Text2d, System.Text.Json.JsonSerializer.Serialize(text));
        Font(ecs, entity, size);
        ecs.InsertReflected(entity, TextLayout);
        ecs.SetVariant(entity, TextLayout, ".justify", justify);
        ecs.SetVariant(entity, TextLayout, ".linebreak", linebreak);
        if (background is { } color)
        {
            ecs.InsertReflected(entity, TextBackground);
            ecs.SetReflectedColor(entity, TextBackground, ".0", color);
        }

        if (shadow) ecs.InsertReflected(entity, Shadow);
        return entity;
    }

    // A further run of the same text, in its own colors.
    private static void Span(EcsWorld ecs, Entity parent, string text, Color color, Color background)
    {
        var span = ecs.Spawn();
        ecs.InsertReflected(span, TextSpan, System.Text.Json.JsonSerializer.Serialize(text));
        Font(ecs, span, 35f);
        ecs.InsertReflected(span, TextColor);
        ecs.SetReflectedColor(span, TextColor, ".0", color);
        ecs.InsertReflected(span, TextBackground);
        ecs.SetReflectedColor(span, TextBackground, ".0", background);
        ecs.SetParent(span, parent);
    }

    private static void Font(EcsWorld ecs, Entity entity, float size)
    {
        ecs.InsertReflected(entity, TextFont);
        ecs.SetReflectedAsset(entity, TextFont, ".font.0", _font);
        ecs.SetReflected(entity, TextFont, ".font_size", FormattableString.Invariant($"{{\"Px\":{size}}}"));
    }

    private static Entity Square(EcsWorld ecs, Vec3 at, Color color, (float Width, float Height) size)
    {
        var square = ecs.Spawn();
        ecs.Add(square, new Transform(at));
        Render2d.SetSprite(ecs, square, _white, new SpriteSettings { Color = (color.R, color.G, color.B, color.A), Size = size });
        return square;
    }

    // Bevy's darker, taking an amount off the color's lightness, here by scaling it toward black.
    private static Color Darker(Color linear, float amount)
    {
        var (r, g, b) = (ToSrgb(linear.R), ToSrgb(linear.G), ToSrgb(linear.B));
        var max = MathF.Max(r, MathF.Max(g, b));
        var min = MathF.Min(r, MathF.Min(g, b));
        var lightness = (max + min) / 2f;
        var scale = lightness <= 0f ? 0f : MathF.Max(lightness - amount, 0f) / lightness;
        return Color.FromSrgb(r * scale, g * scale, b * scale, linear.A);

        static float ToSrgb(float c) => c <= 0.0031308f ? c * 12.92f : 1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f;
    }
}
