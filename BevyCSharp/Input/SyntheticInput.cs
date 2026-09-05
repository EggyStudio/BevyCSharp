using Bevy.Interop;

namespace Bevy;

/// <summary>What a pointer is doing, for <see cref="SyntheticInput"/>.</summary>
public enum PointerAction
{
    /// <summary>Moved somewhere, with no button changing.</summary>
    Move,

    /// <summary>A button went down.</summary>
    Press,

    /// <summary>A button came up.</summary>
    Release,
}

/// <summary>
/// Puts input into the window as though a hand had done it.
/// </summary>
/// <remarks>
/// <para>
/// For tests and tools. What a click does is not one thing: the picking backend raycasts, a widget
/// decides it was clicked, a camera reads a button, and an editor reads all three. Calling the
/// method a click would have called tests the method and not the path to it, and the path is where
/// the interesting failures are — a handle that cannot be grabbed, a menu that opens once, a
/// selection that clears itself.
/// </para>
/// <para>
/// It writes the window's own messages, which is where a real pointer's report begins, so
/// everything downstream behaves exactly as it would. It cannot move the operating system's
/// cursor, and does not try to: what it drives is the application, not the desktop.
/// </para>
/// </remarks>
public static class SyntheticInput
{
    /// <summary>Moves the pointer to a point in the window, in logical pixels.</summary>
    /// <exception cref="BevyNativeException">There is no window.</exception>
    public static void MoveTo(float x, float y) => Send(x, y, PointerAction.Move);

    /// <summary>Presses a button where the pointer is put.</summary>
    /// <exception cref="BevyNativeException">There is no window.</exception>
    public static void Press(float x, float y, MouseButton button = MouseButton.Left) =>
        Send(x, y, PointerAction.Press, button);

    /// <summary>Releases a button where the pointer is put.</summary>
    /// <exception cref="BevyNativeException">There is no window.</exception>
    public static void Release(float x, float y, MouseButton button = MouseButton.Left) =>
        Send(x, y, PointerAction.Release, button);

    /// <summary>Moves, presses or releases the pointer.</summary>
    /// <exception cref="BevyNativeException">There is no window.</exception>
    public static void Send(
        float x, float y, PointerAction action, MouseButton button = MouseButton.Left) =>
        Native.Check(
            Native.bcs_input_pointer(x, y, (int)action, (int)button),
            $"sending a pointer {action} at {x},{y}");
}
