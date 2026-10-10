// Bevy's ui_drag_and_drop example, examples/ui/ui_drag_and_drop.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates dragging and dropping interface nodes. A board of ten by ten numbered tiles, which
// light under the pointer, follow it outlined in white when dragged, and swap places with the tile
// they are dropped on.
internal static class UiDragAndDrop
{
    private const int Columns = 10, Rows = 10;
    private const float TileSize = 40f;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        // The board, in the middle, picked through.
        var board = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, AlignSelf = UiAlignSelf.Center, Color = Color.FromSrgb(0.4f, 0.4f, 0.4f) });
        ecs.Wrap<NodeRef>(board).JustifySelf = NodeRef.JustifySelfVariant.Center;
        var ignore = ecs.Insert<PickableRef>(board);
        (ignore.ShouldBlockLower, ignore.IsHoverable) = (false, false);

        Color[] colors = [Color.FromSrgb(0.2f, 0.2f, 0.8f), Color.FromSrgb(0.8f, 0.2f, 0.2f)];

        for (var column = 0; column < Columns; column++)
        for (var row = 0; row < Rows; row++)
        {
            var i = column + row * Columns;
            var color = colors[((row % 2) + column) % colors.Length];
            var border = Lighter(color, -0.025f);

            var tile = Ui.SpawnNode(new UiSettings
            {
                Width = Length.Px(TileSize),
                Height = Length.Px(TileSize),
                Border = Sides.All(Length.Px(4f)),
                Align = UiAlign.Center,
                Justify = UiJustify.Center,
                Color = color,
                BorderColor = border,
            });
            ecs.SetParent(tile, board);
            ecs.Add(tile, new Tile { Row = row + 1, Column = column + 1, Color = color, Border = border });
            UiGrid.Place(tile, new GridPlacement { Row = row + 1, Column = column + 1 });

            var outline = ecs.Insert<OutlineRef>(tile);
            (outline.Width, outline.Offset, outline.Color) = (new Val.Px(2f), new Val.Px(0f), new Color(0f, 0f, 0f, 0f));
            ecs.Insert<UiTransformRef>(tile);
            ecs.Insert<GlobalZIndexRef>(tile);

            // Picked without hiding what is under it, so a tile dragged over another lets the one
            // under it be dropped on.
            var pickable = ecs.Insert<PickableRef>(tile);
            (pickable.ShouldBlockLower, pickable.IsHoverable) = (false, true);

            ecs.Observe<Pointer<Over>>(tile, on => Paint(on.Ecs, on.Entity, lit: true));
            ecs.Observe<Pointer<Out>>(tile, on => Paint(on.Ecs, on.Entity, lit: false));
            ecs.Observe<Pointer<DragStart>>(tile, on =>
            {
                on.Ecs.Wrap<OutlineRef>(on.Entity).Color = new Color(1f, 1f, 1f, 1f);
                on.Ecs.Wrap<GlobalZIndexRef>(on.Entity).Value = 1;
            });
            ecs.Observe<Pointer<Drag>>(tile, on =>
            {
                var moved = on.Ecs.Wrap<UiTransformRef>(on.Entity);
                var distance = on.Event.Event.Distance;
                (moved.TranslationX, moved.TranslationY) = (new Val.Px(distance.X), new Val.Px(distance.Y));
            });
            ecs.Observe<Pointer<DragEnd>>(tile, on =>
            {
                var moved = on.Ecs.Wrap<UiTransformRef>(on.Entity);
                (moved.TranslationX, moved.TranslationY) = (new Val.Px(0f), new Val.Px(0f));
                on.Ecs.Wrap<OutlineRef>(on.Entity).Color = new Color(0f, 0f, 0f, 0f);
                on.Ecs.Wrap<GlobalZIndexRef>(on.Entity).Value = 0;
            });
            ecs.Observe<Pointer<DragDrop>>(tile, on => Swap(on.Ecs, on.Entity, on.Event.Event.Dropped));

            var number = Ui.SpawnText($"{i}", new UiSettings());
            var through = ecs.Insert<PickableRef>(number);
            (through.ShouldBlockLower, through.IsHoverable) = (false, false);
            ecs.SetParent(number, tile);
        }
    }, "ui_drag_and_drop.Setup");

    // A tile lit under the pointer and back to its own colors off it.
    private static void Paint(EcsWorld ecs, Entity entity, bool lit)
    {
        var tile = ecs.GetOrDefault<Tile>(entity);
        ecs.Wrap<BackgroundColorRef>(entity).Value = lit ? Lighter(tile.Color, 0.1f) : tile.Color;
        var border = ecs.Wrap<BorderColorRef>(entity);
        var color = lit ? Lighter(tile.Border, 0.1f) : tile.Border;
        (border.Top, border.Right, border.Bottom, border.Left) = (color, color, color, color);
    }

    // The tile dropped on and the one dropped swap their places on the board.
    private static void Swap(EcsWorld ecs, Entity target, Entity dropped)
    {
        if (!ecs.Has<Tile>(dropped)) return;

        var (a, b) = (ecs.GetOrDefault<Tile>(target), ecs.GetOrDefault<Tile>(dropped));
        ecs.Set(target, a with { Row = b.Row, Column = b.Column });
        ecs.Set(dropped, b with { Row = a.Row, Column = a.Column });
        UiGrid.Place(target, new GridPlacement { Row = b.Row, Column = b.Column });
        UiGrid.Place(dropped, new GridPlacement { Row = a.Row, Column = a.Column });
    }

    // Bevy's lighter and darker, which move a color's luminance toward white or black by an amount,
    // a negative amount being darker.
    private static Color Lighter(Color color, float amount)
    {
        var luminance = color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f;
        var target = Math.Clamp(luminance + amount, 0f, 1f);
        var (toward, share) = target < luminance
            ? (0f, (luminance - target) / luminance)
            : (1f, target > luminance ? (target - luminance) / (1f - luminance) : 0f);

        return new Color(
            color.R + (toward - color.R) * share,
            color.G + (toward - color.G) * share,
            color.B + (toward - color.B) * share,
            color.A);
    }
}

/// <summary>A tile of the board, where it is on the board and its own colors.</summary>
[Behavior]
public partial struct Tile
{
    /// <summary>Its row, from one.</summary>
    public int Row;

    /// <summary>Its column, from one.</summary>
    public int Column;

    /// <summary>Its color off the pointer.</summary>
    public Color Color;

    /// <summary>Its border's color off the pointer.</summary>
    public Color Border;
}
