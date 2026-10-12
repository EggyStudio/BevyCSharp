using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a window resized, minimized, restored and moved while it draws, a storm of the calls a
/// game and a desktop make, after which it draws at the size last asked for with nothing logged.
/// </summary>
/// <remarks>
/// A window's surface is made again at each size and torn down while it is minimized, which an
/// offscreen run, drawing into an image, never does, so this needs a real window, and a display
/// that gives a minimized one back (<see cref="Needs.WindowStorm"/>). Anything the engine logs as an
/// error during it fails the test (N 3.7).
/// </remarks>
[Collection("engine")]
public sealed class WindowStormTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-storm-");

    public void Dispose() => _folder.Dispose();

    [SkippableFact]
    public void AWindowStormedWhileItDrawsDrawsAtTheSizeLastAskedFor()
    {
        Needs.WindowStorm();

        var picture = _folder.File("storm.png");
        var frame = 0;
        var shot = -1;

        // Sizes one after another a frame apart, as a window dragged larger and smaller, some odd.
        (uint Width, uint Height)[] sizes = [(1024, 640), (320, 200), (801, 449), (64, 48), (1280, 720), (500, 900), (33, 17), (960, 540)];

        using var app = new App(Config.Windowed("Storm", 640, 400));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            _ => Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0.1f, 0.6f, 0.2f, 1f) }),
            "Test.Camera"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                frame++;
                var ecs = world.Resource<EcsWorld>();

                if (frame >= 10 && frame < 10 + sizes.Length)
                {
                    var (width, height) = sizes[frame - 10];
                    Window.SetSize(width, height);
                }

                // Minimized for a while, which draws nothing, and given back, twice.
                if (frame is 30 or 65) Window.Minimize();
                if (frame is 45 or 80) ecs.Wrap<WindowRef>(Window.Entity()).InternalMinimizeRequest = false;

                if (frame == 100) Window.SetPosition(40, 40);
                if (frame == 110) Window.SetSize(800, 450);

                // Captured once the size has had time to reach the surface, and the run ended once
                // the picture has had time to be written.
                if (frame == 150)
                {
                    Render.Screenshot(picture);
                    shot = frame;
                }

                if (shot > 0 && frame == shot + 60) App.RequestExit();
            },
            "Test.Storm"));

        Assert.Equal(0, app.Run());

        Assert.True(File.Exists(picture), "no picture was written after the storm");
        Assert.Equal((800u, 450u), PngSize(picture));
    }

    /// <summary>A PNG's size, from its header.</summary>
    private static (uint Width, uint Height) PngSize(string path)
    {
        using var file = File.OpenRead(path);
        Span<byte> header = stackalloc byte[24];
        file.ReadExactly(header);
        return (System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(header[16..]), System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(header[20..]));
    }
}
