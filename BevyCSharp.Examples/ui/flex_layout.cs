using Bevy;

namespace BevyCSharp.Examples.Interface;

// Shows every pairing of five ways to align a node's children across with six ways to spread them
// along, each cell labeled with the two it uses.
internal static class FlexLayout
{
    private static readonly (float R, float G, float B, float A) AlignColor = Scene.Srgb(1f, 0.066f, 0.349f);
    private static readonly (float R, float G, float B, float A) JustifyColor = Scene.Srgb(0.102f, 0.522f, 1f);

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        var margin = Length.Px(12f);

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Direction = UiDirection.Column,
            Align = UiAlign.Center,
            Padding = Sides.All(margin),
            RowGap = margin,
            Color = (0f, 0f, 0f, 1f),
        });

        var legend = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row });
        ecs.SetParent(legend, root);
        Label(ecs, legend, font, AlignColor, new Sides(Length.Zero, Length.Zero, margin, Length.Zero), "AlignItems");
        Label(ecs, legend, font, JustifyColor, Sides.None, "JustifyContent");

        var rows = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Direction = UiDirection.Column, RowGap = margin });
        ecs.SetParent(rows, root);

        foreach (var align in new[] { UiAlign.Baseline, UiAlign.FlexStart, UiAlign.Center, UiAlign.FlexEnd, UiAlign.Stretch })
        {
            var row = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Direction = UiDirection.Row, ColumnGap = margin });
            ecs.SetParent(row, rows);

            foreach (var justify in new[] { UiJustify.FlexStart, UiJustify.Center, UiJustify.FlexEnd, UiJustify.SpaceEvenly, UiJustify.SpaceAround, UiJustify.SpaceBetween })
            {
                var cell = Ui.SpawnNode(new UiSettings
                {
                    Direction = UiDirection.Column,
                    Align = align,
                    Justify = justify,
                    Width = Length.Percent(100f),
                    Height = Length.Percent(100f),
                    Color = Scene.Srgb(0.25f, 0.25f, 0.25f),
                });
                ecs.SetParent(cell, row);
                Label(ecs, cell, font, AlignColor, Sides.None, $"{align}");
                Label(ecs, cell, font, JustifyColor, new Sides(Length.Zero, Length.Px(3f), Length.Zero, Length.Zero), $"{justify}");
            }
        }
    }, "flex_layout.Setup");

    // Black text on a colored tag, the names being the same as Bevy's own.
    private static void Label(EcsWorld ecs, Entity parent, AssetHandle font, (float R, float G, float B, float A) color, Sides margin, string text)
    {
        var tag = Ui.SpawnNode(new UiSettings { Margin = margin, Padding = new Sides(Length.Px(5f), Length.Px(1f), Length.Px(5f), Length.Px(1f)), Color = color });
        ecs.SetParent(tag, parent);
        ecs.SetParent(Ui.SpawnText(text, new UiSettings { Color = (0f, 0f, 0f, 1f) }, new UiTextSettings { Font = font }), tag);
    }
}
