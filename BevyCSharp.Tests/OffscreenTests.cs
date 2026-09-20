using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers drawing with no window, and reading the picture back.
/// </summary>
/// <remarks>
/// <para>
/// The only kind of run whose output can be checked without a person and without a display. A
/// windowed run needs a display server to open a window on, which a build server does not have,
/// and a headless run installs no renderer at all, so neither can answer the one question a test
/// cannot otherwise ask: was anything actually drawn.
/// </para>
/// <para>
/// Skipped on a bridge with no renderer, which is what the test workflow builds, so this runs
/// where there is something to run and is quiet where there is not.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class OffscreenTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"bcs-offscreen-{Guid.NewGuid():N}");

    public OffscreenTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void ARunWithNoWindowDrawsAndIsCaptured()
    {
        if (!App.HasRenderer) return;

        var path = Path.Combine(_directory, "capture.png");

        using var app = new App(Config.OffscreenFor(320, 180, frames: 90));

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            static world =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f });
                world.Resource<EcsWorld>().Add(
                    camera, Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));
            },
            "Test.Scene"));

        // Asked for once the renderer has a frame behind it, and well before the run ends, because
        // the picture comes back off the GPU over the frames after the request.
        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                if (world.Resource<Time>().FrameCount == 10) Render.Screenshot(path);
            },
            "Test.Capture"));

        Assert.Equal(0, app.Run());

        Assert.True(File.Exists(path), $"no capture appeared at {path}");

        var (width, height) = PngSize(path);

        // The size of the image the run was told to draw into, which a capture of a window could
        // not be, there being no window.
        Assert.Equal(320u, width);
        Assert.Equal(180u, height);
    }

    /// <summary>A run with no renderer draws nothing, and says so rather than pretending.</summary>
    [Fact]
    public void AHeadlessRunHasNothingToCapture()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, world =>
        {
            using (ConsoleHost.Lend(world))
            {
                var answer = ConsoleCommands.Run($"shot {Path.Combine(_directory, "none.png")}");

                Assert.Contains("nothing", answer);
            }
        });

        harness.Run();

        Assert.False(File.Exists(Path.Combine(_directory, "none.png")));
    }

    /// <summary>
    /// The size a PNG declares, read out of its header.
    /// </summary>
    /// <remarks>
    /// Eight bytes of signature, then a length and a type, then the header itself opens with the
    /// two dimensions as big-endian 32-bit numbers. Reading them takes fewer lines than taking a
    /// dependency on an image library to ask.
    /// </remarks>
    private static (uint Width, uint Height) PngSize(string path)
    {
        var bytes = File.ReadAllBytes(path);

        Assert.True(bytes.Length > 24, "the file is too short to be a PNG");
        Assert.Equal<byte[]>([0x89, 0x50, 0x4E, 0x47], bytes[..4]);

        return (
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)),
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)));
    }
}
