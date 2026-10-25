using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The vegetation's texture collection, its import by <c>build/textures.py</c> the same byte for
/// byte, its bounds, its lookup by name and a set loaded as the GPU samples it.
/// </summary>
[Collection("engine")]
public sealed class TreeTexturesTests
{
    private static readonly string Collection = Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp", "Vegetation", "Textures");

    // A set of 64 square made from no source by build/textures.py --sample, kept with the tests so a
    // machine without the import's Python still loads one.
    private static readonly string Sample = Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp.Tests", "references", "textures");

    [Fact]
    public void AFolderOfSetsIsAddedAndFoundByName()
    {
        var added = Assert.Single(TreeTextures.Add(Sample));
        Assert.Equal("sample", added.Name);
        Assert.Equal(TreeTextureKind.Bark, added.Kind);
        Assert.StartsWith($"{TreeTextures.Folder}/added/", added.Albedo, StringComparison.Ordinal);
        Assert.EndsWith("/sample/albedo.ktx2", added.Albedo, StringComparison.Ordinal);
        Assert.Same(added, TreeTextures.Find("sample"));
        Assert.Contains(added, TreeTextures.All);
        Assert.Null(TreeTextures.Find("no-such-bark"));
    }

    [Fact]
    public void AFolderMissingAFileOfASetIsRefused()
    {
        using var folder = new TestFolder("bcs-textures-");
        Directory.CreateDirectory(folder.File("game-bark"));
        foreach (var file in new[] { "albedo.ktx2", "surface.ktx2" })
        {
            File.Copy(Path.Combine(Sample, "sample", file), Path.Combine(folder.File("game-bark"), file));
        }

        File.WriteAllText(folder.File("textures.json"), """{ "entries": [ { "name": "game-bark", "kind": "bark" } ] }""");
        Assert.Throws<InvalidDataException>(() => TreeTextures.Add(folder.Path));
        Assert.Throws<InvalidDataException>(() => TreeTextures.Add(folder.File("game-bark")));
    }

    [Fact]
    public void EveryCarriedSetKeepsToItsBounds()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Collection, "textures.json")));
        var entries = manifest.RootElement.GetProperty("entries").EnumerateArray().ToDictionary(entry => entry.GetProperty("name").GetString()!);

        long total = 0;
        var sizes = new Dictionary<string, long>();
        foreach (var set in TreeTextures.All.Where(set => set.Albedo.StartsWith($"{TreeTextures.Folder}/{set.Name}/", StringComparison.Ordinal)))
        {
            var entry = entries[set.Name];
            long size = 0;
            foreach (var file in new[] { "albedo.ktx2", "normal.ktx2", "surface.ktx2" })
            {
                var bytes = File.ReadAllBytes(Path.Combine(Collection, set.Name, file));
                Assert.Equal(entry.GetProperty("files").GetProperty(file).GetInt64(), bytes.Length);
                var (_, width, height, _, _) = Header(bytes);
                Assert.True(width <= 1024 && height <= 1024, $"{set.Name}/{file} is {width} by {height}");
                size += bytes.Length;
            }

            sizes[set.Name] = size;
            total += size;
        }

        // A species' bark and leaves together, and the whole collection.
        Assert.True(total <= 32L << 20, $"the collection is {total} bytes");
        foreach (var species in new[] { TreeSpecies.QuakingAspen, TreeSpecies.BlackTupelo, TreeSpecies.WeepingWillow, TreeSpecies.CaliforniaBlackOak, TreeSpecies.Bush, TreeSpecies.Fern })
        {
            var size = sizes.GetValueOrDefault(species.Bark ?? "") + sizes.GetValueOrDefault(species.Leaves ?? "");
            Assert.True(size <= 3L << 20, $"a species' sets are {size} bytes");
        }
    }

    [SkippableFact]
    public void TheSampleImportsTheSameBytesAsTheTestsKeep()
    {
        var python = Textures();
        using var folder = new TestFolder("bcs-textures-");
        Run(python, "--sample", folder.Path);

        foreach (var file in new[] { "albedo.ktx2", "normal.ktx2", "surface.ktx2" })
        {
            Assert.Equal(File.ReadAllBytes(Path.Combine(Sample, "sample", file)), File.ReadAllBytes(Path.Combine(folder.Path, "sample", file)));
        }

        Assert.Equal(File.ReadAllText(Path.Combine(Sample, "textures.json")), File.ReadAllText(Path.Combine(folder.Path, "textures.json")));
    }

    [Fact]
    public void ASetsFilesAreKtx2InTheirFormatsWithEveryLevel()
    {
        // The sample's albedo, 64 square with its levels to a texel, in BC7 as sRGB, each level
        // compressed with zstd, its normal half the size in BC5 and its surface in BC7 as data.
        Assert.Equal((146u, 64u, 64u, 7u, 2u), Header(File.ReadAllBytes(Path.Combine(Sample, "sample", "albedo.ktx2"))));
        Assert.Equal((141u, 32u, 32u, 6u, 2u), Header(File.ReadAllBytes(Path.Combine(Sample, "sample", "normal.ktx2"))));
        Assert.Equal((145u, 32u, 32u, 6u, 2u), Header(File.ReadAllBytes(Path.Combine(Sample, "sample", "surface.ktx2"))));
    }

    [SkippableFact]
    public void ASetLoadsAtItsSizeInTheFormatsTheGpuSamples()
    {
        Needs.Renderer();
        var set = TreeTextures.Add(Sample).Single();
        (AssetHandle Albedo, AssetHandle Normal, AssetHandle Surface) images = default;
        var formats = CompressedImageFormats.None;
        var states = new List<AssetLoadState>();
        var sizes = new List<(uint, uint)>();

        var run = new PictureRun
        {
            Scene = _ => images = set.Load(),
        };
        run.Do("formats", _ => formats = Render.CompressedFormats)
            .Until("loaded", _ => new[] { images.Albedo, images.Normal, images.Surface }.All(image => AssetServer.StateOf(image) is AssetLoadState.Loaded or AssetLoadState.Failed))
            .Do("sizes", world =>
            {
                foreach (var image in new[] { images.Albedo, images.Normal, images.Surface })
                {
                    states.Add(AssetServer.StateOf(image));
                    sizes.Add(Render.TryImageSize(image, out var width, out var height, out _) ? (width, height) : default);
                }
            })
            .Go();

        Skip.IfNot(formats.HasFlag(CompressedImageFormats.Bc), "needs a GPU that samples BC7 and BC5");
        Assert.All(states, state => Assert.Equal(AssetLoadState.Loaded, state));
        Assert.Equal([(64u, 64u), (32u, 32u), (32u, 32u)], sizes);
    }

    /// <summary>Python able to run the import, 3.14 or later for its zstd, with Pillow, or a skip.</summary>
    private static string Textures()
    {
        var python = Needs.Python();
        var start = new ProcessStartInfo(python) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "-I", "-c", "import PIL; from compression import zstd" }) start.ArgumentList.Add(argument);
        using var check = Process.Start(start)!;
        check.WaitForExit(30_000);
        Skip.IfNot(check.ExitCode == 0, "needs Python 3.14 or later, whose compression module has zstd, with Pillow, to run build/textures.py");
        return python;
    }

    private static void Run(string python, params string[] arguments)
    {
        var start = new ProcessStartInfo(python)
        {
            WorkingDirectory = CheatsheetTests.RepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add("-I");
        start.ArgumentList.Add(Path.Combine("build", "textures.py"));
        foreach (var argument in arguments) start.ArgumentList.Add(argument);

        using var script = Process.Start(start)!;
        var output = script.StandardOutput.ReadToEndAsync();
        var errors = script.StandardError.ReadToEndAsync();
        Assert.True(script.WaitForExit(300_000), "an import of one entry ends within minutes");
        Assert.True(script.ExitCode == 0, output.Result + errors.Result);
    }

    /// <summary>A KTX2 file's format, width, height, levels and supercompression, from its header.</summary>
    private static (uint Format, uint Width, uint Height, uint Levels, uint Supercompression) Header(byte[] file)
    {
        byte[] identifier = [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];
        Assert.Equal(identifier, file[..12]);
        uint At(int offset) => BitConverter.ToUInt32(file, offset);
        return (At(12), At(20), At(24), At(40), At(44));
    }
}
