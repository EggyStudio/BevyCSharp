// Bevy's custom_projection example, examples/camera/custom_projection.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Cameras;

// Bevy's 3d_scene seen through an oblique projection, the view sheared toward a side and upward,
// as a portal or a reflection in water draws. Bevy declares the projection as a type of its own
// that writes two entries of the perspective's matrix, and this writes the same two through the
// bridge's adjusted perspective, column 2's first two rows.
internal static class CustomProjection
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;

        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));
        Render.SetAdjustedPerspective(camera, [(2, 0, 0.2f), (2, 1, 0.6f)]);

        // A circular base, which Bevy's circle makes facing +Z, laid flat.
        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Circle, 4f),
            Render.CreateMaterial((1f, 1f, 1f, 1f)),
            new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));

        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f),
            Render.CreateMaterial(Color.FromSrgb8(124, 144, 255)),
            Transform.At(0f, 0.5f, 0f));

        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
    }, "custom_projection.Setup");
}
