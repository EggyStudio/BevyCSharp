// Bevy's two_passes example, examples/3d/two_passes.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Renders two 3d passes to the same window from different perspectives.
internal static class TwoPasses
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f)), Transform.At(0f, 0.5f, 0f));
        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);

        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));

        // Drawn after the first and over it, keeping what the first drew rather than clearing it.
        ecs.SpawnCamera3d(
            Transform.LookingAt(new Vec3(10f, 10f, -5f), Vec3.Zero, Vec3.UnitY),
            new CameraSettings { Order = 1, Clear = ClearMode.Keep });
    });
}
