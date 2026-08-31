using Bevy.Interop;

namespace Bevy;

/// <summary>How the window fills the screen.</summary>
public enum WindowMode
{
    /// <summary>An ordinary window.</summary>
    Windowed = 0,

    /// <summary>Fills the monitor the window is on, with no decoration and no mode switch.</summary>
    BorderlessFullscreen = 1,

    /// <summary>
    /// Takes the monitor exclusively, at its current video mode.
    /// </summary>
    /// <remarks>
    /// Lets the display driver hand the window the screen outright, which can be worth a frame of
    /// latency. It also makes alt-tabbing heavier, because the compositor has to take the screen
    /// back. <see cref="BorderlessFullscreen"/> is what most desktop games want.
    /// </remarks>
    Fullscreen = 2,
}

/// <summary>One of the monitors the platform reports.</summary>
/// <remarks>
/// The name is not here: it is the one field that is text, and nothing else in the bridge hands a
/// string back. Monitors are identified by index.
/// </remarks>
/// <param name="Width">Width in physical pixels.</param>
/// <param name="Height">Height in physical pixels.</param>
/// <param name="X">Where its left edge sits in the desktop's coordinate space.</param>
/// <param name="Y">The same, vertically.</param>
/// <param name="RefreshHz">Refresh rate in hertz, or zero when the platform does not report one.</param>
/// <param name="ScaleFactor">Physical pixels per logical pixel.</param>
public readonly record struct MonitorInfo(
    uint Width,
    uint Height,
    int X,
    int Y,
    float RefreshHz,
    float ScaleFactor);

/// <summary>What the window does with the mouse cursor.</summary>
public enum CursorGrab
{
    /// <summary>The cursor moves freely and can leave the window.</summary>
    None = 0,

    /// <summary>The cursor stays inside the window but still moves within it.</summary>
    Confined = 1,

    /// <summary>The cursor is pinned in place and only its movement is reported.</summary>
    Locked = 2,
}

/// <summary>
/// The window, after it has opened.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Config"/> decides how the window is created; everything here changes it while the
/// app runs. Each call addresses the primary window and needs a world loan, so call them from
/// inside a system.
/// </para>
/// <para>
/// A headless run has no window. Every call reports that rather than silently doing nothing, so
/// guard with <see cref="App.HasRenderer"/> when the same behavior has to run either way.
/// </para>
/// </remarks>
public static unsafe class Window
{
    /// <summary>Sets the window's title.</summary>
    public static void SetTitle(string title)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        Native.Check(Native.bcs_window_set_title(title), "Window.SetTitle");
    }

    /// <summary>Resizes the window, in logical pixels.</summary>
    public static void SetSize(uint width, uint height) =>
        Native.Check(Native.bcs_window_set_size(width, height), "Window.SetSize");

    /// <summary>
    /// The window's current size, in logical pixels.
    /// </summary>
    /// <remarks>
    /// What the window ended up at, which is not always what was asked for: a window manager may
    /// refuse a resize, and a fullscreen window takes the monitor's size.
    /// </remarks>
    public static (uint Width, uint Height) Size()
    {
        uint width;
        uint height;
        Native.Check(Native.bcs_window_size(&width, &height), "Window.Size");
        return (width, height);
    }

    /// <summary>Sets how the window fills the screen.</summary>
    public static void SetMode(WindowMode mode) =>
        Native.Check(Native.bcs_window_set_mode((int)mode), "Window.SetMode");

    /// <summary>Moves the window, in physical pixels from the desktop's top-left corner.</summary>
    /// <remarks>
    /// A window manager is free to ignore this, or to adjust it so the window stays on screen.
    /// </remarks>
    public static void SetPosition(int x, int y) =>
        Native.Check(Native.bcs_window_set_position(x, y), "Window.SetPosition");

    /// <summary>
    /// Sets whether the window has a title bar and border, whether it can be resized by dragging,
    /// and whether it stays above other windows.
    /// </summary>
    /// <remarks>
    /// The three are set together because each is one flag, and setting one alone would mean
    /// reading the other two back first to leave them as they were.
    /// </remarks>
    public static void SetStyle(bool decorations = true, bool resizable = true, bool alwaysOnTop = false) =>
        Native.Check(
            Native.bcs_window_set_style(decorations ? 1 : 0, resizable ? 1 : 0, alwaysOnTop ? 1 : 0),
            "Window.SetStyle");

    /// <summary>How many monitors the platform reports.</summary>
    /// <returns>Zero on a windowless run, which has no monitors to report.</returns>
    public static int MonitorCount()
    {
        var count = Native.bcs_monitor_count();
        return count < 0 ? 0 : count;
    }

    /// <summary>
    /// Describes one monitor, by an index below <see cref="MonitorCount"/>.
    /// </summary>
    /// <remarks>
    /// Enough to place a window deliberately, or to offer a choice of screen in a settings menu.
    /// The list can change while the app runs, as monitors are plugged in and unplugged.
    /// </remarks>
    /// <exception cref="BevyNativeException">There is no monitor at that index.</exception>
    public static MonitorInfo Monitor(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        NativeMonitor monitor;
        Native.Check(Native.bcs_monitor_info(index, &monitor), $"reading monitor {index}");

        return new MonitorInfo(
            monitor.Width,
            monitor.Height,
            monitor.X,
            monitor.Y,
            monitor.RefreshMillihertz / 1000f,
            monitor.ScaleFactor);
    }

    /// <summary>
    /// Sets whether the cursor is confined or hidden.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CursorGrab.Locked"/> is what a first-person camera needs: it reads how far the
    /// mouse moved rather than where it is, and a free cursor stops moving at the edge of the
    /// screen.
    /// </para>
    /// <para>
    /// Platforms differ in which grab they support. Windows confines and macOS locks, and each
    /// emulates the other, so asking for one and getting the other is normal. Hide the cursor
    /// while it is grabbed either way, or the emulated case shows it sitting still.
    /// </para>
    /// </remarks>
    /// <param name="grab">How to restrict the cursor.</param>
    /// <param name="visible">Whether the cursor is drawn.</param>
    public static void SetCursor(CursorGrab grab, bool visible) =>
        Native.Check(Native.bcs_window_set_cursor((int)grab, visible ? 1 : 0), "Window.SetCursor");
}
