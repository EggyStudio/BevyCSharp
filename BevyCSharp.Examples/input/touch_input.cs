// Bevy's touch_input example, examples/input/touch_input.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Prints each touch on the screen as it starts, moves and ends, and every one that is active.
internal static class TouchInput
{
    public static void Build(App app) => app.Update(ctx =>
    {
        var touches = ctx.Input.Touches;
        foreach (var touch in touches)
        {
            if (touch.Phase == TouchPhase.Started) Console.WriteLine(FormattableString.Invariant($"just pressed touch with id: {touch.Id}, at: [{touch.X}, {touch.Y}]"));
            if (touch.Phase == TouchPhase.Ended) Console.WriteLine(FormattableString.Invariant($"just released touch with id: {touch.Id}, at: [{touch.X}, {touch.Y}]"));
        }

        foreach (var touch in touches)
        {
            if (touch.Phase == TouchPhase.Ended) continue;
            Console.WriteLine($"active touch: {touch}");
            Console.WriteLine($"  just_pressed: {(touch.Phase == TouchPhase.Started ? "true" : "false")}");
        }
    }, "touch_input.Touch");
}
