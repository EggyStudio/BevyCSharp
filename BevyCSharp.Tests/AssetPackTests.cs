using System.Text;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a game's assets read from one pack file, written from a folder and read back whole or
/// in part, ahead of the assembly and behind the disk, by Bevy, by the ids and by streaming.
/// </summary>
/// <remarks>
/// The asset root and what <see cref="AssetFiles"/> carries are static, so these share the engine
/// collection and put both back.
/// </remarks>
[Collection("engine")]
public sealed class AssetPackTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "bcs-pack-" + Guid.NewGuid().ToString("n"));
    private readonly string _was = Streaming.AssetRoot;

    public AssetPackTests()
    {
        Directory.CreateDirectory(Path.Combine(_work, "source", "carried"));
        Directory.CreateDirectory(Path.Combine(_work, "root"));
    }

    public void Dispose()
    {
        AssetFiles.Use(null);
        Streaming.AssetRoot = _was;
        AssetIds.Reindex();
        Directory.Delete(_work, recursive: true);
    }

    private string Source(string path) => Path.Combine(_work, "source", path);

    /// <summary>Writes a file under the source folder and returns its full path.</summary>
    private string Put(string path, string text)
    {
        File.WriteAllText(Source(path), text);
        return Source(path);
    }

    /// <summary>Packs the source folder into a pack of its own.</summary>
    private string Pack(Func<string, bool>? include = null)
    {
        var pack = Path.Combine(_work, AssetPack.DefaultName);
        AssetPack.Write(Path.Combine(_work, "source"), pack, include);
        return pack;
    }

    [Fact]
    public void APackHoldsTheFilesOfAFolderAndReadsAnyPartOfOne()
    {
        Put("top.txt", "at the top");
        Put("carried/inner.txt", "0123456789");
        Put("carried/left.cs", "left out");

        using var pack = AssetPack.Open(Pack(file => !file.EndsWith(".cs", StringComparison.Ordinal)));

        Assert.Equal(["carried/inner.txt", "top.txt"], pack.Files.Order(StringComparer.Ordinal));
        Assert.False(pack.Contains("carried/left.cs"));
        Assert.Null(pack.OpenFile("absent.txt"));

        using (var top = pack.OpenFile("top.txt")!)
        using (var reader = new StreamReader(top))
        {
            Assert.Equal("at the top", reader.ReadToEnd());
        }

        // A part, as a streamed read takes one, and nothing past the file's end.
        using var inner = pack.OpenFile("carried/inner.txt")!;
        Assert.Equal(10, inner.Length);
        inner.Position = 4;
        var part = new byte[20];
        Assert.Equal(6, inner.Read(part));
        Assert.Equal("456789", Encoding.UTF8.GetString(part, 0, 6));
        Assert.Equal(0, inner.Read(part));
    }

    [Fact]
    public void AFileThatIsNotAWholePackIsRefused()
    {
        var other = Path.Combine(_work, "other.pack");
        File.WriteAllText(other, "not a pack at all");
        Assert.Throws<InvalidDataException>(() => AssetPack.Open(other));

        // A pack cut short by a copy that stopped names the file that runs past its end.
        Put("big.txt", new string('x', 1000));
        var pack = Pack();
        var bytes = File.ReadAllBytes(pack);
        File.WriteAllBytes(pack, bytes[..^100]);

        var refused = Assert.Throws<InvalidDataException>(() => AssetPack.Open(pack));
        Assert.Contains("big.txt", refused.Message);
    }

    [Fact]
    public void APackIsReadBehindTheDiskAndAheadOfTheAssembly()
    {
        // The test assembly carries carried/note.json, saying it comes from the assembly.
        Put("carried/note.json", """{ "carried": "from the pack" }""");
        Streaming.AssetRoot = Path.Combine(_work, "root");
        AssetFiles.Use(typeof(AssetPackTests).Assembly, AssetPack.Open(Pack()));

        Assert.True(AssetFiles.IsCarried("carried/note.json"));
        Assert.Contains("from the pack", AssetFiles.ReadAllText("carried/note.json"));

        Directory.CreateDirectory(Path.Combine(_work, "root", "carried"));
        File.WriteAllText(Path.Combine(_work, "root", "carried", "note.json"), """{ "carried": "from the folder" }""");
        Assert.False(AssetFiles.IsCarried("carried/note.json"));
        Assert.Contains("from the folder", AssetFiles.ReadAllText("carried/note.json"));
    }

    [Fact]
    public void TheIdsAnExportIndexedAreFoundInThePackWithNoAssetFolder()
    {
        Put("carried/ship.glb", "a model");
        Put("carried/ship.glb.uid", "00000000000000d2\n");
        Assert.Equal(1, AssetIds.IndexForShipping(Path.Combine(_work, "source")));

        // No folder at the root at all, as a packed game ships.
        Streaming.AssetRoot = Path.Combine(_work, "nowhere");
        AssetFiles.Use(null, AssetPack.Open(Pack()));
        AssetIds.Reindex();

        Assert.Equal("carried/ship.glb", AssetIds.PathOf(0xd2));
        Assert.Equal(0xd2UL, AssetIds.IdOf("carried/ship.glb"));
    }

    [SkippableFact]
    public void BevyLoadsAModelAndAStreamedReadTakesAPartOfAPackedFile()
    {
        Needs.Renderer();

        File.Copy(Path.Combine(EngineHarness.AssetDirectory, "models", "triangle.gltf"), Source("carried/packed.gltf"));
        Put("carried/numbers.bin", "0123456789");
        var pack = Pack();

        var model = AssetHandle.None;
        var state = AssetLoadState.Loading;
        var read = default(StreamRead);
        byte[]? part = null;

        using var harness = new EngineHarness(frames: 200, fps: 120, pack: pack);
        harness.OnContext(Stage.Startup, _ =>
        {
            model = AssetServer.LoadGltfMesh("carried/packed.gltf");
            read = Streaming.Read("carried/numbers.bin", offset: 3, length: 4);
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            state = model.State;
            if (part is null && Streaming.TryTake(read, out var arrived)) part = arrived;
            if (state != AssetLoadState.Loading && part is not null) ctx.Exit();
        });

        harness.Run();

        Assert.Equal(AssetLoadState.Loaded, state);
        Assert.Equal("3456", Encoding.UTF8.GetString(part!));
    }
}
