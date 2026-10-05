using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Shows a 2D mesh's vertex colors, blended across it on their own and multiplying an image.
internal static class Mesh2dVertexColorTexture
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        // Bevy's unit rectangle, with its corners in the order Bevy builds them, top right first and
        // around, each given red, green, blue or white.
        var mesh = Render.CreateMesh(new MeshData
        {
            Positions = [new(0.5f, 0.5f, 0f), new(-0.5f, 0.5f, 0f), new(-0.5f, -0.5f, 0f), new(0.5f, -0.5f, 0f)],
            Normals = [Vec3.UnitZ, Vec3.UnitZ, Vec3.UnitZ, Vec3.UnitZ],
            Uvs = [1f, 0f, 0f, 0f, 0f, 1f, 1f, 1f],
            Colors = [1f, 0f, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 0f, 1f, 1f, 1f, 1f, 1f, 1f],
            Indices = [0, 1, 2, 0, 2, 3],
        });

        // The colors alone on the left, and on the right tinting Bevy's banner.
        foreach (var (x, image) in new[] { (-96f, AssetHandle.None), (96f, AssetServer.Load(AssetKind.Image, "branding/banner.png")) })
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, new Transform(new Vec3(x, 0f, 0f), Quat.Identity, new Vec3(128f)));
            Render2d.SetMesh(ecs, entity, mesh);
            Render2d.SetMaterial(ecs, entity, Render2d.CreateMaterial(new ColorMaterialSettings { Texture = image }));
        }
    }, "mesh2d_vertex_color_texture.Setup");
}
