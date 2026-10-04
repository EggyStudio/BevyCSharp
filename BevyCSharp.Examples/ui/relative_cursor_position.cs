using System.Text.Json.Nodes;
using Bevy;

namespace BevyCSharp.Examples.Interface;

// Shows where the pointer is over a node, from its top-left corner to its bottom-right as nought
// to one, printed under it in green while the pointer is over it and red while it is not, all
// inside a camera's viewport smaller than the window.
internal static class RelativeCursorPosition
{
    private const string Relative = "bevy_ui::focus::RelativeCursorPosition";
    private const string Camera = "bevy_camera::camera::Camera";

    private static Entity _square, _text;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var camera = Render2d.SpawnCamera2d();
            ecs.SetVariant(camera, Camera, ".viewport", "Some");
            ecs.SetReflected(camera, Camera, ".viewport.0.physical_position", "[200,100]");
            ecs.SetReflected(camera, Camera, ".viewport.0.physical_size", "[600,600]");

            var column = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center, Direction = UiDirection.Column });
            _square = Ui.SpawnNode(new UiSettings { Width = Length.Px(250f), Height = Length.Px(250f), Margin = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(15f)), Color = Scene.Srgb(0.92f, 0.14f, 0.05f) });
            ecs.InsertReflected(_square, Relative);
            ecs.SetParent(_square, column);

            _text = Ui.SpawnText("(0.0, 0.0)", new UiSettings { Color = Scene.Srgb(0.9f, 0.9f, 0.9f) }, new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 33f });
            ecs.SetParent(_text, column);
        }, "relative_cursor_position.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var normalized = ecs.GetReflected(_square, Relative, ".normalized") is { } json ? JsonNode.Parse(json) : null;
            var over = ecs.GetReflected(_square, Relative, ".cursor_over") == "true";

            // An Option<Vec2>, written as {"Some":[x,y]} or as "None".
            Ui.SetText(_text, normalized?["Some"] is JsonArray at
                ? FormattableString.Invariant($"({(float)at[0]!:0.0}, {(float)at[1]!:0.0})")
                : "unknown");
            ecs.SetReflectedColor(_text, "bevy_text::text::TextColor", ".0", over ? Color.FromSrgb(0.1f, 0.9f, 0.1f) : Color.FromSrgb(0.9f, 0.1f, 0.1f));
        }, "relative_cursor_position.Update");
    }
}
