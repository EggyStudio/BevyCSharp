// Bevy's mouse_input_events example, examples/input/mouse_input_events.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Prints mouse events, the buttons, the motion, the cursor moving, the wheel and a touchpad's
// pinch, rotation and double tap, each kind in the order it came.
internal static class MouseInputEvents
{
    public static void Build(App app) => app.Update(ctx =>
    {
        foreach (var mouseButtonInput in ctx.Read<MouseButtonInput>()) Console.WriteLine(mouseButtonInput);
        foreach (var mouseMotion in ctx.Read<MouseMotion>()) Console.WriteLine(mouseMotion);
        foreach (var cursorMoved in ctx.Read<CursorMoved>()) Console.WriteLine(cursorMoved);
        foreach (var mouseWheel in ctx.Read<MouseWheel>()) Console.WriteLine(mouseWheel);
        foreach (var pinchGesture in ctx.Read<PinchGesture>()) Console.WriteLine(pinchGesture);
        foreach (var rotationGesture in ctx.Read<RotationGesture>()) Console.WriteLine(rotationGesture);
        foreach (var doubleTapGesture in ctx.Read<DoubleTapGesture>()) Console.WriteLine(doubleTapGesture);
    }, "mouse_input_events.PrintMouseEventsSystem");

    // The wheel alone, forward and to the side, since a capture runs with no window and nothing
    // drawn for a pretended button to press on, which refuses it.
    public static void Drive(App app) => app.Script(
        (2, () => SyntheticInput.Wheel(1f)),
        (4, () => SyntheticInput.Wheel(0f, 1f)));
}
