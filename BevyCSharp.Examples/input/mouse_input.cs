// Bevy's mouse_input example, examples/input/mouse_input.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Prints mouse button presses and releases, and how far the mouse moved and scrolled each frame.
// A pointer is pretended only where there is a window or an interface to give it to, so its
// capture, run headless, has nothing to print.
internal static class MouseInput
{
    public static void Build(App app)
    {
        app.Update(ctx =>
        {
            var input = ctx.Input;
            if (input.MouseDown(MouseButton.Left)) Console.WriteLine("left mouse currently pressed");
            if (input.MousePressed(MouseButton.Left)) Console.WriteLine("left mouse just pressed");
            if (input.MouseReleased(MouseButton.Left)) Console.WriteLine("left mouse just released");
        }, "mouse_input.MouseClick");

        app.Update(ctx =>
        {
            var input = ctx.Input;
            if (input.MouseDelta != (0f, 0f)) Console.WriteLine(FormattableString.Invariant($"mouse moved ({input.MouseDeltaX}, {input.MouseDeltaY})"));
            if (input.WheelX != 0f || input.WheelY != 0f) Console.WriteLine(FormattableString.Invariant($"mouse scrolled ({input.WheelX}, {input.WheelY})"));
        }, "mouse_input.MouseMove");
    }
}
