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
    /// Draws the primary window's picture as it says, its scale or exact size, shape, fit and
    /// filter, or the window's own picture where it is the window's size and shape.
    /// </summary>
    /// <exception cref="BevyNativeException">
    /// There is no window, or this build has none.
    /// </exception>
    internal static void SetPicture(Picture picture)
    {
        var (width, height) = picture.Size ?? (0u, 0u);
        var (aspectWidth, aspectHeight) = picture.Aspect ?? (0f, 0f);
        Native.Check(
            NativeCore.bcs_window_set_picture(NativeCore.PrimaryWindow, width, height, picture.Scale, aspectWidth, aspectHeight, (uint)picture.Fit, (uint)picture.Filter),
            "Window.SetPicture");
    }

    /// <summary>
    /// The size in pixels the primary window's picture is drawn at, the window's own where its
    /// picture is, and the part of the window it stands in, in the window's logical pixels from its
    /// corner, or none where there is no window or no world is lent to ask in.
    /// </summary>
    internal static ((uint Width, uint Height) Size, (float X, float Y, float Width, float Height) Area)? PictureInUse()
    {
        var size = stackalloc uint[2];
        var area = stackalloc float[4];
        if (NativeCore.bcs_window_picture(NativeCore.PrimaryWindow, size, area) != NativeStatus.Ok) return null;
        return ((size[0], size[1]), (area[0], area[1], area[2], area[3]));
    }

    /// <summary>
    /// Caps the frames drawn a second at a number, or takes the cap away at 0.
    /// </summary>
    /// <exception cref="BevyNativeException">This build has no window.</exception>
    internal static void SetFrameLimit(int fps) =>
        Native.Check(NativeCore.bcs_window_set_frame_limit(fps), "Window.SetFrameLimit");
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
/// Whether its picture is drawn at a size or a shape unlike its own (<see cref="Picture"/>).
/// </param>
internal readonly record struct WindowShown(WindowMode Mode, int Monitor, bool Resizable, bool Vsync, bool Decorated, bool Scaled);

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
