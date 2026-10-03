using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers reading the managed side's asset files from the folder or, where a game compiled them
/// into itself, from its assembly, with the ids they carry.
/// </summary>
/// <remarks>
/// The test assembly carries <c>embedded/</c> under <c>assets/</c>, as an exported game carries its
/// asset files. The asset root and the assembly looked in are static, so these share the engine
/// collection and put both back.
/// </remarks>
[Collection("engine")]
public sealed class AssetFilesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-files-" + Guid.NewGuid().ToString("n"));
    private readonly string _was = Streaming.AssetRoot;

    public AssetFilesTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "carried"));
        Streaming.AssetRoot = _root;
        AssetFiles.Use(typeof(AssetFilesTests).Assembly);
        AssetIds.Reindex();
    }

    public void Dispose()
    {
        AssetFiles.Use(null);
        Streaming.AssetRoot = _was;
        AssetIds.Reindex();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void AFileNotOnDiskIsReadFromTheAssemblyAndOneOnDiskWins()
    {
        Assert.True(AssetFiles.HasResources);
        Assert.True(AssetFiles.Exists("carried/note.json"));
        Assert.Contains("from the assembly", AssetFiles.ReadAllText("carried/note.json"));

        // Asked for by its full path under the root, as a scene path resolves.
        Assert.Contains("from the assembly", AssetFiles.ReadAllText(Path.Combine(_root, "carried", "note.json")));

        // A file beside the game replaces the one it carries.
        File.WriteAllText(Path.Combine(_root, "carried", "note.json"), """{ "carried": "from the folder" }""");
        Assert.Contains("from the folder", AssetFiles.ReadAllText("carried/note.json"));

        // Neither has it.
        Assert.False(AssetFiles.Exists("carried/missing.json"));
        Assert.Throws<FileNotFoundException>(() => AssetFiles.ReadAllText("carried/missing.json"));

        // A path outside the root is never looked for among the resources.
        Assert.False(AssetFiles.Exists(Path.Combine(Path.GetTempPath(), "carried", "note.json")));
    }

    [SkippableFact]
    public void BevyReadsAModelTheAssemblyCarriesThroughTheSharedBridge()
    {
        // The model is nowhere on disk, under the harness's asset root or this one, so Bevy can
        // only have read it through the reader the managed side hands the bridge.
        using var harness = new EngineHarness(frames: 60, fps: 120, carried: typeof(AssetFilesTests).Assembly);
        Needs.Renderer();

        var carried = AssetHandle.None;
        var absent = AssetHandle.None;
        var states = (Carried: AssetLoadState.Loading, Absent: AssetLoadState.Loading);

        harness.OnContext(Stage.Startup, _ =>
        {
            carried = AssetServer.LoadGltfMesh("carried/triangle.gltf");
            absent = AssetServer.LoadGltfMesh("carried/absent.gltf");
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            states = (carried.State, absent.State);
            if (states.Carried != AssetLoadState.Loading && states.Absent != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        Assert.Equal(AssetLoadState.Loaded, states.Carried);
        Assert.Equal(AssetLoadState.Failed, states.Absent);
    }

    [Fact]
    public void AStreamedReadTakesItsPartOfACarriedFile()
    {
        var whole = AssetFiles.ReadAllBytes("carried/note.json");
        byte[]? bytes = null;
        var read = default(StreamRead);

        using var engine = new EngineHarness(frames: 200, fps: 120, carried: typeof(AssetFilesTests).Assembly);
        engine.On(Stage.Update, _ =>
        {
            if (read.Ticket == 0) read = Streaming.Read("carried/note.json", offset: 2, length: 5);
            else if (bytes is null && Streaming.TryTake(read, out var arrived)) bytes = arrived;

            if (bytes is not null) App.RequestExit();
        });
        engine.Run();

        Assert.Equal(whole[2..7], bytes);
    }

    [Fact]
    public void AnIdTheAssemblyCarriesIsTrustedWithoutItsFile()
    {
        // The model is in the bridge, where the managed side cannot look, so its id is taken as
        // given rather than checked against a file that is not on disk.
        Assert.Equal("carried/ship.glb", AssetIds.PathOf(0xc1));
        Assert.Equal(0xc1UL, AssetIds.IdOf("carried/ship.glb"));
    }
}
