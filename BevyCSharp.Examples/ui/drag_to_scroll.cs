// Bevy's drag_to_scroll example, examples/ui/scroll_and_overflow/drag_to_scroll.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Scrolling a node by dragging it, at half the interface's scale. A board of tiles larger than the
// window, every other one colored by where it is, turns red under the pointer, and a drag anywhere
// moves the board with the pointer.
internal static class DragToScroll
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        const int W = 60, H = 40;
        Render2d.SpawnCamera2d();
        if (ecs.Resource<UiScaleRef>() is { } scale) scale.Value = 0.5f;

        // The window's whole size, scrolled by the drag.
        var scrolled = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), OverflowX = UiOverflow.Scroll, OverflowY = UiOverflow.Scroll });
        ecs.Insert<ScrollPositionRef>(scrolled);
        ecs.Add(scrolled, new ScrollableNode());
        ecs.Add(scrolled, new ScrollStart());

        ecs.Observe<Pointer<Drag>>(scrolled, on =>
        {
            var uiScale = on.Ecs.Resource<UiScaleRef>()?.Value ?? 1f;
            var start = on.Ecs.GetOrDefault<ScrollStart>(on.Entity).Start;
            var distance = on.Event.Event.Distance;
            var at = new Vec2(start.X - distance.X / uiScale, start.Y - distance.Y / uiScale);
            on.Ecs.Wrap<ScrollPositionRef>(on.Entity).Value = new Vec2(MathF.Max(at.X, 0f), MathF.Max(at.Y, 0f));
        });
        ecs.Observe<Pointer<DragStart>>(scrolled, on =>
        {
            var computed = on.Ecs.Wrap<ComputedNodeRef>(on.Entity);
            var scroll = computed.ScrollPosition;
            on.Ecs.Set(on.Entity, new ScrollStart { Start = new Vec2(scroll.X * computed.InverseScaleFactor, scroll.Y * computed.InverseScaleFactor) });
        });

        // The board, its rows and columns a hundred pixels each as Bevy's lays them, which is picked
        // through to its tiles and hides what is under it.
        var board = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid });
        UiGrid.Set(board, new GridSettings { Rows = [Track.Px(100f).Repeated(W)], Columns = [Track.Px(100f).Repeated(H)] });
        var blocking = ecs.Insert<PickableRef>(board);
        (blocking.IsHoverable, blocking.ShouldBlockLower) = (false, true);
        ecs.SetParent(board, scrolled);

        for (var y = 0; y < H; y++)
        for (var x = 0; x < W; x++)
        {
            var color = (x + y) % 2 == 1
                ? Color.FromHsl((float)x / W * 270f + (float)y / H * 90f, 1f, 0.5f)
                : Color.FromSrgb(0f, 0f, 0f);

            var tile = Ui.SpawnNode(new UiSettings { Color = color });
            UiGrid.Place(tile, new GridPlacement { Row = y + 1, Column = x + 1 });
            var pickable = ecs.Insert<PickableRef>(tile);
            (pickable.ShouldBlockLower, pickable.IsHoverable) = (false, true);
            ecs.Add(tile, new TileColor { Color = color });
            ecs.SetParent(tile, board);

            ecs.Observe<Pointer<Over>>(tile, on => on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = Color.FromSrgb(1f, 0f, 0f));
            ecs.Observe<Pointer<Out>>(tile, on => on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = on.Ecs.GetOrDefault<TileColor>(on.Entity).Color);
        }
    }, "drag_to_scroll.Setup");
}

/// <summary>The node a drag scrolls.</summary>
[Behavior]
public partial struct ScrollableNode;

/// <summary>A tile's own color, which it goes back to when the pointer leaves it.</summary>
[Behavior]
public partial struct TileColor
{
    /// <summary>Its color.</summary>
    public Color Color;
}

/// <summary>Where the node was scrolled to when the drag began, in logical pixels.</summary>
[Behavior]
public partial struct ScrollStart
{
    /// <summary>The scroll when the drag began.</summary>
    public Vec2 Start;
}
