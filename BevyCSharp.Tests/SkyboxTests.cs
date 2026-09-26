using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the cubemap behind the scene.
/// </summary>
/// <remarks>
/// A skybox is the first thing the bridge does with a cubemap, and a cubemap is an ordinary image
/// told that it is six square faces stacked on top of each other. The telling can only happen once
/// the file has decoded, which is a frame or more after the handle is handed over, so what is worth
/// pinning down is that the waiting works, so the picture ends up with the sky in it rather than
/// with the camera's clear color.
/// </remarks>
[Collection("engine")]
public sealed class SkyboxTests
{
    /// <summary>Frames to let the image load and the pipelines compile.</summary>
    private const ulong Settled = 140;

    [Fact]
    public void ASkyboxDrawsWhereTheSceneDoesNot()
    {
        if (!App.HasRenderer) return;

        CapturedImage? picture = null;

        using var app = new App(new Config
        {
            Offscreen = true,
            Width = 64,
            Height = 64,
            HeadlessFps = 60,
            HeadlessFrames = (uint)Settled + 40,
            AssetRoot = EngineHarness.AssetDirectory,
        });

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    FieldOfView = 60f,

                    // Black, and nothing about the sky is, so a black pixel is one the skybox did
                    // not reach.
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                world.Resource<EcsWorld>().Add(
                    camera, Transform.LookingAt(new Vec3(0f, 0f, 3f), Vec3.Zero, Vec3.UnitY));

                Render.SetSkybox(
                    camera,
                    AssetServer.Load(AssetKind.Image, "textures/cubemap.png"),
                    brightness: 1000f);
            },
            "Test.Sky"));

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
        Assert.NotNull(picture);

        var lit = 0;

        for (var i = 0; i < picture.Pixels.Length; i += 4)
        {
            if (picture.Pixels[i] + picture.Pixels[i + 1] + picture.Pixels[i + 2] > 60) lit++;
        }

        // Nothing is in the scene, so every pixel is either the sky or the clear color, and the
        // faces of this cubemap are all bright.
        Assert.True(
            lit > picture.Width * picture.Height / 2,
            $"only {lit} of {picture.Width * picture.Height} pixels have anything in them");
    }

    [Fact]
    public void ASkyboxOnSomethingThatIsNotACameraIsRefused()
    {
        if (!App.HasRenderer) return;

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, world =>
        {
            var entity = world.Resource<EcsWorld>().Spawn();

            var refused = Assert.Throws<BevyNativeException>(
                () => Render.SetSkybox(entity, AssetHandle.None));

            Assert.Equal(NativeStatus.NotPresent, refused.Status);
        });

        harness.Run();
    }

    /// <summary>Where the run keeps what it asked for, so a later frame can pick it up.</summary>
    private sealed record Ticket(Capture Capture);
}
