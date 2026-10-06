// Bevy's anchor_layout example, examples/ui/layout/anchor_layout.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Interface;

// Shows nine ways to anchor a label in its cell, by its distance from an edge or by margins that
// center it, on a grid of three by three.
internal static class AnchorLayout
{
    private static Sides AutoHorizontal => new(Length.Auto, Length.Zero, Length.Auto, Length.Zero);
    private static Sides AutoVertical => new(Length.Zero, Length.Auto, Length.Zero, Length.Auto);

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        var px10 = Length.Px(10f);

        var grid = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Padding = Sides.All(Length.Px(12f)),
            RowGap = Length.Px(12f),
            ColumnGap = Length.Px(12f),
            Display = UiDisplay.Grid,
            Color = (0f, 0f, 0f, 1f),
        });
        UiGrid.Set(grid, new GridSettings { Columns = [Track.Fr(1f), Track.Fr(1f), Track.Fr(1f)], Rows = [Track.Fr(1f), Track.Fr(1f), Track.Fr(1f)] });

        foreach (var (label, place) in new (string, UiSettings)[]
        {
            ("left: 10px\ntop: 10px", new UiSettings { Left = px10, Top = px10 }),
            ("center: 10px\ntop: 10px", new UiSettings { Margin = AutoHorizontal, Top = px10 }),
            ("right: 10px\ntop: 10px", new UiSettings { Right = px10, Top = px10 }),
            ("left: 10px\ncenter: 10px", new UiSettings { Left = px10, Margin = AutoVertical }),
            ("center: 10px\ncenter: 10px", new UiSettings { Margin = Sides.All(Length.Auto) }),
            ("right: 10px\ncenter: 10px", new UiSettings { Right = px10, Margin = AutoVertical }),
            ("left: 10px\nbottom: 10px", new UiSettings { Left = px10, Bottom = px10 }),
            ("center: 10px\nbottom: 10px", new UiSettings { Margin = AutoHorizontal, Bottom = px10 }),
            ("right: 10px\nbottom: 10px", new UiSettings { Right = px10, Bottom = px10 }),
        })
        {
            var cell = Ui.SpawnNode(new UiSettings { Color = Color.FromSrgb(0.25f, 0.25f, 0.25f) });
            ecs.SetParent(cell, grid);

            place.Display = UiDisplay.Block;
            place.Absolute = true;
            place.Padding = new Sides(Length.Px(5f), Length.Px(1f), Length.Px(5f), Length.Px(1f));
            place.Color = Color.FromSrgb(1f, 0.066f, 0.349f);
            var tag = Ui.SpawnNode(place);
            ecs.SetParent(tag, cell);
            ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = (0f, 0f, 0f, 1f) }, new UiTextSettings { Font = font }), tag);
        }
    }, "anchor_layout.Setup");
}
