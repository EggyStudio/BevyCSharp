using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers loading geometry out of a glTF file.
/// </summary>
/// <remarks>
/// These load a real file, <c>assets/models/triangle.gltf</c>, which is one mesh of one primitive
/// with one material, written by hand so it stays small enough to read. Loading is asynchronous,
/// so each test runs frames until the handle settles rather than asserting on the first one.
/// </remarks>
[Collection("engine")]
public sealed class GltfTests
{
    private const string Model = "models/triangle.gltf";

    [Fact]
    public void AMeshLoadsOutOfAGltfFile()
    {
        using var harness = new EngineHarness(frames: 40, fps: 120);
        if (!App.HasRenderer) return;

        var handle = AssetHandle.None;
        var state = AssetLoadState.Loading;

        harness.OnContext(Stage.Startup, _ => handle = AssetServer.LoadGltfMesh(Model));

        harness.OnContext(Stage.Update, ctx =>
        {
            state = handle.State;
            if (state != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        Assert.True(handle.IsValid);
        Assert.Equal(AssetLoadState.Loaded, state);
    }

    [Fact]
    public void AGltfMaterialNeedsTheRendererThatTranslatesIt()
    {
        // The material a file describes is not the one the renderer draws with, and the plugin
        // that translates between them comes with the window. A windowless run has to say so
        // rather than wait, which is what a caller polling the state depends on.
        using var harness = new EngineHarness(frames: 60, fps: 240);
        if (!App.HasRenderer) return;

        var state = AssetLoadState.Loading;

        harness.OnContext(Stage.Startup, _ => _material = AssetServer.LoadGltfMaterial(Model));

        harness.OnContext(Stage.Update, ctx =>
        {
            state = _material.State;
            if (state != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        // Windowless here, so the translated material was never published. In a windowed run this
        // is the artist's own material, and the same call returns it.
        Assert.Equal(AssetLoadState.Failed, state);
    }

    [Fact]
    public void AGltfMeshIsAnOrdinaryMeshHandle()
    {
        // The point of loading the part rather than the file: what comes back is the same kind of
        // handle Render.CreateMesh produces, so nothing downstream needs a glTF-shaped path.
        using var harness = new EngineHarness(frames: 40, fps: 120);
        if (!App.HasRenderer) return;

        var entity = Entity.None;
        var attached = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            entity = ctx.Ecs.Spawn();
            Render.SetMesh(ctx.Ecs, entity, AssetServer.LoadGltfMesh(Model));

            // The file's own material is not reachable, so the entity gets a built one. See the
            // remarks on LoadGltfMesh.
            Render.SetMaterial(ctx.Ecs, entity, Render.CreateMaterial(0.6f, 0.6f, 0.62f));
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            // Attaching a mesh goes through Bevy's own insert, which brings what it requires.
            attached = ctx.Ecs.HasById(entity, NativeComponents.Transform);
            ctx.Exit();
        });

        harness.Run();

        Assert.True(attached);
    }

    [Fact]
    public void AskingForAPartThatIsNotThereFailsRatherThanHangs()
    {
        // A label naming a mesh the file does not define is a load failure, not a load that never
        // finishes, so a caller polling the state gets an answer.
        using var harness = new EngineHarness(frames: 40, fps: 120);
        if (!App.HasRenderer) return;

        var state = AssetLoadState.Loading;

        harness.OnContext(Stage.Startup, _ => _missing = AssetServer.LoadGltfMesh(Model, mesh: 7));

        harness.OnContext(Stage.Update, ctx =>
        {
            state = _missing.State;
            if (state != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        Assert.Equal(AssetLoadState.Failed, state);
    }

    [Fact]
    public void TheGltfKindLoadsTheWholeFile()
    {
        using var harness = new EngineHarness(frames: 40, fps: 120);
        if (!App.HasRenderer) return;

        var state = AssetLoadState.Loading;

        harness.OnContext(Stage.Startup, _ => _file = AssetServer.Load(AssetKind.Gltf, Model));

        harness.OnContext(Stage.Update, ctx =>
        {
            state = _file.State;
            if (state != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        Assert.Equal(AssetLoadState.Loaded, state);
    }

    [Fact]
    public void AGltfSceneSpawnsTheEntitiesTheFileDescribes()
    {
        // The other half of a glTF file: not one mesh, but the arrangement an artist laid out.
        // The scene entity comes back at once and fills in when the asset has loaded, so the
        // test waits on WorldInstance rather than on a frame count.
        using var harness = new EngineHarness(frames: 300, fps: 240);
        if (!App.HasRenderer) return;

        var root = Entity.None;
        var ready = false;
        var children = 0;
        var readyBeforeChildren = false;

        harness.OnContext(Stage.Startup, ctx =>
            root = ctx.Ecs.SpawnScene(AssetServer.LoadGltfScene(Model)));

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!ctx.Ecs.Has<WorldInstance>(root)) return;

            ready = true;
            children = ctx.Ecs.ChildrenOf(root).Length;

            // WorldInstance marks the spawn as done, but the children it produced are not
            // always visible in the same frame, so waiting on the component alone is a race.
            if (children == 0)
            {
                readyBeforeChildren = true;
                return;
            }

            ctx.Exit();
        });

        harness.Run();

        Assert.NotEqual(Entity.None, root);
        Assert.True(ready, "the scene never finished spawning");
        Assert.True(children > 0, $"the scene spawned nothing beneath its root (raced: {readyBeforeChildren})");
    }

    [Fact]
    public void ASpawnedSceneCanBeComposedOnTopOf()
    {
        // What replaces bsn! on this side: spawn what the file describes, then patch it through
        // the ordinary ECS surface. Here the artist's placement is overwritten and a component
        // the file knows nothing about is added.
        using var harness = new EngineHarness(frames: 300, fps: 240);
        if (!App.HasRenderer) return;

        var root = Entity.None;
        var patched = 0;
        var moved = Vec3.Zero;

        harness.OnContext(Stage.Startup, ctx =>
            root = ctx.Ecs.SpawnScene(AssetServer.LoadGltfScene(Model)));

        harness.OnContext(Stage.Update, ctx =>
        {
            if (patched > 0) return;

            foreach (var child in ctx.Ecs.ChildrenOf(root))
            {
                ctx.Ecs.Add(child, Transform.At(3f, 0f, 0f));
                ctx.Ecs.Add(child, new SceneTag());
                patched++;
            }
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            if (patched == 0) return;

            foreach (var row in ctx.Ecs.Query<SceneTag>(markChanged: false))
                moved = ctx.Ecs.GetRef<Transform>(row.Entity).Translation;

            ctx.Exit();
        });

        harness.Run();

        Assert.True(patched > 0, "nothing was there to patch");
        Assert.Equal(new Vec3(3f, 0f, 0f), moved);
    }

    [Fact]
    public void SpawningAHandleThatIsNotASceneIsRefused()
    {
        using var harness = new EngineHarness(frames: 4);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var ex = Assert.Throws<BevyNativeException>(
                () => ctx.Ecs.SpawnScene(AssetHandle.None));

            Assert.Equal(NativeStatus.NoComponent, ex.Status);
        });

        harness.Run();
    }

    /// <summary>Added to a scene's entities, to show composition reached them.</summary>
    private struct SceneTag;

    private static AssetHandle _missing;
    private static AssetHandle _file;
    private static AssetHandle _material;
}
