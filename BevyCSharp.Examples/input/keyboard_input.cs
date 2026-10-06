// Bevy's keyboard_input example, examples/input/keyboard_input.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Demonstrates handling a key press and release, by where the key is and by what it types.
internal static class KeyboardInputExample
{
    public static void Build(App app) => app.Update(KeyboardInputSystem, "keyboard_input.KeyboardInputSystem");

    // This system responds to certain key presses.
    private static void KeyboardInputSystem(BehaviorContext ctx)
    {
        var input = ctx.Input;

        // A physical key is where the key is, the same whatever the keyboard's layout, as the
        // codes at https://w3c.github.io/uievents-code/#code-value-tables place it.
        if (input.KeyDown(Key.A)) Console.WriteLine("'A' currently pressed");
        if (input.KeyPressed(Key.A)) Console.WriteLine("'A' just pressed");
        if (input.KeyReleased(Key.A)) Console.WriteLine("'A' just released");

        // A logical key is named by what the key types, wherever the layout puts it, which suits a
        // symbol that means something, '?' for a help menu or '+' and '-' for zoom.
        var key = LogicalKey.Character("?");
        if (input.KeyDown(key)) Console.WriteLine("'?' currently pressed");
        if (input.KeyPressed(key)) Console.WriteLine("'?' just pressed");
        if (input.KeyReleased(key)) Console.WriteLine("'?' just released");
    }

    // A held for two frames, then the key that types '?' tapped.
    public static void Drive(App app) => app.Script(
        (2, () => SyntheticInput.Press(Key.A, "a")),
        (4, () => SyntheticInput.Lift(Key.A)),
        (6, () => SyntheticInput.Tap(Key.Slash, "?")));
}
