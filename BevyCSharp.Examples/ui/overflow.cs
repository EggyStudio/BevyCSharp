// Bevy's overflow example, examples/ui/scroll_and_overflow/overflow.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Simple example demonstrating overflow, four frames each holding a logo larger than itself and
// letting it spill out, cutting it off across, down, or both, the logo outlined red while the pointer
// is over it.
internal static class OverflowExample
{

    private static readonly List<Entity> Logos = [];
    private static readonly Dictionary<Entity, bool> Last = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Logos.Clear();
            Last.Clear();
            Render2d.SpawnCamera2d();
            var logo = AssetServer.Load(AssetKind.Image, "branding/icon.png");

            var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center, Color = Color.FromSrgb8(250, 235, 215) });
            foreach (var (x, y) in new[] { (UiOverflow.Visible, UiOverflow.Visible), (UiOverflow.Clip, UiOverflow.Visible), (UiOverflow.Visible, UiOverflow.Clip), (UiOverflow.Clip, UiOverflow.Clip) })
            {
                var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Center, Margin = Sides.Horizontal(Length.Px(25f)) });
                ecs.SetParent(column, root);

                // Bevy's label is the overflow as its pretty debug output prints it.
                var labelBox = Ui.SpawnNode(new UiSettings { Padding = Sides.All(Length.Px(10f)), Margin = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(25f)), Color = Color.FromSrgb(0.25f, 0.25f, 0.25f) });
                ecs.SetParent(labelBox, column);
                ecs.SetParent(Ui.SpawnText($"Overflow {{\n    x: {x},\n    y: {y},\n}}", new UiSettings()), labelBox);

                var frame = Ui.SpawnNode(new UiSettings
                {
                    Width = Length.Px(100f),
                    Height = Length.Px(100f),
                    Padding = new Sides(Length.Px(25f), Length.Px(25f), Length.Zero, Length.Zero),
                    Border = Sides.All(Length.Px(5f)),
                    OverflowX = x,
                    OverflowY = y,
                    BorderColor = (0f, 0f, 0f, 1f),
                    Color = Color.FromSrgb8(128, 128, 128),
                });
                ecs.SetParent(frame, column);

                var image = Ui.SpawnNode(new UiSettings { MinWidth = Length.Px(100f), MinHeight = Length.Px(100f) });
                Ui.SetImage(image, logo);
                ecs.Insert<HoveredRef>(image);
                var outline = ecs.Insert<OutlineRef>(image);
                (outline.Width, outline.Offset, outline.Color) = (new Val.Px(2f), new Val.Px(2f), new Color(0f, 0f, 0f, 0f));
                ecs.SetParent(image, frame);
                Logos.Add(image);
            }
        }, "overflow.Setup");

        app.Update(ctx =>
        {
            // Bevy's update_outlines, as an image's Hovered changes.
            foreach (var image in Logos)
            {
                var hovered = ctx.Ecs.Get<HoveredRef>(image)?.Value == true;
                if (Last.TryGetValue(image, out var last) && last == hovered) continue;
                Last[image] = hovered;
                ctx.Ecs.Wrap<OutlineRef>(image).Color = hovered ? new Color(1f, 0f, 0f) : new Color(0f, 0f, 0f, 0f);
            }
        }, "overflow.UpdateOutlines");
    }
}
