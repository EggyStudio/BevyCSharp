using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers meshes made in memory kept in files of their own: a primitive as its shape, a mesh built
/// vertex by vertex as its geometry, each shared by the scene that refers to it.
/// </summary>
[Collection("engine")]
public sealed class MeshFileTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-meshes-" + Guid.NewGuid().ToString("n"));
    private readonly string _was = Streaming.AssetRoot;

    public MeshFileTests()
    {
        Directory.CreateDirectory(_root);
        Streaming.AssetRoot = _root;
        AssetIds.Reindex();
    }

    public void Dispose()
    {
        Streaming.AssetRoot = _was;
        AssetIds.Reindex();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void APrimitiveAndABuiltMeshAreSavedAsFilesAndSharedByTheScene()
    {
        var scene = Path.Combine(_root, "yard.scene.json");
        var ran = false;

        var ramp = new MeshData
        {
            Positions = [new Vec3(0f, 0f, 0f), new Vec3(2f, 0f, 0f), new Vec3(2f, 1f, 0f)],
            Uvs = [0f, 0f, 1f, 0f, 1f, 1f],
            Indices = [0, 1, 2],
        };

        using (var saving = new EngineHarness(frames: 2))
        {
            saving.OnContext(Stage.Startup, ctx =>
            {
                if (!App.HasRenderer) return;
                Streaming.AssetRoot = _root;

                var post = Render.CreateMesh(MeshShape.Cylinder, 0.1f, 2f);
                var built = Render.CreateMesh(ramp);
                MeshFiles.SaveAs(post, "props/post.mesh.json");
                MeshFiles.SaveAs(built, "props/ramp.mesh.json");

                // A handle this side made no mesh from, here a material, has nothing to write.
                Assert.Throws<ArgumentException>(() => MeshFiles.SaveAs(Render.CreateMaterial(new MaterialSettings()), "x.mesh.json"));

                foreach (var (name, mesh) in new[] { ("Post A", post), ("Post B", post), ("Ramp", built) })
                {
                    var thing = ctx.Ecs.Spawn();
                    ctx.Ecs.SetName(thing, name);
                    ctx.Ecs.Add(thing, Transform.Identity);
                    Render.SetMesh(ctx.Ecs, thing, mesh);
                }

                SceneFile.Save(ctx.Ecs, scene);
            });

            saving.Run();
        }

        if (!App.HasRenderer) return;

        var text = File.ReadAllText(scene);
        Assert.Contains("props/post.mesh.json", text);
        Assert.DoesNotContain("\"resources\"", text);

        using var loading = new EngineHarness(frames: 2);
        loading.OnContext(Stage.Startup, ctx =>
        {
            Streaming.AssetRoot = _root;

            var loaded = SceneFile.Load(ctx.Ecs, scene);
            Entity Named(string name) => loaded.Entities.Single(entity => ctx.Ecs.NameOf(entity) == name);

            var a = Render.MeshOf(ctx.Ecs, Named("Post A"));
            Assert.Equal(a, Render.MeshOf(ctx.Ecs, Named("Post B")));
            Assert.Equal(new MeshRecipe(MeshShape.Cylinder, 0.1f, 2f, 1f), Render.RecipeOf(a));

            var built = Render.DataOf(Render.MeshOf(ctx.Ecs, Named("Ramp")));
            Assert.NotNull(built);
            Assert.Equal(ramp.Positions, built.Positions);
            Assert.Equal(ramp.Uvs, built.Uvs);
            ran = true;
        });

        loading.Run();
        Assert.True(ran);
    }
}
