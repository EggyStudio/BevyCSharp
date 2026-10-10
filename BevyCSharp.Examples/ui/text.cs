// Bevy's text example, examples/ui/text/text.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using System.Globalization;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Illustrates interface text made and changed in a system. The frames per second at the top left,
// text whose color cycles at the bottom right, underlined and a fifth of the window tall, and a
// font's OpenType features at the top right, each drawn on a line of its own.
internal static class TextExample
{
    public static void Build(App app) => app.Startup(Setup, "text.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var bold = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        Render2d.SpawnCamera2d();

        // Text with one section, a fifth of the window's height tall.
        var animated = Ui.SpawnText("hello\nbevy!",
            new UiSettings { Absolute = true, Bottom = Length.Px(5f), Right = Length.Px(5f) },
            new UiTextSettings { Font = bold, Justify = TextJustify.Center });
        Ui.SetUnderline(animated);
        ecs.Wrap<TextFontRef>(animated).FontSize = new FontSize.Vh(20f);
        ecs.Insert<TextShadowRef>(animated);
        ecs.Add(animated, new AnimatedText());

        // Text with multiple sections, the second the frames per second in gold.
        var fps = Ui.SpawnText("FPS: ", new UiSettings(), new UiTextSettings { Font = bold, FontSize = 42f });
        ecs.Add(Ui.SpawnTextSpan(fps, string.Empty, new UiTextSettings { FontSize = 33f }, Color.FromSrgb8(255, 215, 0)), new FpsText());

        // Text with OpenType features, each line's text drawn with one turned on.
        var garamond = AssetServer.Load(AssetKind.Font, "fonts/EBGaramond12-Regular.otf");
        var features = Ui.SpawnText("Opentype features:\n",
            new UiSettings { Margin = Sides.All(Length.Px(12f)), Absolute = true, Top = Length.Px(5f), Right = Length.Px(5f) },
            new UiTextSettings { Font = garamond, FontSize = 32f });

        (string Title, string Feature, string Text)[] rows =
        [
            ("Smallcaps: ", "smcp", "Hello World"),
            ("Ligatures: ", "liga", "fi fl ff ffi ffl"),
            ("Fractions: ", "frac", "12/134"),
            ("Superscript: ", "sups", "Up here!"),
            ("Subscript: ", "subs", "Down here!"),
            ("Oldstyle figures: ", "onum", "1234567890"),
            ("Lining figures: ", "lnum", "1234567890"),
        ];

        var white = Color.FromSrgb8(255, 255, 255);
        foreach (var (title, feature, text) in rows)
        {
            Ui.SpawnTextSpan(features, title, new UiTextSettings { Font = garamond, FontSize = 24f }, white);
            var shown = Ui.SpawnTextSpan(features, $"{text}\n", new UiTextSettings { Font = garamond, FontSize = 24f }, white);
            Ui.SetFontFeatures(shown, (feature, 1u));
        }

        // Bevy's default font, a small part of FiraMono compiled in.
        Ui.SpawnText("From an &str into a Text with the default font!",
            new UiSettings { Absolute = true, Bottom = Length.Px(5f), Left = Length.Px(15f) });
    }
}

/// <summary>The span showing the frames per second, Bevy's example's <c>FpsText</c>.</summary>
[Behavior]
public partial struct FpsText
{
    /// <summary>Bevy's text_update_system, the frames per second smoothed as Bevy's diagnostic smooths it.</summary>
    [OnUpdate]
    public void TextUpdateSystem(BehaviorContext ctx)
    {
        if (ctx.Time.SmoothedFps > 0.0)
            ctx.Ecs.Wrap<TextSpanRef>(ctx.Entity).Value = ctx.Time.SmoothedFps.ToString("0.00", CultureInfo.InvariantCulture);
    }
}

/// <summary>The text whose color cycles, Bevy's example's <c>AnimatedText</c>.</summary>
[Behavior]
public partial struct AnimatedText
{
    /// <summary>Bevy's text_color_system, the color cycling through the hues at three rates.</summary>
    [OnUpdate]
    public void TextColorSystem(BehaviorContext ctx)
    {
        var seconds = ctx.Time.Elapsed;
        ctx.Ecs.Wrap<TextColorRef>(ctx.Entity).Value = Color.FromSrgb(
            MathF.Sin(1.25f * seconds) / 2f + 0.5f,
            MathF.Sin(0.75f * seconds) / 2f + 0.5f,
            MathF.Sin(0.50f * seconds) / 2f + 0.5f);
    }
}
