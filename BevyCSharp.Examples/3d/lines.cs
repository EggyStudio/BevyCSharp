// Bevy's lines example, examples/3d/lines.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Create a custom material to draw basic lines in 3D.
//
// Bevy's material is a shader that gives every fragment one color, which an unlit material of
// that color does here.
internal static class Lines
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        // A list of lines, each two vertices.
        var list = new MeshData
        {
            Topology = MeshTopology.Lines,
            Positions = [Vec3.Zero, new(1f, 1f, 0f), new(1f, 1f, 0f), new(1f, 0f, 0f)],
        };
        ctx.Ecs.Mesh(Render.CreateMesh(list), Unlit(0f, 1f, 0f), Transform.At(-1.5f, 0f, 0f));

        // A strip of lines, broken in two by the index that restarts it.
        var strip = new MeshData
        {
            Topology = MeshTopology.LineStrip,
            Positions = [Vec3.Zero, new(1f, 1f, 0f), new(2f, 0f, 0f), new(2f, 1f, 0f), new(3f, 1f, 0f)],
            Indices = [0, 1, uint.MaxValue, 2, 3, 4],
        };
        ctx.Ecs.Mesh(Render.CreateMesh(strip), Unlit(0f, 0f, 1f), Transform.At(0.5f, 0f, 0f));

        ctx.Ecs.Camera(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
    });

    private static AssetHandle Unlit(float r, float g, float b) =>
        Render.CreateMaterial(new MaterialSettings { BaseColor = (r, g, b, 1f), Unlit = true });
}
