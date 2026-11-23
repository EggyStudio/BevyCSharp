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
    /// The window opens at the resolution and the vsync of the settings file's lines, each of the
    /// page's settings sets the window it reads, a borderless window stands without its frame at
    /// the resolution in the middle of its screen and covers the screen at the screen's size, a
    /// resolution larger than the screen's and a mode the system cannot give are refused with the
    /// reason and the window keeps its own, the render page sets the picture and reads it, the size
    /// and the place as a window are given back over a size of the system's as it returns to one,
    /// and the file written holds what the player set, which the next start gives the window.
    /// </summary>
    /// <remarks>
    /// xvfb-run's screen is one monitor of 1920 by 1080, so the monitor only reads. Its modes carry
    /// no rate, so the screen lists no mode at its own size and rate and fullscreen is refused, as
    /// it would be on a screen whose driver names no rate. It runs no window manager, which on X11
    /// sizes a window covering its screen, so the run reads the mode the window holds for the
    /// cover rather than the size it covers. A size the game fixes holds the scale unused, which
    /// reads as the player set it, and the scale's row says why.
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
                "window.mode borderless: done", "window.mode reads Borderless", "window.resolution reads 700x420", "window.resolution 1000x450: done",
                $"window.mode fullscreen: {Answer.Refused}, The screen lists no mode at its own size and rate for a fullscreen window to take, so the window stays borderless. Borderless fullscreen covers the screen at its own mode.",
                "borderless at 1000 by 450, at 460 315, no frame", "window.resolution 1920x1080: done",
                "covering its screen, not scaled", "window.mode reads Borderless",
                $"window.resolution 2560x1440: {Answer.Refused}, The screen is 1920x1080, so a window takes no size larger than it. The window stays at 1920x1080.",
                "window.resolution 900x450: done", "borderless at 900 by 450, at 510 315, no frame",
                "render.filter nearest: done", "render.scale 50: done", "render.filter reads Nearest", "render.scale reads 50%",
                "picture 450 by 225 over 0 0 900 450",
                "The game fixes the render size at 600x300, so the scale waits until it lets the size go.", "render.scale reads 50%",
                "render.filter sharp: done", "render.scale is not locked", "render.filter reads Sharp",
                "render.scale 100%: done", "render.aspect 4:3: done", "render.aspect reads 4:3", "render.fit reads Fit",
                "picture 600 by 450 over 150 0 600 450", "the node over the picture 600 by 450 about 450 225",
                "render.fit stretch: done", "render.fit reads Stretch", "picture 600 by 450 over 0 0 900 450",
                "render.aspect native: done", "render.fit fit: done", "render.scale 50%: done",
                "picture 450 by 225 over 0 0 900 450", "the interface's whole node 900 by 450 about 450 225",
                "the node over the picture 900 by 450 about 450 225", "render.scale 100%: done",
                "window.mode windowed: done", "window.mode reads Windowed", "windowed at 900 by 450, at 40 40, framed", "window.resizable off: done",
                "window.resizable reads off", "window.resizable on: done", "window.vsync on: done", "window.vsync reads on", "window.monitor reads 0",
                $"window.monitor 0: {Answer.NotSettable}", $"window.monitor 1: {Answer.NotSettable}",
            ],
            run.Page);
        Assert.Equal(["render.filter Sharp", "window.resolution 900x450"], run.Kept);
        Assert.Equal("Windowed, resizable, 900 by 450, no monitor, vsync, Picture { Scale = 1, Size = , Aspect = , Fit = Fit, Filter = Sharp }", run.Next);
    }

    /// <summary>
    /// A 4 to 3 picture on a 2 to 1 window fitted keeps its shape with black bars at the sides and
    /// none over the picture, and stretched covers the whole window.
    /// </summary>
    /// <remarks>
    /// The window is 900 by 450, so the fitted picture is 600 by 450 with bars of 150 at either
    /// side, and the noise laid over the camera's interface shows no black of its own.
    /// </remarks>
    [SkippableFact]
    public void AnAspectUnlikeTheWindowsIsFittedWithBarsAndStretchedWithout()
    {
        Needs.WindowStorm();

        var run = storm.Run;
        Assert.Equal(0, run.Exit);
        Assert.Equal("150 black columns at the left and 150 at the right, 0 black pixels between, 900 by 450", run.Fitted);
        Assert.Equal("0 black columns at the left and 0 at the right, 0 black pixels between, 900 by 450", run.Stretched);
    }

    /// <summary>
    /// Under a picture drawn at half the window's size, the interface lays its nodes out at the
    /// window's own size, and a node kept over the picture covers the window it fills.
    /// </summary>
    [SkippableFact]
    public void TheInterfaceKeepsTheWindowsSizeUnderASmallPicture()
    {
        Needs.WindowStorm();

        var run = storm.Run;
        Assert.Equal(0, run.Exit);
        var at = run.Page.IndexOf("render.scale 50%: done");
        Assert.True(at >= 0, "the scale of half was not set");
        Assert.Equal(
            ["picture 450 by 225 over 0 0 900 450", "the interface's whole node 900 by 450 about 450 225", "the node over the picture 900 by 450 about 450 225"],
            run.Page.Skip(at + 1).Take(3));
    }

    /// <summary>
    /// The sharp filter equals the nearest one to the bit where the window is a whole number of
    /// times the picture.
    /// </summary>
    /// <remarks>
    /// The borderless window of 900 by 450 at a render scale of 50% draws a picture of 450 by 225,
    /// which fills it twice over. The picture is a noise image laid over the
    /// camera's interface, so neighboring picture pixels differ, and it is read as the noise only
    /// where Bevy's interface is drawn into the camera's picture before the picture is scaled.
    /// </remarks>
    [SkippableFact]
    public void TheSharpFilterEqualsTheNearestAtAWholeRatio()
    {
        Needs.WindowStorm();

        var run = storm.Run;
        Assert.Equal(0, run.Exit);
        Assert.Equal("sharp equals nearest at every pixel of 900 by 450", run.WholeRatio);
    }

    /// <summary>
    /// At one and a half the sharp filter fills the window, every window pixel inside a picture
    /// pixel takes it whole as the nearest filter does, and every one across an edge lies between
    /// the picture pixels round it.
    /// </summary>
    /// <remarks>
    /// A picture of 600 by 300, a size the game fixes, in the window of 900 by 450. Of every three
    /// window pixels across or down, the first and the last lie inside a picture pixel and the
    /// middle one spans the edge between two, whose colors are read from the nearest filter's
    /// picture at a window pixel inside each. The linear filter blends every window pixel and fails
    /// it.
    /// </remarks>
    [SkippableFact]
    public void TheSharpFilterFillsTheWindowAtOneAndAHalfWithEachEdgeInOnePixel()
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
        string Opened, List<string> Page, string[] Kept, string Next, string WholeRatio, string HalfRatio, string Fitted, string Stretched);

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
            File.WriteAllText(settings, "window.resolution 700x420\nwindow.vsync off\n");
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
            // The pictures read back, the nearest and the sharp filter's at a ratio of two and of
            // one and a half, and a 4 to 3 picture fitted and stretched.
            var (capture, into) = (default(Capture?), 0);
            var pictures = new CapturedImage?[6];
            var (whole, onPicture) = (default(Entity), default(Entity));
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
            string Pictured() => (Render.PictureSize, Render.PictureArea) is ({ } size, { } area)
                ? $"picture {size.Width} by {size.Height} over {area.X} {area.Y} {area.Width} {area.Height}"
                : "no picture";
            // A node's laid out size and its middle, in the window's pixels.
            static string Laid(EcsWorld ecs, Entity node, string what)
            {
                var size = ecs.Wrap<ComputedNodeRef>(node).Size;
                var middle = ecs.Wrap<UiGlobalTransformRef>(node).Item0Translation;
                return $"{what} {size.X} by {size.Y} about {middle.X} {middle.Y}";
            }

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

                        // Two nodes of the interface on no camera of their own, one over the whole
                        // window and one kept over the picture.
                        whole = Ui.SpawnNode(new UiSettings { Absolute = true, Width = Length.Percent(100f), Height = Length.Percent(100f) });
                        onPicture = Ui.SpawnNode(new UiSettings { Absolute = true, Width = Length.Px(10f), Height = Length.Px(10f) });
                        Ui.SetOnPicture(onPicture);
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
                            Read("window.resolution");
                            Line(world, "window.resolution 1000x450");
                            Line(world, "window.mode fullscreen");
                        }

                        // Borderless at a resolution under the screen's stands in its middle
                        // without a frame, and covers it at the screen's size, refuses one larger
                        // than the screen's, then stands in the middle again at one under it.
                        if (frame == 176)
                        {
                            page.Add(Placed("borderless"));
                            Line(world, "window.resolution 1920x1080");
                        }
                        if (frame == 180)
                        {
                            if (Window.Shown() is { Mode: WindowMode.BorderlessFullscreen, Scaled: var drawnScaled })
                                page.Add($"covering its screen, {(drawnScaled ? "scaled" : "not scaled")}");
                            Read("window.mode");
                            Line(world, "window.resolution 2560x1440");
                            Line(world, "window.resolution 900x450");
                        }
                        if (frame == 184)
                        {
                            page.Add(Placed("borderless"));
                            Line(world, "render.filter nearest");
                            Line(world, "render.scale 50");
                        }

                        // Each picture read back once it has reached the window, the nearest
                        // filter's at a ratio of two by the scale and of one and a half by a size
                        // the game fixes, then the sharp one's at each.
                        if (frame == 190)
                        {
                            Read("render.filter");
                            Read("render.scale");
                            page.Add(Pictured());
                            Begin(0);
                        }
                        if (frame == 196)
                        {
                            Render.Picture = Render.Picture with { Size = (600, 300) };
                            page.Add(Registry.Process.FindSetting("render.scale")!.Locked ?? "render.scale is not locked");
                            Read("render.scale");
                        }
                        if (frame == 202) Begin(1);
                        if (frame == 208) Line(world, "render.filter sharp");
                        if (frame == 214) Begin(2);
                        if (frame == 220)
                        {
                            Render.Picture = Render.Picture with { Size = null };
                            page.Add(Registry.Process.FindSetting("render.scale")!.Locked ?? "render.scale is not locked");
                        }
                        if (frame == 226)
                        {
                            Read("render.filter");
                            Begin(3);
                        }

                        // A 4 to 3 picture in the 2 to 1 window, fitted with its shape kept and
                        // then stretched, the node kept over the picture standing over it.
                        if (frame == 232)
                        {
                            Line(world, "render.scale 100%");
                            Line(world, "render.aspect 4:3");
                        }
                        if (frame == 238)
                        {
                            Read("render.aspect");
                            Read("render.fit");
                            page.Add(Pictured());
                            page.Add(Laid(ecs, onPicture, "the node over the picture"));
                            Begin(4);
                        }
                        if (frame == 244) Line(world, "render.fit stretch");
                        if (frame == 250)
                        {
                            Read("render.fit");
                            page.Add(Pictured());
                            Begin(5);
                        }

                        // The interface at the window's size under a picture drawn at half of it.
                        if (frame == 256)
                        {
                            Line(world, "render.aspect native");
                            Line(world, "render.fit fit");
                            Line(world, "render.scale 50%");
                        }
                        if (frame == 262)
                        {
                            page.Add(Pictured());
                            page.Add(Laid(ecs, whole, "the interface's whole node"));
                            page.Add(Laid(ecs, onPicture, "the node over the picture"));
                            Line(world, "render.scale 100%");
                        }
                        if (capture is { } asked && Render.TryReadCapture(asked, out var read))
                        {
                            pictures[into] = read;
                            capture = null;
                        }
                        if (frame == 268) Line(world, "window.mode windowed");

                        // A size of the system's own a frame on, as a window manager restores the
                        // one it saved as the window leaves its screen, which the size given back
                        // is set over.
                        if (frame == 270) Window.SetSize(1000, 300);
                        if (frame == 276)
                        {
                            Read("window.mode");
                            page.Add(Placed("windowed"));
                            Line(world, "window.resizable off");
                        }
                        if (frame == 280)
                        {
                            Read("window.resizable");
                            Line(world, "window.resizable on");
                            Line(world, "window.vsync on");
                        }
                        if (frame == 284)
                        {
                            Read("window.vsync");
                            Read("window.monitor");
                            Line(world, "window.monitor 0");
                            Line(world, $"window.monitor {monitors}");
                        }

                        if (frame == 348) App.RequestExit();
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
                + $"{(next.Monitor is { } monitor ? $"monitor {monitor}" : "no monitor")}, {(next.Vsync ? "vsync" : "no vsync")}, "
                + $"{PictureSettings.Current}";
            return new Result(exit, picture, monitors, first, name, modes, refused, opened, page, kept, said,
                Matched(pictures[3], pictures[0]), Blended(pictures[2], pictures[1]), Bars(pictures[4]), Bars(pictures[5]));
        }

        // The black columns of a picture at each side, and whether the rest shows the noise.
        private static string Bars(CapturedImage? picture)
        {
            if (picture is null) return "a picture was not read";
            static bool Black((byte R, byte G, byte B, byte A) seen) => seen.R < 8 && seen.G < 8 && seen.B < 8;
            bool Column(uint x)
            {
                for (var y = 0u; y < picture.Height; y++)
                    if (!Black(picture.At(x, y))) return false;
                return true;
            }
            var (left, right) = (0u, 0u);
            while (left < picture.Width && Column(left)) left++;
            while (right < picture.Width - left && Column(picture.Width - 1 - right)) right++;
            var black = 0;
            for (var y = 0u; y < picture.Height; y++)
                for (var x = left; x < picture.Width - right; x++)
                    if (Black(picture.At(x, y))) black++;
            return $"{left} black columns at the left and {right} at the right, {black} black pixels between, {picture.Width} by {picture.Height}";
        }

        // Whether a scaled picture shows the noise, its picture pixels nearly all of different
        // colors, where a picture with the interface left out holds the camera's clear color alone.
        private static string? NotTheNoise(CapturedImage picture)
        {
            var colors = new HashSet<(byte, byte, byte, byte)>();
            for (var y = 0u; y < picture.Height; y++)
                for (var x = 0u; x < picture.Width; x++)
                    colors.Add(picture.At(x, y));
            return colors.Count > 450 * 225 / 2 ? null : $"the nearest filter's picture holds {colors.Count} colors, not the noise";
        }

        // The sharp filter's picture at a whole ratio against the nearest one's, pixel by pixel.
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

        // The sharp filter's picture at one and a half against the nearest one's. Of every three
        // window pixels along an axis the first and the last lie inside the first and the second
        // of two picture pixels and the middle one spans the edge between them, so a picture pixel
        // is read from the nearest filter's window pixel inside it, the first of three for an
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
