using Bevy;

namespace BevyCSharp.Examples.Transforms;

// Illustrates how to rotate an object around an axis, a cube turning about Y at three tenths of a
// turn a second.
internal static class Rotation3d
{
    private const float Speed = 0.3f;
    private static Entity _cube;

    public static void Build(App app)
    {
        app.Startup(ctx => _cube = CubeScene.Spawn(ctx.Ecs, Transform.Identity), "3d_rotation.Setup");
        app.Update(ctx =>
        {
            var transform = ctx.Ecs.GetOrDefault<Transform>(_cube);
            transform.Rotation = Quat.FromRotationY(Speed * MathF.Tau * ctx.Time.Delta) * transform.Rotation;
            ctx.Ecs.Set(_cube, transform);
        }, "3d_rotation.RotateCube");
    }
}
