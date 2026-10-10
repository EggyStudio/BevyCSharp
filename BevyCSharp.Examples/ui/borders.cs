// Bevy's borders example, examples/ui/styling/borders.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates borders on each side and every combination of them, a color to a side and a white
// outline around each, drawn square and then rounded where two bordered sides meet, the rounded
// ones followed by four with elliptical corners, a radius across and another down at each.
internal static class Borders
{

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

    // Bevy's elliptical_border_radii, across and down at the top left, the top right, the bottom
    // right and the bottom left.
    private static readonly (string Label, Val[] Radii)[] Ellipses =
    [
        ("Ellipse Wide", [Px(25), Px(8), Px(25), Px(8), Px(25), Px(8), Px(25), Px(8)]),
        ("Ellipse Tall", [Px(8), Px(25), Px(8), Px(25), Px(8), Px(25), Px(8), Px(25)]),
        ("Ellipse Mixed", [Px(25), Px(12), Px(12), Px(25), Px(25), Px(12), Px(12), Px(25)]),
        ("Ellipse Percent", [Pc(50), Pc(20), Pc(20), Pc(50), Pc(50), Pc(20), Pc(20), Pc(50)]),
    ];

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings { Margin = Sides.All(Length.Px(25f)), Direction = UiDirection.Column, AlignSelf = UiAlignSelf.Stretch, Color = Color.FromSrgb(0.25f, 0.25f, 0.25f) });
        ecs.Wrap<NodeRef>(root).JustifySelf = NodeRef.JustifySelfVariant.Stretch;

        Heading(ecs, root, "Borders");
        Examples(ecs, root, rounded: false);
        Heading(ecs, root, "Borders Rounded");
        Examples(ecs, root, rounded: true);
    }, "borders.Setup");

    private static void Examples(EcsWorld ecs, Entity root, bool rounded)
    {
        var wrap = Ui.SpawnNode(new UiSettings { Margin = Sides.All(Length.Px(25f)), Wrap = UiWrap.Wrap });
        ecs.SetParent(wrap, root);

        // Rounded where both sides meeting at a corner have a border, as Bevy's border_size does.
        var round = Length.Px(1_000_000f);
        for (var i = 0; i < Labels.Length; i++)
        {
            var (l, t, r, b) = Widths[i];
            Length Corner(float a, float c) => rounded && a > 0f && c > 0f ? round : Length.Zero;
            Example(ecs, wrap, Labels[i], (l, t, r, b), new Corners(Corner(l, t), Corner(r, t), Corner(r, b), Corner(l, b)), rounded);
        }

        if (!rounded) return;

        foreach (var (label, radii) in Ellipses)
        {
            var node = ecs.Wrap<NodeRef>(Example(ecs, wrap, label, (10, 10, 10, 10), Corners.None, rounded: true));
            (node.BorderRadiusTopLeftX, node.BorderRadiusTopLeftY) = (radii[0], radii[1]);
            (node.BorderRadiusTopRightX, node.BorderRadiusTopRightY) = (radii[2], radii[3]);
            (node.BorderRadiusBottomRightX, node.BorderRadiusBottomRightY) = (radii[4], radii[5]);
            (node.BorderRadiusBottomLeftX, node.BorderRadiusBottomLeftY) = (radii[6], radii[7]);
        }
    }

    // A box of borders under its label, a color to a side, a white outline and a yellow dot in it.
    private static Entity Example(EcsWorld ecs, Entity wrap, string label, (float L, float T, float R, float B) widths, Corners corners, bool rounded)
    {
        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Center });
        ecs.SetParent(column, wrap);

        var (l, t, r, b) = widths;
        var box = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(50f),
            Height = Length.Px(50f),
            Border = new Sides(Length.Px(l), Length.Px(t), Length.Px(r), Length.Px(b)),
            Margin = Sides.All(Length.Px(20f)),
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            Corners = corners,
            Color = Color.FromSrgb8(128, 0, 0),
        });
        var border = ecs.Wrap<BorderColorRef>(box);
        border.Top = new Color(1f, 0f, 0f);
        border.Bottom = new Color(1f, 1f, 0f);
        border.Left = Color.FromSrgb(0f, 128f / 255f, 0f);
        border.Right = new Color(0f, 0f, 1f);
        var outline = ecs.Insert<OutlineRef>(box);
        (outline.Width, outline.Offset, outline.Color) = (new Val.Px(6f), new Val.Px(6f), Color.White);
        ecs.SetParent(box, column);

        var dot = Ui.SpawnNode(new UiSettings { Width = Length.Px(10f), Height = Length.Px(10f), Corners = rounded ? Corners.All(Length.Px(1_000_000f)) : Corners.None, Color = (1f, 1f, 0f, 1f) });
        ecs.SetParent(dot, box);
        ecs.SetParent(Ui.SpawnText(label, new UiSettings(), 9f), column);
        return box;
    }

    private static Val Px(float value) => new Val.Px(value);

    private static Val Pc(float value) => new Val.Percent(value);

    private static void Heading(EcsWorld ecs, Entity root, string text)
    {
        var node = Ui.SpawnNode(new UiSettings { Margin = Sides.All(Length.Px(25f)) });
        ecs.SetParent(node, root);
        ecs.SetParent(Ui.SpawnText(text, new UiSettings(), 20f), node);
    }
}
