using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Demonstrates locking and hiding the mouse cursor, a left click taking it and Escape giving it
// back, in a window that draws nothing.
internal static class MouseGrab
{
    public static void Build(App app) => app.Update(ctx =>
    {
        if (ctx.Input.MousePressed(MouseButton.Left)) Window.SetCursor(CursorGrab.Locked, visible: false);
        if (ctx.Input.KeyPressed(Key.Escape)) Window.SetCursor(CursorGrab.None, visible: true);
    }, "mouse_grab.GrabMouse");
}
