using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what the window can be asked, its size, style, position, frame, cursor and mode, and the
/// monitors it can fill, and that a run without a window says so rather than pretending.
/// </summary>
/// <remarks>
/// The window's half of <see cref="RenderControlTests"/>, kept apart for the length of one file.
/// </remarks>
[Collection("engine")]
public sealed class WindowControlTests
{
    [Fact]
    public void WindowCallsReportThatAHeadlessRunHasNoWindow()
    {
        // The whole point of failing loudly. A behavior that locks the cursor should say why it
        // could not rather than leave a first-person camera mysteriously dead.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            var title = Assert.Throws<BevyNativeException>(() => Window.SetTitle("test"));
            var cursor = Assert.Throws<BevyNativeException>(() => Window.SetCursor(CursorGrab.Locked, false));
            var size = Assert.Throws<BevyNativeException>(() => Window.Size());

            // Unsupported on a headless build, absent on a render build running windowless.
            foreach (var status in new[] { title.Status, cursor.Status, size.Status })
                Assert.True(
                    status is NativeStatus.Unsupported or NativeStatus.NotPresent,
                    $"unexpected status {status}");
        });

        harness.Run();
    }

    /// <summary>
    /// An offscreen run answers the window's size with its image's from the first startup system
    /// on, since a game lays out what it spawns at startup by that size.
    /// </summary>
    [SkippableFact]
    public void AnOffscreenRunHasItsSizeAtStartup()
    {
        Needs.Renderer();

        (uint Width, uint Height)? size = null;
        var run = new PictureRun { Width = 120, Height = 80, Scene = _ => size = Window.Size() };
        run.Wait(2).Go();

        Assert.Equal((120u, 80u), size);
    }

    [Fact]
    public void MonitorsAreCountedWithoutThrowing()
    {
        // A windowless run reports none rather than failing, so a settings screen can ask without
        // guarding first. With a window this is however many the platform sees.
        using var harness = new EngineHarness(frames: 2);
        var count = -1;

        harness.OnContext(Stage.Update, _ => count = Window.MonitorCount());
        harness.Run();

        Assert.True(count >= 0, "monitor count came back negative");

        if (count == 0) return;

        // Anything reported has to be describable, and past the end has to be refused.
        using var second = new EngineHarness(frames: 2);
        second.OnContext(Stage.Update, _ =>
        {
            var first = Window.Monitor(0);
            Assert.True(first.Width > 0 && first.Height > 0);
            Assert.Throws<BevyNativeException>(() => Window.Monitor(Window.MonitorCount()));

            // A name is optional, so the only thing guaranteed is that asking is safe and that
            // past the end is refused the same way the rest of the monitor surface refuses it.
            Assert.NotNull(Window.MonitorName(0));
            Assert.Throws<BevyNativeException>(() => Window.MonitorName(Window.MonitorCount()));

            // The video modes are the same shape of answer. A platform may report none, so what
            // is guaranteed is that every mode reported describes a screen somebody could see.
            var modes = Window.MonitorModes(0);
            Assert.All(modes, mode => Assert.True(mode.Width > 0 && mode.Height > 0));

            Assert.Throws<BevyNativeException>(
                () => Window.MonitorModes(Window.MonitorCount()));

            // And a mode past the end of the list is refused rather than taken as the last one.
            Assert.Throws<BevyNativeException>(
                () => Window.SetVideoMode(0, modes.Length));
        });

        second.Run();
    }

    [Fact]
    public void MonitorNamesAreRefusedWithoutAWindow()
    {
        // Windowless has no monitors, so every index is past the end.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            if (Window.MonitorCount() > 0) return;

            Assert.Throws<BevyNativeException>(() => Window.MonitorName(0));
        });

        harness.Run();
    }

    [Fact]
    public void WindowStyleAndPositionReportTheirAbsence()
    {
        // Same contract as the rest of the window surface, where a windowless run says so rather
        // than pretending, so a behavior that arranges the window fails visibly in a test.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            var position = Assert.Throws<BevyNativeException>(() => Window.SetPosition(100, 100));
            var style = Assert.Throws<BevyNativeException>(
                () => Window.SetStyle(decorations: false, alwaysOnTop: true));

            foreach (var status in new[] { position.Status, style.Status })
                Assert.True(
                    status is NativeStatus.Unsupported or NativeStatus.NotPresent,
                    $"unexpected status {status}");
        });

        harness.Run();
    }

    [Fact]
    public void AWindowsOwnFrameReportsItsAbsence()
    {
        // What an app drawing its own title bar calls, and the same contract as the rest, in which
        // a run with no window says so rather than minimizing nothing.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            var calls = new Action[]
            {
                Window.Minimize,
                () => Window.SetMaximized(true),
                Window.StartDragMove,
                () => Window.StartDragResize(WindowEdge.BottomRight),
                () => Window.SetCursorShape(CursorShape.Text),
            };

            foreach (var call in calls)
            {
                var refused = Assert.Throws<BevyNativeException>(call);

                Assert.True(
                    refused.Status is NativeStatus.Unsupported or NativeStatus.NotPresent,
                    $"unexpected status {refused.Status}");
            }
        });

        harness.Run();
    }

    [Fact]
    public void CursorAndModeRejectValuesTheyDoNotUnderstand()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            // Straight at the bridge, because the managed enums cannot express these.
            Assert.Equal(NativeStatus.NullArgument, WorstCase(Native.bcs_window_set_mode(Native.PrimaryWindow, 9)));
            Assert.Equal(NativeStatus.NullArgument, WorstCase(Native.bcs_window_set_cursor(Native.PrimaryWindow, 9, 1)));
        });

        harness.Run();
        return;

        // A headless build refuses before it looks at the argument, which is equally correct.
        static int WorstCase(int status) =>
            status == NativeStatus.Unsupported ? NativeStatus.NullArgument : status;
    }
}
