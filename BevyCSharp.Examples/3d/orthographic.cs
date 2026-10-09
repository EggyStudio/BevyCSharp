// Bevy's orthographic example, examples/3d/orthographic.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Shows how to create a 3D orthographic view (for isometric-look in games or CAD applications).
internal static class Orthographic
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        // Six world units the height of the window.
        ctx.Ecs.SpawnCamera3d(
            Transform.LookingAt(new Vec3(5f, 5f, 5f), Vec3.Zero, Vec3.UnitY),
            new CameraSettings { Projection = CameraProjection.Orthographic, Height = 6f });

        ctx.Ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);

        var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        var color = Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f));
        foreach (var (x, z) in new[] { (1.5f, 1.5f), (1.5f, -1.5f), (-1.5f, 1.5f), (-1.5f, -1.5f) })
            ctx.Ecs.SpawnMesh(cube, color, Transform.At(x, 0.5f, z));

        ctx.Ecs.SpawnPointLight(new Vec3(3f, 8f, 5f));
    });
}
