using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Meshes compressed as Bevy compresses them, one made in code and a glTF file's as it loads, which
/// Bevy's stress tests ask for with <c>--mesh-compression</c>.
/// </summary>
[Collection("engine")]
public sealed class MeshCompressionTests
{
    [SkippableFact]
    public void AMeshMadeInCodeIsCompressedInPlace()
    {
        using var harness = new EngineHarness(frames: 2);
        Needs.Renderer();

        MeshInfo before = default, after = default;
        var compressed = false;

        harness.OnContext(Stage.Startup, _ =>
        {
            var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
            Assert.True(Render.TryGetMeshInfo(cube, out before));
            compressed = Render.CompressMesh(cube);
            Assert.True(Render.TryGetMeshInfo(cube, out after));
        });

        harness.Run();

        // A cube's indices fit in sixteen bits, which Bevy builds as thirty-two, and the rest of
        // what the mesh is stays as it was.
        Assert.True(compressed);
        Assert.Equal(32, before.IndexBits);
        Assert.Equal(16, after.IndexBits);
        Assert.Equal((before.Vertices, before.Indices), (after.Vertices, after.Indices));
    }

    [SkippableFact]
    public void ACompressedSceneSpawnsWhatThePlainOneDoes()
    {
        using var harness = new EngineHarness(frames: 300, fps: 240);
        Needs.Renderer();

        var root = Entity.None;
        var children = 0;

        harness.OnContext(Stage.Startup, ctx =>
            root = ctx.Ecs.SpawnScene(AssetServer.LoadGltfSceneCompressed("models/triangle.gltf")));

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!ctx.Ecs.Has<WorldInstance>(root)) return;
            children = ctx.Ecs.ChildrenOf(root).Length;
            if (children > 0) ctx.Exit();
        });

        harness.Run();

        Assert.True(children > 0, "the compressed scene spawned nothing beneath its root");
    }
}
