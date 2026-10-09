// Bevy's keyboard_input_events example, examples/input/keyboard_input_events.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Prints out all keyboard events, each key going down and coming up in the order it came, with
// the key it is, what it reads as and what it typed. Bevy prints each with Rust's debug form and
// here each is printed as C# writes the record.
internal static class KeyboardInputEvents
{
    public static void Build(App app) => app.Update(ctx =>
    {
        foreach (var keyboardInput in ctx.Read<KeyboardInput>()) Console.WriteLine(keyboardInput);
    }, "keyboard_input_events.PrintKeyboardEventSystem");

    public static void Drive(App app) => app.Script(
        (2, () => SyntheticInput.Tap(Key.H, "h")),
        (4, () => SyntheticInput.Tap(Key.ShiftLeft)),
        (6, () => SyntheticInput.Tap(Key.Enter)));
}
