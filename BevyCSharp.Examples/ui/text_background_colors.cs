// Bevy's text_background_colors example, examples/ui/text/text_background_colors.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates interface text with a background color, each letter a span over a color of its own
// that moves on along the palette each second, and drawn white while the pointer is over it.
internal static class TextBackgroundColors
{
    private static readonly Color[] Palette =
    [
        Color.FromSrgb8(255, 0, 0),
        Color.FromSrgb8(0, 128, 0),
        Color.FromSrgb8(0, 0, 255),
        Color.FromSrgb8(255, 255, 0),
        Color.FromSrgb8(128, 0, 128),
    ];

    private static readonly List<Entity> Spans = [];

    public static void Build(App app)
    {
        app.Startup(Setup, "text_background_colors.Setup");
        app.Update(CycleTextBackgroundColors, "text_background_colors.CycleTextBackgroundColors");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Spans.Clear();
        Render2d.SpawnCamera2d();

        string[] messageText = ["T", "e", "x", "t\n", "B", "a", "c", "k", "g", "r", "o", "u", "n", "d\n", "C", "o", "l", "o", "r", "s", "!"];

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
        });

        var text = Ui.SpawnText(string.Empty,
            new UiSettings
            {
                Border = Sides.All(Length.Px(5f)),
                Padding = Sides.All(Length.Px(10f)),
                BorderColor = Color.FromSrgb8(0, 139, 139),
            },
            new UiTextSettings { Justify = TextJustify.Center });
        ecs.SetParent(text, root);

        for (var i = 0; i < messageText.Length; i++)
        {
            var span = Ui.SpawnTextSpan(text, messageText[i], new UiTextSettings { FontSize = 100f }, Color.Black);
            ecs.Insert<TextBackgroundColorRef>(span).Value = Palette[i % Palette.Length];
            ecs.Observe<Pointer<Over>>(span, on => on.Ecs.Wrap<TextColorRef>(on.Entity).Value = Color.White);
            ecs.Observe<Pointer<Out>>(span, on => on.Ecs.Wrap<TextColorRef>(on.Entity).Value = Color.Black);
            Spans.Add(span);
        }
    }

    // Each span's background moved on along the palette by the seconds elapsed.
    private static void CycleTextBackgroundColors(BehaviorContext ctx)
    {
        var n = (int)ctx.Time.Elapsed;
        for (var i = 0; i < Spans.Count; i++)
        {
            var background = ctx.Ecs.Wrap<TextBackgroundColorRef>(Spans[i]);
            var color = Palette[(i + n) % Palette.Length];
            if (background.Value != color) background.Value = color;
        }
    }
}
