using Bevy;
using Xunit;

namespace Bevy.Tests;

// The screen probes' tile, the same up to a picture of 1080 rows and growing with its height past
// that, as 3DEngine's TileAt grows it.
public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// The tile a screen probe stands in is 16, 12 and 8 pixels a side at Low, Medium and High up
    /// to a picture of 1080 rows, and grows with the height past it, to 21, 16 and 11 at 1440 rows
    /// and to 32, 24 and 16 at 2160.
    /// </summary>
    [Fact]
    public void TheScreenProbesTileHoldsUpTo1080RowsAndGrowsWithTheHeightPastThem()
    {
        foreach (var (quality, at1080) in new[] { (GlobalIllumination.Low, 16), (GlobalIllumination.Medium, 12), (GlobalIllumination.High, 8) })
        {
            Assert.Equal(at1080, WorldProbes.TileAt(quality, 450));
            Assert.Equal(at1080, WorldProbes.TileAt(quality, 720));
            Assert.Equal(at1080, WorldProbes.TileAt(quality, 1080));
            Assert.Equal((int)MathF.Round(at1080 * 1440 / 1080f), WorldProbes.TileAt(quality, 1440));
            Assert.Equal(at1080 * 2, WorldProbes.TileAt(quality, 2160));
        }
        Assert.Equal(11, WorldProbes.TileAt(GlobalIllumination.High, 1440));
    }

    /// <summary>
    /// A picture of 1440 rows at High stands its screen probes every 11 pixels and one of 720 rows
    /// every 8, the tile taken from the camera's picture's height.
    /// </summary>
    /// <remarks>
    /// The tile was 16, 12 and 8 pixels at Low, Medium and High whatever the picture, so the
    /// probes' count and their cost grew with the picture's pixels. In a box at High their trace
    /// took 14.1, 29.0 and 64.2 ms at 1080p, 1440p and 4K, and with the tile grown past 1080 rows
    /// 14.2, 14.1 and 15.1, while another program's draws shared the GPU (WorldProbes.Screen's
    /// TileAt).
    /// </remarks>
    [SkippableTheory]
    [InlineData(720u, 8)]
    [InlineData(1440u, 11)]
    public void APicturesScreenProbesStandEveryTileItsHeightGives(uint height, int tile)
    {
        Needs.Shaders();
        var said = "";
        var eye = new Vec3(-1.9f, 0.6f, 1.5f);
        new PictureRun
        {
            Width = 320,
            Height = height,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Add(camera, Transform.LookingAt(eye, new Vec3(0f, 0.5f, 0f), Vec3.UnitY));
                Floor(ecs, (0.8f, 0.8f, 0.8f));
                Block(ecs, (0.9f, 0.9f, 0.9f), new Vec3(0.5f, 0.5f, 0f), new Vec3(1f, 1f, 1f));
                Sun(ecs, new Vec3(4f, 6f, 1f));
                Render.SetSceneField(2, 0.25f);
                Render.SetGlobalIllumination(GlobalIllumination.High);
            },
        }
            .Until("settled", _ => Settled(2))
            .Until("traced", _ => WorldProbes.TracedFrames >= 10)
            .Until("tiled", _ => (said = WorldProbes.ScreenSaid()).StartsWith("Screen probes every"))
            .Go();

        Assert.Contains($"every {tile} pixels", said);
    }
}
