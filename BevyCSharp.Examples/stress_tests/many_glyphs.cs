// Bevy's many_glyphs example, examples/stress_tests/many_glyphs.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;
using Justify = Bevy.Reflected.TextLayoutRef.JustifyVariant;
using Linebreak = Bevy.Reflected.TextLayoutRef.LinebreakVariant;

namespace BevyCSharp.Examples.StressTests;

// Lays out a hundred thousand glyphs, the digits over and over at four pixels, once in the
// interface and once in the world, each broken into lines a thousand pixels wide. --no-ui and
// --no-text2d leave either out, and --recompute-text has both laid out again every frame.
internal static class ManyGlyphs
{
    // Bevy's Args, read from the command line.
    private static bool _noUi, _noText2d;

    // The texts laid out, which Bevy finds by their layouts each frame it lays them out again.
    private static readonly List<Entity> Texts = [];

    // Bevy's window for its stress tests, 1920 by 1080 at a scale factor of one with no vertical
    // sync, drawn as fast as it can, and its frame times logged once a second by Bevy's plugins.
    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
        config.LogFrameTimes = true;
    }

    public static void Build(App app)
    {
        var arguments = Environment.GetCommandLineArgs();
        (_noUi, _noText2d) = (arguments.Contains("--no-ui"), arguments.Contains("--no-text2d"));
        Texts.Clear();

        app.Startup(Setup, "many_glyphs.Setup");
        if (arguments.Contains("--recompute-text")) app.Update(ForceTextRecomputation, "many_glyphs.ForceTextRecomputation");
    }

    private static void Setup(BehaviorContext ctx)
    {
        StressTest.Warn();
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var text = string.Concat(Enumerable.Repeat("0123456789", 10_000));

        if (!_noUi)
        {
            var row = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center });
            var column = Ui.SpawnNode(new UiSettings { Width = Length.Px(1000f) });
            ecs.SetParent(column, row);
            var interfaceText = Ui.SpawnText(text, new UiSettings(), 4f);
            Lay(ecs, interfaceText);
            ecs.SetParent(interfaceText, column);
        }

        if (!_noText2d)
        {
            var worldText = ecs.Spawn();
            ecs.Add(worldText, Transform.Identity);
            ecs.Insert<Text2dRef>(worldText).Value = text;
            ecs.Insert<TextFontRef>(worldText).FontSize = new FontSize.Px(4f);
            ecs.Insert<TextColorRef>(worldText).Value = new Color(1f, 0f, 0f, 1f);
            ecs.Insert<AnchorRef>(worldText).Value = Vec2.Zero;
            ecs.Insert<TextBoundsRef>(worldText).Width = 1000f;
            Lay(ecs, worldText);
        }
    }

    // Left-justified, broken at any character, as Bevy lays out both.
    private static void Lay(EcsWorld ecs, Entity text)
    {
        var layout = ecs.Insert<TextLayoutRef>(text);
        (layout.Justify, layout.Linebreak) = (Justify.Left, Linebreak.AnyCharacter);
        Texts.Add(text);
    }

    // Each text's layout marked changed, which Bevy does by reaching it mutably, and here by
    // writing it again as it is, so the glyphs are laid out again.
    private static void ForceTextRecomputation(BehaviorContext ctx)
    {
        foreach (var text in Texts)
        {
            var layout = ctx.Ecs.Wrap<TextLayoutRef>(text);
            layout.Justify = layout.Justify;
        }
    }
}
