using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A camera's corners taken off to nothing, and a window cleared to nothing round a viewport.
/// </summary>
/// <remarks>
/// Both are about alpha, which only a capture shows, since a picture composited onto a desktop is
/// the platform's to look at. A capture keeps the alpha the picture was left with, so a corner
/// painted black and a corner made clear read differently here, which is the difference that
/// matters on a see-through window.
/// </remarks>
[Collection("engine")]
public sealed class RenderCornerTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    private const uint Settled = 60;

    /// <summary>Corners taken off leave them clear, and the middle as it was.</summary>
    [SkippableFact]
    public void ARoundedCameraLeavesItsCornersClear()
    {
        Needs.Renderer();

        var run = new PictureRun
        {
            Scene = _ =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (1f, 0f, 0f, 1f),
                });

                Render.SetRoundedCorners(camera, 20f);
            },
        };

        run.Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var corner = picture.At(0, 0);
        var middle = picture.At(48, 48);

        Assert.True(corner.A == 0 && corner.R == 0, $"the corner is {corner}, not clear");
        Assert.True(middle.A == 255 && middle.R > 200, $"the middle is {middle}, not the camera's red");
    }

    /// <summary>
    /// A world cleared to nothing leaves the window clear where a camera's viewport does not reach.
    /// </summary>
    [SkippableFact]
    public void AViewportOverAClearWorldLeavesTheRestClear()
    {
        Needs.Renderer();

        var run = new PictureRun
        {
            Scene = _ =>
            {
                Render.SetClearColor((0f, 0f, 0f, 0f));

                Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 1f, 0f, 1f),
                    Viewport = (24, 24, 48, 48),
                });
            },
        };

        run.Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var outside = picture.At(4, 4);
        var inside = picture.At(48, 48);

        Assert.True(outside.A == 0, $"outside the viewport is {outside}, not clear");
        Assert.True(inside.A == 255 && inside.G > 200, $"inside the viewport is {inside}, not the camera's green");
    }

    /// <summary>
    /// A viewport's corners filled with what surrounds it match it, rather than being clear notches.
    /// </summary>
    [SkippableFact]
    public void AViewportsCornersShowTheirFill()
    {
        Needs.Renderer();

        var run = new PictureRun
        {
            Scene = _ =>
            {
                Render.SetClearColor((0f, 0f, 1f, 1f));

                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (1f, 0f, 0f, 1f),
                    Viewport = (24, 24, 48, 48),
                });

                Render.SetRoundedCorners(camera, 16f, (0f, 0f, 1f, 1f));
            },
        };

        run.Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var corner = picture.At(24, 24);
        var middle = picture.At(48, 48);

        Assert.True(corner.B > 200 && corner.R < 40 && corner.A == 255, $"the viewport's corner is {corner}, not the blue round it");
        Assert.True(middle.R > 200 && middle.B < 40, $"the middle is {middle}, not the camera's red");
    }

    /// <summary>A radius is a length, so a negative one is refused before it reaches the engine.</summary>
    [Fact]
    public void ANegativeRadiusIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetRoundedCorners(Entity.None, -1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetRoundedCorners(Entity.None, float.NaN));
    }

    /// <summary>A lens that cannot be is refused before it reaches the engine.</summary>
    [Fact]
    public void AnImpossibleLensIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetPerspective(Entity.None, 0f, 0.1f, 100f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetPerspective(Entity.None, 180f, 0.1f, 100f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetPerspective(Entity.None, 60f, 0f, 100f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetPerspective(Entity.None, 60f, 10f, 5f));
    }
}
