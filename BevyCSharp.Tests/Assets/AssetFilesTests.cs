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
    private readonly TestFolder _folder = new("bcs-files-");
    private readonly string _was = Streaming.AssetRoot;

    private string Root => _folder.Path;

    public AssetFilesTests()
    {
        Directory.CreateDirectory(Path.Combine(Root, "carried"));
        Streaming.AssetRoot = Root;
        AssetFiles.Use(typeof(AssetFilesTests).Assembly);
        AssetIds.Reindex();
    }

    public void Dispose()
    {
        AssetFiles.Use(null);
        Streaming.AssetRoot = _was;
        AssetIds.Reindex();
        _folder.Dispose();
    }

    [Fact]
    public void AFileNotOnDiskIsReadFromTheAssemblyAndOneOnDiskWins()
    {
        Assert.True(AssetFiles.CarriesFiles);
        Assert.True(AssetFiles.Exists("carried/note.json"));
        Assert.Contains("from the assembly", AssetFiles.ReadAllText("carried/note.json"));

        // Asked for by its full path under the root, as a scene path resolves.
        Assert.Contains("from the assembly", AssetFiles.ReadAllText(Path.Combine(Root, "carried", "note.json")));

        // A file beside the game replaces the one it carries.
        File.WriteAllText(Path.Combine(Root, "carried", "note.json"), """{ "carried": "from the folder" }""");
        Assert.Contains("from the folder", AssetFiles.ReadAllText("carried/note.json"));

        // Neither has it.
        Assert.False(AssetFiles.Exists("carried/missing.json"));
        Assert.Throws<FileNotFoundException>(() => AssetFiles.ReadAllText("carried/missing.json"));

        // A path outside the root is never looked for among the resources.
        Assert.False(AssetFiles.Exists(Path.Combine(Path.GetDirectoryName(Root)!, "carried", "note.json")));
    }

    [SkippableFact]
    [ExpectsError("bevy", "carried/absent.gltf")]
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

    /// <summary>
    /// A pack mounted under a folder while an app runs is read by the managed side at once and by
    /// Bevy from then on, the bridge having carried nothing of it when the app was built, and is
    /// gone from both once unmounted.
    /// </summary>
    [SkippableFact]
    public void APackMountedWhileTheAppRunsIsReadOnBothSidesUntilUnmounted()
    {
        // A picture and a note in a pack, as a scene pack fetched for the player holds its files.
        var source = _folder.File("source");
        Directory.CreateDirectory(Path.Combine(source, "textures"));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "assets", "textures", "checker.png"), Path.Combine(source, "textures", "checker.png"));
        File.WriteAllText(Path.Combine(source, "note.txt"), "from the pack");
        var packed = _folder.File("scene.pack");
        AssetPack.Write(source, packed);

        using var harness = new EngineHarness(frames: 240, fps: 240);
        Needs.Renderer();

        var image = AssetHandle.None;
        var state = AssetLoadState.Unknown;
        string? note = null;
        var gone = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!image.IsValid)
            {
                // A few frames in, so the app has long been built when the pack arrives.
                if (ctx.Time.FrameCount < 5) return;

                AssetFiles.Mount("packs/scene", AssetPack.Open(packed));
                note = AssetFiles.ReadAllText("packs/scene/note.txt");
                image = AssetServer.Load(AssetKind.Image, "packs/scene/textures/checker.png");
                return;
            }

            state = image.State;
            if (state is AssetLoadState.Loading) return;

            gone = AssetFiles.Unmount("packs/scene") && !AssetFiles.Exists("packs/scene/note.txt") && !AssetFiles.Unmount("packs/scene");
            ctx.Exit();
        });

        harness.Run();

        Assert.Equal("from the pack", note);
        Assert.Equal(AssetLoadState.Loaded, state);
        Assert.True(gone, "the pack was still read after it was unmounted");
    }

    /// <summary>
    /// A folder on this machine mounted under a folder of the asset root is read by its files as
    /// they were when it was mounted, one written after found once it is mounted again.
    /// </summary>
    [Fact]
    public void AFolderMountedIsReadAsItWasListedUntilMountedAgain()
    {
        var cache = _folder.File("cache");
        Directory.CreateDirectory(Path.Combine(cache, "inner"));
        File.WriteAllText(Path.Combine(cache, "inner", "first.txt"), "the first");

        AssetFiles.Mount("cached", cache);
        Assert.Equal("the first", AssetFiles.ReadAllText("cached/inner/first.txt"));

        File.WriteAllText(Path.Combine(cache, "second.txt"), "the second");
        Assert.False(AssetFiles.Exists("cached/second.txt"));

        AssetFiles.Mount("cached", cache);
        Assert.Equal("the second", AssetFiles.ReadAllText("cached/second.txt"));

        Assert.True(AssetFiles.Unmount("cached"));
        Assert.False(AssetFiles.Exists("cached/inner/first.txt"));

        // A folder not there yet mounts as one holding nothing.
        AssetFiles.Mount("nowhere", _folder.File("nowhere"));
        Assert.False(AssetFiles.Exists("nowhere/anything.txt"));
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
