using System.Diagnostics;
using System.Text;
using Bevy;
using Vegetation;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The Vegetation module's texture collection as this engine reads it, its recipe making the same
/// bytes the module holds, every file read under the engine's folder of the asset root and a set
/// loaded as the GPU samples it.
/// </summary>
[Collection("engine")]
public sealed class TreeTexturesTests
{
    private static readonly string Module = Path.Combine(CheatsheetTests.RepositoryRoot(), "modules", "Vegetation");

    private static readonly string Collection = Path.Combine(Module, "Vegetation", "Textures");

    // A set of 64 square made from no source by the module's build/textures.py --sample, which its
    // tests keep, so a machine without the import's Python still loads one.
    private static readonly string Sample = Path.Combine(Module, "Vegetation.Tests", "references", "textures");

    // The sorted sources the sets are imported from, which this repository's root keeps on the
    // machine that imports them.
    private static readonly string Sources = Path.Combine(CheatsheetTests.RepositoryRoot(), ".vegetation");

    [Fact]
    public void EveryFileOfTheCollectionIsReadUnderItsFolder()
    {
        var added = Assert.Single(TreeTextures.Add(Sample));
        TreeTextureAssets.Carry();
        var files = TreeTextures.Files;
        Assert.Contains(added.Albedo, files);
        Assert.Contains("weeping-willow-bark/depth.ktx2", files);
        foreach (var file in files)
        {
            using var stream = TreeTextures.Open(file)!;
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            Assert.Equal(bytes.ToArray(), AssetFiles.ReadAllBytes($"{TreeTextureAssets.Folder}/{file}"));
        }
    }

    [SkippableFact]
    public void ASetImportsAsTheCollectionHoldsIt()
    {
        var python = Textures();
        Needs.Folder(Path.Combine(Sources, "leaves", "shingle-oak"), "the shingle oak leaves' source");
        using var folder = new TestFolder("bcs-textures-");
        Run(python, "--sources", Sources, "--out", folder.Path, "shingle-oak-leaves");

        foreach (var file in new[] { "albedo.ktx2", "normal.ktx2", "surface.ktx2" })
        {
            Assert.Equal(File.ReadAllBytes(Path.Combine(Collection, "shingle-oak-leaves", file)), File.ReadAllBytes(Path.Combine(folder.Path, "shingle-oak-leaves", file)));
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
            WorkingDirectory = Module,
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
}
