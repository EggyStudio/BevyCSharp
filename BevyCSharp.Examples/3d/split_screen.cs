// Bevy's split_screen example, examples/3d/split_screen.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Renders four cameras to the same window to accomplish "split screen".
//
// Each camera has a quarter of the size the example was opened at. Bevy's cameras take a quarter
// of the window again when it is resized, which these do not.
internal static class SplitScreen
{
    private static readonly List<(Entity Button, Entity Camera, float Angle)> Buttons = [];
    private static readonly Dictionary<Entity, UiInteraction> Was = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Buttons.Clear();
            Was.Clear();

            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 100f, 100f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
            ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(1f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
            Render.SetShadowCascades(sun, cascades: 2, maximum: 280f, firstBound: 200f);

            var (width, height) = Scene.Size;
            var (halfWidth, halfHeight) = (width / 2, height / 2);
            var players = new[]
            {
                ("Player 1", new Vec3(0f, 200f, -150f)),
                ("Player 2", new Vec3(150f, 150f, 50f)),
                ("Player 3", new Vec3(100f, 150f, -150f)),
                ("Player 4", new Vec3(-100f, 80f, 150f)),
            };

            for (var index = 0; index < players.Length; index++)
            {
                var (name, at) = players[index];
                var camera = ecs.SpawnCamera3d(
                    Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY),
                    new CameraSettings
                    {
                        Order = index,
                        Viewport = ((uint)(index % 2) * halfWidth, (uint)(index / 2) * halfHeight, halfWidth, halfHeight),
                    });

                // Each camera's own interface, its name and a button either side to turn it.
                var panel = Ui.SpawnNode(new UiSettings { Camera = camera, Width = Length.Percent(100f), Height = Length.Percent(100f) });
                var label = Ui.SpawnText(name, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
                ecs.SetParent(label, panel);

                var row = Ui.SpawnNode(new UiSettings
                {
                    Absolute = true,
                    Width = Length.Percent(100f),
                    Height = Length.Percent(100f),
                    Direction = UiDirection.Row,
                    Justify = UiJustify.SpaceBetween,
                    Align = UiAlign.Center,
                    Padding = Length.Px(20f),
                });
                ecs.SetParent(row, panel);

                foreach (var (caption, angle) in new[] { ("<", -0.1f), (">", 0.1f) })
                {
                    var button = Ui.SpawnNode(new UiSettings
                    {
                        Interactive = true,
                        Width = Length.Px(40f),
                        Height = Length.Px(40f),
                        Border = Length.Px(2f),
                        Justify = UiJustify.Center,
                        Align = UiAlign.Center,
                        BorderColor = (1f, 1f, 1f, 1f),
                        Color = Color.FromSrgb(0.25f, 0.25f, 0.25f),
                    });
                    ecs.SetParent(button, row);
                    ecs.SetParent(Ui.SpawnText(caption, new UiSettings()), button);
                    Buttons.Add((button, camera, angle));
                }
            }
        });

        app.SpawnGltf("models/animated/Fox.glb");

        // A button pressed turns its own camera about the middle of the scene.
        app.Update(ctx =>
        {
            foreach (var (button, camera, angle) in Buttons)
            {
                var now = Ui.InteractionOf(button);
                var pressed = now == UiInteraction.Pressed && Was.GetValueOrDefault(button) != UiInteraction.Pressed;
                Was[button] = now;
                if (!pressed) continue;

                var transform = ctx.Ecs.GetOrDefault<Transform>(camera);
                var turn = Quat.FromAxisAngle(Vec3.UnitY, angle);
                transform.Translation = turn * transform.Translation;
                transform.Rotation = turn * transform.Rotation;
                ctx.Ecs.Set(camera, transform);
            }
        }, "split_screen.ButtonSystem");
    }
}
