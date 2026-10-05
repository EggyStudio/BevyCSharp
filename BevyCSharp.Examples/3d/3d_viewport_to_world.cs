// Bevy's 3d_viewport_to_world example, examples/3d/3d_viewport_to_world.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// This example demonstrates how to use the camera's viewport to world method, drawing a circle on
// the ground where the cursor points. An offscreen run has no cursor, so it draws the ground alone.
internal static class Example3dViewportToWorld
{
    private static Entity _camera;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            ctx.Ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 20f, 20f), Scene.Material(Scene.Srgb(0.3f, 0.5f, 0.3f)), Transform.Identity);

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
            ctx.Ecs.Add(sun, Transform.LookingAt(Vec3.One, Vec3.Zero, Vec3.UnitY));

            _camera = ctx.Ecs.Camera(Transform.LookingAt(new Vec3(15f, 5f, 15f), Vec3.Zero, Vec3.UnitY));
        });

        app.Update(ctx =>
        {
            var (x, y) = ctx.Input.MousePosition;
            if (!Render.TryRay(_camera, x, y, out var origin, out var direction)) return;

            // Where the ray meets the ground, which is the plane through the origin facing up.
            if (MathF.Abs(direction.Y) < 1e-6f) return;
            var distance = -origin.Y / direction.Y;
            if (distance < 0f) return;

            var point = origin + direction * distance;
            Gizmos.Circle(point + Vec3.UnitY * 0.01f, Quat.FromRotationX(-MathF.PI / 2f), 0.2f, (1f, 1f, 1f, 1f), inFront: false);
        }, "3d_viewport_to_world.DrawCursor");
    }
}
