using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

/// <summary>
/// Covers Bevy's dev tools, the FPS overlay, the infinite grid and picking's debug overlay, each
/// there where the app asks for it.
/// </summary>
[Collection("engine")]
public sealed class DevToolsTests
{
    /// <summary>
    /// An app that asks for no dev tool has neither the overlay's resource nor picking's debug mode,
    /// so a game reaching for one it did not ask for is told so.
    /// </summary>
    [SkippableFact]
    public void AToolIsThereOnlyWhereTheAppAsksForIt()
    {
        Needs.Renderer();

        bool? overlay = null;
        BevyNativeException? refused = null;
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                overlay = ecs.Resource<FpsOverlayConfigRef>() is not null;
                try { _ = Picking.DebugMode; }
                catch (BevyNativeException e) { refused = e; }
            },
        };
        run.Until("started", world => world.Resource<Time>().FrameCount >= 1).Go();

        Assert.False(overlay);
        Assert.NotNull(refused);
    }

    /// <summary>
    /// The overlay's settings are Bevy's resource at Bevy's defaults, read back as they are written,
    /// and the overlay is drawn at the window's top left in the color the resource holds.
    /// </summary>
    [SkippableFact]
    public void TheOverlayIsDrawnInTheColorItsResourceHolds()
    {
        Needs.Renderer();

        (bool Enabled, float Target, Color Color)? defaults = null, written = null;
        var run = new PictureRun
        {
            // Wide enough that the text stays on one line whatever the frame rate it shows.
            Width = 320,
            Height = 96,
            Configure = config => config.DevTools = DevTools.FpsOverlay,
            Scene = ecs => PictureRun.Camera(ecs),
        };

        run.Until("started", world => world.Resource<Time>().FrameCount >= 3)
            .Do("write", world =>
            {
                var overlay = world.Resource<EcsWorld>().Resource<FpsOverlayConfigRef>()!.Value;
                defaults = (overlay.Enabled, overlay.FrameTimeGraphConfigTargetFps, overlay.TextColor);
                overlay.TextColor = new Color(0f, 1f, 0f, 1f);
                overlay.FrameTimeGraphConfigTargetFps = 144f;
                overlay.FrameTimeGraphConfigEnabled = false;
            })
            .Wait(10)
            .Do("read", world =>
            {
                var overlay = world.Resource<EcsWorld>().Resource<FpsOverlayConfigRef>()!.Value;
                written = (overlay.Enabled, overlay.FrameTimeGraphConfigTargetFps, overlay.TextColor);
            })
            .Capture("overlay")
            .Go();

        Assert.Equal((true, 60f, new Color(1f, 1f, 1f, 1f)), defaults);
        Assert.Equal((true, 144f, new Color(0f, 1f, 0f, 1f)), written);

        // The text, "FPS" and its number, in green on the black the camera clears to, in the top
        // left, and nothing green in the bottom right, which the overlay does not reach.
        var picture = run.Picture("overlay");
        Assert.True(Green(picture, 0, 0, 80, 40) > 10, "no green text was drawn at the top left");
        Assert.Equal(0, Green(picture, 240, 64, 320, 96));
    }

    /// <summary>
    /// An entity given the grid takes Bevy's settings for it, which it requires, and the grid is
    /// drawn under a camera looking down at it, and is gone from the picture once it is hidden.
    /// </summary>
    [SkippableFact]
    public void TheGridIsDrawnWithBevysSettingsUntilItIsHidden()
    {
        Needs.Renderer();

        var grid = Entity.None;
        (float Scale, uint Interval, float Fadeout)? settings = null;
        var run = new PictureRun
        {
            Configure = config => config.DevTools = DevTools.InfiniteGrid,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 6f, 6f));
                grid = ecs.Spawn();
                ecs.Insert<InfiniteGridRef>(grid);
            },
        };

        run.Until("started", world => world.Resource<Time>().FrameCount >= 3)
            .Do("read", world =>
            {
                var read = world.Resource<EcsWorld>().Get<InfiniteGridSettingsRef>(grid)!.Value;
                settings = (read.Scale, read.MajorLineInterval, read.FadeoutDistance);
            })
            .Capture("shown")
            .Do("hide", world => world.Resource<EcsWorld>().Wrap<VisibilityRef>(grid).Value = Visibility.Hidden)
            .Wait(3)
            .Capture("hidden")
            .Go();

        Assert.Equal((1f, 10u, 100f), settings);
        var shown = run.Picture("shown");
        var hidden = run.Picture("hidden");
        Assert.True(Lit(shown) > 50, "the grid drew too few lines on the black the camera clears to");
        Assert.Equal(0, Lit(hidden));
    }

    /// <summary>
    /// Picking's debug mode starts disabled, as Bevy's does, and reads back each mode it is set to.
    /// </summary>
    [SkippableFact]
    public void PickingsDebugModeIsSetAndReadBack()
    {
        Needs.Renderer();

        var read = new List<PickingDebugMode>();
        var run = new PictureRun
        {
            Configure = config => (config.MeshPicking, config.DevTools) = (true, DevTools.PickingDebug),
            Scene = ecs => PictureRun.Camera(ecs),
        };

        run.Until("started", world => world.Resource<Time>().FrameCount >= 3)
            .Do("cycle", _ =>
            {
                read.Add(Picking.DebugMode);
                foreach (var mode in new[] { PickingDebugMode.Normal, PickingDebugMode.Noisy, PickingDebugMode.Disabled })
                {
                    Picking.DebugMode = mode;
                    read.Add(Picking.DebugMode);
                }
            })
            .Go();

        Assert.Equal([PickingDebugMode.Disabled, PickingDebugMode.Normal, PickingDebugMode.Noisy, PickingDebugMode.Disabled], read);
    }

    // The pixels in a box whose green stands well above their red and blue.
    private static int Green(CapturedImage picture, uint left, uint top, uint right, uint bottom)
    {
        var count = 0;
        for (var y = top; y < bottom; y++)
            for (var x = left; x < right; x++)
                if (picture.At(x, y) is var (r, g, b, _) && g > 100 && g > r + 60 && g > b + 60) count++;
        return count;
    }

    // The pixels brighter than the black the camera clears to.
    private static int Lit(CapturedImage picture)
    {
        var count = 0;
        for (uint y = 0; y < picture.Height; y++)
            for (uint x = 0; x < picture.Width; x++)
                if (picture.At(x, y) is var (r, g, b, _) && r + g + b > 30) count++;
        return count;
    }
}
