using Bevy;

namespace BevyCSharp.Examples.Interface;

// Demonstrates borders on each side and every combination of them, a color to a side and a white
// outline around each, drawn square and then rounded where two bordered sides meet.
internal static class Borders
{
    private const string BorderColor = "bevy_ui::ui_node::BorderColor";
    private const string Outline = "bevy_ui::ui_node::Outline";
    private const string Node = "bevy_ui::ui_node::Node";

    private static readonly string[] Labels =
    [
        "None", "All", "Left", "Right", "Top", "Bottom", "Horizontal", "Vertical",
        "Top Left", "Bottom Left", "Top Right", "Bottom Right",
        "Top Bottom Right", "Top Bottom Left", "Top Left Right", "Bottom Left Right",
    ];

    // Left, top, right and bottom, in pixels.
    private static readonly (float L, float T, float R, float B)[] Widths =
    [
        (0, 0, 0, 0), (10, 10, 10, 10), (10, 0, 0, 0), (0, 0, 10, 0), (0, 10, 0, 0), (0, 0, 0, 10), (10, 0, 10, 0), (0, 10, 0, 10),
        (20, 10, 0, 0), (10, 0, 0, 20), (0, 10, 20, 0), (0, 0, 10, 10),
        (0, 20, 10, 10), (10, 10, 0, 10), (20, 10, 10, 0), (10, 0, 10, 20),
    ];

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings { Margin = Sides.All(Length.Px(25f)), Direction = UiDirection.Column, AlignSelf = UiAlignSelf.Stretch, Color = Scene.Srgb(0.25f, 0.25f, 0.25f) });
        ecs.SetVariant(root, Node, ".justify_self", "Stretch");

        Heading(ecs, root, "Borders");
        Examples(ecs, root, rounded: false);
        Heading(ecs, root, "Borders Rounded");
        Examples(ecs, root, rounded: true);
    }, "borders.Setup");

    private static void Examples(EcsWorld ecs, Entity root, bool rounded)
    {
        var wrap = Ui.SpawnNode(new UiSettings { Margin = Sides.All(Length.Px(25f)), Wrap = UiWrap.Wrap });
        ecs.SetParent(wrap, root);

        for (var i = 0; i < Labels.Length; i++)
        {
            var (l, t, r, b) = Widths[i];
            var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Center });
            ecs.SetParent(column, wrap);

            // Rounded where both sides meeting at a corner have a border, as Bevy's border_size does.
            var round = Length.Px(1_000_000f);
            Length Corner(float a, float c) => rounded && a > 0f && c > 0f ? round : Length.Zero;
            var box = Ui.SpawnNode(new UiSettings
            {
                Width = Length.Px(50f),
                Height = Length.Px(50f),
                Border = new Sides(Length.Px(l), Length.Px(t), Length.Px(r), Length.Px(b)),
                Margin = Sides.All(Length.Px(20f)),
                Align = UiAlign.Center,
                Justify = UiJustify.Center,
                Corners = new Corners(Corner(l, t), Corner(r, t), Corner(r, b), Corner(l, b)),
                Color = Scene.Srgb8(128, 0, 0),
            });
            ecs.SetReflectedColor(box, BorderColor, ".top", new Color(1f, 0f, 0f));
            ecs.SetReflectedColor(box, BorderColor, ".bottom", new Color(1f, 1f, 0f));
            ecs.SetReflectedColor(box, BorderColor, ".left", Color.FromSrgb(0f, 128f / 255f, 0f));
            ecs.SetReflectedColor(box, BorderColor, ".right", new Color(0f, 0f, 1f));
            ecs.InsertReflected(box, Outline);
            ecs.SetReflected(box, Outline, ".width", "{\"Px\":6.0}");
            ecs.SetReflected(box, Outline, ".offset", "{\"Px\":6.0}");
            ecs.SetReflectedColor(box, Outline, ".color", Color.White);
            ecs.SetParent(box, column);

            var dot = Ui.SpawnNode(new UiSettings { Width = Length.Px(10f), Height = Length.Px(10f), Corners = rounded ? Corners.All(round) : Corners.None, Color = (1f, 1f, 0f, 1f) });
            ecs.SetParent(dot, box);
            ecs.SetParent(Ui.SpawnText(Labels[i], new UiSettings(), 9f), column);
        }
    }

    private static void Heading(EcsWorld ecs, Entity root, string text)
    {
        var node = Ui.SpawnNode(new UiSettings { Margin = Sides.All(Length.Px(25f)) });
        ecs.SetParent(node, root);
        ecs.SetParent(Ui.SpawnText(text, new UiSettings(), 20f), node);
    }
}
