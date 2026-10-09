// Bevy's scrollbars example, examples/ui/scroll_and_overflow/scrollbars.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows Bevy's scrollbar widgets beside and below a list that scrolls, each moving the list as its
// thumb is dragged or its track clicked, the thumb white while the pointer is on it or drags it.
internal static class Scrollbars
{
    private static readonly Color Gray1 = Color.FromSrgb(0.224f, 0.224f, 0.243f);
    private static readonly Color Gray2 = Color.FromSrgb(0.486f, 0.486f, 0.529f);
    private static readonly Color Gray3 = Color.FromSrgb(0.71f, 0.71f, 0.772f);

    private static readonly List<Entity> Thumbs = [];

    public static void Build(App app)
    {
        app.Startup(Setup, "scrollbars.SetupViewRoot");
        app.Update(UpdateScrollbarThumb, "scrollbars.UpdateScrollbarThumb");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Thumbs.Clear();
        if (ecs.Resource<UiScaleRef>() is { } scale) scale.Value = 1.25f;
        var camera = Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(0f),
            Top = Length.Px(0f),
            Right = Length.Px(0f),
            Bottom = Length.Px(0f),
            Direction = UiDirection.Column,
            Padding = Sides.All(Length.Px(3f)),
            RowGap = Length.Px(6f),
            Color = Color.FromSrgb(0.1f, 0.1f, 0.1f),
            Camera = camera,
        });
        ecs.Insert<TabGroupRef>(root);
        ecs.SetParent(Ui.SpawnText("Scrolling", new UiSettings()), root);

        // A grid of the list and its two scrollbars, the list taking what the bars leave.
        var demo = Ui.SpawnNode(new UiSettings { Width = Length.Px(200f), Height = Length.Px(150f), RowGap = Length.Px(2f), ColumnGap = Length.Px(2f) });
        UiGrid.Set(demo, new GridSettings { Columns = [Track.Flex(1f), Track.Auto], Rows = [Track.Flex(1f), Track.Auto] });
        ecs.SetParent(demo, root);

        var list = Ui.SpawnNode(new UiSettings
        {
            Direction = UiDirection.Column,
            Padding = Sides.All(Length.Px(4f)),
            OverflowX = UiOverflow.Scroll,
            OverflowY = UiOverflow.Scroll,
            Color = (Gray1.R, Gray1.G, Gray1.B, 1f),
        });
        ecs.SetParent(list, demo);
        foreach (var caption in new[]
        {
            "Alpha Wolf", "Beta Blocker", "Delta Sleep", "Gamma Ray", "Epsilon Eridani", "Zeta Function",
            "Lambda Calculus", "Nu Metal", "Pi Day", "Chi Pants", "Psi Powers", "Omega Fatty Acid",
        })
        {
            ecs.SetParent(Ui.SpawnText(caption, new UiSettings(), new UiTextSettings { FontSize = 14f }), list);
        }
        Ui.SetScroll(list, 0f, 10f);

        foreach (var vertical in new[] { true, false })
        {
            var bar = Ui.SpawnNode(new UiSettings());
            ecs.Wrap<NodeRef>(bar).MinWidth = vertical ? new Val.Px(8f) : new Val.Auto();
            ecs.Wrap<NodeRef>(bar).MinHeight = vertical ? new Val.Auto() : new Val.Px(8f);
            UiGrid.Place(bar, new GridPlacement { Row = vertical ? 1 : 2, Column = vertical ? 2 : 1 });
            var scrollbar = ecs.Insert<ScrollbarRef>(bar);
            (scrollbar.Target, scrollbar.MinThumbLength) = (list, 8f);
            scrollbar.Orientation = vertical ? ScrollbarRef.OrientationVariant.Vertical : ScrollbarRef.OrientationVariant.Horizontal;
            ecs.SetParent(bar, demo);

            var thumb = Ui.SpawnNode(new UiSettings { Color = (Gray2.R, Gray2.G, Gray2.B, 1f), BorderColor = (Gray3.R, Gray3.G, Gray3.B, 1f) });
            ecs.Insert<HoveredRef>(thumb);
            var style = ecs.Insert<ScrollbarThumbRef>(thumb);
            style.BorderRadiusTopLeftX = style.BorderRadiusTopRightX = style.BorderRadiusBottomLeftX = style.BorderRadiusBottomRightX = new Val.Px(4f);
            style.BorderLeft = style.BorderTop = style.BorderRight = style.BorderBottom = new Val.Px(1f);
            ecs.SetParent(thumb, bar);
            Thumbs.Add(thumb);
        }
    }

    private static void UpdateScrollbarThumb(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var thumb in Thumbs)
        {
            var active = ecs.Wrap<HoveredRef>(thumb).Value || (ecs.Get<ScrollbarDragStateRef>(thumb)?.Dragging ?? false);
            var color = active ? Color.White : Gray2;
            var background = ecs.Wrap<BackgroundColorRef>(thumb);
            if (background.Value != color) background.Value = color;
        }
    }
}
