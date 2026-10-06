// Bevy's 3d_viewport_to_world example, examples/3d/3d_viewport_to_world.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// This example demonstrates how to use the camera's viewport to world method, drawing a circle on
// the ground where the cursor points. An offscreen run has no cursor, so it draws the ground alone.
internal static class Example3dViewportToWorld
{
    internal static Entity Camera;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ground = ctx.Ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 20f, 20f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
        ctx.Ecs.Add(ground, new Ground());

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
        ctx.Ecs.Add(sun, Transform.LookingAt(Vec3.One, Vec3.Zero, Vec3.UnitY));

        Camera = ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(15f, 5f, 15f), Vec3.Zero, Vec3.UnitY));
    }, "3d_viewport_to_world.Setup");
}

/// <summary>The ground the cursor is drawn on.</summary>
[Behavior]
public partial struct Ground
{
    /// <summary>
    /// A circle drawn where the ray under the cursor meets the ground's plane, a hair above it,
    /// facing the ground's up.
    /// </summary>
    [OnUpdate]
    public void DrawCursor(BehaviorContext ctx, in Transform transform)
    {
        var (x, y) = ctx.Input.MousePosition;
        if (!Render.TryRay(Example3dViewportToWorld.Camera, x, y, out var origin, out var direction)) return;

        var up = transform.Rotation * Vec3.UnitY;
        var facing = Vec3.Dot(direction, up);
        if (MathF.Abs(facing) < 1e-6f) return;
        var distance = Vec3.Dot(transform.Translation - origin, up) / facing;
        if (distance < 0f) return;

        var point = origin + direction * distance;
        Gizmos.Circle(point + up * 0.01f, Quat.FromRotationX(-MathF.PI / 2f), 0.2f, (1f, 1f, 1f, 1f), inFront: false);
    }
}
