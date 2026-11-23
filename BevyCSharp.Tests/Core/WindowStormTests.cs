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
                "covering its screen, not scaled", "window.mode reads Borderless", "window.height 450: done", "window.width 900: done",
                "borderless at 900 by 450, at 510 315, no frame", "window.mode borderlessfullscreen: done", "window.scaling nearest: done",
                "window.width 450: done", "window.height 225: done",
                "window.mode reads BorderlessFullscreen", "window.scaling reads Nearest", "window.width reads 450",
                "window.width 600: done", "window.height 300: done", "window.scaling sharp: done",
                "window.width 450: done", "window.height 225: done", "window.scaling reads Sharp",
                "window.mode windowed: done", "window.mode reads Windowed", "windowed at 450 by 225, at 40 40, framed", "window.resizable off: done",
                "window.resizable reads off", "window.resizable on: done", "window.vsync on: done", "window.vsync reads on", "window.monitor reads 0",
                $"window.monitor 0: {Answer.NotSettable}", $"window.monitor 1: {Answer.NotSettable}",
            ],
            run.Page);
        Assert.Equal(["window.height 225", "window.scaling Sharp", "window.width 450"], run.Kept);
        Assert.Equal("Windowed, resizable, 450 by 225, no monitor, vsync, Sharp", run.Next);
    }

    /// <summary>
    /// Borderless fullscreen's sharp scaling equals the nearest one to the bit where the screen is
    /// a whole number of times the picture.
    /// </summary>
    /// <remarks>
    /// With no window manager the window covering its screen keeps the size it had, 900 by 450,
    /// which a picture of 450 by 225 fills twice over. The picture is a noise image laid over the
    /// camera's interface, so neighboring picture pixels differ, and it is read as the noise only
    /// where Bevy's interface is drawn into the camera's picture before the picture is scaled.
    /// </remarks>
    [SkippableFact]
    public void TheSharpScalingEqualsTheNearestAtAWholeRatio()
    {
        Needs.WindowStorm();

        var run = storm.Run;
        Assert.Equal(0, run.Exit);
        Assert.Equal("sharp equals nearest at every pixel of 900 by 450", run.WholeRatio);
    }

    /// <summary>
    /// At one and a half the sharp scaling fills the screen, every window pixel inside a picture
    /// pixel takes it whole as the nearest scaling does, and every one across an edge lies between
    /// the picture pixels round it.
    /// </summary>
    /// <remarks>
    /// A picture of 600 by 300 in the window of 900 by 450. Of every three window pixels across or
    /// down, the first and the last lie inside a picture pixel and the middle one spans the edge
    /// between two, whose colors are read from the nearest scaling's picture at a window pixel
    /// inside each. The linear scaling blends every window pixel and fails it.
    /// </remarks>
    [SkippableFact]
    public void TheSharpScalingFillsTheScreenAtOneAndAHalfWithEachEdgeInOnePixel()
    {
        Needs.WindowStorm();

        var run = storm.Run;
        Assert.Equal(0, run.Exit);
        Assert.Equal(
            "180000 of 180000 window pixels inside a picture pixel equal nearest's, 225000 of 225000 across an edge lie between "
                + "the picture pixels round it, 0 black, 900 by 450",
            run.HalfRatio);
    }

    /// <summary>
    /// What the storm's run left, its picture, what it read of the monitors and of the window's
    /// page.
    /// </summary>
    public sealed record Result(
        int Exit, string Picture, int Monitors, MonitorInfo First, string? Name, VideoMode[] Modes, List<string> Refused,
        string Opened, List<string> Page, string[] Kept, string Next, string WholeRatio, string HalfRatio);

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
            // The scaled pictures read back, the nearest and the sharp scaling's at a ratio of two
            // and of one and a half.
            var (capture, into) = (default(Capture?), 0);
            var pictures = new CapturedImage?[4];
            void Begin(int index)
            {
                if (capture is not null) page.Add($"picture {into} was still being read");
                (capture, into) = (Render.BeginCapture(), index);
            }

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

                // A noise picture over the camera's interface, so neighboring picture pixels differ
                // in a scaled picture read back, none of its colors near black.
                app.AddSystem(Stage.Startup, new SystemDescriptor(
                    _ =>
                    {
                        var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0.1f, 0.6f, 0.2f, 1f) });
                        var noise = new byte[450 * 225 * 4];
                        var random = new Random(163);
                        for (var i = 0; i < noise.Length; i += 4)
                            (noise[i], noise[i + 1], noise[i + 2], noise[i + 3]) = ((byte)random.Next(48, 256), (byte)random.Next(48, 256), (byte)random.Next(48, 256), 255);
                        var node = Ui.SpawnNode(new UiSettings
                        {
                            Absolute = true,
                            Width = Length.Percent(100f),
                            Height = Length.Percent(100f),
                            Camera = camera,
                        });
                        Ui.SetImage(node, new UiImageSettings { Image = Render.CreateImage(noise, 450, 225) });
                    },
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
                            Line(world, "window.width 900");
                        }
                        if (frame == 184)
                        {
                            page.Add(Placed("borderless"));
                            Line(world, "window.mode borderlessfullscreen");
                            Line(world, "window.scaling nearest");
                            Line(world, "window.width 450");
                            Line(world, "window.height 225");
                        }

                        // Each scaled picture read back once it has reached the window, the nearest
                        // scaling's at a ratio of two and of one and a half, then the sharp one's.
                        if (frame == 190)
                        {
                            Read("window.mode");
                            Read("window.scaling");
                            Read("window.width");
                            Begin(0);
                        }
                        if (frame == 196)
                        {
                            Line(world, "window.width 600");
                            Line(world, "window.height 300");
                        }
                        if (frame == 202) Begin(1);
                        if (frame == 208) Line(world, "window.scaling sharp");
                        if (frame == 214) Begin(2);
                        if (frame == 220)
                        {
                            Line(world, "window.width 450");
                            Line(world, "window.height 225");
                        }
                        if (frame == 226)
                        {
                            Read("window.scaling");
                            Begin(3);
                        }
                        if (capture is { } asked && Render.TryReadCapture(asked, out var read))
                        {
                            pictures[into] = read;
                            capture = null;
                        }
                        if (frame == 232) Line(world, "window.mode windowed");

                        // A size of the system's own a frame on, as a window manager restores the
                        // one it saved as the window leaves its screen, which the size given back
                        // is set over.
                        if (frame == 234) Window.SetSize(1000, 300);
                        if (frame == 240)
                        {
                            Read("window.mode");
                            page.Add(Placed("windowed"));
                            Line(world, "window.resizable off");
                        }
                        if (frame == 244)
                        {
                            Read("window.resizable");
                            Line(world, "window.resizable on");
                            Line(world, "window.vsync on");
                        }
                        if (frame == 248)
                        {
                            Read("window.vsync");
                            Read("window.monitor");
                            Line(world, "window.monitor 0");
                            Line(world, $"window.monitor {monitors}");
                        }

                        if (frame == 312) App.RequestExit();
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
            return new Result(exit, picture, monitors, first, name, modes, refused, opened, page, kept, said,
                Matched(pictures[3], pictures[0]), Blended(pictures[2], pictures[1]));
        }

        // Whether a scaled picture shows the noise, its picture pixels nearly all of different
        // colors, where a picture with the interface left out holds the camera's clear color alone.
        private static string? NotTheNoise(CapturedImage picture)
        {
            var colors = new HashSet<(byte, byte, byte, byte)>();
            for (var y = 0u; y < picture.Height; y++)
                for (var x = 0u; x < picture.Width; x++)
                    colors.Add(picture.At(x, y));
            return colors.Count > 450 * 225 / 2 ? null : $"the nearest scaling's picture holds {colors.Count} colors, not the noise";
        }

        // The sharp scaling's picture at a whole ratio against the nearest one's, pixel by pixel.
        private static string Matched(CapturedImage? sharp, CapturedImage? nearest)
        {
            if (sharp is null || nearest is null) return "a scaled picture was not read";
            if ((sharp.Width, sharp.Height) != (nearest.Width, nearest.Height))
                return $"sharp {sharp.Width} by {sharp.Height} against nearest {nearest.Width} by {nearest.Height}";
            if (NotTheNoise(nearest) is { } plain) return plain;
            var differ = 0;
            for (var y = 0u; y < sharp.Height; y++)
                for (var x = 0u; x < sharp.Width; x++)
                    if (sharp.At(x, y) != nearest.At(x, y)) differ++;
            return differ == 0
                ? $"sharp equals nearest at every pixel of {sharp.Width} by {sharp.Height}"
                : $"{differ} of {sharp.Width * sharp.Height} pixels differ from nearest's";
        }

        // The sharp scaling's picture at one and a half against the nearest one's. Of every three
        // window pixels along an axis the first and the last lie inside the first and the second
        // of two picture pixels and the middle one spans the edge between them, so a picture pixel
        // is read from the nearest scaling's window pixel inside it, the first of three for an
        // even one and the last for an odd one.
        private static string Blended(CapturedImage? sharp, CapturedImage? nearest)
        {
            if (sharp is null || nearest is null) return "a scaled picture was not read";
            if ((sharp.Width, sharp.Height) != (nearest.Width, nearest.Height))
                return $"sharp {sharp.Width} by {sharp.Height} against nearest {nearest.Width} by {nearest.Height}";
            if (NotTheNoise(nearest) is { } plain) return plain;

            static uint Inside(uint pixel) => pixel % 2 == 0 ? 3 * pixel / 2 : (3 * pixel + 1) / 2;
            // The picture pixels a window pixel takes along an axis, the one it lies inside or the
            // two either side of the edge it spans.
            static uint[] Under(uint at) => at % 3 == 1 ? [at / 3 * 2, at / 3 * 2 + 1] : [(2 * at + 1) / 3];

            var (inside, equal, edges, between, black) = (0, 0, 0, 0, 0);
            for (var y = 0u; y < sharp.Height; y++)
                for (var x = 0u; x < sharp.Width; x++)
                {
                    var seen = sharp.At(x, y);
                    if (seen.R < 8 && seen.G < 8 && seen.B < 8) black++;
                    if (x % 3 != 1 && y % 3 != 1)
                    {
                        inside++;
                        if (seen == nearest.At(x, y)) equal++;
                        continue;
                    }
                    edges++;
                    var round = Under(x).SelectMany(px => Under(y).Select(py => nearest.At(Inside(px), Inside(py)))).ToArray();
                    // A blend lies between the colors it is made of, give or take the rounding of
                    // its eight bits.
                    static bool Within(byte value, IEnumerable<byte> those) => value + 1 >= those.Min() && value <= those.Max() + 1;
                    if (Within(seen.R, round.Select(p => p.R)) && Within(seen.G, round.Select(p => p.G)) && Within(seen.B, round.Select(p => p.B)))
                        between++;
                }
            return $"{equal} of {inside} window pixels inside a picture pixel equal nearest's, {between} of {edges} across an edge "
                + $"lie between the picture pixels round it, {black} black, {sharp.Width} by {sharp.Height}";
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
