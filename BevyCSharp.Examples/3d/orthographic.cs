// Bevy's orthographic example, examples/3d/orthographic.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Shows how to create a 3D orthographic view (for isometric-look in games or CAD applications).
internal static class Orthographic
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        // Six world units the height of the window.
        ctx.Ecs.Camera(
            Transform.LookingAt(new Vec3(5f, 5f, 5f), Vec3.Zero, Vec3.UnitY),
            new CameraSettings { Projection = CameraProjection.Orthographic, Height = 6f });

        ctx.Ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Scene.Material(Scene.Srgb(0.3f, 0.5f, 0.3f)), Transform.Identity);

        var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        var color = Scene.Material(Scene.Srgb(0.8f, 0.7f, 0.6f));
        foreach (var (x, z) in new[] { (1.5f, 1.5f), (1.5f, -1.5f), (-1.5f, 1.5f), (-1.5f, -1.5f) })
            ctx.Ecs.Mesh(cube, color, Transform.At(x, 0.5f, z));

        ctx.Ecs.PointLight(new Vec3(3f, 8f, 5f));
    });
}
