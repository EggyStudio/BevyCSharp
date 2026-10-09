// Bevy's text_debug example, examples/ui/text/text_debug.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows text laid out and justified every way, wrapped by a width, broken by hand and fully
// justified, around the window's corners, with a block at the bottom right that reports the frame
// rate as it runs and holds runs of zero and negative size.
internal static class TextDebug
{
    // Bevy's Locals of change_text, the times of the last frames and the rates worked out from them.
    internal static readonly List<double> Times = [];
    internal static readonly List<double> Rates = [];

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Times.Clear();
        Rates.Clear();
        Render2d.SpawnCamera2d();
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        var maroon = Color.FromSrgb(128f / 255f, 0f, 0f);

        var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.SpaceBetween });
        var margin = new Sides(Length.Px(15f), Length.Px(5f), Length.Px(15f), Length.Px(5f));
        var left = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Justify = UiJustify.SpaceBetween, Align = UiAlign.Start, Grow = 1f, Margin = margin });
        var right = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Justify = UiJustify.SpaceBetween, Align = UiAlign.End, Grow = 1f, Margin = margin });
        ecs.SetParent(left, root);
        ecs.SetParent(right, root);

        Entity Block(Entity parent, string text, float size, (float R, float G, float B, float A) color, TextJustify justify = TextJustify.Left, float maxWidth = 0f)
        {
            var block = Ui.SpawnText(text, new UiSettings { Color = color, MaxWidth = maxWidth > 0f ? Length.Px(maxWidth) : Length.Auto }, new UiTextSettings { Font = font, FontSize = size, Justify = justify });
            ecs.Insert<BackgroundColorRef>(block).Value = maroon;
            ecs.SetParent(block, parent);
            return block;
        }

        var white = (1f, 1f, 1f, 1f);
        var yellow = Color.FromSrgb8(255, 255, 0);
        Block(left, "This is\ntext with\nline breaks\nin the top left.", 25f, white);
        Block(left, "This text is right-justified. The `Justify` component controls the horizontal alignment of the lines of multi-line text relative to each other, and does not affect the text node's position in the UI layout.", 25f, yellow, TextJustify.Right, 300f);
        Block(left, "This\ntext has\nline breaks and also a set width in the bottom left.", 25f, white, maxWidth: 300f);

        Block(right, "This text is very long, has a limited width, is center-justified, is positioned in the top right and is also colored pink.", 33f, Color.FromSrgb(0.8f, 0.2f, 0.7f), TextJustify.Center, 400f);
        Block(right, "This text is left-justified and is vertically positioned to distribute the empty space equally above and below it.", 29f, yellow, TextJustify.Left, 300f);
        Block(right, "This text is fully justified and is positioned in the same way.", 29f, Color.FromSrgb8(173, 255, 47), TextJustify.Justified, 300f);

        // The block whose runs change as it runs, in sizes from nothing to below nothing.
        var changes = Block(right, string.Empty, 21f, white);
        void Span(string text, float size, (float R, float G, float B, float A) color) => Ui.SpawnTextSpan(changes, text, new UiTextSettings { Font = font, FontSize = size }, color);
        Span("\nThis text changes in the bottom right", 21f, white);
        Span(" this text has zero font size", 0f, Color.FromSrgb8(0, 0, 255));
        Span("\nThis text changes in the bottom right - ", 21f, Color.FromSrgb8(255, 0, 0));
        Span(string.Empty, 21f, Color.FromSrgb8(255, 69, 0));
        Span(" fps, ", 10f, yellow);
        Span(string.Empty, 21f, Color.FromSrgb8(0, 255, 0));
        Span(" ms/frame", 42f, Color.FromSrgb8(0, 0, 255));
        Span(" this text has negative font size", -42f, Color.FromSrgb8(0, 0, 255));
        ecs.Add(changes, new TextChanges());
    }, "text_debug.Setup");
}

/// <summary>The block whose runs change as the example runs.</summary>
[Behavior]
public partial struct TextChanges
{
    /// <summary>
    /// The rates written into the block and its spans each frame, each run by its place, the block
    /// itself first and its spans after it, as Bevy's text writer counts them.
    /// </summary>
    [OnUpdate]
    public void ChangeText(BehaviorContext ctx)
    {
        var (ecs, time) = (ctx.Ecs, ctx.Time);
        var (times, rates) = (TextDebug.Times, TextDebug.Rates);

        // The average rate over the last 120 frames, and how much that average has varied.
        times.Insert(0, time.ElapsedSeconds);
        if (times.Count > 120) times.RemoveAt(times.Count - 1);
        var average = times.Count / Math.Max(times[0] - times[^1], 0.0001);
        rates.Insert(0, average);
        if (rates.Count > 120) rates.RemoveAt(rates.Count - 1);
        var mean = rates.Average();
        var deviation = Math.Sqrt(rates.Sum(rate => (mean - rate) * (mean - rate)) / rates.Count);

        // Bevy's frame time diagnostics, smoothed, as this engine's own clock keeps them, the
        // frame time in milliseconds as the diagnostic gives it.
        var fps = time.SmoothedFps;
        var frameTime = time.DeltaSeconds * 1000.0;
        var spans = ecs.ChildrenOf(ctx.Entity);
        Ui.SetText(ctx.Entity, FormattableString.Invariant($"{average:0.0} avg fps, {deviation:0.0} frametime variance"));
        ecs.Wrap<TextSpanRef>(spans[2]).Value = FormattableString.Invariant($"\nThis text changes in the bottom right - {fps:0.0} fps, {frameTime:0.000} ms/frame");
        ecs.Wrap<TextSpanRef>(spans[3]).Value = FormattableString.Invariant($"{fps:0.0}");
        ecs.Wrap<TextSpanRef>(spans[5]).Value = FormattableString.Invariant($"{frameTime:0.000}");
    }
}
