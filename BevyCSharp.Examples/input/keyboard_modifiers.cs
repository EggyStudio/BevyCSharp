// Bevy's keyboard_modifiers example, examples/input/keyboard_modifiers.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

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
}
