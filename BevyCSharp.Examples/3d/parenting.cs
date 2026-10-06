// Bevy's parenting example, examples/3d/parenting.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Illustrates how to create parent-child relationships between entities and how parent transforms
// are propagated to their descendants.
internal static class Parenting
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var cube = Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f);
        var color = Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f));

        var parent = ctx.Ecs.SpawnMesh(cube, color, Transform.At(0f, 0f, 1f));
        ctx.Ecs.Add(parent, new Rotator());

        // Placed from its parent, so it turns about the parent as the parent turns.
        var child = ctx.Ecs.SpawnMesh(cube, color, Transform.At(0f, 0f, 3f));
        ctx.Ecs.SetParent(child, parent);

        ctx.Ecs.SpawnPointLight(new Vec3(4f, 5f, -4f));
        ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(5f, 10f, 10f), Vec3.Zero, Vec3.UnitY));
    });
}

/// <summary>Turns its entity about X, a radian a second.</summary>
[Behavior]
public partial struct Rotator
{
    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationX(ctx.Time.Delta) * transform.Rotation;
}
