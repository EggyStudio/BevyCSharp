// Bevy's window_drag_move example, examples/window/window_drag_move.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Windowing;

// A window without a border, moved or resized by dragging anywhere in it. A says what a left click
// does, move, resize or nothing, and S and D pick the edge or corner a resize drags.
internal static class WindowDragMove
{
    private enum LeftClickAction { Move, Resize, Nothing }

    private static readonly (WindowEdge Edge, string Name)[] Directions =
    [
        (WindowEdge.Top, "North"),
        (WindowEdge.TopRight, "NorthEast"),
        (WindowEdge.Right, "East"),
        (WindowEdge.BottomRight, "SouthEast"),
        (WindowEdge.Bottom, "South"),
        (WindowEdge.BottomLeft, "SouthWest"),
        (WindowEdge.Left, "West"),
        (WindowEdge.TopLeft, "NorthWest"),
    ];

    private static LeftClickAction _action;
    private static int _direction;
    private static Entity _actionSpan, _directionSpan;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_action, _direction) = (LeftClickAction.Move, 7);
            if (Window.Entity() != Entity.None) Window.SetStyle(decorations: false);
            ecs.SpawnCamera3d(Transform.Identity);

            var panel = Ui.SpawnNode(new UiSettings { Absolute = true, Padding = Sides.All(Length.Px(5f)), Color = (0f, 0f, 0f, 0.75f) });
            ecs.Insert<GlobalZIndexRef>(panel).Value = int.MaxValue;
            var text = Ui.SpawnText(string.Empty, new UiSettings());
            ecs.SetParent(text, panel);

            var style = new UiTextSettings();
            var white = (1f, 1f, 1f, 1f);
            Ui.SpawnTextSpan(text, "Demonstrate drag move and drag resize without window decorations.\n\n", style, white);
            Ui.SpawnTextSpan(text, "Controls:\n", style, white);
            Ui.SpawnTextSpan(text, "A - change left click action [", style, white);
            _actionSpan = Ui.SpawnTextSpan(text, "Move", style, white);
            Ui.SpawnTextSpan(text, "]\n", style, white);
            Ui.SpawnTextSpan(text, "S / D - change resize direction [", style, white);
            _directionSpan = Ui.SpawnTextSpan(text, "NorthWest", style, white);
            Ui.SpawnTextSpan(text, "]\n", style, white);
        }, "window_drag_move.Setup");

        app.Update(ctx =>
        {
            var (ecs, input) = (ctx.Ecs, ctx.Input);
            if (input.KeyPressed(Key.A))
            {
                _action = _action switch { LeftClickAction.Move => LeftClickAction.Resize, LeftClickAction.Resize => LeftClickAction.Nothing, _ => LeftClickAction.Move };
                ecs.Wrap<TextSpanRef>(_actionSpan).Value = _action.ToString();
            }

            if (input.KeyPressed(Key.S)) _direction = (_direction + Directions.Length - 1) % Directions.Length;
            if (input.KeyPressed(Key.D)) _direction = (_direction + 1) % Directions.Length;
            if (input.KeyPressed(Key.S) || input.KeyPressed(Key.D)) ecs.Wrap<TextSpanRef>(_directionSpan).Value = Directions[_direction].Name;
        }, "window_drag_move.HandleInput");

        app.Update(ctx =>
        {
            if (!ctx.Input.MousePressed(MouseButton.Left) || Window.Entity() == Entity.None) return;
            if (_action == LeftClickAction.Move) Window.StartDragMove();
            else if (_action == LeftClickAction.Resize) Window.StartDragResize(Directions[_direction].Edge);
        }, "window_drag_move.MoveOrResizeWindows");
    }
}
