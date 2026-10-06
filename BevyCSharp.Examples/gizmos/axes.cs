// Bevy's axes example, examples/gizmos/axes.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Gizmo;

// Draws the axes of two cubes as they move, turn and stretch from one random transform to the
// next, each arm as long as the cube's bounding box is from its middle to a corner.
internal static class Axes
{
    private const float TransitionDuration = 2f;

    internal struct ShowAxes;

    internal struct TransformTracking
    {
        public Transform Initial;
        public Transform Target;
        public float Progress;
    }

    // Seeded, as Bevy's is, so the cubes go the same way every run.
    private static Random _random = new(19878367);

    public static void Build(App app)
    {
        app.Startup(Setup, "axes.Setup");

        // Bevy chains the two, so the axes are drawn where this frame moved the cubes to.
        app.Update(MoveCubes, "axes.MoveCubes");
        app.Update(DrawAxes, "axes.DrawAxes");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _random = new Random(19878367);
        ecs.SpawnPointLight(new Vec3(2f, 6f, 0f), shadows: true);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 1.5f, -8f), new Vec3(0f, -0.5f, 0f), Vec3.UnitY));

        foreach (var (size, color) in new[] { (1f, Color.FromSrgb(0.8f, 0.7f, 0.6f)), (0.5f, Color.FromSrgb(0.6f, 0.7f, 0.8f)) })
        {
            var cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, size, size, size), Render.CreateMaterial(color), Transform.Identity);
            ecs.Add(cube, new ShowAxes());
            ecs.Add(cube, new TransformTracking { Initial = Transform.Identity, Target = RandomTransform() });
        }

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 20f, 20f), Render.CreateMaterial(Color.FromSrgb(0.1f, 0.1f, 0.1f)), Transform.At(0f, -2f, 0f));
    }

    // The bounding box Bevy keeps for each mesh arrives a frame after the mesh does, and a cube
    // without one yet is passed over, as Bevy's query of it passes over one.
    private static void DrawAxes(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var entity in ecs.EntitiesWith<ShowAxes>())
        {
            if (ecs.Get<AabbRef>(entity) is not { } aabb) continue;
            Gizmos.Axes(ecs.GetOrDefault<Transform>(entity), aabb.HalfExtents.Length, inFront: false);
        }
    }

    // Each cube moves toward its target over two seconds, and once there is given another.
    private static void MoveCubes(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var entity in ecs.EntitiesWith<TransformTracking>())
        {
            var tracking = ecs.GetOrDefault<TransformTracking>(entity);
            var transform = Interpolate(tracking.Initial, tracking.Target, tracking.Progress / TransitionDuration);
            ecs.Set(entity, transform);

            if (tracking.Progress < TransitionDuration)
            {
                tracking.Progress += ctx.Time.Delta;
            }
            else
            {
                (tracking.Initial, tracking.Target, tracking.Progress) = (transform, RandomTransform(), 0f);
            }

            ecs.Set(entity, tracking);
        }
    }

    private static float Next() => (float)_random.NextDouble();

    // Somewhere in a box in front of the camera, turned any way about any axis, and stretched by
    // up to a little over twice or shrunk to a little under half along each axis.
    private static Transform RandomTransform()
    {
        var translation = new Vec3(Next() * 10f - 5f, Next() * 2f - 1f, Next() * 8f - 2f);

        // A direction spread evenly over the sphere, by its height and its angle around.
        var height = Next() * 2f - 1f;
        var theta = Next() * 2f * MathF.PI;
        var around = MathF.Sin(MathF.Acos(height));
        var direction = new Vec3(MathF.Cos(theta) * around, MathF.Sin(theta) * around, height);
        var rotation = Quat.FromAxisAngle(direction, Next() * 2f * MathF.PI);

        var scale = new Vec3(MathF.Pow(2f, Next() * 2.4f - 1.2f), MathF.Pow(2f, Next() * 2.4f - 1.2f), MathF.Pow(2f, Next() * 2.4f - 1.2f));
        return new Transform(translation, rotation, scale);
    }

    // Position in a straight line, rotation along the shortest turn, and scale evenly in its
    // logarithm, so a stretch and the shrink back take as long as each other.
    private static Transform Interpolate(Transform from, Transform to, float t)
    {
        var translation = from.Translation + (to.Translation - from.Translation) * t;
        var rotation = Quat.Slerp(from.Rotation, to.Rotation, t);
        static float Elerp(float a, float b, float t) => MathF.Pow(2f, (1f - t) * MathF.Log2(a) + t * MathF.Log2(b));
        var scale = new Vec3(Elerp(from.Scale.X, to.Scale.X, t), Elerp(from.Scale.Y, to.Scale.Y, t), Elerp(from.Scale.Z, to.Scale.Z, t));
        return new Transform(translation, rotation, scale);
    }
}
