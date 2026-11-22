using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the console and the settings panel holding the keys and the pads from the program while
/// either is open, and the overlay, which only shows, holding nothing.
/// </summary>
/// <remarks>
/// <para>
/// A key and a stick are pushed before the panel opens and held through it, another key, a button
/// and the other stick are pushed while it is open and held as it closes, every one is let go a
/// few frames later, and the key and the button are pressed once more. Each frame the program's
/// reads of the key's state, the pad's and their messages are written down under the part of the
/// run they came in, so a read the hold should have kept shows where it slipped through.
/// </para>
/// <para>
/// A held panel leaves the program the press it read before the panel opened, and that key's
/// release as a message, so a program building its state from the messages holds nothing down that
/// the hand let go of, and it leaves the stick's value as it comes back to rest. What went down
/// while the panel was open stays unseen until it is let go, its release with it, and the press
/// after is the program's.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class HeldInputTests : IDisposable
{
    public void Dispose()
    {
        ConsoleLog.Model.Open = false;
        Debug.Panel.Open = false;
        Debug.Overlay.Shown = false;
    }

    [SkippableTheory]
    [InlineData("console")]
    [InlineData("settings panel")]
    public void APanelThatTakesInputHoldsTheKeysAndThePadFromTheProgram(string open)
    {
        Assert.Equal(
            [
                "before: W down", "before: W message Pressed", "before: W pressed", "before: stick message pushed", "before: stick pushed",
                "let go: W message Released", "let go: stick message near rest", "let go: stick near rest",
                "again: D down", "again: D message Pressed", "again: D pressed",
                "again: South down", "again: South message Pressed", "again: South pressed", "again: stick near rest",
            ],
            Run(open));
    }

    // The overlay's run reads as a run with nothing open does, the press, the button and the stick
    // pushed while it is shown among what the program reads.
    [SkippableFact]
    public void TheOverlayHoldsNothing()
    {
        var shown = Run("overlay");

        Assert.Equal(Run("nothing"), shown);
        Assert.Contains("open: D pressed", shown);
        Assert.Contains("open: South pressed", shown);
        Assert.Contains("open: other stick", shown);
    }

    // What the program read in each part of a run with the panel named open from frame 9 to 16,
    // or nothing for none, in the order the parts come and by name within each.
    private static List<string> Run(string open)
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 34);
        var pad = Entity.None;
        var frame = 0;
        var seen = new List<(int Part, string What)>();
        string[] parts = ["before", "open", "held", "let go", "again"];

        harness.OnContext(Stage.Startup, _ => pad = SyntheticInput.ConnectGamepad("Held pad"));
        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            var part = frame switch { <= 8 => 0, <= 16 => 1, <= 22 => 2, <= 28 => 3, _ => 4 };
            void Saw(string what) => seen.Add((part, what));

            foreach (var key in (Key[])[Key.W, Key.D])
            {
                if (ctx.Input.KeyDown(key)) Saw($"{key} down");
                if (ctx.Input.KeyPressed(key)) Saw($"{key} pressed");
                if (ctx.Input.KeyReleased(key)) Saw($"{key} released");
            }

            foreach (var message in ctx.Read<KeyboardInput>())
                if (message.KeyCode is Key.W or Key.D) Saw($"{message.KeyCode} message {message.State}");

            if (ctx.Input.Gamepads.FirstOrDefault(known => known.Entity == pad) is { } now)
            {
                if (now.Down(GamepadButton.South)) Saw("South down");
                if (now.Pressed(GamepadButton.South)) Saw("South pressed");
                if (now.Released(GamepadButton.South)) Saw("South released");
                if (Stick(now.LeftStick.X) is { } stick) Saw($"stick {stick}");
                if (now.RightStick.Y != 0f) Saw("other stick");
            }

            foreach (var message in ctx.Read<GamepadButtonStateChangedEvent>())
                if (message.Button == GamepadButton.South) Saw($"South message {message.State}");
            foreach (var message in ctx.Read<GamepadAxisChangedEvent>())
            {
                if (message.Axis == GamepadAxis.LeftX && Stick(message.Value) is { } stick) Saw($"stick message {stick}");
                if (message.Axis == GamepadAxis.RightY && message.Value != 0f) Saw("other stick message");
            }

            switch (frame)
            {
                case 2:
                    SyntheticInput.Press(Key.W, "w");
                    SyntheticInput.SetGamepadAxis(pad, GamepadAxis.LeftX, 0.8f);
                    break;
                case 8:
                    Open(open, true);
                    break;
                case 11:
                    SyntheticInput.Press(Key.D, "d");
                    SyntheticInput.SetGamepadButton(pad, GamepadButton.South);
                    SyntheticInput.SetGamepadAxis(pad, GamepadAxis.RightY, 0.9f);
                    break;
                case 16:
                    Open(open, false);
                    break;
                case 22:
                    SyntheticInput.Lift(Key.W);
                    SyntheticInput.Lift(Key.D);
                    SyntheticInput.SetGamepadButton(pad, GamepadButton.South, 0f);
                    SyntheticInput.SetGamepadAxis(pad, GamepadAxis.LeftX, 0.2f);
                    SyntheticInput.SetGamepadAxis(pad, GamepadAxis.RightY, 0f);
                    break;
                case 28:
                    SyntheticInput.Press(Key.D, "d");
                    SyntheticInput.SetGamepadButton(pad, GamepadButton.South);
                    break;
            }
        });
        harness.Run();

        return [.. seen.Distinct().OrderBy(read => read.Part).ThenBy(read => read.What, StringComparer.Ordinal).Select(read => $"{parts[read.Part]}: {read.What}")];
    }

    // A stick's value as pushed or near rest, the hold's line between them a quarter of its travel,
    // or nothing at rest.
    private static string? Stick(float value) => MathF.Abs(value) switch
    {
        0f => null,
        <= 0.25f => "near rest",
        _ => "pushed",
    };

    private static void Open(string open, bool shown)
    {
        switch (open)
        {
            case "console": ConsoleLog.Model.Open = shown; break;
            case "settings panel": Debug.Panel.Open = shown; break;
            case "overlay": Debug.Overlay.Shown = shown; break;
        }
    }
}
