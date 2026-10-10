// Bevy's text_pipeline example, examples/stress_tests/text_pipeline.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;
using Justify = Bevy.Reflected.TextLayoutRef.JustifyVariant;
using Linebreak = Bevy.Reflected.TextLayoutRef.LinebreakVariant;

namespace BevyCSharp.Examples.StressTests;

// Text in the world of many spans in two fonts at many sizes, whose bounds widen and narrow with
// the clock, so it is laid out again every frame.
internal static class TextPipeline
{
    private static Entity _text;

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
        app.Startup(Spawn, "text_pipeline.Spawn");
        app.Update(UpdateTextBounds, "text_pipeline.UpdateTextBounds");
    }

    private static void Spawn(BehaviorContext ctx)
    {
        // Bevy's warning_string.txt, which its stress tests say as they start.
        Console.Error.WriteLine("This is a stress test used to push Bevy to its limit and debug performance issues. It is not representative of an actual game. It must be built in Release or it will be very slow.");
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        _text = ecs.Spawn();
        ecs.Add(_text, Transform.Identity);
        ecs.Insert<Text2dRef>(_text);
        var layout = ecs.Insert<TextLayoutRef>(_text);
        (layout.Justify, layout.Linebreak) = (Justify.Center, Linebreak.AnyCharacter);
        ecs.Insert<TextBoundsRef>(_text);

        var mono = AssetServer.Load(AssetKind.Font, "fonts/FiraMono-Medium.ttf");
        var sans = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        for (var i = 1; i < 50; i++)
        {
            Span(ecs, string.Concat(Enumerable.Repeat("text", i)), mono, 4 + i % 10, Color.FromSrgb(0f, 0f, 1f));
            Span(ecs, string.Concat(Enumerable.Repeat("pipeline", i)), sans, 4 + i % 11, Color.FromSrgb(1f, 1f, 0f));
        }
    }

    private static void Span(EcsWorld ecs, string text, AssetHandle font, int size, Color color)
    {
        var span = ecs.Spawn();
        ecs.Insert<TextSpanRef>(span).Value = text;
        var style = ecs.Insert<TextFontRef>(span);
        style.Font = new FontSource.Handle(font);
        style.FontSize = new FontSize.Px(size);
        ecs.Insert<TextColorRef>(span).Value = color;
        ecs.SetParent(span, _text);
    }

    // Changing the bounds has the text laid out again.
    private static void UpdateTextBounds(BehaviorContext ctx) =>
        ctx.Ecs.Wrap<TextBoundsRef>(_text).Width = (1f + MathF.Sin(ctx.Time.Elapsed)) * 600f;
}
