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

    private static AssetHandle _missing;
    private static AssetHandle _file;
}
