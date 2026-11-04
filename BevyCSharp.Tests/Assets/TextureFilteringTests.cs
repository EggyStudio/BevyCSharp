using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// Every texture whose filters are linear filtered at sixteen samples at a slant, a texture left at
/// Bevy's default sampler as one given its settings, and a sampler that reads a table at one.
/// </summary>
[Collection("engine")]
public sealed class TextureFilteringTests(ITestOutputHelper output)
{
    /// <summary>
    /// Two checkered floors side by side seen at a grazing angle, the left's texture left at Bevy's
    /// default sampler and the right's given <see cref="TextureSettings.Tiling"/>, each keep their
    /// checks apart in a band near their far end, the band's light spread at least twice as wide as
    /// one sample at a slant leaves it.
    /// </summary>
    /// <remarks>
    /// A texture seen at a slant is squeezed along the view, and with one sample the level of its
    /// mipmaps is chosen by the squeezed way, so the far floor reads as the checks' gray mean.
    /// Bevy's default sampler takes one unless set and a texture's settings took one unless asked,
    /// so each floor read its far band's checks at a spread of 6.7 levels of green. The bridge
    /// gives Bevy's default sampler and a glTF file's textures sixteen samples, and a texture whose
    /// three filters are linear takes sixteen from its settings, and both floors read 21.9, the bar
    /// at 14.
    /// </remarks>
    [SkippableFact]
    public void TwoCheckeredFloorsKeepTheirChecksFarOffAtAGrazingAngle()
    {
        Needs.Renderer();
        var run = new PictureRun
        {
            Width = 640,
            Height = 360,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 1f, 6f));
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1f, 6f), new Vec3(0f, 0.6f, -40f), Vec3.UnitY));
                foreach (var (x, given) in new[] { (-5.05f, false), (5.05f, true) })
                {
                    var image = Render.CreateImage(Checks(), Across, Along, srgb: true, mipmaps: true);
                    if (given) Render.SetSampler(image, TextureSettings.Tiling);
                    var floor = ecs.Spawn();
                    Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Plane, 10f, 60f));
                    Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings { BaseColorTexture = image, Unlit = true }));
                    ecs.Add(floor, Transform.At(x, 0f, -24f));
                }
            },
        };
        run.Capture("floors").Go();
        var picture = run.Picture("floors");

        var faults = new List<string>();
        foreach (var (half, from, to) in new[] { ("left, at Bevy's default sampler", 0u, picture.Width / 2 - 8), ("right, given its settings", picture.Width / 2 + 8, picture.Width) })
        {
            var spread = Spread(picture, from, to);
            output.WriteLine($"the floor on the {half} spreads its far band's light {spread:0.0} levels");
            if (spread < 14) faults.Add($"the floor on the {half} spreads its far band's light {spread:0.0} levels");
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// A sampler's settings ask for sixteen samples at a slant where its three filters are linear,
    /// one where any is nearest, and one where it reads one of the engine's tables, and a texture's
    /// settings likewise.
    /// </summary>
    [Fact]
    public void ASamplerTakesSixteenSamplesWhereItsFiltersAreLinearAndOneElsewhere()
    {
        Assert.Equal(16, SamplerSettings.Linear.Anisotropy);
        Assert.Equal(16, SamplerSettings.Clamped.Anisotropy);
        Assert.Equal(1, SamplerSettings.Nearest.Anisotropy);
        Assert.Equal(1, (SamplerSettings.Linear with { Mipmaps = SamplerFilter.Nearest }).Anisotropy);
        Assert.Equal(1, SamplerSettings.ClampedTable.Anisotropy);
        Assert.Equal(1, SamplerSettings.LinearTable.Anisotropy);
        Assert.Equal(16u, TextureSettings.Tiling.Anisotropy);
        Assert.Equal(16u, TextureSettings.Data.Anisotropy);
        Assert.Equal(1u, new TextureSettings().Anisotropy);
        Assert.Equal(1u, new TextureSettings { MagFilter = TextureFilter.Linear, MinFilter = TextureFilter.Linear }.Anisotropy);
    }

    private const uint Across = 512, Along = 3072;

    /// <summary>
    /// A white and dark gray checkerboard of eight pixels a square, 64 squares across and 384
    /// along, so a floor six times as long as it is wide holds square checks, the dark squares
    /// lighter than the black the camera clears to.
    /// </summary>
    private static byte[] Checks()
    {
        var pixels = new byte[Across * Along * 4];
        for (var i = 0; i < Across * Along; i++)
        {
            var white = (i % Across / 8 + i / Across / 8) % 2 == 0;
            var value = white ? (byte)255 : (byte)64;
            (pixels[i * 4], pixels[i * 4 + 1], pixels[i * 4 + 2], pixels[i * 4 + 3]) = (value, value, value, (byte)255);
        }
        return pixels;
    }

    /// <summary>
    /// The standard deviation of the green levels of the floor's pixels across columns
    /// <paramref name="from"/> to <paramref name="to"/> of the rows from a twelfth of the picture's
    /// height below the floor's far edge to an eighth, where the floor lies about ten units off and
    /// is squeezed about ten times along the view, the black the camera clears to left out.
    /// </summary>
    private static double Spread(CapturedImage picture, uint from, uint to)
    {
        var columns = Enumerable.Range((int)from, (int)(to - from)).Select(x => (uint)x).ToList();
        var edge = 0u;
        while (edge < picture.Height && columns.All(x => picture.At(x, edge).G < 32)) edge++;
        var rows = Enumerable.Range((int)edge + (int)picture.Height / 12, (int)picture.Height / 24).Select(y => (uint)y);
        var values = rows.SelectMany(y => columns.Select(x => (double)picture.At(x, y).G)).Where(g => g >= 32).ToList();
        var mean = values.Average();
        return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / values.Count);
    }
}
