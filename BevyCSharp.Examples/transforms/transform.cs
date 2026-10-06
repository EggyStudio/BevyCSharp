// Bevy's transform example, examples/transforms/transform.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Transforms;

// Shows translation, rotation and scale together, a cube flying forward while it turns toward a
// yellow sphere, and the sphere shrinking the farther the cube has come.
internal static class TransformExample
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        var sphere = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Sphere, 3f), Render.CreateMaterial(Color.FromSrgb(1f, 1f, 0f)), Transform.Identity);
        ecs.Add(sphere, new Center { MaxSize = 1f, MinSize = 0.1f, ScaleFactor = 0.05f });

        // Away from the sphere and turned so it flies around it rather than at it.
        var spawn = new Transform(new Vec3(0f, 0f, -10f), Quat.FromRotationY(MathF.PI / 2f), Vec3.One);
        ecs.Add(CubeScene.Spawn(ecs, spawn), new CubeState { StartPos = spawn.Translation, MoveSpeed = 2f, TurnSpeed = 0.2f });
    }, "transform.Setup");
}

/// <summary>A cube that flies forward while it turns toward the centers, and so circles them.</summary>
[Behavior]
public partial struct CubeState
{
    /// <summary>Where it started.</summary>
    public Vec3 StartPos;

    /// <summary>How fast it flies.</summary>
    public float MoveSpeed;

    /// <summary>How fast it turns toward the centers, the smaller the wider its circle.</summary>
    public float TurnSpeed;

    /// <summary>Moved forward, along its own -Z, at its speed.</summary>
    [OnUpdate]
    public void MoveCube(BehaviorContext ctx, ref Transform transform) =>
        transform.Translation += transform.Rotation * -Vec3.UnitZ * MoveSpeed * ctx.Time.Delta;

    /// <summary>
    /// Turned a little of the way toward facing the centers each frame, up being the cube's own,
    /// after it has moved, as Bevy chains the two.
    /// </summary>
    [OnUpdate]
    [After("CubeState.MoveCube")]
    public void RotateCube(BehaviorContext ctx, ref Transform transform)
    {
        // The point it circles, the sum of where the centers are, as Bevy's adds them up.
        var center = Vec3.Zero;
        foreach (var sphere in ctx.Ecs.Query<Center>(markChanged: false)) center += ctx.Ecs.GetOrDefault<Transform>(sphere.Entity).Translation;

        var facing = Transform.LookingAt(transform.Translation, center, transform.Rotation * Vec3.UnitY).Rotation;
        transform.Rotation = Quat.Lerp(transform.Rotation, facing, TurnSpeed * ctx.Time.Delta);
    }
}

/// <summary>A sphere at the middle, which shrinks the farther the cubes have come from where they started.</summary>
[Behavior]
public partial struct Center
{
    /// <summary>Its size while the cubes are where they started.</summary>
    public float MaxSize;

    /// <summary>The smallest it shrinks to.</summary>
    public float MinSize;

    /// <summary>How much it shrinks for each unit the cubes have come.</summary>
    public float ScaleFactor;

    /// <summary>
    /// Sized by how far the cubes have come all told, after they have moved and turned, as Bevy
    /// chains the three.
    /// </summary>
    [OnUpdate]
    [After("CubeState.RotateCube")]
    public void ScaleDownSphereProportionalToCubeTravelDistance(BehaviorContext ctx, ref Transform transform)
    {
        var distances = 0f;
        foreach (var cube in ctx.Ecs.Query<CubeState>(markChanged: false))
            distances += (cube.Component.StartPos - ctx.Ecs.GetOrDefault<Transform>(cube.Entity).Translation).Length;

        transform.Scale = new Vec3(MathF.Max(MaxSize - ScaleFactor * distances, MinSize));
    }
}
