using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers Escape going to what is open first, the console and the settings panel taking a press of
/// it from the program, and the overlay, which takes no key, leaving it the program's.
/// </summary>
/// <remarks>
/// Each panel is opened by a call, as its key opens it, and the program's reads of the key, its
/// logical key and its messages are counted on every frame after a tap, so a press kept from the
/// program is kept from each way of reading it, and its release with it.
/// </remarks>
[Collection("engine")]
public sealed class EscapeTests : IDisposable
{
    public void Dispose()
    {
        ConsoleLog.Model.Open = false;
        Debug.Panel.Open = false;
        Debug.Overlay.Shown = false;
    }

    [SkippableTheory]
    [InlineData("console", false)]
    [InlineData("settings panel", false)]
    [InlineData("overlay", true)]
    [InlineData("nothing", true)]
    public void EscapeIsTheProgramsOnlyWhereNothingOpenTakesIt(string open, bool programs)
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 10);
        var frame = 0;
        var seen = new List<string>();

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (frame == 2) Open(open);
            if (ctx.Input.KeyPressed(Key.Escape)) seen.Add("pressed");
            if (ctx.Input.KeyReleased(Key.Escape)) seen.Add("released");
            if (ctx.Input.KeyPressed(LogicalKey.Escape)) seen.Add("logical");
            foreach (var message in ctx.Read<KeyboardInput>())
                if (message.KeyCode == Key.Escape) seen.Add($"message {message.State}");
            if (frame == 3) SyntheticInput.Tap(Key.Escape);
        });
        harness.Run();

        string[] all = ["logical", "message Pressed", "message Released", "pressed", "released"];
        Assert.Equal(programs ? all : [], seen.Order());
    }

    private static void Open(string open)
    {
        switch (open)
        {
            case "console": ConsoleLog.Model.Open = true; break;
            case "settings panel": Debug.Panel.Open = true; break;
            case "overlay": Debug.Overlay.Shown = true; break;
        }
    }
}
