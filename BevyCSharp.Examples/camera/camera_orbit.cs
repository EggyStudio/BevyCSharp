// Bevy's camera_orbit example, examples/camera/camera_orbit.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Cameras;

// Shows how to orbit a camera around a point, the mouse turning it up, down and around and its
// buttons rolling it, always the same distance from the middle.
internal static class CameraOrbit
{
    private const float OrbitDistance = 20f;
    private const float PitchSpeed = 0.003f;
    private const float PitchLimit = MathF.PI / 2f - 0.01f;
    private const float RollSpeed = 1f;
    private const float YawSpeed = 0.004f;

    private static Entity _camera;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(5f, 5f, 5f), Vec3.Zero, Vec3.UnitY));
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.3f, 0.5f, 0.3f), DoubleSided = true }), Transform.Identity);
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid), Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f)), Transform.At(1.5f, 0.51f, 1.5f));
            ecs.SpawnPointLight(new Vec3(3f, 8f, 5f));

            Ui.SpawnText("Mouse up or down: pitch\nMouse left or right: yaw\nMouse buttons: roll",
                new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        }, "camera_orbit.Setup");

        app.Update(ctx =>
        {
            var input = ctx.Input;
            var roll = 0f;
            if (input.MouseDown(MouseButton.Left)) roll -= 1f;
            if (input.MouseDown(MouseButton.Right)) roll += 1f;

            // Bevy's YXZ angles, which Quat.FromEuler and ToEuler are, yaw about Y, then pitch,
            // then roll.
            var transform = ctx.Ecs.GetOrDefault<Transform>(_camera);
            var euler = transform.Rotation.ToEuler();
            var pitch = Math.Clamp(euler.X + input.MouseDeltaY * PitchSpeed, -PitchLimit, PitchLimit);
            var yaw = euler.Y + input.MouseDeltaX * YawSpeed;
            transform.Rotation = Quat.FromEuler(pitch, yaw, euler.Z + roll * RollSpeed * ctx.Time.Delta);

            // Back from the middle along where the camera now looks.
            transform.Translation = Vec3.Zero - transform.Rotation * -Vec3.UnitZ * OrbitDistance;
            ctx.Ecs.Set(_camera, transform);
        }, "camera_orbit.Orbit");
    }
}
