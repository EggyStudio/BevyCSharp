// Bevy's axes example, examples/gizmos/axes.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Gizmo;

// Draws the axes of two cubes as they move, turn and stretch from one random transform to the
// next, each arm as long as the cube's bounding box is from its middle to a corner.
internal static class Axes
{
    internal const float TransitionDuration = 2f;

    // Bevy's SeededRng resource, seeded so the cubes go the same way every run.
    private static Random _random = new(19878367);

    public static void Build(App app) => app.Startup(Setup, "axes.Setup");

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

    private static float Next() => (float)_random.NextDouble();

    // Somewhere in a box in front of the camera, turned any way about any axis, and stretched by
    // up to a little over twice or shrunk to a little under half along each axis.
    internal static Transform RandomTransform()
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
    internal static Transform Interpolate(Transform from, Transform to, float t)
    {
        var translation = from.Translation + (to.Translation - from.Translation) * t;
        var rotation = Quat.Slerp(from.Rotation, to.Rotation, t);
        static float Elerp(float a, float b, float t) => MathF.Pow(2f, (1f - t) * MathF.Log2(a) + t * MathF.Log2(b));
        var scale = new Vec3(Elerp(from.Scale.X, to.Scale.X, t), Elerp(from.Scale.Y, to.Scale.Y, t), Elerp(from.Scale.Z, to.Scale.Z, t));
        return new Transform(translation, rotation, scale);
    }
}

/// <summary>A cube whose axes are drawn, each arm as long as its bounding box is from its middle to a corner.</summary>
[Behavior]
public partial struct ShowAxes
{
    /// <summary>
    /// The axes drawn where this frame moved the cube to, as Bevy chains the two. The bounding box
    /// Bevy keeps for a mesh arrives a frame after the mesh does, and a cube without one yet is
    /// passed over, as Bevy's query of it passes over one.
    /// </summary>
    [OnUpdate]
    [After("TransformTracking.MoveCubes")]
    public void DrawAxes(BehaviorContext ctx, ref Transform transform)
    {
        if (ctx.Ecs.Get<AabbRef>(ctx.Entity) is not { } aabb) return;
        Gizmos.Axes(transform, aabb.HalfExtents.Length, inFront: false);
    }
}

/// <summary>A cube on its way from one transform to the next, and how far along it is.</summary>
[Behavior]
public partial struct TransformTracking
{
    /// <summary>Where it set out from.</summary>
    public Transform Initial;

    /// <summary>Where it is headed.</summary>
    public Transform Target;

    /// <summary>How long it has been on its way, in seconds.</summary>
    public float Progress;

    /// <summary>Moved toward its target over two seconds, and once there given another.</summary>
    [OnUpdate]
    public void MoveCubes(BehaviorContext ctx, ref Transform transform)
    {
        transform = Axes.Interpolate(Initial, Target, Progress / Axes.TransitionDuration);
        if (Progress < Axes.TransitionDuration) Progress += ctx.Time.Delta;
        else (Initial, Target, Progress) = (transform, Axes.RandomTransform(), 0f);
    }
}
