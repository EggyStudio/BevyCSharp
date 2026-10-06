// Bevy's scroll example, examples/ui/scroll_and_overflow/scroll.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Scrolling in Bevy's interface by the wheel. A list across the top, scrolled with Ctrl held, whose
// items a left press removes, and below it a list scrolled down that keeps room for a scrollbar,
// two scrolled both ways, one of them with headers along its top and left that stay put, and lists
// inside a list, where the wheel scrolls the innermost that can still move and what is left goes
// on to the one around it.
internal static class ScrollExample
{
    private const float LineHeight = 21f;
    private const float FontSize = 20f;

    public static void Build(App app) => app.Startup(Setup, "scroll.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        Render2d.SpawnCamera2d();

        // Bevy's send_scroll_events reads the wheel's messages and triggers its Scroll at
        // everything each pointer is over. Picking's Pointer<Scroll> is that same pair, one for
        // each turn of the wheel at each entity the pointer is over, so the example's own event
        // starts from it there, and picking's goes no further.
        ecs.Observe<Pointer<Bevy.Scroll>>(on =>
        {
            if (on.Entity != on.Event.Entity) return;
            on.Propagate(false);

            var wheel = on.Event.Event;
            var delta = new Vec2(-wheel.X, -wheel.Y);
            if (wheel.Unit == ScrollUnit.Line) delta *= LineHeight;
            if (on.Context.Input.AnyKeyDown([Key.ControlLeft, Key.ControlRight])) delta = new Vec2(delta.Y, delta.X);

            on.Ecs.Trigger(new Scroll(on.Entity, delta));
        });
        ecs.Observe<Scroll>(OnScrollHandler);

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Justify = UiJustify.SpaceBetween,
            Direction = UiDirection.Column,
        });

        // The list across the top.
        var horizontal = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Direction = UiDirection.Column });
        ecs.SetParent(horizontal, root);
        ecs.SetParent(Title("Horizontally Scrolling list (Ctrl + MouseWheel)", font, ecs), horizontal);

        var row = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(80f),
            Margin = Sides.All(Length.Px(10f)),
            Direction = UiDirection.Row,
            OverflowX = UiOverflow.Scroll,
            Color = Color.FromSrgb(0.10f, 0.10f, 0.10f),
        });
        ecs.SetParent(row, horizontal);

        for (var i = 0; i < 100; i++)
        {
            var item = Item($"Item {i}", font, ecs, new UiSettings { MinWidth = Length.Px(200f), AlignContent = UiJustify.Center });
            ecs.SetParent(item, row);
            ecs.Observe<Pointer<Press>>(item, on =>
            {
                if (on.Event.Event.Button == PointerButton.Primary) on.Ecs.Despawn(on.Entity);
            });
        }

        // The other four, side by side.
        var others = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Direction = UiDirection.Row,
            Justify = UiJustify.SpaceBetween,
        });
        ecs.SetParent(others, root);

        ecs.SetParent(VerticallyScrollingList(font, ecs), others);
        ecs.SetParent(BidirectionalScrollingList(font, ecs), others);
        ecs.SetParent(BidirectionalScrollingListWithSticky(font, ecs), others);
        ecs.SetParent(NestedScrollingList(font, ecs), others);
    }

    // Bevy's on_scroll_handler, at each entity the event reaches on its way up. A node that scrolls
    // along an axis and is not yet at its end there takes that part of the delta, and once nothing
    // is left the event stops.
    private static void OnScrollHandler(On<Scroll> on)
    {
        var ecs = on.Ecs;
        if (ecs.Get<ScrollPositionRef>(on.Entity) is not { } scrollPosition) return;

        var node = ecs.Wrap<NodeRef>(on.Entity);
        var computed = ecs.Wrap<ComputedNodeRef>(on.Entity);
        var maxOffset = (computed.ContentSize - computed.Size) * computed.InverseScaleFactor;
        var position = scrollPosition.Value;
        var delta = on.Event.Delta;

        if (node.OverflowX == NodeRef.OverflowXVariant.Scroll && delta.X != 0f)
        {
            // Is this node already scrolled all the way in the direction of the scroll?
            var max = delta.X > 0f ? position.X >= maxOffset.X : position.X <= 0f;
            if (!max)
            {
                position = position with { X = position.X + delta.X };
                delta = delta with { X = 0f };
            }
        }

        if (node.OverflowY == NodeRef.OverflowYVariant.Scroll && delta.Y != 0f)
        {
            var max = delta.Y > 0f ? position.Y >= maxOffset.Y : position.Y <= 0f;
            if (!max)
            {
                position = position with { Y = position.Y + delta.Y };
                delta = delta with { Y = 0f };
            }
        }

        if (position != scrollPosition.Value) scrollPosition.Value = position;

        // What is left goes on to the parent, the event being handed by reference.
        on.Event.Delta = delta;

        // Stop propagating when the delta is fully consumed.
        if (delta == Vec2.Zero) on.Propagate(false);
    }

    private static Entity VerticallyScrollingList(AssetHandle font, EcsWorld ecs)
    {
        var column = Column();
        ecs.SetParent(Title("Vertically Scrolling List", font, ecs), column);

        var list = Ui.SpawnNode(new UiSettings
        {
            Direction = UiDirection.Column,
            AlignSelf = UiAlignSelf.Stretch,
            Height = Length.Percent(50f),
            OverflowY = UiOverflow.Scroll,
            Color = Color.FromSrgb(0.10f, 0.10f, 0.10f),
        });
        ecs.Wrap<NodeRef>(list).ScrollbarWidth = 20f;
        ecs.SetParent(list, column);

        for (var i = 0; i < 25; i++)
        {
            var line = Ui.SpawnNode(new UiSettings { MinHeight = Length.Px(LineHeight), MaxHeight = Length.Px(LineHeight) });
            ecs.SetParent(line, list);
            ecs.SetParent(Item($"Item {i}", font, ecs, new UiSettings()), line);
        }

        return column;
    }

    private static Entity BidirectionalScrollingList(AssetHandle font, EcsWorld ecs)
    {
        var column = Column();
        ecs.SetParent(Title("Bidirectionally Scrolling List", font, ecs), column);

        var list = Ui.SpawnNode(new UiSettings
        {
            Direction = UiDirection.Column,
            AlignSelf = UiAlignSelf.Stretch,
            Height = Length.Percent(50f),
            OverflowX = UiOverflow.Scroll,
            OverflowY = UiOverflow.Scroll,
            Color = Color.FromSrgb(0.10f, 0.10f, 0.10f),
        });
        ecs.SetParent(list, column);

        for (var outer = 0; outer < 25; outer++)
        {
            var line = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row });
            ecs.SetParent(line, list);
            for (var i = 0; i < 10; i++) ecs.SetParent(Item($"Item {outer * 10 + i}", font, ecs, new UiSettings()), line);
        }

        return column;
    }

    private static Entity BidirectionalScrollingListWithSticky(AssetHandle font, EcsWorld ecs)
    {
        var column = Column();
        ecs.SetParent(Title("Bidirectionally Scrolling List With Sticky Nodes", font, ecs), column);

        var grid = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Grid,
            AlignSelf = UiAlignSelf.Stretch,
            Height = Length.Percent(50f),
            OverflowX = UiOverflow.Scroll,
            OverflowY = UiOverflow.Scroll,
        });
        UiGrid.Set(grid, new GridSettings { Columns = [Track.Auto.Repeated(30)] });

        // The wheel moves this one no more than it does Bevy's, since Taffy 0.10.1 measures what a
        // grid holds from each item's own cell rather than from the grid, so the grid reads as
        // holding no more than one cell, and Bevy holds its scroll within that.
        ecs.SetParent(grid, column);

        for (var y = 0; y < 30; y++)
        for (var x = 0; x < 30; x++)
        {
            // Simple sticky nodes at top and left sides of UI node can be achieved by combining
            // such effects as IgnoreScroll, ZIndex, BackgroundColor for child UI nodes.
            var (zIndex, background) = (x == 0, y == 0) switch
            {
                (true, true) => (2, Color.FromSrgb(1f, 0f, 0f)),
                (true, false) or (false, true) => (1, Color.FromSrgb(0f, 0f, 1f)),
                _ => (0, Color.FromSrgb(0f, 0f, 0f)),
            };

            var cell = Ui.SpawnText($"|{y},{x}|", new UiSettings(), new UiTextSettings { Font = font, Wrap = TextWrap.NoWrap });
            ecs.Insert<LabelRef>(cell);
            var ignore = ecs.Insert<IgnoreScrollRef>(cell);
            (ignore.Item0X, ignore.Item0Y) = (x == 0, y == 0);
            ecs.Insert<ZIndexRef>(cell).Value = zIndex;
            ecs.Insert<BackgroundColorRef>(cell).Value = background;
            ecs.SetParent(cell, grid);
        }

        return column;
    }

    private static Entity NestedScrollingList(AssetHandle font, EcsWorld ecs)
    {
        var column = Column();
        ecs.SetParent(Title("Nested Scrolling Lists", font, ecs), column);

        // Outer, bi-directional scrolling container
        var outer = Ui.SpawnNode(new UiSettings
        {
            ColumnGap = Length.Px(20f),
            Direction = UiDirection.Row,
            AlignSelf = UiAlignSelf.Stretch,
            Height = Length.Percent(50f),
            OverflowX = UiOverflow.Scroll,
            OverflowY = UiOverflow.Scroll,
            Color = Color.FromSrgb(0.10f, 0.10f, 0.10f),
        });
        ecs.SetParent(outer, column);

        // Inner, scrolling columns
        for (var index = 0; index < 5; index++)
        {
            var inner = Ui.SpawnNode(new UiSettings
            {
                Direction = UiDirection.Column,
                AlignSelf = UiAlignSelf.Stretch,
                Height = Length.Percent(200f / 5f * (index + 1)),
                OverflowY = UiOverflow.Scroll,
                Color = Color.FromSrgb(0.05f, 0.05f, 0.05f),
            });
            ecs.SetParent(inner, outer);
            for (var i = 0; i < 20; i++) ecs.SetParent(Item($"Item {index * 20 + i}", font, ecs, new UiSettings()), inner);
        }

        return column;
    }

    // A column two hundred pixels wide holding a title over its list.
    private static Entity Column() => Ui.SpawnNode(new UiSettings
    {
        Direction = UiDirection.Column,
        Justify = UiJustify.Center,
        Align = UiAlign.Center,
        Width = Length.Px(200f),
    });

    private static Entity Title(string text, AssetHandle font, EcsWorld ecs)
    {
        var title = Ui.SpawnText(text, new UiSettings(), new UiTextSettings { Font = font, FontSize = FontSize });
        ecs.Insert<LabelRef>(title);
        return title;
    }

    // An item of a list, a label as Bevy's are, which Bevy also marks as a list item for
    // accessibility.
    private static Entity Item(string text, AssetHandle font, EcsWorld ecs, UiSettings settings)
    {
        var item = Ui.SpawnText(text, settings, new UiTextSettings { Font = font });
        ecs.Insert<LabelRef>(item);
        return item;
    }

    // UI scrolling event, which goes up from the node under the pointer until a node takes all of
    // it, its delta how far is left to scroll in logical pixels. Picking's own Scroll is named in
    // full here.
    internal record struct Scroll(Entity Entity, Vec2 Delta) : IPropagatingEvent;
}
