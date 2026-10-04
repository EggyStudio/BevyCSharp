using Bevy;

namespace BevyCSharp.Examples.Transforms;

// Illustrates how to move an object along an axis, a cube sliding along its own X and turning back
// once it is five units from where it started.
internal static class Translation
{
    private const float MaxDistance = 5f;
    private static Entity _cube;
    private static float _speed;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            _speed = 2f;
            _cube = CubeScene.Spawn(ctx.Ecs, Transform.Identity);
        }, "translation.Setup");

        app.Update(ctx =>
        {
            var transform = ctx.Ecs.GetOrDefault<Transform>(_cube);
            if (transform.Translation.Length > MaxDistance) _speed = -_speed;

            // Along the cube's own X, which is the world's while it is not turned.
            transform.Translation += transform.Rotation * Vec3.UnitX * _speed * ctx.Time.Delta;
            ctx.Ecs.Set(_cube, transform);
        }, "translation.MoveCube");
    }
}
