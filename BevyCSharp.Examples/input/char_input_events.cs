using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Prints the characters typed on the keyboard as they arrive. Bevy prints the keyboard event each
// came in, and here the characters are what the frame's typed text holds.
internal static class CharInputEvents
{
    public static void Build(App app) => app.Update(ctx =>
    {
        foreach (var character in ctx.Input.Text) Console.WriteLine($"'{character}'");
    }, "char_input_events.PrintCharEvents");

    public static void Drive(App app) => app.Script(
        (2, () => SyntheticInput.Tap(Key.H, "h")),
        (4, () => SyntheticInput.Tap(Key.I, "i")),
        (6, () => SyntheticInput.Tap(Key.Digit1, "!")));
}
