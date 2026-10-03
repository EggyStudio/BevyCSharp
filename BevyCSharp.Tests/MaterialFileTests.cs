using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers materials kept in files of their own: saved, shared by a scene's entities, and read again
/// when the file changes.
/// </summary>
/// <remarks>
/// A material is the renderer's asset, so these return early on a headless bridge. The asset root
/// points at a directory of the test's own.
/// </remarks>
[Collection("engine")]
public sealed class MaterialFileTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-materials-" + Guid.NewGuid().ToString("n"));
    private readonly string _was = Streaming.AssetRoot;

    public MaterialFileTests()
    {
        Directory.CreateDirectory(_root);
        Streaming.AssetRoot = _root;
        AssetIds.Reindex();
    }

    public void Dispose()
    {
        DataAssets.Watching = false;
        Streaming.AssetRoot = _was;
        AssetIds.Reindex();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void AMaterialSavedToAFileIsSharedByTheSceneThatUsesIt()
    {
        var scene = Path.Combine(_root, "room.scene.json");
        var ran = false;

        using (var saving = new EngineHarness(frames: 2))
        {
            saving.OnContext(Stage.Startup, ctx =>
            {
                if (!App.HasRenderer) return;

                // The harness points the root at its own assets, and these files go in the test's.
                Streaming.AssetRoot = _root;

                var paint = Render.CreateMaterial(new MaterialSettings { BaseColor = (0.9f, 0.1f, 0.1f, 1f), Roughness = 0.2f });
                MaterialFiles.SaveAs(paint, "paints/red.material.json");
                Assert.Equal("paints/red.material.json", MaterialFiles.PathOf(paint));

                foreach (var name in new[] { "Left", "Right" })
                {
                    var wall = ctx.Ecs.Spawn();
                    ctx.Ecs.SetName(wall, name);
                    ctx.Ecs.Add(wall, Transform.Identity);
                    Render.SetMesh(ctx.Ecs, wall, Render.CreateMesh(MeshShape.Cuboid));
                    Render.SetMaterial(ctx.Ecs, wall, paint);
                }

                SceneFile.Save(ctx.Ecs, scene);
            });

            saving.Run();
        }

        if (!App.HasRenderer) return;

        // The scene names the file rather than writing the settings out again.
        var text = File.ReadAllText(scene);
        Assert.Contains("paints/red.material.json", text);
        Assert.DoesNotContain("\"roughness\"", text);
        Assert.True(File.Exists(Path.Combine(_root, "paints/red.material.json.uid")));

        using var loading = new EngineHarness(frames: 2);
        loading.OnContext(Stage.Startup, ctx =>
        {
            Streaming.AssetRoot = _root;

            var loaded = SceneFile.Load(ctx.Ecs, scene);
            var left = Render.MaterialOf(ctx.Ecs, loaded.Entities.Single(entity => ctx.Ecs.NameOf(entity) == "Left"));
            var right = Render.MaterialOf(ctx.Ecs, loaded.Entities.Single(entity => ctx.Ecs.NameOf(entity) == "Right"));

            Assert.Equal(left, right);
            Assert.True(Render.TryReadMaterial(left, out var settings));
            Assert.Equal(0.2f, settings!.Roughness);
            ran = true;
        });

        loading.Run();
        Assert.True(ran);
    }

    [Fact]
    public void AMaterialFileChangedOnDiskChangesTheMaterialInPlace()
    {
        var seen = 0f;

        using var harness = new EngineHarness(frames: 200, fps: 120);
        if (!App.HasRenderer) return;

        var material = AssetHandle.None;
        var file = Path.Combine(_root, "blue.material.json");

        harness.OnContext(Stage.Startup, ctx =>
        {
            Streaming.AssetRoot = _root;
            DataAssets.Watching = true;

            var made = Render.CreateMaterial(new MaterialSettings { Roughness = 0.5f });
            MaterialFiles.SaveAs(made, "blue.material.json");
            material = MaterialFiles.Load("blue.material.json");
            Assert.Equal(made, material);
        });

        var edited = false;
        harness.OnContext(Stage.Update, ctx =>
        {
            if (!edited && ctx.Time.FrameCount > 5)
            {
                // Edited outside, as a text editor would.
                File.WriteAllText(file, File.ReadAllText(file).Replace("\"roughness\": 0.5", "\"roughness\": 0.9"));
                edited = true;
            }

            if (Render.TryReadMaterial(material, out var settings)) seen = settings!.Roughness;
            if (seen == 0.9f) ctx.Exit();
        });

        harness.Run();
        Assert.Equal(0.9f, seen);
    }
}
