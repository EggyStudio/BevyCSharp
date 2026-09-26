using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what a camera does to the picture after the scene is drawn.
/// </summary>
/// <remarks>
/// A grade is a look rather than a value, so what can be asserted is the direction it moves the
/// picture in. Draining the saturation has to leave gray, and lifting the exposure has to leave
/// something brighter. Both are read off the pixels, because a setting that is accepted and ignored
/// looks exactly like one that worked.
/// </remarks>
[Collection("engine")]
public sealed class GradingTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    private const ulong Settled = 120;

    [Fact]
    public void DrainingTheSaturationLeavesGray()
    {
        if (!App.HasRenderer) return;

        var graded = Draw(new GradingSettings { Saturation = 0f });

        Assert.NotNull(graded);

        var (r, g, b, _) = graded.At(32, 32);

        // The cube is red. With no saturation left, the three channels have to agree, and what
        // they agree on is the luminance of that red rather than nothing at all.
        Assert.True(r > 20, $"the picture went black instead of gray, at {r},{g},{b}");
        Assert.InRange(g, r - 12, r + 12);
        Assert.InRange(b, r - 12, r + 12);
    }

    [Fact]
    public void ExposureMovesTheWholePicture()
    {
        if (!App.HasRenderer) return;

        var dark = Draw(new GradingSettings { Exposure = -2f });
        var bright = Draw(new GradingSettings { Exposure = 2f });

        Assert.NotNull(dark);
        Assert.NotNull(bright);

        Assert.True(
            bright.At(32, 32).R > dark.At(32, 32).R,
            "two stops of exposure either way left the same picture");
    }

    /// <summary>A grade of nothing is the picture the camera would have drawn anyway.</summary>
    [Fact]
    public void ClearingItPutsThePictureBack()
    {
        if (!App.HasRenderer) return;

        var plain = Draw(null);
        var cleared = Draw(new GradingSettings { Saturation = 0f }, thenClear: true);

        Assert.NotNull(plain);
        Assert.NotNull(cleared);

        var (r, g, b, _) = plain.At(32, 32);
        var (clearedR, clearedG, clearedB, _) = cleared.At(32, 32);

        Assert.InRange(clearedR, r - 6, r + 6);
        Assert.InRange(clearedG, g - 6, g + 6);
        Assert.InRange(clearedB, b - 6, b + 6);
    }

    [Fact]
    public void GradingSomethingThatIsNotACameraIsRefused()
    {
        if (!App.HasRenderer) return;

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, world =>
        {
            var entity = world.Resource<EcsWorld>().Spawn();

            var refused = Assert.Throws<BevyNativeException>(
                () => Render.SetColorGrading(entity, new GradingSettings()));

            Assert.Equal(NativeStatus.NotPresent, refused.Status);

            var metered = Assert.Throws<BevyNativeException>(
                () => Render.SetExposure(entity, 12f));

            Assert.Equal(NativeStatus.NotPresent, metered.Status);
        });

        harness.Run();
    }

    /// <summary>Draws a red cube through a graded camera and hands back the picture.</summary>
    private static CapturedImage? Draw(GradingSettings? grading, bool thenClear = false)
    {
        CapturedImage? picture = null;

        using var app = new App(Config.OffscreenFor(64, 64, frames: (uint)Settled + 40));

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();

                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    FieldOfView = 50f,
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));

                Render.SetColorGrading(camera, grading);
                if (thenClear) Render.SetColorGrading(camera, null);

                var cube = ecs.Spawn();

                Render.SetMesh(ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 4f, 4f, 4f));
                Render.SetMaterial(ecs, cube, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.8f, 0.1f, 0.1f, 1f),
                    Unlit = true,
                }));

                ecs.Add(cube, Transform.Identity);
            },
            "Test.Scene"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                if (world.Resource<Time>().FrameCount == Settled)
                {
                    world.InsertResource(new Ticket(Render.BeginCapture()));
                    return;
                }

                if (picture is not null) return;
                if (!world.TryGetResource<Ticket>(out var ticket)) return;

                if (Render.TryReadCapture(ticket.Capture, out var arrived)) picture = arrived;
            },
            "Test.Read"));

        Assert.Equal(0, app.Run());
        return picture;
    }

    /// <summary>Where the run keeps what it asked for, so a later frame can pick it up.</summary>
    private sealed record Ticket(Capture Capture);
}
