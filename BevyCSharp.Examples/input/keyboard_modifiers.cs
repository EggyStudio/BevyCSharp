using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Demonstrates using key modifiers, Ctrl and Shift held with A.
internal static class KeyboardModifiers
{
    public static void Build(App app) => app.Update(ctx =>
    {
        var input = ctx.Input;
        var shift = input.AnyKeyDown([Key.ShiftLeft, Key.ShiftRight]);
        var ctrl = input.AnyKeyDown([Key.ControlLeft, Key.ControlRight]);
        if (ctrl && shift && input.KeyPressed(Key.A)) Console.WriteLine("Just pressed Ctrl + Shift + A!");
    }, "keyboard_modifiers.KeyboardInput");

    public static void Drive(App app) => app.Script(
        (2, () => SyntheticInput.Press(Key.ControlLeft)),
        (3, () => SyntheticInput.Press(Key.ShiftRight)),
        (4, () => SyntheticInput.Tap(Key.A, "A")),
        (6, () => { SyntheticInput.Lift(Key.ShiftRight); SyntheticInput.Lift(Key.ControlLeft); }),
        (8, () => SyntheticInput.Tap(Key.A, "a")));
}
