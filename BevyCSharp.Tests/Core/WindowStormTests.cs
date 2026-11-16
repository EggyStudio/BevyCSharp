using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a window resized, minimized, restored and moved while it draws, a storm of the calls a
/// game and a desktop make, after which it draws at the size last asked for with nothing logged,
/// and the monitors such a window's platform lists.
/// </summary>
/// <remarks>
/// A window's surface is made again at each size and torn down while it is minimized, which an
/// offscreen run, drawing into an image, never does, so this needs a real window, and a display
/// that gives a minimized one back (<see cref="Needs.WindowStorm"/>). Anything the engine logs as an
/// error during it fails the test (N 3.7). Both tests read the one run of <see cref="Storm"/>, since
/// winit makes one event loop a process and a second windowed app is refused.
/// </remarks>
[Collection("engine")]
public sealed class WindowStormTests(WindowStormTests.Storm storm) : IClassFixture<WindowStormTests.Storm>
{
    [SkippableFact]
    public void AWindowStormedWhileItDrawsDrawsAtTheSizeLastAskedFor()
    {
        Needs.WindowStorm();

        var run = storm.Run;
        Assert.Equal(0, run.Exit);
        Assert.True(File.Exists(run.Picture), "no picture was written after the storm");
        Assert.Equal((800u, 450u), PngSize(run.Picture));
    }

    /// <summary>
    /// The monitors a real window's platform lists are described, and an index past the last is
    /// refused by each call that takes one, as a video mode past a monitor's last is.
    /// </summary>
    /// <remarks>
    /// A run with no window lists no monitor, so these calls are reached here alone, where
    /// xvfb-run's screen is one monitor.
    /// </remarks>
    [SkippableFact]
    public void AWindowsMonitorsAreDescribedAndAnIndexPastTheLastIsRefused()
    {
        Needs.WindowStorm();

        var run = storm.Run;
        Assert.Equal(0, run.Exit);
        Assert.True(run.Monitors > 0, "the window's platform listed no monitor");
        Assert.True(run.First.Width > 0 && run.First.Height > 0, $"the first monitor is {run.First.Width} by {run.First.Height}");
        Assert.NotNull(run.Name);
        Assert.All(run.Modes, mode => Assert.True(mode.Width > 0 && mode.Height > 0));
        Assert.Equal(["Monitor", "MonitorName", "MonitorModes", "SetVideoMode"], run.Refused);
    }

    /// <summary>What the storm's run left, its picture and what it read of the monitors.</summary>
    public sealed record Result(int Exit, string Picture, int Monitors, MonitorInfo First, string? Name, VideoMode[] Modes, List<string> Refused);

    /// <summary>
    /// The window stormed, made once for the tests that read it, when the first of them that is
    /// asked to run reads <see cref="Run"/>.
    /// </summary>
    public sealed class Storm : IDisposable
    {
        private readonly TestFolder _folder = new("bcs-storm-");
        private readonly Lazy<Result> _run;

        public Storm() => _run = new(Make);

        public Result Run => _run.Value;

        public void Dispose() => _folder.Dispose();

        private Result Make()
        {
            var picture = _folder.File("storm.png");
            var (frame, shot) = (0, -1);
            var (monitors, first, name, modes) = (0, default(MonitorInfo), (string?)null, Array.Empty<VideoMode>());
            var refused = new List<string>();

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

                    // The monitors, read once winit has long listed them and the window stands at
                    // its last size, each call given an index past the last as well.
                    if (frame == 120 && (monitors = Window.MonitorCount()) > 0)
                    {
                        (first, name, modes) = (Window.Monitor(0), Window.MonitorName(0), Window.MonitorModes(0));
                        Refuses(refused, "Monitor", () => Window.Monitor(monitors));
                        Refuses(refused, "MonitorName", () => Window.MonitorName(monitors));
                        Refuses(refused, "MonitorModes", () => Window.MonitorModes(monitors));
                        Refuses(refused, "SetVideoMode", () => Window.SetVideoMode(0, modes.Length));
                    }

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

            var exit = app.Run();
            return new Result(exit, picture, monitors, first, name, modes, refused);
        }
    }

    /// <summary>Notes <paramref name="what"/> where <paramref name="call"/> is refused.</summary>
    private static void Refuses(List<string> refused, string what, Action call)
    {
        try
        {
            call();
        }
        catch (BevyNativeException)
        {
            refused.Add(what);
        }
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
