// Bevy's touch_input_events example, examples/input/touch_input_events.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Prints out all touch inputs, each finger starting, moving, ending or canceled in the order it
// came, which a machine with no touch screen never sends.
internal static class TouchInputEvents
{
    public static void Build(App app) => app.Update(ctx =>
    {
        // Bevy's TouchInput, named whole beside this namespace's touch_input example of that name.
        foreach (var touchInput in ctx.Read<Bevy.TouchInput>()) Console.WriteLine(touchInput);
    }, "touch_input_events.TouchEventSystem");
}
