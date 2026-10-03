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
    /// back. Most desktop games use <see cref="BorderlessFullscreen"/>.
    /// </remarks>
    Fullscreen = 2,
}

/// <summary>One of the monitors the platform reports.</summary>
/// <remarks>
/// The name is read separately, with <see cref="Window.MonitorName"/>, because it is text and the
/// rest is not.
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

/// <summary>Where the window is, how large, and whether it is maximized.</summary>
/// <remarks>
/// What a game keeps to reopen where it was closed (<see cref="Config.RememberWindow"/>), read
/// with <see cref="Window.Place"/>. Wayland never tells an app where its window is, so there
/// <see cref="HasPosition"/> is false and the place is the size alone.
/// </remarks>
/// <param name="HasPosition">Whether <see cref="X"/> and <see cref="Y"/> say anything.</param>
/// <param name="X">The window's left edge, in physical pixels in the desktop's coordinate space.</param>
/// <param name="Y">Its top edge, the same way.</param>
/// <param name="Width">Width in logical pixels.</param>
/// <param name="Height">Height in logical pixels.</param>
/// <param name="Maximized">Whether it fills the screen less the taskbar.</param>
public readonly record struct WindowPlace(bool HasPosition, int X, int Y, uint Width, uint Height, bool Maximized);

/// <summary>
/// One video mode a monitor can be driven at.
/// </summary>
/// <remarks>
/// A resolution, a color depth and a refresh rate together, which exclusive fullscreen takes the
/// screen over with. A monitor offers a fixed list of these and can be driven at no others, so a
/// settings screen offers what <see cref="Window.MonitorModes"/> returns rather than a pair of
/// number boxes.
/// </remarks>
/// <param name="Width">Width in physical pixels.</param>
/// <param name="Height">Height in physical pixels.</param>
/// <param name="BitDepth">Bits per pixel.</param>
/// <param name="RefreshHz">Refresh rate in hertz.</param>
public readonly record struct VideoMode(
    uint Width,
    uint Height,
    uint BitDepth,
    float RefreshHz);

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

/// <summary>The shape of the pointer while it is over the window.</summary>
/// <remarks>The shapes an interface asks for, taken from the platform's own set.</remarks>
public enum CursorShape
{
    /// <summary>The platform's arrow.</summary>
    Default = 0,

    /// <summary>A text caret, over something to type in.</summary>
    Text = 1,

    /// <summary>A hand, over something to press.</summary>
    Pointer = 2,

    /// <summary>Four arrows, over something to move.</summary>
    Move = 3,

    /// <summary>A no-entry sign, over something that cannot be used now.</summary>
    NotAllowed = 4,

    /// <summary>Left and right, over a vertical edge to drag.</summary>
    ResizeHorizontal = 5,

    /// <summary>Up and down, over a horizontal edge to drag.</summary>
    ResizeVertical = 6,

    /// <summary>From the bottom left to the top right, over that pair of corners.</summary>
    ResizeRising = 7,

    /// <summary>From the top left to the bottom right, over that pair of corners.</summary>
    ResizeFalling = 8,

    /// <summary>An open hand, over something to grab.</summary>
    Grab = 9,

    /// <summary>A closed hand, while something is held.</summary>
    Grabbing = 10,
}

/// <summary>An edge or corner of the window, counted clockwise from the top.</summary>
/// <remarks>What <see cref="Window.StartDragResize"/> resizes from.</remarks>
public enum WindowEdge
{
    /// <summary>The top edge.</summary>
    Top = 0,

    /// <summary>The top right corner.</summary>
    TopRight = 1,

    /// <summary>The right edge.</summary>
    Right = 2,

    /// <summary>The bottom right corner.</summary>
    BottomRight = 3,

    /// <summary>The bottom edge.</summary>
    Bottom = 4,

    /// <summary>The bottom left corner.</summary>
    BottomLeft = 5,

    /// <summary>The left edge.</summary>
    Left = 6,

    /// <summary>The top left corner.</summary>
    TopLeft = 7,
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
    /// What the window ended up at, which is not always what was asked for, because a window
    /// manager may refuse a resize, and a fullscreen window takes the monitor's size.
    /// </remarks>
    public static (uint Width, uint Height) Size()
    {
        uint width;
        uint height;
        Native.Check(Native.bcs_window_size(&width, &height), "Window.Size");
        return (width, height);
    }

    /// <summary>
    /// How many physical pixels a logical one is.
    /// </summary>
    /// <remarks>
    /// Everything a window reports is in logical pixels, so this turns one into the units the
    /// framebuffer is divided into, such as a viewport, a scissor rectangle or a screenshot.
    /// </remarks>
    public static float Scale()
    {
        float scale;
        Native.Check(Native.bcs_window_scale(&scale), "Window.Scale");
        return scale <= 0f ? 1f : scale;
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
    /// Turns the platform's input method on or off, and says where the text being composed is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An input method (IME) is how Japanese, Chinese or Korean is typed, by composing a candidate
    /// and choosing it. With it on, <see cref="ImeComposing"/> and <see cref="ImeCommit"/> arrive on
    /// the message bus, and the keys it takes stop reaching the game, so it is turned on when a text
    /// field takes the focus and off when it lets go. <see cref="Input.Text"/> covers typing that
    /// needs no composing, dead keys included.
    /// </para>
    /// <para>
    /// The position, in logical pixels from the window's top left, is where the platform shows its
    /// candidate list, which belongs beside the field's caret rather than in a corner.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">There is no window, or this build has none.</exception>
    public static void SetIme(bool enabled, float x = 0f, float y = 0f) =>
        Native.Check(Native.bcs_window_set_ime(enabled ? 1 : 0, x, y), "Window.SetIme");

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

    /// <summary>Sets the shape of the pointer while it is over the window.</summary>
    /// <remarks>
    /// The interface sets this itself as the pointer crosses a field or an edge, whenever the shape
    /// it asks for changes, so a shape set here holds until the pointer next moves onto something
    /// of the interface's that asks for another.
    /// </remarks>
    /// <param name="shape">Which shape.</param>
    /// <exception cref="BevyNativeException">There is no window, or this build has none.</exception>
    public static void SetCursorShape(CursorShape shape) =>
        Native.Check(Native.bcs_window_set_cursor_shape((int)shape), "Window.SetCursorShape");

    /// <summary>Minimizes the window to the taskbar or dock.</summary>
    /// <remarks>
    /// A window made without the platform's title bar (<see cref="SetStyle"/> with
    /// <c>decorations</c> off) has no button of its own for this, so an app that draws its own
    /// title bar calls this from one.
    /// </remarks>
    /// <exception cref="BevyNativeException">There is no window, or this build has none.</exception>
    public static void Minimize() =>
        Native.Check(Native.bcs_window_minimize(), "Window.Minimize");

    /// <summary>
    /// Maximizes the window to fill the screen less the taskbar, or puts it back to the size it had.
    /// </summary>
    /// <remarks>
    /// Apart from <see cref="WindowMode.BorderlessFullscreen"/>, which covers the taskbar too and is
    /// a mode rather than a size. The platform remembers the size to go back to, and
    /// <see cref="Place"/> says whether the window is maximized now.
    /// </remarks>
    /// <param name="maximized">Whether to maximize, or put it back.</param>
    /// <exception cref="BevyNativeException">There is no window, or this build has none.</exception>
    public static void SetMaximized(bool maximized) =>
        Native.Check(Native.bcs_window_set_maximized(maximized ? 1 : 0), "Window.SetMaximized");

    /// <summary>Where the window is, how large, and whether it is maximized.</summary>
    /// <remarks>
    /// The position is the one the platform last reported, so it is missing on Wayland, which
    /// reports none, and missing everywhere until the window has been moved or placed once.
    /// Whether the window is maximized is read from the platform, since Bevy keeps no record of it.
    /// </remarks>
    /// <exception cref="BevyNativeException">There is no window, or this build has none.</exception>
    public static WindowPlace Place()
    {
        NativeWindowPlace place;
        Native.Check(Native.bcs_window_place(&place), "Window.Place");
        return new WindowPlace(place.HasPosition != 0, place.X, place.Y, place.Width, place.Height, place.Maximized != 0);
    }

    /// <summary>
    /// Hands the window to the platform to be moved by the pointer, until the button held now is
    /// let go.
    /// </summary>
    /// <remarks>
    /// <para>
    /// How a window without a title bar is dragged, called when the button goes down over
    /// whatever stands in for one. The platform moves the window rather than the app, since only
    /// the platform knows where it may go, and on Wayland an app is never told where its window is.
    /// </para>
    /// <para>
    /// Called on the press, while the button is still down. Called later, the platform has no drag
    /// to hand over and ignores it.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">There is no window, or this build has none.</exception>
    public static void StartDragMove() =>
        Native.Check(Native.bcs_window_start_drag_move(), "Window.StartDragMove");

    /// <summary>
    /// Hands the window to the platform to be resized by the pointer from one edge or corner, until
    /// the button held now is let go.
    /// </summary>
    /// <remarks>
    /// A window without the platform's border has no edge the platform resizes it by, so an app
    /// drawing its own frame calls this when the button goes down along the edges of it. Called on
    /// the press, like <see cref="StartDragMove"/>.
    /// </remarks>
    /// <param name="edge">Which edge or corner moves.</param>
    /// <exception cref="BevyNativeException">There is no window, or this build has none.</exception>
    public static void StartDragResize(WindowEdge edge) =>
        Native.Check(Native.bcs_window_start_drag_resize((int)edge), "Window.StartDragResize");

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
    /// The name the platform gives a monitor, by an index below <see cref="MonitorCount"/>.
    /// </summary>
    /// <remarks>
    /// What a settings menu shows next to each screen. Empty when the platform names the monitor
    /// nothing, which happens often enough that a menu needs a fallback such as the index and the
    /// resolution.
    /// </remarks>
    /// <exception cref="BevyNativeException">There is no monitor at that index.</exception>
    public static string MonitorName(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return Native.ReadText(
            (buffer, capacity) => Native.bcs_monitor_name(index, buffer, capacity),
            $"reading the name of monitor {index}");
    }

    /// <summary>
    /// Every video mode a monitor can be driven at, by an index below <see cref="MonitorCount"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a settings screen lists when it offers exclusive fullscreen. A monitor can only be
    /// driven at the modes it reports, so the list is the choice rather than a validation of one
    /// somebody typed.
    /// </para>
    /// <para>
    /// The list is in whatever order the platform gives it, which is not sorted, and it can hold
    /// the same resolution several times at different refresh rates.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">There is no monitor at that index.</exception>
    public static VideoMode[] MonitorModes(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        var count = Native.bcs_monitor_mode_count(index);
        if (count == NativeStatus.Unsupported) return [];

        Native.Check(count, $"counting the video modes of monitor {index}");
        if (count == 0) return [];

        var modes = new VideoMode[count];

        for (var i = 0; i < count; i++)
        {
            NativeVideoMode mode;
            Native.Check(
                Native.bcs_monitor_mode(index, i, &mode),
                $"reading video mode {i} of monitor {index}");

            modes[i] = new VideoMode(
                mode.Width,
                mode.Height,
                mode.BitDepth,
                mode.RefreshMillihertz / 1000f);
        }

        return modes;
    }

    /// <summary>
    /// Takes the screen over in exclusive fullscreen at one of a monitor's own video modes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SetMode"/> with <see cref="WindowMode.Fullscreen"/> takes the mode the monitor is
    /// already in, which avoids a switch the compositor has to undo on every alt-tab. This is the
    /// other case, where a game runs at a resolution the desktop is not in.
    /// </para>
    /// <para>
    /// The mode is named by its place in <see cref="MonitorModes"/> rather than by numbers, because
    /// a monitor can only be driven at the modes it offers.
    /// </para>
    /// </remarks>
    /// <param name="monitor">An index below <see cref="MonitorCount"/>.</param>
    /// <param name="mode">An index into that monitor's <see cref="MonitorModes"/>.</param>
    /// <exception cref="BevyNativeException">
    /// There is no such monitor, it has no such mode, or this build has no window.
    /// </exception>
    public static void SetVideoMode(int monitor, int mode)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monitor);
        ArgumentOutOfRangeException.ThrowIfNegative(mode);

        Native.Check(
            Native.bcs_window_set_video_mode(monitor, mode),
            $"taking monitor {monitor} over at video mode {mode}");
    }

    /// <summary>
    /// Sets whether the cursor is confined or hidden.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A first-person camera needs <see cref="CursorGrab.Locked"/>, because it reads how far the
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
