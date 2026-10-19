// Bevy's free_camera_controller example, examples/camera/free_camera_controller.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Cameras;

// Bevy's free camera, moved with WASD, Q and E, turned with the mouse while the right button is
// held or after M, and faster with the wheel or Shift, through a room with a wall, a cube behind
// it and one under the floor. Z and X change how fast the mouse turns it, C and V its friction, F
// and G how much the wheel changes its speed, B turns it off and on, and T moves it up along its
// own up rather than the world's.
internal static class FreeCameraController
{
    private static Entity _camera;
    private static Entity _info;

    public static void Configure(Config config) => config.CameraControllers = CameraControllers.Free;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;

            // Looking along X from a meter up, the settings Bevy's example gives it.
            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 1f, 0f), new Vec3(1f, 1f, 0f), Vec3.UnitY));
            Render.SetFreeCamera(_camera, new FreeCameraSettings { Sensitivity = 0.2f, Friction = 25f, WalkSpeed = 3f, RunSpeed = 9f });

            SpawnLights(ecs);
            SpawnWorld(ecs);

            // Bevy spawns its text after the camera, in a stage of its own, since the first line
            // is the controller's description of its controls.
            Ui.SpawnText(Render.FreeCameraControls(_camera), new UiSettings { Absolute = true, Top = Length.Px(-16f), Left = Length.Px(12f) });
            Ui.SpawnText(
                "Z/X: decrease/increase sensitivity\n"
                    + "C/V: decrease/increase friction\n"
                    + "F/G: decrease/increase scroll factor\n"
                    + "B: enable/disable controller\n"
                    + "T: world/local vertical movement",
                new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
            _info = Ui.SpawnText("", new UiSettings { Absolute = true, Top = Length.Px(12f), Right = Length.Px(12f) });
        }, "free_camera_controller.Setup");

        app.Update(ctx =>
        {
            var input = ctx.Input;
            var settings = Render.FreeCameraOf(_camera)!;
            var state = Render.FreeCameraStateOf(_camera)!.Value;
            var (changed, toggled) = (false, false);

            if (input.KeyDown(Key.Z)) (settings.Sensitivity, changed) = (MathF.Max(settings.Sensitivity - 0.005f, 0.005f), true);
            if (input.KeyDown(Key.X)) (settings.Sensitivity, changed) = (settings.Sensitivity + 0.005f, true);
            if (input.KeyDown(Key.C)) (settings.Friction, changed) = (MathF.Max(settings.Friction - 0.2f, 0f), true);
            if (input.KeyDown(Key.V)) (settings.Friction, changed) = (settings.Friction + 0.2f, true);
            if (input.KeyDown(Key.F)) (settings.ScrollFactor, changed) = (MathF.Max(settings.ScrollFactor - 0.02f, 0.02f), true);
            if (input.KeyDown(Key.G)) (settings.ScrollFactor, changed) = (settings.ScrollFactor + 0.02f, true);
            if (input.KeyPressed(Key.B)) (state.Enabled, toggled) = (!state.Enabled, true);
            if (input.KeyPressed(Key.T)) (settings.LocalVerticalMovement, changed) = (!settings.LocalVerticalMovement, true);

            if (changed) Render.SetFreeCamera(_camera, settings);
            if (toggled) Render.SetFreeCameraState(_camera, state);

            Ui.SetText(_info,
                $"Enabled: {(state.Enabled ? "true" : "false")},\n"
                    + $"Sensitivity: {settings.Sensitivity:0.000}\n"
                    + $"Friction: {settings.Friction:0.0}\n"
                    + $"Scroll factor: {settings.ScrollFactor:0.00}\n"
                    + $"Walk Speed: {settings.WalkSpeed * state.SpeedMultiplier:0.00}\n"
                    + $"Run Speed: {settings.RunSpeed * state.SpeedMultiplier:0.00}\n"
                    + $"Speed: {state.Velocity.Length:0.00}");
        }, "free_camera_controller.UpdateSettings");
    }

    private static void SpawnLights(EcsWorld ecs)
    {
        // The main light, one behind the wall and one under the floor, in Tailwind's orange and red
        // at 300, Bevy's default point light with shadows.
        Light(ecs, Color.FromSrgb8(253, 186, 116), new Vec3(0f, 3f, 0f));
        Light(ecs, Color.FromSrgb8(255, 255, 255), new Vec3(-3.5f, 3f, 0f));
        Light(ecs, Color.FromSrgb8(252, 165, 165), new Vec3(0f, -0.5f, 0f));
    }

    private static void Light(EcsWorld ecs, Color color, Vec3 at)
    {
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 1_000_000f, Color = (color.R, color.G, color.B), Shadows = true });
        ecs.Add(light, Transform.At(at.X, at.Y, at.Z));
    }

    private static void SpawnWorld(EcsWorld ecs)
    {
        var cube = Render.CreateMesh(MeshShape.Cuboid);
        var floor = Render.CreateMesh(MeshShape.Plane, 20f, 20f);
        var sphere = Render.CreateMesh(MeshShape.Sphere, 0.5f);
        var wall = Render.CreateMesh(MeshShape.Cuboid, 0.2f, 4f, 3f);

        // Tailwind's blue at 700 and red at 950.
        var blue = Render.CreateMaterial(Color.FromSrgb8(29, 78, 216));
        var red = Render.CreateMaterial(Color.FromSrgb8(69, 10, 10));
        var white = Render.CreateMaterial(Color.FromSrgb8(255, 255, 255));

        // The floor's top, and its underside a hundredth of a unit below it facing down.
        ecs.SpawnMesh(floor, white, Transform.Identity);
        ecs.SpawnMesh(floor, white, new Transform(new Vec3(0f, -0.01f, 0f), Quat.FromRotationX(MathF.PI), Vec3.One));
        ecs.SpawnMesh(sphere, blue, Transform.At(3f, 1.5f, 0f));
        ecs.SpawnMesh(wall, white, Transform.At(-3f, 2f, 0f));
        ecs.SpawnMesh(cube, blue, Transform.At(-4.2f, 0.5f, 0f));

        // Under the floor, turned an eighth about the world's Y and then its X, Bevy's extrinsic
        // YXZ angles.
        var turned = Quat.FromRotationX(MathF.PI / 4f) * Quat.FromRotationY(MathF.PI / 4f);
        ecs.SpawnMesh(cube, red, new Transform(new Vec3(3f, -2f, 0f), turned, Vec3.One));
    }
}
