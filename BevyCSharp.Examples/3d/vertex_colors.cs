// Bevy's vertex_colors example, examples/3d/vertex_colors.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Illustrates the use of vertex colors.
internal static class VertexColors
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);

        // A cube whose every vertex is colored by where it is.
        if (!Render.TryReadMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), out var cube) || cube is null)
            throw new InvalidOperationException("The cube's vertices could not be read back.");

        cube.Colors = [.. cube.Positions.SelectMany(p => new[] { (1f - p.X) / 2f, (1f - p.Y) / 2f, (1f - p.Z) / 2f, 1f })];
        ecs.SpawnMesh(Render.CreateMesh(cube), Render.CreateMaterial((1f, 1f, 1f, 1f)), Transform.At(0f, 0.5f, 0f));

        var light = ecs.SpawnPointLight(new Vec3(4f, 5f, 4f), shadows: true);
        ecs.Set(light, Transform.LookingAt(new Vec3(4f, 5f, 4f), Vec3.Zero, Vec3.UnitY));

        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
    });
}
