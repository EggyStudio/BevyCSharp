using Bevy.Interop;

namespace Bevy;

// What the system shows of the primary window and the sets the window's settings make of it
// (WindowSettings), kept internal since the settings are how a game or a player reaches them, and
// Bevy's own reflected window carries the rest for a game that sets it from code.
public static unsafe partial class Window
{
    /// <summary>
    /// What the system shows of the primary window, or none where there is no window, winit has not
    /// made it yet, or no world is lent to ask in.
    /// </summary>
    internal static WindowShown? Shown()
    {
        int mode, monitor, resizable, vsync, decorated, scaled;
        if (NativeCore.bcs_window_shown(NativeCore.PrimaryWindow, &mode, &monitor, &resizable, &vsync, &decorated, &scaled) != NativeStatus.Ok)
            return null;
        return new WindowShown((WindowMode)mode, monitor, resizable != 0, vsync != 0, decorated != 0, scaled != 0);
    }

    /// <summary>
    /// Why the system would refuse the primary window a mode, as one of the bridge's reasons
    /// (<see cref="WindowRefusal"/>).
    /// </summary>
    /// <exception cref="BevyNativeException">
    /// There is no window, or this build has none.
    /// </exception>
    internal static WindowRefusal ModeRefusal(WindowMode mode)
    {
        int reason;
        Native.Check(NativeCore.bcs_window_mode_refusal(NativeCore.PrimaryWindow, (int)mode, &reason), "Window.ModeRefusal");
        return (WindowRefusal)reason;
    }

    /// <summary>
    /// Moves the primary window to a monitor, by its index, in the mode it holds.
    /// </summary>
    /// <exception cref="BevyNativeException">There is no window or no such monitor.</exception>
    internal static void SetMonitor(int index) =>
        Native.Check(NativeCore.bcs_window_set_monitor(NativeCore.PrimaryWindow, index), "Window.SetMonitor");

    /// <summary>Makes the primary window's edges resize it, or not.</summary>
    /// <exception cref="BevyNativeException">
    /// There is no window, or this build has none.
    /// </exception>
    internal static void SetResizable(bool resizable) =>
        Native.Check(NativeCore.bcs_window_set_resizable(NativeCore.PrimaryWindow, resizable ? 1 : 0), "Window.SetResizable");

    /// <summary>Makes a frame of the primary window wait for the display, or not.</summary>
    /// <exception cref="BevyNativeException">
    /// There is no window, or this build has none.
    /// </exception>
    internal static void SetVsync(bool vsync) =>
        Native.Check(NativeCore.bcs_window_set_vsync(NativeCore.PrimaryWindow, vsync ? 1 : 0), "Window.SetVsync");

    /// <summary>
    /// Gives the primary window the platform's frame, or takes it away, leaving its edges and its
    /// level as they are.
    /// </summary>
    /// <exception cref="BevyNativeException">
    /// There is no window, or this build has none.
    /// </exception>
    internal static void SetDecorations(bool decorations) =>
        Native.Check(NativeCore.bcs_window_set_decorations(NativeCore.PrimaryWindow, decorations ? 1 : 0), "Window.SetDecorations");

    /// <summary>
    /// Draws the primary window's picture at a size in its logical pixels while it covers its
    /// screen borderless, brought to the screen by the filter, or at its own size again for none.
    /// </summary>
    /// <exception cref="BevyNativeException">
    /// There is no window, or this build has none.
    /// </exception>
    internal static void SetScaling((uint Width, uint Height)? size, WindowScaling filter) =>
        Native.Check(
            NativeCore.bcs_window_set_scaling(NativeCore.PrimaryWindow, size?.Width ?? 0, size?.Height ?? 0, (uint)filter),
            "Window.SetScaling");
}

/// <summary>What the system shows of a window, as <see cref="Window.Shown"/> reads it.</summary>
/// <param name="Mode">How it covers its screen.</param>
/// <param name="Monitor">
/// The monitor it is on, by its index, or -1 where the system names none.
/// </param>
/// <param name="Resizable">Whether its edges resize it.</param>
/// <param name="Vsync">Whether a frame waits for the display.</param>
/// <param name="Decorated">Whether it has the platform's frame.</param>
/// <param name="Scaled">
/// Whether its picture is drawn at a size of its own while it covers its screen borderless.
/// </param>
internal readonly record struct WindowShown(WindowMode Mode, int Monitor, bool Resizable, bool Vsync, bool Decorated, bool Scaled);

/// <summary>
/// How a window's picture drawn at a size of its own is brought to its screen, the choices of
/// <c>window.scaling</c>, by the numbers the bridge takes.
/// </summary>
internal enum WindowScaling
{
    /// <summary>Filtered, each screen pixel a blend of the picture pixels round it, soft.</summary>
    Linear = 0,

    /// <summary>
    /// Each screen pixel from the picture pixel under it, sharp, and even only at a whole ratio.
    /// </summary>
    Nearest = 1,

    /// <summary>
    /// Each screen pixel inside a picture pixel from it whole and one across an edge a blend of the
    /// two, so every picture pixel is a block of one width that holds still at any ratio, equal to
    /// <see cref="Nearest"/> at a whole one.
    /// </summary>
    Sharp = 2,
}

/// <summary>Why the system would refuse a window a mode, as the bridge numbers it.</summary>
internal enum WindowRefusal
{
    /// <summary>The system takes the mode.</summary>
    None = 0,

    /// <summary>
    /// The platform gives no window a mode of the screen's, as Wayland gives none.
    /// </summary>
    NoScreenModes = 1,

    /// <summary>The window is on no screen the system names.</summary>
    NoScreen = 2,

    /// <summary>
    /// The screen lists no mode at its own size and rate for a fullscreen window.
    /// </summary>
    NoScreenMode = 3,
}
