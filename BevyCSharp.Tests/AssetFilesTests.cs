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

    [Fact]
    public void AnIdTheAssemblyCarriesIsTrustedWithoutItsFile()
    {
        // The model is in the bridge, where the managed side cannot look, so its id is taken as
        // given rather than checked against a file that is not on disk.
        Assert.Equal("carried/ship.glb", AssetIds.PathOf(0xc1));
        Assert.Equal(0xc1UL, AssetIds.IdOf("carried/ship.glb"));
    }
}
