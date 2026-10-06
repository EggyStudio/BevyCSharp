// Bevy's overflow_clip_margin example, examples/ui/scroll_and_overflow/overflow_clip_margin.rs at
// v0.19.1, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Interface;

// Shows where a frame that clips its contents cuts them, at its border with a margin past it, at
// its border, inside its padding, and at its content, a logo too large for each frame showing how
// much of it is left.
internal static class OverflowClipMargin
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var logo = AssetServer.Load(AssetKind.Image, "branding/icon.png");

        var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center, RowGap = Length.Px(40f), Direction = UiDirection.Column, Color = Color.FromSrgb8(250, 235, 215) });
        foreach (var (box, margin) in new[] { (UiClipBox.Border, 25f), (UiClipBox.Border, 0f), (UiClipBox.Padding, 0f), (UiClipBox.Content, 0f) })
        {
            var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, ColumnGap = Length.Px(20f) });
            ecs.SetParent(row, root);

            // The setting as Bevy's pretty debug output prints it.
            var labelBox = Ui.SpawnNode(new UiSettings { Padding = Sides.All(Length.Px(10f)), Margin = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(25f)), Color = Color.FromSrgb(0.25f, 0.25f, 0.25f) });
            ecs.SetParent(labelBox, row);
            var name = box switch { UiClipBox.Border => "BorderBox", UiClipBox.Padding => "PaddingBox", _ => "ContentBox" };
            ecs.SetParent(Ui.SpawnText(FormattableString.Invariant($"OverflowClipMargin {{\n    visual_box: {name},\n    margin: {margin:0.0},\n}}"), new UiSettings()), labelBox);

            var frame = Ui.SpawnNode(new UiSettings
            {
                Margin = new Sides(Length.Zero, Length.Px(10f), Length.Zero, Length.Zero),
                Width = Length.Px(100f),
                Height = Length.Px(100f),
                Padding = Sides.All(Length.Px(20f)),
                Border = Sides.All(Length.Px(5f)),
                OverflowX = UiOverflow.Clip,
                OverflowY = UiOverflow.Clip,
                ClipBox = box,
                ClipMargin = margin,
                Color = Color.FromSrgb8(128, 128, 128),
                BorderColor = (0f, 0f, 0f, 1f),
            });
            ecs.SetParent(frame, row);

            var cyan = Ui.SpawnNode(new UiSettings { MinWidth = Length.Px(50f), MinHeight = Length.Px(50f), Color = Color.FromSrgb8(224, 255, 255) });
            ecs.SetParent(cyan, frame);
            var image = Ui.SpawnNode(new UiSettings { MinWidth = Length.Px(100f), MinHeight = Length.Px(100f) });
            Ui.SetImage(image, logo);
            ecs.SetParent(image, cyan);
        }
    }, "overflow_clip_margin.Setup");
}
