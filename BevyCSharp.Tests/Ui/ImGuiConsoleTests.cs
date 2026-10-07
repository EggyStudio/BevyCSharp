using Bevy;
using ImGuiNET;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The console a game draws with one call, opened by the key under Escape over the top of the
/// window, running what is typed into it, and closed by the same key.
/// </summary>
/// <remarks>
/// Driven offscreen with the interface asked for, the key pressed where a real one starts
/// (<see cref="SyntheticInput.Tap"/>) and the command typed into the interface's own queue, as the
/// editor's tests of a field type. In the engine collection, since it runs an app and the
/// console's ring is the process's.
/// </remarks>
[Collection("engine")]
public sealed class ImGuiConsoleTests : IDisposable
{
    public void Dispose()
    {
        ImGuiConsole.IsOpen = false;
        ImGuiConsole.View.Search = string.Empty;
    }

    [SkippableFact]
    public void TheKeyOpensItOverTheTopRunsWhatIsTypedAndClosesIt()
    {
        Needs.Editor();
        ConsoleLog.Write(LogLevel.Error, "a line the console shows");

        var run = new PictureRun
        {
            Width = 320,
            Height = 180,
            Configure = config => config.Gui = true,
            Scene = ecs =>
            {
                ImGuiRuntime.Start();
                PictureRun.Camera(ecs);
            },
            EachFrame = world =>
            {
                var ctx = new BehaviorContext(world);
                ImGuiRuntime.Begin(ctx);
                ImGuiConsole.Draw(ctx);
                ImGuiRuntime.End();
            },
        };

        // Frames enough for the camera to have drawn before the first picture, which is the image's
        // own color until it has.
        run.Wait(15)
            .Capture("before")
            .Do("pressing the key", _ => SyntheticInput.Tap(Key.Backquote, "`"))
            .Until("opening", _ => ImGuiConsole.IsOpen)
            .Wait(3)
            .Capture("open")
            .Do("typing", _ => SyntheticInput.Key(ImGuiKey.E, "echo typed into the console"))
            .Wait(2)
            .Do("entering it", _ => SyntheticInput.Key(ImGuiKey.Enter))
            .Wait(3)
            .Do("pressing the key again", _ => SyntheticInput.Tap(Key.Backquote, "`"))
            .Until("closing", _ => !ImGuiConsole.IsOpen)
            .Wait(3)
            .Capture("closed")
            .Go();

        var lines = ConsoleLog.All();
        Assert.Contains(lines, line => line is { Level: LogLevel.Echo, Text: "> echo typed into the console" });
        Assert.Contains(lines, line => line is { Level: LogLevel.Echo, Text: "typed into the console" });

        // Drawn over the top of the picture and nowhere else, the share of its height it takes, and
        // gone again once closed.
        var before = run.Picture("before");
        var open = run.Picture("open");
        var closed = run.Picture("closed");
        Assert.True(Differing(before, open, 0, 60) > 200, "the top of the picture is the same with the console open");
        Assert.Equal(0, Differing(before, open, 90, 180));
        Assert.Equal(0, Differing(before, closed, 0, 180));
    }

    [Fact]
    public void WithNoInterfaceRunningItDrawsNothing()
    {
        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Update, ImGuiConsole.Draw);
        harness.Run();

        Assert.False(ImGuiConsole.IsOpen);
    }

    /// <summary>How many pixels differ between two pictures in a band of rows.</summary>
    private static int Differing(CapturedImage first, CapturedImage second, uint from, uint to)
    {
        var count = 0;
        for (var y = from; y < Math.Min(to, first.Height); y++)
            for (var x = 0u; x < first.Width; x++)
                if (first.At(x, y) != second.At(x, y)) count++;

        return count;
    }
}
