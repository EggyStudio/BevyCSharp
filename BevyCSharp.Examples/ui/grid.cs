// Bevy's grid example, examples/ui/layout/grid.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Interface;

// Demonstrates grid layout, a header across the top, a square of sixteen colored cells, a sidebar
// of wrapped text and a footer, with a hidden panel laid over them.
internal static class Grid
{
    // Bevy's window for it, 800 by 600, which its square of cells is sized to fill.
    public static void Configure(Config config) => (config.Width, config.Height) = (800u, 600u);

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

        var root = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, Width = Length.Percent(100f), Height = Length.Percent(100f), Color = (1f, 1f, 1f, 1f) });
        UiGrid.Set(root, new GridSettings { Columns = [Track.MinContent, Track.Flex(1f)], Rows = [Track.Auto, Track.Flex(1f), Track.Px(20f)] });

        var header = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, Padding = Sides.All(Length.Px(6f)) });
        UiGrid.Place(header, new GridPlacement { ColumnSpan = 2 });
        ecs.SetParent(header, root);
        ecs.SetParent(Ui.SpawnText("Bevy CSS Grid Layout Example", new UiSettings { Color = (0f, 0f, 0f, 1f) }, new UiTextSettings { Font = font }), header);

        // Square, as tall as the row it is in.
        var square = Ui.SpawnNode(new UiSettings
        {
            Height = Length.Percent(100f),
            AspectRatio = 1f,
            Display = UiDisplay.Grid,
            Padding = Sides.All(Length.Px(24f)),
            RowGap = Length.Px(12f),
            ColumnGap = Length.Px(12f),
            Color = Color.FromSrgb(0.25f, 0.25f, 0.25f),
        });
        UiGrid.Set(square, new GridSettings { Columns = [Track.Flex(1f).Repeated(4)], Rows = [Track.Flex(1f).Repeated(4)] });
        ecs.SetParent(square, root);
        foreach (var (r, g, b) in new (byte, byte, byte)[]
        {
            (255, 165, 0), (255, 228, 196), (0, 0, 255), (220, 20, 60), (0, 255, 255), (255, 69, 0), (0, 100, 0), (255, 0, 255),
            (0, 128, 128), (240, 248, 255), (220, 20, 60), (250, 235, 215), (255, 255, 0), (255, 20, 147), (154, 205, 50), (250, 128, 114),
        })
        {
            var frame = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, Padding = Sides.All(Length.Px(3f)), Color = (0f, 0f, 0f, 1f) });
            ecs.SetParent(frame, square);
            ecs.SetParent(Ui.SpawnNode(new UiSettings { Color = Color.FromSrgb8(r, g, b) }), frame);
        }

        var sidebar = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, Align = UiAlign.Start, Padding = Sides.All(Length.Px(10f)), RowGap = Length.Px(10f), Color = (0f, 0f, 0f, 1f) });
        UiGrid.Set(sidebar, new GridSettings { Rows = [Track.Auto, Track.Auto, Track.Fr(1f)], Align = CellAlign.Center });
        ecs.SetParent(sidebar, root);
        ecs.SetParent(Ui.SpawnText("Sidebar", new UiSettings(), new UiTextSettings { Font = font }), sidebar);
        ecs.SetParent(Ui.SpawnText(string.Concat(Enumerable.Repeat("A paragraph of text which ought to wrap nicely. ", 7)).TrimEnd(), new UiSettings(), new UiTextSettings { Font = font, FontSize = 13f }), sidebar);
        ecs.SetParent(Ui.SpawnNode(new UiSettings()), sidebar);

        var footer = Ui.SpawnNode(new UiSettings { Color = (1f, 1f, 1f, 1f) });
        UiGrid.Place(footer, new GridPlacement { ColumnSpan = 2 });
        ecs.SetParent(footer, root);

        // A panel over the rest, hidden as Bevy's is, to show an absolute node in a grid.
        var overlay = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Margin = new Sides(Length.Auto, Length.Px(100f), Length.Auto, Length.Auto),
            Width = Length.Percent(60f),
            Height = Length.Px(300f),
            MaxWidth = Length.Px(600f),
            Color = (1f, 1f, 1f, 0.8f),
        });
        ecs.Add(overlay, Visibility.Hidden);
        ecs.SetParent(overlay, root);
    }, "grid.Setup");
}
