using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a camera drawing into an image instead of onto the screen.
/// </summary>
/// <remarks>
/// What a portal, a security monitor, a minimap and a second viewport are all built from. The
/// camera is ordinary in every other way, so what is worth pinning down is the part that is not:
/// that the image is created, that a camera can be pointed at it and put back, and that what the
/// camera drew can be read out of it.
/// </remarks>
[Collection("engine")]
public sealed class RenderTargetTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"bcs-target-{Guid.NewGuid():N}");

    public RenderTargetTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void ATargetIsAHandleLikeAnyOther()
    {
        if (!App.HasRenderer) return;

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, _ =>
        {
            var first = Render.CreateTarget(64, 48);
            var second = Render.CreateTarget(64, 48);

            Assert.True(first.IsValid);
            Assert.True(second.IsValid);

            // Two calls, two images. A target is a thing rather than a mode, so a scene can hold a
            // portal and a minimap at once.
            Assert.NotEqual(first, second);
        });

        harness.Run();
    }

    /// <summary>
    /// A camera pointed at a target draws into it, and what it drew reads back at that size.
    /// </summary>
    /// <remarks>
    /// The size is the assertion that matters. The run is drawing at one size and the target is
    /// another, so a capture that comes back at the target's size can only have come from the
    /// image the camera was pointed at.
    /// </remarks>
    [Fact]
    public void WhatACameraDrawsIntoATargetCanBeReadBack()
    {
        if (!App.HasRenderer) return;

        var path = Path.Combine(_directory, "target.png");

        using var app = new App(Config.OffscreenFor(320, 180, frames: 90));

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var target = Render.CreateTarget(64, 48);

                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    FieldOfView = 60f,
                    Clear = ClearMode.Custom,
                    ClearColor = (0.8f, 0.1f, 0.1f, 1f),
                });

                world.Resource<EcsWorld>().Add(
                    camera, Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));

                Render.SetCameraTarget(camera, target);
                world.InsertResource(new Target(target));
            },
            "Test.Target"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                if (world.Resource<Time>().FrameCount != 10) return;

                Render.Screenshot(path, world.Resource<Target>().Handle);
            },
            "Test.Capture"));

        Assert.Equal(0, app.Run());
        Assert.True(File.Exists(path), $"no capture appeared at {path}");

        var (width, height) = PngSize(path);

        Assert.Equal(64u, width);
        Assert.Equal(48u, height);
    }

    [Fact]
    public void ACameraCanBePutBackOnTheWindow()
    {
        if (!App.HasRenderer) return;

        using var harness = new EngineHarness(frames: 3);

        harness.On(Stage.Update, world =>
        {
            var camera = Render.SpawnCamera3d();
            var target = Render.CreateTarget(32, 32);

            Render.SetCameraTarget(camera, target);

            // A camera starts with nothing, and it has to be able to get back there, since an
            // editor that opens a preview into a texture has to be able to close it again.
            Render.SetCameraTarget(camera, AssetHandle.None);
        });

        harness.Run();
    }

    [Fact]
    public void AnEntityThatIsNotACameraIsRefused()
    {
        if (!App.HasRenderer) return;

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, world =>
        {
            var entity = world.Resource<EcsWorld>().Spawn();
            var target = Render.CreateTarget(32, 32);

            var refused = Assert.Throws<BevyNativeException>(
                () => Render.SetCameraTarget(entity, target));

            Assert.Equal(NativeStatus.NotPresent, refused.Status);
        });

        harness.Run();
    }

    [Fact]
    public void AHandleThatNamesNoImageIsRefused()
    {
        if (!App.HasRenderer) return;

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, _ =>
        {
            var camera = Render.SpawnCamera3d();

            // A key out of any table's range, as a fabricated or released handle looks from the
            // other side. Drawing into nothing is not a default worth having.
            var refused = Assert.Throws<BevyNativeException>(
                () => Render.SetCameraTarget(camera, new AssetHandle(0x7FFF_FFFF)));

            Assert.Equal(NativeStatus.NoComponent, refused.Status);
        });

        harness.Run();
    }

    /// <summary>
    /// A target can be handed to the interface, and a thumbnail is made of one.
    /// </summary>
    /// <remarks>
    /// The editor draws its icons from files, and a preview of a mesh or a material has no file to
    /// draw from. What it has is a camera pointed at an image, and this is the seam between that
    /// image and a draw call.
    /// </remarks>
    [Fact]
    public void ATargetCanBeDrawnByTheInterface()
    {
        if (!App.HasRenderer || !App.HasEditor) return;

        // The interface has to be installed for a picture to have anywhere to be named, and it is
        // installed only when an app asks to draw one. An offscreen run can, which makes this
        // checkable without a window.
        var config = Config.OffscreenFor(64, 64, frames: 4);
        config.Gui = true;

        using var app = new App(config);

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                if (world.Resource<Time>().FrameCount != 2) return;

                var target = Render.CreateTarget(32, 32);
                var named = ImGuiTextures.Of(target);

                Assert.NotEqual(0ul, named);

                // Asked for twice, named once, because what it names does not change when the
                // picture behind it is redrawn.
                Assert.Equal(named, ImGuiTextures.Of(target));

                // A handle that names nothing is not a picture, and says so rather than naming one.
                Assert.Equal(0ul, ImGuiTextures.Of(AssetHandle.None));
            },
            "Test.Name"));

        Assert.Equal(0, app.Run());
    }

    /// <summary>Where a test keeps the handle it made, so a later frame can capture it.</summary>
    private sealed record Target(AssetHandle Handle);

    /// <summary>The size a PNG declares, read out of its header.</summary>
    private static (uint Width, uint Height) PngSize(string path)
    {
        var bytes = File.ReadAllBytes(path);

        Assert.True(bytes.Length > 24, "the file is too short to be a PNG");

        return (
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)),
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)));
    }
}
