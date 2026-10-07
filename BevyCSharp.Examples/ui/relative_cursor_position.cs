// Bevy's relative_cursor_position example, examples/ui/relative_cursor_position.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows where the pointer is over a node, from its top-left corner to its bottom-right as nought
// to one, printed under it in green while the pointer is over it and red while it is not, all
// inside a camera's viewport smaller than the window.
internal static class RelativeCursorPosition
{

    private static Entity _square, _text;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var camera = Render2d.SpawnCamera2d();
            ecs.Wrap<CameraRef>(camera).Viewport = new Viewport(200, 100, 600, 600, new FloatRange(0f, 1f));

            var column = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center, Direction = UiDirection.Column });
            _square = Ui.SpawnNode(new UiSettings { Width = Length.Px(250f), Height = Length.Px(250f), Margin = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(15f)), Color = Color.FromSrgb(0.92f, 0.14f, 0.05f) });
            ecs.Insert<RelativeCursorPositionRef>(_square);
            ecs.SetParent(_square, column);

            _text = Ui.SpawnText("(0.0, 0.0)", new UiSettings { Color = Color.FromSrgb(0.9f, 0.9f, 0.9f) }, new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 33f });
            ecs.SetParent(_text, column);
        }, "relative_cursor_position.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var relative = ecs.Wrap<RelativeCursorPositionRef>(_square);
            var over = relative.CursorOver;

            Ui.SetText(_text, relative.Normalized is { } at
                ? FormattableString.Invariant($"({at.X:0.0}, {at.Y:0.0})")
                : "unknown");
            ecs.Wrap<TextColorRef>(_text).Value = over ? Color.FromSrgb(0.1f, 0.9f, 0.1f) : Color.FromSrgb(0.9f, 0.1f, 0.1f);
        }, "relative_cursor_position.Update");
    }
}
