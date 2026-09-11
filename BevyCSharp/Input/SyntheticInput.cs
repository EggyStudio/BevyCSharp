using Bevy.Interop;
using ImGuiNET;

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
/// Puts input into the interface as though a hand had done it.
/// </summary>
/// <remarks>
/// <para>
/// For tests and tools. What a click does is not one thing: a widget decides it was clicked, an
/// editor decides what that meant, and something in the world changes. Calling the method a click
/// would have called tests the method and not the path to it, and the path is where the interesting
/// failures are: a handle that cannot be grabbed, a menu that opens once, a value that goes in and
/// does not come back.
/// </para>
/// <para>
/// It writes into ImGui's own event queue, which is where a real pointer's report ends up, so
/// everything the interface does behaves exactly as it would. It cannot move the operating system's
/// cursor, and does not try to: what it drives is the application, not the desktop.
/// </para>
/// <para>
/// While anything here has been called, the pointer the interface sees is the one that was asked
/// for rather than the one on the desk. <see cref="Release"/> leaves it where it was put; there is
/// no need to hand it back.
/// </para>
/// </remarks>
public static class SyntheticInput
{
    /// <summary>Where the pretend pointer is, or nothing while the real one is in charge.</summary>
    internal static (float X, float Y)? Pretend { get; private set; }

    /// <summary>Moves the pointer to a point in the window, in logical pixels.</summary>
    public static void MoveTo(float x, float y) => Send(x, y, PointerAction.Move);

    /// <summary>Presses a button where the pointer is put.</summary>
    public static void Press(float x, float y, MouseButton button = MouseButton.Left) =>
        Send(x, y, PointerAction.Press, button);

    /// <summary>Releases a button where the pointer is put.</summary>
    public static void Release(float x, float y, MouseButton button = MouseButton.Left) =>
        Send(x, y, PointerAction.Release, button);

    /// <summary>
    /// Rolls the wheel, in the lines a wheel with detents reports.
    /// </summary>
    /// <remarks>
    /// Positive is away from the hand, which is up in a list. The pointer is not moved first: what
    /// the wheel affects is decided by where it already is, so a test moves it and then rolls.
    /// </remarks>
    public static void Wheel(float lines, float sideways = 0f)
    {
        if (!ImGuiRuntime.IsRunning) return;

        ImGui.GetIO().AddMouseWheelEvent(sideways, lines);
    }

    /// <summary>
    /// Presses and releases a key.
    /// </summary>
    /// <remarks>
    /// A key that types something types it as well, because that is what a keyboard does and what
    /// a field is waiting for.
    /// </remarks>
    public static void Key(ImGuiKey key, string? typed = null)
    {
        if (!ImGuiRuntime.IsRunning) return;

        var io = ImGui.GetIO();

        io.AddKeyEvent(key, true);

        if (typed is { Length: > 0 })
        {
            foreach (var character in typed) io.AddInputCharacter(character);
        }

        io.AddKeyEvent(key, false);
    }

    /// <summary>Types a run of characters.</summary>
    public static void Type(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!ImGuiRuntime.IsRunning) return;

        var io = ImGui.GetIO();
        foreach (var character in text) io.AddInputCharacter(character);
    }

    /// <summary>
    /// Moves, presses or releases the pointer.
    /// </summary>
    /// <remarks>
    /// Into both halves of what a pointer does: the interface's own event queue, and the window's
    /// messages, which is what raycasts the scene and steers the camera. A click that is only told
    /// to one of them tests half the path a hand takes.
    /// </remarks>
    public static void Send(
        float x, float y, PointerAction action, MouseButton button = MouseButton.Left)
    {
        Native.Check(
            Native.bcs_input_pointer(x, y, (int)action, (int)button),
            $"sending a pointer {action} at {x},{y}");

        if (!ImGuiRuntime.IsRunning) return;

        Pretend = (x, y);

        var io = ImGui.GetIO();
        io.AddMousePosEvent(x, y);

        var which = button switch
        {
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            _ => 0,
        };

        switch (action)
        {
            case PointerAction.Press:
                io.AddMouseButtonEvent(which, true);
                break;

            case PointerAction.Release:
                io.AddMouseButtonEvent(which, false);
                break;
        }
    }

    /// <summary>Gives the pointer back to the hand on the desk.</summary>
    public static void Forget() => Pretend = null;
}
