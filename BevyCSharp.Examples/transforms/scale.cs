// Bevy's scale example, examples/transforms/scale.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Transforms;

// Illustrates how to scale an object along each axis in turn, a cube stretching to five along one
// and back to one before moving on to the next.
internal static class ScaleExample
{
    public static void Build(App app) => app.Startup(ctx => ctx.Ecs.Add(
        SpawnScene(ctx.Ecs, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI / 4f), Vec3.One)),
        new Scaling { ScaleDirection = Vec3.UnitX, ScaleSpeed = 2f, MaxElementSize = 5f, MinElementSize = 1f }), "scale.Setup");

    // A white cube at the middle seen from above and in front, under a directional light, the scene
    // Bevy's transform examples each spawn, which returns the cube.
    private static Entity SpawnScene(EcsWorld ecs, Transform at)
    {
        var cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid), Render.CreateMaterial((1f, 1f, 1f, 1f)), at);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 10f, 20f), Vec3.Zero, Vec3.UnitY));

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
        ecs.Add(sun, Transform.LookingAt(new Vec3(3f, 3f, 3f), Vec3.Zero, Vec3.UnitY));
        return cube;
    }
}

/// <summary>A thing stretching along one axis at a time, between its smallest and largest size.</summary>
[Behavior]
public partial struct Scaling
{
    /// <summary>The axis it stretches along now, negative while it shrinks.</summary>
    public Vec3 ScaleDirection;

    /// <summary>How fast it stretches.</summary>
    public float ScaleSpeed;

    /// <summary>The largest it grows along any axis.</summary>
    public float MaxElementSize;

    /// <summary>The smallest it shrinks to along any axis.</summary>
    public float MinElementSize;

    /// <summary>
    /// Turned back past its largest, and past its smallest turned back and on to the next axis, Y
    /// after X and Z after Y, as Bevy's <c>zxy</c> swizzle moves it.
    /// </summary>
    /// <remarks>
    /// The scale is floored or ceiled to whole numbers as it turns, so the same edge is not crossed
    /// again on the next frame.
    /// </remarks>
    [OnUpdate]
    public void ChangeScaleDirection(BehaviorContext ctx, ref Transform transform)
    {
        var scale = transform.Scale;
        if (MathF.Max(scale.X, MathF.Max(scale.Y, scale.Z)) > MaxElementSize)
        {
            ScaleDirection = -ScaleDirection;
            transform.Scale = new Vec3(MathF.Floor(scale.X), MathF.Floor(scale.Y), MathF.Floor(scale.Z));
        }

        scale = transform.Scale;
        if (MathF.Min(scale.X, MathF.Min(scale.Y, scale.Z)) < MinElementSize)
        {
            ScaleDirection = -ScaleDirection;
            transform.Scale = new Vec3(MathF.Ceiling(scale.X), MathF.Ceiling(scale.Y), MathF.Ceiling(scale.Z));
            ScaleDirection = new Vec3(ScaleDirection.Z, ScaleDirection.X, ScaleDirection.Y);
        }
    }

    /// <summary>Stretched along its axis by its speed times the frame's time.</summary>
    [OnUpdate]
    public void ScaleCube(BehaviorContext ctx, ref Transform transform) =>
        transform.Scale += ScaleDirection * ScaleSpeed * ctx.Time.Delta;
}
