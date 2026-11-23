using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a window resized, minimized, restored and moved while it draws, a storm of the calls a
/// game and a desktop make, after which it draws at the size last asked for with nothing logged,
/// the monitors such a window's platform lists, and the settings' window page set on it.
/// </summary>
/// <remarks>
/// A window's surface is made again at each size and torn down while it is minimized, which an
/// offscreen run, drawing into an image, never does, so this needs a real window, and a display
/// that gives a minimized one back (<see cref="Needs.WindowStorm"/>). Anything the engine logs as
/// an error during it fails the test (N 3.7). The tests read the one run of <see cref="Storm"/>,
/// since winit makes one event loop a process and a second windowed app is refused.
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

    /// <summary>
    /// The window opens at the size and the vsync of the settings file's lines, each of the page's
    /// settings sets the window it reads, a borderless window stands without its frame at the size
    /// set in the middle of its screen and covers the screen at the screen's size, a mode the
    /// system cannot give is refused with the reason and the window keeps its own, the size and
    /// the place as a window are given back over a size of the system's as it returns to one, and
    /// the file written holds what the player set, which the next start gives the window.
    /// </summary>
    /// <remarks>
    /// xvfb-run's screen is one monitor of 1920 by 1080, so the monitor only reads. Its modes carry
    /// no rate, so the screen lists no mode at its own size and rate and fullscreen is refused, as
    /// it would be on a screen whose driver names no rate. It runs no window manager, which on X11
    /// sizes a window covering its screen, so the run reads the mode the window holds for the
    /// cover rather than the size it covers.
    /// </remarks>
    [SkippableFact]
    public void TheWindowPageSetsTheWindowAndTheFileItWritesOpensTheNext()
    {
        Needs.WindowStorm();

        var run = storm.Run;
        Assert.Equal(0, run.Exit);
        Assert.Equal("opened at 700 by 420, vsync off", run.Opened);
        Assert.Equal(
            [
                "window.mode borderless: done", "window.mode reads Borderless", "window.width reads 800", "window.width 1000: done",
                $"window.mode fullscreen: {Answer.Refused}, The screen lists no mode at its own size and rate for a fullscreen window to take, so the window stays borderless. Borderless fullscreen covers the screen at its own mode.",
                "borderless at 1000 by 450, at 460 315, no frame", "window.width 1920: done", "window.height 1080: done",
                "covering its screen, not scaled", "window.mode reads Borderless", "window.height 450: done", "window.width 1000: done",
                "borderless at 1000 by 450, at 460 315, no frame", "window.mode borderlessfullscreen: done", "window.scaling integer: done",
                "window.width 300: done", "window.height 200: done",
                "window.mode reads BorderlessFullscreen", "window.scaling reads Integer", "window.width reads 300",
                "window.mode windowed: done", "window.mode reads Windowed", "windowed at 300 by 200, at 40 40, framed", "window.resizable off: done",
                "window.resizable reads off", "window.resizable on: done", "window.vsync on: done", "window.vsync reads on", "window.monitor reads 0",
                $"window.monitor 0: {Answer.NotSettable}", $"window.monitor 1: {Answer.NotSettable}",
            ],
            run.Page);
        Assert.Equal(["window.height 200", "window.scaling Integer", "window.width 300"], run.Kept);
        Assert.Equal("Windowed, resizable, 300 by 200, no monitor, vsync, Integer", run.Next);
    }

    /// <summary>
    /// Borderless fullscreen draws its picture at the size set, and the integer scaling brings it
    /// to the window by the largest whole number that fits, in the window's middle with black
    /// round it.
    /// </summary>
    /// <remarks>
    /// With no window manager the window covering its screen keeps the size it had, 1000 by 450,
    /// which the picture of 300 by 200 fits twice in, so it is drawn at 600 by 400 from 200 across
    /// and 24 down. The run reads the green of the camera's clear along the window's middle row
    /// and column, and the black at its corners.
    /// </remarks>
    [SkippableFact]
    public void BorderlessFullscreenScalesItsPictureByTheLargestWholeNumberThatFits()
    {
        Needs.WindowStorm();

        var run = storm.Run;
        Assert.Equal(0, run.Exit);
        Assert.Equal("green from 200 to 800 across and from 24 to 424 down of 1000 by 450, black corners", run.Scaled);
    }

    /// <summary>
    /// What the storm's run left, its picture, what it read of the monitors and of the window's
    /// page.
    /// </summary>
    public sealed record Result(
        int Exit, string Picture, int Monitors, MonitorInfo First, string? Name, VideoMode[] Modes, List<string> Refused,
        string Opened, List<string> Page, string[] Kept, string Next, string Scaled);

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

        public void Dispose()
        {
            WindowSettingsTests.Forget(_folder);
            _folder.Dispose();
        }

        private Result Make()
        {
            var picture = _folder.File("storm.png");

            // Sizes one after another a frame apart, as a window dragged larger and smaller, some
            // odd.
            (uint Width, uint Height)[] sizes = [(1024, 640), (320, 200), (801, 449), (64, 48), (1280, 720), (500, 900), (33, 17), (960, 540)];

            // The run's settings file, in place of the player's, holding a size and a vsync the
            // window opens with in place of the config's.
            var settings = _folder.File(SettingsPath.FileName);
            File.WriteAllText(settings, "window.width 700\nwindow.height 420\nwindow.vsync off\n");
            var pathFor = SettingsPath.For;
            SettingsPath.For = _ => settings;
            try
            {
                return Stormed(picture, sizes, settings);
            }
            finally
            {
                SettingsPath.For = pathFor;
            }
        }

        private static Result Stormed(string picture, (uint Width, uint Height)[] sizes, string settings)
        {
            var frame = 0;
            var (monitors, first, name, modes) = (0, default(MonitorInfo), (string?)null, Array.Empty<VideoMode>());
            var refused = new List<string>();
            var (opened, page) = ("", new List<string>());
            var (capture, scaled) = (default(Capture?), "no picture was read");

            // A line as the console or the panel gives it, with the reason of a refusal, and a
            // setting's value as its row reads it.
            void Line(World world, string line)
            {
                var answer = ConsoleHost.Submit(line, world);
                page.Add($"{line}: {answer.Code ?? "done"}{(answer.Code == Answer.Refused ? $", {answer.Text}" : "")}");
            }
            void Read(string name) => page.Add($"{name} reads {Registry.Process.FindSetting(name)!.Text}");
            string Sized(string what) => Window.Size() is var (width, height) ? $"{what} {width} by {height}" : what;
            string Placed(string what) =>
                $"{Sized($"{what} at")}, at {Window.Place().X} {Window.Place().Y}, {(Window.Shown()?.Decorated == true ? "framed" : "no frame")}";

            // Disposed before the file is read, since the settings file writes a last time as the
            // app's world goes.
            var app = new App(Config.Windowed("Storm", 640, 400));
            int exit;
            try
            {
                app.AddPlugin(new EnginePlugin());

                app.AddSystem(Stage.Startup, new SystemDescriptor(
                    _ => Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0.1f, 0.6f, 0.2f, 1f) }),
                    "Test.Camera"));

                app.AddSystem(Stage.Update, new SystemDescriptor(
                    world =>
                    {
                        frame++;
                        var ecs = world.Resource<EcsWorld>();

                        if (frame == 3) opened = $"{Sized("opened at")}, vsync {Registry.Process.FindSetting("window.vsync")!.Text}";

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

                        // The monitors, read once winit has long listed them and the window stands
                        // at its last size, each call given an index past the last as well.
                        if (frame == 120 && (monitors = Window.MonitorCount()) > 0)
                        {
                            (first, name, modes) = (Window.Monitor(0), Window.MonitorName(0), Window.MonitorModes(0));
                            Refuses(refused, "Monitor", () => Window.Monitor(monitors));
                            Refuses(refused, "MonitorName", () => Window.MonitorName(monitors));
                            Refuses(refused, "MonitorModes", () => Window.MonitorModes(monitors));
                            Refuses(refused, "SetVideoMode", () => Window.SetVideoMode(0, modes.Length));
                        }

                        // Captured once the size has had time to reach the surface, and the run
                        // ended once the picture has had time to be written.
                        if (frame == 150) Render.Screenshot(picture);

                        // The window's page once the picture is written, a set of each setting as
                        // the console or the panel makes it and a read of each a few frames on,
                        // once winit has taken it.
                        if (frame == 165) Line(world, "window.mode borderless");
                        if (frame == 172)
                        {
                            Read("window.mode");
                            Read("window.width");
                            Line(world, "window.width 1000");
                            Line(world, "window.mode fullscreen");
                        }

                        // Borderless at a size under the screen's stands in its middle without a
                        // frame, and covers it at the screen's size, then stands in the middle
                        // again at a size under it.
                        if (frame == 176)
                        {
                            page.Add(Placed("borderless"));
                            Line(world, "window.width 1920");
                            Line(world, "window.height 1080");
                        }
                        if (frame == 180)
                        {
                            if (Window.Shown() is { Mode: WindowMode.BorderlessFullscreen, Scaled: var drawnScaled })
                                page.Add($"covering its screen, {(drawnScaled ? "scaled" : "not scaled")}");
                            Read("window.mode");
                            Line(world, "window.height 450");
                            Line(world, "window.width 1000");
                        }
                        if (frame == 184)
                        {
                            page.Add(Placed("borderless"));
                            Line(world, "window.mode borderlessfullscreen");
                            Line(world, "window.scaling integer");
                            Line(world, "window.width 300");
                            Line(world, "window.height 200");
                        }

                        // The scaled picture read back once it has reached the window.
                        if (frame == 190)
                        {
                            Read("window.mode");
                            Read("window.scaling");
                            Read("window.width");
                            capture = Render.BeginCapture();
                        }
                        if (capture is { } asked && Render.TryReadCapture(asked, out var read))
                        {
                            scaled = Described(read!);
                            capture = null;
                        }
                        if (frame == 210) Line(world, "window.mode windowed");

                        // A size of the system's own a frame on, as a window manager restores the
                        // one it saved as the window leaves its screen, which the size given back
                        // is set over.
                        if (frame == 212) Window.SetSize(1000, 300);
                        if (frame == 218)
                        {
                            Read("window.mode");
                            page.Add(Placed("windowed"));
                            Line(world, "window.resizable off");
                        }
                        if (frame == 222)
                        {
                            Read("window.resizable");
                            Line(world, "window.resizable on");
                            Line(world, "window.vsync on");
                        }
                        if (frame == 226)
                        {
                            Read("window.vsync");
                            Read("window.monitor");
                            Line(world, "window.monitor 0");
                            Line(world, $"window.monitor {monitors}");
                        }

                        if (frame == 290) App.RequestExit();
                    },
                    "Test.Storm"));

                exit = app.Run();
            }
            finally
            {
                app.Dispose();
            }

            // The file as the run left it, and what the next start gives the window from it.
            var kept = File.ReadAllLines(settings);
            var next = WindowSettings.Open(Config.Windowed("Storm", 640, 400), new WindowPlace(false, 0, 0, 640, 400, false));
            var said = $"{next.Mode}, {(next.Resizable ? "resizable" : "fixed")}, {next.Width} by {next.Height}, "
                + $"{(next.Monitor is { } monitor ? $"monitor {monitor}" : "no monitor")}, {(next.Vsync ? "vsync" : "no vsync")}, {next.Scaling}";
            return new Result(exit, picture, monitors, first, name, modes, refused, opened, page, kept, said, scaled);
        }

        // Where the picture's green lies along the window's middle row and column, and whether
        // the corners are black.
        private static string Described(CapturedImage window)
        {
            static bool Green((byte R, byte G, byte B, byte A) pixel) => pixel.G > 100 && pixel.G > pixel.R + 40;
            static bool Black((byte R, byte G, byte B, byte A) pixel) => pixel.R < 8 && pixel.G < 8 && pixel.B < 8;

            var (row, column) = (window.Height / 2, window.Width / 2);
            var across = Enumerable.Range(0, (int)window.Width).Where(x => Green(window.At((uint)x, row))).ToArray();
            var down = Enumerable.Range(0, (int)window.Height).Where(y => Green(window.At(column, (uint)y))).ToArray();
            if (across.Length == 0 || down.Length == 0) return $"no green in {window.Width} by {window.Height}";

            uint[] xs = [0, window.Width - 1];
            uint[] ys = [0, window.Height - 1];
            var corners = xs.All(x => ys.All(y => Black(window.At(x, y)))) ? "black corners" : "corners not black";
            return $"green from {across[0]} to {across[^1] + 1} across and from {down[0]} to {down[^1] + 1} down of {window.Width} by {window.Height}, {corners}";
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
