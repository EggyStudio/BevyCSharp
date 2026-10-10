using Bevy;

namespace BevyCSharp.Examples;

/// <summary>
/// The input given to an example that waits for it, a few keys, buttons or turns of the wheel at set
/// frames, added when it is run with <c>--drive</c> as a capture runs it, so one that prints what it
/// is given has something to print.
/// </summary>
/// <remarks>
/// Kept here beside the catalog rather than in the examples, since the input is the capture's, a
/// person's hands pretended, and no part of the example a reader copies into a game of their own.
/// </remarks>
internal static class Drives
{
    // A key held over a few frames and another over them in part, for a capture to print.
    public static void ComponentHooks(App app) => app.Script(
        (2, () => SyntheticInput.Press(Key.A, "a")),
        (4, () => SyntheticInput.Press(Key.B, "b")),
        (6, () => SyntheticInput.Lift(Key.A)),
        (8, () => SyntheticInput.Lift(Key.B)));

    // Three characters typed, h, i and an exclamation mark.
    public static void CharInputEvents(App app) => app.Script(
        (2, () => SyntheticInput.Tap(Key.H, "h")),
        (4, () => SyntheticInput.Tap(Key.I, "i")),
        (6, () => SyntheticInput.Tap(Key.Digit1, "!")));

    // A pad connected, and each of the buttons that rumble pressed and let go in turn.
    public static void GamepadRumble(App app)
    {
        var pad = Entity.None;
        var step = 2;
        var script = new List<(int, Action)> { (step, () => pad = SyntheticInput.ConnectGamepad()) };
        foreach (var button in new[] { GamepadButton.North, GamepadButton.East, GamepadButton.South, GamepadButton.West, GamepadButton.Start })
        {
            script.Add((step += 2, () => SyntheticInput.SetGamepadButton(pad, button)));
            script.Add((step += 2, () => SyntheticInput.SetGamepadButton(pad, button, 0f)));
        }

        app.Script([.. script]);
    }

    // A with Control and Shift held, then A alone.
    public static void KeyboardModifiers(App app) => app.Script(
        (2, () => SyntheticInput.Press(Key.ControlLeft)),
        (3, () => SyntheticInput.Press(Key.ShiftRight)),
        (4, () => SyntheticInput.Tap(Key.A, "A")),
        (6, () => { SyntheticInput.Lift(Key.ShiftRight); SyntheticInput.Lift(Key.ControlLeft); }),
        (8, () => SyntheticInput.Tap(Key.A, "a")));

    // The wheel alone, forward and to the side, since a capture runs with no window and nothing
    // drawn for a pretended button to press on, which refuses it.
    public static void MouseInputEvents(App app) => app.Script(
        (2, () => SyntheticInput.Wheel(1f)),
        (4, () => SyntheticInput.Wheel(0f, 1f)));

    // A pad connected, its south button pressed and let go, and its left stick pushed right halfway
    // and let go.
    public static void GamepadInput(App app)
    {
        var pad = Entity.None;
        app.Script(
            (2, () => pad = SyntheticInput.ConnectGamepad()),
            (4, () => SyntheticInput.SetGamepadButton(pad, GamepadButton.South)),
            (6, () => SyntheticInput.SetGamepadButton(pad, GamepadButton.South, 0f)),
            (8, () => SyntheticInput.SetGamepadAxis(pad, GamepadAxis.LeftX, 0.5f)),
            (10, () => SyntheticInput.SetGamepadAxis(pad, GamepadAxis.LeftX, 0f)));
    }

    // A held for two frames, then the key that types '?' tapped.
    public static void KeyboardInput(App app) => app.Script(
        (2, () => SyntheticInput.Press(Key.A, "a")),
        (4, () => SyntheticInput.Lift(Key.A)),
        (6, () => SyntheticInput.Tap(Key.Slash, "?")));

    // A pad connected, its south button pressed and let go, and its left stick pushed right halfway.
    public static void GamepadInputEvents(App app)
    {
        var pad = Entity.None;
        app.Script(
            (2, () => pad = SyntheticInput.ConnectGamepad()),
            (4, () => SyntheticInput.SetGamepadButton(pad, GamepadButton.South)),
            (6, () => SyntheticInput.SetGamepadButton(pad, GamepadButton.South, 0f)),
            (8, () => SyntheticInput.SetGamepadAxis(pad, GamepadAxis.LeftX, 0.5f)));
    }

    // An h typed, Shift tapped and Enter tapped.
    public static void KeyboardInputEvents(App app) => app.Script(
        (2, () => SyntheticInput.Tap(Key.H, "h")),
        (4, () => SyntheticInput.Tap(Key.ShiftLeft)),
        (6, () => SyntheticInput.Tap(Key.Enter)));
}
