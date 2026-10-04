using Bevy;

namespace BevyCSharp.Examples.Transforms;

// Illustrates how to scale an object along each axis in turn, a cube stretching to five along one
// and back to one before moving on to the next.
internal static class ScaleExample
{
    private const float Speed = 2f;
    private const float Max = 5f;
    private const float Min = 1f;

    private static Entity _cube;
    private static Vec3 _direction;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            _direction = Vec3.UnitX;
            _cube = CubeScene.Spawn(ctx.Ecs, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI / 4f), Vec3.One));
        }, "scale.Setup");

        app.Update(ctx =>
        {
            var transform = ctx.Ecs.GetOrDefault<Transform>(_cube);
            var scale = transform.Scale;

            // Past the largest it turns back, and below the smallest it turns to the next axis,
            // Y after X and Z after Y, as Bevy's zxy swizzle moves it.
            if (MathF.Max(scale.X, MathF.Max(scale.Y, scale.Z)) > Max)
            {
                _direction = -_direction;
                scale = new Vec3(MathF.Floor(scale.X), MathF.Floor(scale.Y), MathF.Floor(scale.Z));
            }

            if (MathF.Min(scale.X, MathF.Min(scale.Y, scale.Z)) < Min)
            {
                _direction = -_direction;
                scale = new Vec3(MathF.Ceiling(scale.X), MathF.Ceiling(scale.Y), MathF.Ceiling(scale.Z));
                _direction = new Vec3(_direction.Z, _direction.X, _direction.Y);
            }

            transform.Scale = scale + _direction * Speed * ctx.Time.Delta;
            ctx.Ecs.Set(_cube, transform);
        }, "scale.ScaleCube");
    }
}
