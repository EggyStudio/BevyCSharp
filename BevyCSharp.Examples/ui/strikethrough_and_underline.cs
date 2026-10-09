// Bevy's strikethrough_and_underline example, examples/ui/text/strikethrough_and_underline.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Illustrates interface text with lines through it and under it, on whole texts and on the spans
// within one, in the text's own color or one of their own, over backgrounds of the text's.
internal static class StrikethroughAndUnderline
{
    private static readonly Color Green = Color.FromSrgb8(0, 128, 0);
    private static readonly Color Navy = Color.FromSrgb8(0, 0, 128);
    private static readonly Color Red = Color.FromSrgb8(255, 0, 0);
    private static readonly Color Yellow = Color.FromSrgb8(255, 255, 0);
    private static readonly Color Black = Color.FromSrgb8(0, 0, 0);
    private static readonly Color White = Color.FromSrgb8(255, 255, 255);

    public static void Build(App app) => app.Startup(Setup, "strikethrough_and_underline.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var bold = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

        // A line through any text, a node's, a 2D one or a span, and its text is struck through.
        var corner = Ui.SpawnText("struck\nstruck",
            new UiSettings { Absolute = true, Bottom = Length.Px(5f), Right = Length.Px(5f) },
            new UiTextSettings { Font = bold, FontSize = 67f, Justify = TextJustify.Center });
        Ui.SetStrikethrough(corner);
        ecs.Insert<TextBackgroundColorRef>(corner).Value = Black;

        var column = Ui.SpawnNode(new UiSettings
        {
            Direction = UiDirection.Column,
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
        });

        var red = Ui.SpawnText("struck\nstruckstruck\nstruckstuckstruck", new UiSettings());
        Ui.SetStrikethrough(red);
        ecs.Insert<StrikethroughColorRef>(red).Value = Red;
        ecs.Insert<TextBackgroundColorRef>(red).Value = Green;
        ecs.SetParent(red, column);

        // A line under text.
        var underline = Ui.SpawnText("underline", new UiSettings());
        Ui.SetUnderline(underline);
        ecs.SetParent(underline, column);

        var mixed = Ui.SpawnText("struck", new UiSettings());
        Ui.SetStrikethrough(mixed);
        ecs.Insert<TextBackgroundColorRef>(mixed).Value = Green;
        Ui.SetUnderline(Ui.SpawnTextSpan(mixed, "underline", new UiTextSettings(), White));
        Ui.SetStrikethrough(Ui.SpawnTextSpan(mixed, "struck", new UiTextSettings(), White));
        ecs.SetParent(mixed, column);

        var large = Ui.SpawnText("struck struck", new UiSettings(), 67f);
        Ui.SetStrikethrough(large);
        ecs.SetParent(large, column);

        var navy = Ui.SpawnText("2struck\nstruck", new UiSettings(), new UiTextSettings { Font = bold, FontSize = 67f });
        Ui.SetStrikethrough(navy);
        ecs.Insert<BackgroundColorRef>(navy).Value = Navy;
        ecs.SetParent(navy, column);

        var spans = Ui.SpawnText(string.Empty, new UiSettings());
        ecs.SetParent(spans, column);

        var small = Ui.SpawnTextSpan(spans, "struck", new UiTextSettings { FontSize = 15f }, Red);
        Ui.SetStrikethrough(small);
        ecs.Insert<TextBackgroundColorRef>(small).Value = Black;

        var yellow = Ui.SpawnTextSpan(spans, "\nunderline", new UiTextSettings { FontSize = 30f }, Red);
        Ui.SetUnderline(yellow);
        ecs.Insert<UnderlineColorRef>(yellow).Value = Yellow;
        ecs.Insert<TextBackgroundColorRef>(yellow).Value = Green;

        var struck = Ui.SpawnTextSpan(spans, "\nstruck", new UiTextSettings { FontSize = 50f }, Red);
        Ui.SetStrikethrough(struck);
        ecs.Insert<TextBackgroundColorRef>(struck).Value = Navy;

        var both = Ui.SpawnTextSpan(spans, "underlined and struck", new UiTextSettings { FontSize = 70f }, Red);
        Ui.SetStrikethrough(both);
        Ui.SetUnderline(both);
        ecs.Insert<TextBackgroundColorRef>(both).Value = Navy;
        ecs.Insert<StrikethroughColorRef>(both).Value = White;
        ecs.Insert<UnderlineColorRef>(both).Value = White;
    }
}
