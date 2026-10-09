// Bevy's split_screen example, examples/3d/split_screen.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Renders four cameras to the same window to accomplish "split screen".
//
// Each camera has a quarter of the size the example was opened at. Bevy's cameras take a quarter
// of the window again when it is resized, which these do not.
internal static class SplitScreen
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 100f, 100f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
            ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(1f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
            Render.SetShadowCascades(sun, cascades: 2, maximum: 280f, firstBound: 200f);

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
                var camera = ecs.SpawnCamera3d(Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY), new CameraSettings { Order = index });
                ecs.Add(camera, new CameraPosition { X = (uint)(index % 2), Y = (uint)(index / 2) });

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

                foreach (var (caption, direction) in new[] { ("<", Direction.Left), (">", Direction.Right) })
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
                    ecs.Add(button, new RotateCamera { Direction = direction });
                }
            }
        }, "split_screen.Setup");

        app.SpawnGltf("models/animated/Fox.glb");
    }
}

/// <summary>A way a camera can be turned.</summary>
public enum Direction { Left, Right }

/// <summary>Which quarter of the window a camera draws, by its column and row.</summary>
[Behavior]
public partial struct CameraPosition
{
    /// <summary>Its column, zero or one.</summary>
    public uint X;

    /// <summary>Its row, zero or one.</summary>
    public uint Y;

    /// <summary>
    /// The camera's viewport made its quarter of the window. Bevy sets it when the window is
    /// resized, the first time as the window opens, and here it is set whenever it differs from
    /// what the window's size makes it, which the first frame does too.
    /// </summary>
    [OnUpdate]
    public void SetCameraViewports(BehaviorContext ctx)
    {
        var (width, height) = Window.Size();
        var (halfWidth, halfHeight) = (width / 2, height / 2);
        var wanted = new Viewport(X * halfWidth, Y * halfHeight, halfWidth, halfHeight, new FloatRange(0f, 1f));
        if (ctx.Ecs.Wrap<CameraRef>(ctx.Entity).Viewport != wanted) Render.SetViewport(ctx.Entity, wanted.PhysicalPositionX, wanted.PhysicalPositionY, wanted.PhysicalSizeX, wanted.PhysicalSizeY);
    }
}

/// <summary>A button turning the camera its interface is drawn on, and which way.</summary>
[Behavior]
public partial struct RotateCamera
{
    /// <summary>Which way it turns the camera.</summary>
    public Direction Direction;

    /// <summary>
    /// Pressed, the camera its interface is drawn on turned a tenth of a radian about the middle of
    /// the scene, the camera found as Bevy finds it, by the target the interface carries down to it.
    /// </summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void ButtonSystem(BehaviorContext ctx)
    {
        if (Ui.InteractionOf(ctx.Entity) != UiInteraction.Pressed
            || ctx.Ecs.Get<ComputedUiTargetCameraRef>(ctx.Entity) is not { Camera: { } camera }) return;

        var transform = ctx.Ecs.GetOrDefault<Transform>(camera);
        var turn = Quat.FromAxisAngle(Vec3.UnitY, Direction == Direction.Left ? -0.1f : 0.1f);
        ctx.Ecs.Set(camera, new Transform(turn * transform.Translation, turn * transform.Rotation, transform.Scale));
    }
}
