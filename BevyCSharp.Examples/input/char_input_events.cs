// Bevy's char_input_events example, examples/input/char_input_events.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

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
}
