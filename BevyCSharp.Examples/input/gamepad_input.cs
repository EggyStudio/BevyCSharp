// Bevy's gamepad_input example, examples/input/gamepad_input.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Shows handling of gamepad input, the south button, the right trigger and the left stick of
// every pad connected.
internal static class GamepadInput
{
    public static void Build(App app) => app.Update(ctx =>
    {
        foreach (var pad in ctx.Input.Gamepads)
        {
            if (pad.Pressed(GamepadButton.South)) Console.WriteLine($"{pad.Entity} just pressed South");
            else if (pad.Released(GamepadButton.South)) Console.WriteLine($"{pad.Entity} just released South");

            var rightTrigger = pad.Axis(GamepadAxis.RightTrigger);
            if (MathF.Abs(rightTrigger) > 0.01f) Console.WriteLine(FormattableString.Invariant($"{pad.Entity} RightTrigger2 value is {rightTrigger}"));

            var leftStickX = pad.Axis(GamepadAxis.LeftX);
            if (MathF.Abs(leftStickX) > 0.01f) Console.WriteLine(FormattableString.Invariant($"{pad.Entity} LeftStickX value is {leftStickX}"));
        }
    }, "gamepad_input.Gamepad");
}
