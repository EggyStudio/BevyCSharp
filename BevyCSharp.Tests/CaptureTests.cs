using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers reading a picture back into memory.
/// </summary>
/// <remarks>
/// The difference between checking that a setting was accepted and checking that anything was
/// drawn. A capture written to a PNG needs a person, or an image library, to say what is in it; a
/// capture read into memory can be asserted on, which is what lets the colour a camera was told to
/// clear to be the thing the test actually checks.
/// </remarks>
[Collection("engine")]
public sealed class CaptureTests
{
    /// <summary>
    /// A camera clearing to a known colour produces a picture of that colour.
    /// </summary>
    /// <remarks>
    /// Every pixel, because a clear covers the whole target and nothing is in front of the camera
    /// to interrupt it. Compared loosely, because the target is an sRGB texture and the colour
    /// given to the camera is linear, so the bytes that come back are the encoded form rather than
    /// the numbers that went in.
    /// </remarks>
    [Fact]
    public void WhatWasDrawnCanBeReadBack()
    {
        if (!App.HasRenderer) return;

        CapturedImage? picture = null;

        using var app = new App(Config.OffscreenFor(320, 180, frames: 120));

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var target = Render.CreateTarget(32, 16);

                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (1f, 0f, 0f, 1f),
                });

                Render.SetCameraTarget(camera, target);
                world.InsertResource(new Asked(target));
            },
            "Test.Scene"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                var asked = world.Resource<Asked>();

                if (world.Resource<Time>().FrameCount == 10)
                {
                    world.InsertResource(new Ticket(Render.BeginCapture(asked.Target)));
                    return;
                }

                if (picture is not null) return;
                if (!world.TryGetResource<Ticket>(out var ticket)) return;

                if (Render.TryReadCapture(ticket.Capture, out var arrived)) picture = arrived;
            },
            "Test.Read"));

        Assert.Equal(0, app.Run());

        Assert.NotNull(picture);
        Assert.Equal(32u, picture.Width);
        Assert.Equal(16u, picture.Height);
        Assert.Equal(32 * 16 * 4, picture.Pixels.Length);

        var corner = picture.At(0, 0);
        var middle = picture.At(16, 8);

        Assert.True(corner.R > 200, $"the red channel came back as {corner.R}");
        Assert.True(corner.G < 60, $"the green channel came back as {corner.G}");
        Assert.True(corner.B < 60, $"the blue channel came back as {corner.B}");
        Assert.Equal(corner, middle);
    }

    /// <summary>A capture that has not arrived says so rather than answering with nothing.</summary>
    [Fact]
    public void ACaptureIsNotReadableBeforeItArrives()
    {
        if (!App.HasRenderer) return;

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, _ =>
        {
            var capture = Render.BeginCapture();

            Assert.True(capture.IsValid);
            Assert.False(Render.TryReadCapture(capture, out var picture));
            Assert.Null(picture);

            Render.ReleaseCapture(capture);
        });

        harness.Run();
    }

    [Fact]
    public void ATicketThatNamesNothingIsRefused()
    {
        if (!App.HasRenderer) return;

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, _ =>
        {
            var refused = Assert.Throws<BevyNativeException>(
                () => Render.TryReadCapture(new Capture(9999), out var nothing));

            Assert.Equal(NativeStatus.NotPresent, refused.Status);
        });

        harness.Run();
    }

    /// <summary>Where a test keeps what it asked for, so a later frame can pick it up.</summary>
    private sealed record Asked(AssetHandle Target);

    /// <summary>And the ticket it got back.</summary>
    private sealed record Ticket(Capture Capture);
}
