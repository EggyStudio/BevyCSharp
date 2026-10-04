using Bevy;

namespace BevyCSharp.Examples.Transforms;

// Shows translation, rotation and scale together, a cube flying forward while it turns toward a
// yellow sphere, and the sphere shrinking the farther the cube has come.
internal static class TransformExample
{
    private const float MoveSpeed = 2f;
    private const float TurnSpeed = 0.2f;
    private const float MaxSize = 1f;
    private const float MinSize = 0.1f;
    private const float ScaleFactor = 0.05f;

    private static Entity _sphere, _cube;
    private static Vec3 _start;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _sphere = ecs.Mesh(Render.CreateMesh(MeshShape.Sphere, 3f), Scene.Material(Scene.Srgb(1f, 1f, 0f)), Transform.Identity);

            var spawn = new Transform(new Vec3(0f, 0f, -10f), Quat.FromRotationY(MathF.PI / 2f), Vec3.One);
            _start = spawn.Translation;
            _cube = CubeScene.Spawn(ecs, spawn);
        }, "transform.Setup");

        // Bevy chains its three systems, and one system doing the three in turn is the same order.
        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var cube = ecs.GetOrDefault<Transform>(_cube);

            cube.Translation += cube.Rotation * -Vec3.UnitZ * MoveSpeed * ctx.Time.Delta;

            // A little of the way toward facing the sphere each frame, up being the cube's own.
            var center = ecs.GetOrDefault<Transform>(_sphere).Translation;
            var facing = Transform.LookingAt(cube.Translation, center, cube.Rotation * Vec3.UnitY).Rotation;
            cube.Rotation = Scene.Lerp(cube.Rotation, facing, TurnSpeed * ctx.Time.Delta);
            ecs.Set(_cube, cube);

            var size = MathF.Max(MaxSize - ScaleFactor * (_start - cube.Translation).Length, MinSize);
            var sphere = ecs.GetOrDefault<Transform>(_sphere);
            sphere.Scale = new Vec3(size);
            ecs.Set(_sphere, sphere);
        }, "transform.MoveRotateAndScale");
    }
}
