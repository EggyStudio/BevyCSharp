using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Renders two 3d passes to the same window from different perspectives.
internal static class TwoPasses
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Scene.Material(Scene.Srgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
        ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Scene.Material(Scene.Srgb(0.8f, 0.7f, 0.6f)), Transform.At(0f, 0.5f, 0f));
        ecs.PointLight(new Vec3(4f, 8f, 4f), shadows: true);

        ecs.Camera(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));

        // Drawn after the first and over it, keeping what the first drew rather than clearing it.
        ecs.Camera(
            Transform.LookingAt(new Vec3(10f, 10f, -5f), Vec3.Zero, Vec3.UnitY),
            new CameraSettings { Order = 1, Clear = ClearMode.Keep });
    });
}
