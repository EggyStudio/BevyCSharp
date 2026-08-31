using Bevy.Interop;

namespace Bevy;

/// <summary>How the window fills the screen.</summary>
public enum WindowMode
{
    /// <summary>An ordinary window.</summary>
    Windowed = 0,

    /// <summary>Fills the monitor the window is on, with no decoration and no mode switch.</summary>
    BorderlessFullscreen = 1,
}

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

    /// <summary>Switches between windowed and borderless fullscreen.</summary>
    public static void SetMode(WindowMode mode) =>
        Native.Check(Native.bcs_window_set_mode((int)mode), "Window.SetMode");

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
