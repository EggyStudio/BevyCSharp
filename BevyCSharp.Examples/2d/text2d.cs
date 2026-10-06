// Bevy's text2d example, examples/2d/text2d.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

using Justify = Bevy.Reflected.TextLayoutRef.JustifyVariant;
using Linebreak = Bevy.Reflected.TextLayoutRef.LinebreakVariant;

// Shows text drawn in the world rather than on the interface, moved, turned and scaled, wrapped in
// boxes two ways, left unsmoothed, and anchored by each of its corners to one point. Bevy's
// underlines the first box's text, which is not reachable here, so this is written in part.
internal static class Text2dExample
{
    private static AssetHandle _font, _white;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        _font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        _white = Render.CreateImage([255, 255, 255, 255], 1, 1);
        var black = Color.FromSrgb(0f, 0f, 0f, 0.5f);

        ecs.Add(Text(ecs, " translation ", 50f, Justify.Center, background: black, shadow: true), new AnimateTranslation());
        ecs.Add(Text(ecs, " rotation ", 50f, Justify.Center, background: black, shadow: true), new AnimateRotation());
        var scaled = Text(ecs, " scale ", 50f, Justify.Center, background: black, shadow: true);
        ecs.Set(scaled, Transform.At(400f, 0f, 0f));
        ecs.Add(scaled, new AnimateScale());

        // Two boxes the text wraps inside, at word boundaries and at any character.
        var boxColor = Color.FromSrgb(0.25f, 0.25f, 0.55f);
        var shadowColor = Darker(boxColor, 0.05f);
        foreach (var (x, label, linebreak) in new[] { (0f, "Unicode linebreaks", Linebreak.WordBoundary), (320f, "AnyCharacter linebreaks", Linebreak.AnyCharacter) })
        {
            var box = Square(ecs, new Vec3(x, -250f, 0f), boxColor, (300f, 200f));
            var text = Text(ecs, $"this text wraps in the box\n({label})", 35f, Justify.Left, linebreak: linebreak);
            var bounds = ecs.Wrap<TextBoundsRef>(text);
            (bounds.Width, bounds.Height) = (300f, 200f);
            ecs.Insert<Text2dShadowRef>(text).Color = shadowColor;
            ecs.Set(text, Transform.At(0f, 0f, 1f));
            ecs.SetParent(text, box);
        }

        var unsmoothed = Text(ecs, "This text has\nFontSmoothing::None\nAnd Justify::Center", 35f, Justify.Center, shadow: true);
        ecs.Wrap<TextFontRef>(unsmoothed).FontSmoothing = TextFontRef.FontSmoothingVariant.None;
        ecs.Set(unsmoothed, Transform.At(-400f, -250f, 0f));

        // Four labels hung from one small square, each by a different corner.
        var point = Square(ecs, new Vec3(0f, 250f, 0f), Color.FromSrgb(224f / 255f, 1f, 1f), (10f, 10f));
        foreach (var (name, anchor, color) in new[]
        {
            ("TOP_LEFT", new Vec2(-0.5f, 0.5f), Color.FromSrgb(1f, 160f / 255f, 122f / 255f)),
            ("TOP_RIGHT", new Vec2(0.5f, 0.5f), Color.FromSrgb(144f / 255f, 238f / 255f, 144f / 255f)),
            ("BOTTOM_RIGHT", new Vec2(0.5f, -0.5f), Color.FromSrgb(173f / 255f, 216f / 255f, 230f / 255f)),
            ("BOTTOM_LEFT", new Vec2(-0.5f, -0.5f), Color.FromSrgb(1f, 1f, 224f / 255f)),
        })
        {
            var label = Text(ecs, " Anchor", 35f, Justify.Left, background: Darker(Color.White, 0.8f));
            ecs.Insert<AnchorRef>(label).Value = anchor;
            ecs.Set(label, Transform.At(0f, 0f, -1f));
            ecs.SetParent(label, point);
            Span(ecs, label, "::", Color.FromSrgb(211f / 255f, 211f / 255f, 211f / 255f), Color.FromSrgb(0f, 0f, 139f / 255f));
            Span(ecs, label, $"{name} ", color, Darker(color, 0.3f));
        }
    }, "text2d.Setup");

    // A run of text in the world, in Fira Sans at a size, justified, and given a background and a
    // shadow where asked.
    private static Entity Text(EcsWorld ecs, string text, float size, Justify justify, Color? background = null, bool shadow = false, Linebreak linebreak = Linebreak.WordBoundary)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, Transform.Identity);
        ecs.Insert<Text2dRef>(entity).Value = text;
        Font(ecs, entity, size);
        var layout = ecs.Insert<TextLayoutRef>(entity);
        (layout.Justify, layout.Linebreak) = (justify, linebreak);
        if (background is { } color) ecs.Insert<TextBackgroundColorRef>(entity).Value = color;
        if (shadow) ecs.Insert<Text2dShadowRef>(entity);
        return entity;
    }

    // A further run of the same text, in its own colors.
    private static void Span(EcsWorld ecs, Entity parent, string text, Color color, Color background)
    {
        var span = ecs.Spawn();
        ecs.Insert<TextSpanRef>(span).Value = text;
        Font(ecs, span, 35f);
        ecs.Insert<TextColorRef>(span).Value = color;
        ecs.Insert<TextBackgroundColorRef>(span).Value = background;
        ecs.SetParent(span, parent);
    }

    private static void Font(EcsWorld ecs, Entity entity, float size)
    {
        var font = ecs.Insert<TextFontRef>(entity);
        font.Font = new FontSource.Handle(_font);
        font.FontSize = new FontSize.Px(size);
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

/// <summary>A text that circles a point to the left.</summary>
[Behavior]
public partial struct AnimateTranslation
{
    /// <summary>Moved around a circle of a hundred about a point four hundred to the left, a radian a second.</summary>
    [OnUpdate]
    public void Animate(BehaviorContext ctx, ref Transform transform)
    {
        transform.Translation.X = 100f * MathF.Sin(ctx.Time.Elapsed) - 400f;
        transform.Translation.Y = 100f * MathF.Cos(ctx.Time.Elapsed);
    }
}

/// <summary>A text that swings about its middle.</summary>
[Behavior]
public partial struct AnimateRotation
{
    /// <summary>Turned to the cosine of the time, in radians.</summary>
    [OnUpdate]
    public void Animate(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationZ(MathF.Cos(ctx.Time.Elapsed));
}

/// <summary>A text that grows and shrinks, which scales the drawn quad and so looks pixelated, where a font size would not.</summary>
[Behavior]
public partial struct AnimateScale
{
    /// <summary>Scaled across and up by the sine of the time, between a fifth and four and a fifth.</summary>
    [OnUpdate]
    public void Animate(BehaviorContext ctx, ref Transform transform)
    {
        var scale = (MathF.Sin(ctx.Time.Elapsed) + 1.1f) * 2f;
        transform.Scale.X = scale;
        transform.Scale.Y = scale;
    }
}
