using Bevy;

namespace BevyCSharp.Examples.Interface;

// Shows each box of an interface drawn by a camera of its own, red, green and blue, a left click on
// a box raising its camera's order and a right click lowering it, so the box on top changes.
internal static class UiTargetCamera
{
    private const string Camera = "bevy_camera::camera::Camera";
    private const string Node = "bevy_ui::ui_node::Node";

    private static readonly List<(Entity Box, Entity Camera, Entity Label)> Boxes = [];
    private static readonly Dictionary<Entity, int> Orders = [];
    private static readonly Dictionary<Entity, UiInteraction> Last = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Boxes.Clear();
            Orders.Clear();
            Last.Clear();
            const float Size = 100f;

            var help = Ui.SpawnText("Each box is rendered by a different camera\n* left-click: increase the camera's order\n* right-click: decrease the camera's order",
                new UiSettings { AlignSelf = UiAlignSelf.Center, Bottom = Length.Px(2f * Size) });
            ecs.SetVariant(help, Node, ".justify_self", "Center");

            var colors = new[] { Scene.Srgb8(255, 0, 0), Scene.Srgb8(0, 128, 0), Scene.Srgb8(0, 0, 255) };
            for (var i = 0; i < colors.Length; i++)
            {
                // A camera drawn as Bevy draws one, clearing to its color and replacing what the
                // cameras under it drew, where the bridge's 2D cameras above the first are overlays.
                // It draws nothing but its color and its box, so a 3D one serves.
                var camera = Render.SpawnCamera3d(new CameraSettings { Order = i });
                ecs.SetVariant(camera, Camera, ".clear_color", "Custom");
                ecs.SetReflectedColor(camera, Camera, ".clear_color.0", new Color(colors[i].R, colors[i].G, colors[i].B, colors[i].A));
                Orders[camera] = i;

                var box = Ui.SpawnNode(new UiSettings
                {
                    Interactive = true,
                    Camera = camera,
                    AlignSelf = UiAlignSelf.Center,
                    Justify = UiJustify.Center,
                    Align = UiAlign.Center,
                    Left = Length.Px(0.67f * Size * (i - 1)),
                    Top = Length.Px(0.67f * Size * (i - 1)),
                    Width = Length.Px(Size),
                    Height = Length.Px(Size),
                    Border = Sides.All(Length.Px(0.1f * Size)),
                    Color = (0f, 0f, 0f, 1f),
                    BorderColor = Scene.Srgb8(255, 255, 0),
                });
                var label = Ui.SpawnText($"{i}", new UiSettings { Color = colors[i] }, 50f);
                ecs.SetParent(label, box);
                ecs.SetVariant(box, Node, ".justify_self", "Center");
                Boxes.Add((box, camera, label));
            }
        }, "ui_target_camera.Setup");

        // A press on a box moves its camera up or down by the button that pressed it.
        app.Update(ctx =>
        {
            foreach (var (box, camera, label) in Boxes)
            {
                var interaction = Ui.InteractionOf(box);
                var pressed = interaction == UiInteraction.Pressed && (!Last.TryGetValue(box, out var last) || last != UiInteraction.Pressed);
                Last[box] = interaction;
                if (!pressed) continue;

                Orders[camera] += ctx.Input.MouseDown(MouseButton.Left) ? 1 : -1;
                ctx.Ecs.SetReflected(camera, Camera, ".order", $"{Orders[camera]}");
                Ui.SetText(label, $"{Orders[camera]}");
            }
        }, "ui_target_camera.ChangeOrder");
    }
}
