// Bevy's 3d_scene example, examples/3d/3d_scene.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// A simple 3D scene with light shining over a cube sitting on a plane.
internal static class Example3dScene
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        // A circular base, which Bevy's circle makes facing +Z, laid flat.
        ctx.Ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Circle, 4f),
            Render.CreateMaterial((1f, 1f, 1f, 1f)),
            new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));

        ctx.Ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f),
            Render.CreateMaterial(Color.FromSrgb8(124, 144, 255)),
            Transform.At(0f, 0.5f, 0f));

        ctx.Ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
        ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));
    });
}
