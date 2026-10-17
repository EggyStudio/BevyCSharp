using Bevy.Interop;

namespace Bevy;

/// <summary>
/// The window, after it has opened.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Config"/> decides how the window is created; everything here changes it while the
/// app runs. Each call addresses the primary window and needs a world loan, so call them from
/// inside a system. A window a game spawns beside it, an entity carrying Bevy's <c>Window</c>, is
/// addressed by the overload of each call that takes its entity first.
/// </para>
/// <para>
/// A headless run has no window. Every call reports that rather than silently doing nothing, so
/// guard with <see cref="App.HasRenderer"/> when the same behavior has to run either way.
/// </para>
/// </remarks>
public static unsafe partial class Window
{
    /// <summary>Sets the window's title.</summary>
    public static void SetTitle(string title)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        Native.Check(Native.bcs_window_set_title(Native.PrimaryWindow, title), "Window.SetTitle");
    }

    /// <summary>Resizes the window, in logical pixels.</summary>
    public static void SetSize(uint width, uint height) =>
        Native.Check(Native.bcs_window_set_size(Native.PrimaryWindow, width, height), "Window.SetSize");

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
        Native.Check(Native.bcs_window_size(Native.PrimaryWindow, &width, &height), "Window.Size");
        return (width, height);
    }

    /// <summary>
    /// The primary window's entity, or <see cref="Bevy.Entity.None"/> where there is no window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The calls on this class cover what a game commonly changes about its window. Everything
    /// else Bevy keeps on the window's entity, in its <c>Window</c> and <c>CursorOptions</c>
    /// components, and is reached through their wrappers on this entity, such as whether the
    /// pointer passes through the window to whatever is behind it:
    /// </para>
    /// <code>
    /// ecs.Wrap&lt;CursorOptionsRef&gt;(Window.Entity()).HitTest = false;
    /// </code>
    /// <para>
    /// An offscreen run draws into an image and has no window entity, so this answers
    /// <see cref="Bevy.Entity.None"/> there rather than throwing, and a wrapper on it reads as
    /// absent. A headless build throws, as every call here does.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">This build has no window.</exception>
    public static Entity Entity()
    {
        ulong bits;
        var status = Native.bcs_window_entity(&bits);
        if (status == NativeStatus.NotPresent) return Bevy.Entity.None;
        Native.Check(status, "Window.Entity");
        return new Entity(bits);
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
        Native.Check(Native.bcs_window_scale(Native.PrimaryWindow, &scale), "Window.Scale");
        return scale <= 0f ? 1f : scale;
    }

    /// <summary>Sets how the window fills the screen.</summary>
    public static void SetMode(WindowMode mode) =>
        Native.Check(Native.bcs_window_set_mode(Native.PrimaryWindow, (int)mode), "Window.SetMode");

    /// <summary>Moves the window, in physical pixels from the desktop's top-left corner.</summary>
    /// <remarks>
    /// A window manager is free to ignore this, or to adjust it so the window stays on screen.
    /// </remarks>
    public static void SetPosition(int x, int y) =>
        Native.Check(Native.bcs_window_set_position(Native.PrimaryWindow, x, y), "Window.SetPosition");

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
        Native.Check(Native.bcs_window_set_ime(Native.PrimaryWindow, enabled ? 1 : 0, x, y), "Window.SetIme");

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
            Native.bcs_window_set_style(Native.PrimaryWindow, decorations ? 1 : 0, resizable ? 1 : 0, alwaysOnTop ? 1 : 0),
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
        Native.Check(Native.bcs_window_set_cursor_shape(Native.PrimaryWindow, (int)shape), "Window.SetCursorShape");

    /// <summary>
    /// Sets the pointer over the window to an image, its hotspot, the pixel that points, given in
    /// pixels across and down from the image's top left corner.
    /// </summary>
    /// <remarks>
    /// The image is shown at the size it is, so a pointer is drawn at the size it should appear,
    /// and needs eight bits a channel of color and alpha, as a PNG gives. Nothing changes until it
    /// has loaded. <see cref="SetCursorShape(CursorShape)"/> gives the platform's own shapes back, and the
    /// interface sets those as the pointer crosses what it draws.
    /// </remarks>
    /// <param name="image">The picture, loaded or made.</param>
    /// <param name="hotspotX">How far across the hotspot is.</param>
    /// <param name="hotspotY">How far down.</param>
    /// <exception cref="BevyNativeException">There is no window, the handle names no image, or this build has none.</exception>
    public static void SetCursorImage(AssetHandle image, uint hotspotX = 0, uint hotspotY = 0) =>
        Native.Check(Native.bcs_window_set_cursor_image(Native.PrimaryWindow, image.Key, hotspotX, hotspotY), "Window.SetCursorImage");

    /// <summary>Shows the window by an image, as the platform's icon for it in its title bar and task switcher.</summary>
    /// <remarks>
    /// Put on once the image has loaded and the window has opened, so it can be asked for at
    /// startup. Windows and X11 show it. Wayland takes an app's icon from its desktop file and macOS
    /// from its bundle, so there it changes nothing. A project's <see cref="ProjectSettings.Icon"/>
    /// is put on the window this way as an app starts.
    /// </remarks>
    /// <param name="image">The picture, loaded or made, eight bits a channel as a PNG gives.</param>
    /// <exception cref="BevyNativeException">There is no window, the handle names no image, or this build has none.</exception>
    public static void SetIcon(AssetHandle image) =>
        Native.Check(Native.bcs_window_set_icon(Native.PrimaryWindow, image.Key), "Window.SetIcon");

    /// <summary>
    /// Cuts the pointer's image to one frame of an atlas, its hotspot given across and down from the
    /// frame's top left corner, or shows the whole image again with no layout.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pointer is the image <see cref="SetCursorImage(AssetHandle, uint, uint)"/> set, a sheet of frames the layout cuts,
    /// as <see cref="Render2d.CreateAtlas"/> makes one, so a pointer can be animated as Bevy's
    /// custom_cursor_image animates its crosshairs. The hotspot is set here beside the frame because
    /// a frame's tip is where the frame has it.
    /// </para>
    /// <para>
    /// Another frame, the image flipped either way and a rectangle of it are written through the
    /// window's <c>CursorIconRef</c>, whose <c>CustomCursor.Image</c> holds them, and writing the
    /// frame there keeps the layout set here.
    /// </para>
    /// </remarks>
    /// <param name="layout">The atlas's layout, or <see langword="null"/> for the whole image.</param>
    /// <param name="index">Which frame of the layout.</param>
    /// <param name="hotspotX">How far across the frame the hotspot is.</param>
    /// <param name="hotspotY">How far down.</param>
    /// <exception cref="BevyNativeException">
    /// There is no window, its pointer is no image, the layout names no atlas layout, or this build
    /// has no windows.
    /// </exception>
    public static void SetCursorAtlas(AssetHandle? layout, uint index = 0, uint hotspotX = 0, uint hotspotY = 0) =>
        Native.Check(Native.bcs_window_set_cursor_atlas(Native.PrimaryWindow, layout?.Key ?? -1, index, hotspotX, hotspotY), "Window.SetCursorAtlas");

    /// <summary>
    /// Sets how often the app updates while its window has focus and while it does not, as
    /// inserting Bevy's <c>WinitSettings</c> does. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An app updates continuously while focused unless it sets otherwise, as a game is played.
    /// A tool updates when something happens, with <see cref="UpdateSettings.DesktopApp"/>, and
    /// asks for an update the events would not give with <see cref="RequestRedraw"/> from inside
    /// or <see cref="WakeUp"/>, as Bevy's low_power shows each.
    /// </para>
    /// <para>
    /// Everything that answers on an update waits for one, so a request from <c>./bcs</c> to an app
    /// in a reactive mode is answered once the app next updates, after its wait at the longest.
    /// An offscreen run has no window and runs its own loop, so the settings are kept and change
    /// nothing there.
    /// </para>
    /// </remarks>
    /// <param name="settings">The two modes, focused and not, or one of Bevy's presets.</param>
    /// <exception cref="ArgumentOutOfRangeException">A reactive mode waits less than nothing.</exception>
    /// <exception cref="BevyNativeException">This build has no windows.</exception>
    public static void SetUpdateSettings(UpdateSettings settings)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Focused.Wait, TimeSpan.Zero, "settings.Focused.Wait");
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Unfocused.Wait, TimeSpan.Zero, "settings.Unfocused.Wait");

        Native.Check(
            Native.bcs_window_set_update_modes(
                settings.Focused.IsContinuous ? 1u : 0u, settings.Focused.Wait.TotalSeconds, Reacts(settings.Focused),
                settings.Unfocused.IsContinuous ? 1u : 0u, settings.Unfocused.Wait.TotalSeconds, Reacts(settings.Unfocused)),
            "Window.SetUpdateSettings");

        // The kinds of event a reactive mode wakes for, a bit a kind, as the bridge reads them.
        static uint Reacts(UpdateMode mode) =>
            (mode.ReactToDeviceEvents ? 1u : 0u) | (mode.ReactToUserEvents ? 2u : 0u) | (mode.ReactToWindowEvents ? 4u : 0u);
    }

    /// <summary>
    /// Asks for one more update, as Bevy's <c>RequestRedraw</c> does, for an app in a reactive mode
    /// with something still moving that no event would wake it for. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// Asked every update, it keeps a reactive app updating as a continuous one does, so an
    /// animation plays out, and stopping lets it rest again. A run with no window never waits, so
    /// this does nothing there.
    /// </remarks>
    /// <exception cref="BevyNativeException">This build has no windows.</exception>
    public static void RequestRedraw() => Native.Check(Native.bcs_window_request_redraw(), "Window.RequestRedraw");

    /// <summary>
    /// Wakes winit's loop for one more update, as Bevy's <c>WinitUserEvent::WakeUp</c> does. Only
    /// valid inside a system.
    /// </summary>
    /// <remarks>
    /// The event an app's own work sends where it finishes outside the updates, and which wakes a
    /// reactive mode reacting to the app's own events. A run with no window never waits, so this
    /// does nothing there.
    /// </remarks>
    /// <exception cref="BevyNativeException">This build has no windows.</exception>
    public static void WakeUp() => Native.Check(Native.bcs_window_wake_up(), "Window.WakeUp");

    /// <summary>Minimizes the window to the taskbar or dock.</summary>
    /// <remarks>
    /// A window made without the platform's title bar (<see cref="SetStyle(bool, bool, bool)"/> with
    /// <c>decorations</c> off) has no button of its own for this, so an app that draws its own
    /// title bar calls this from one.
    /// </remarks>
    /// <exception cref="BevyNativeException">There is no window, or this build has none.</exception>
    public static void Minimize() =>
        Native.Check(Native.bcs_window_minimize(Native.PrimaryWindow), "Window.Minimize");

    /// <summary>
    /// Maximizes the window to fill the screen less the taskbar, or puts it back to the size it had.
    /// </summary>
    /// <remarks>
    /// Apart from <see cref="WindowMode.BorderlessFullscreen"/>, which covers the taskbar too and is
    /// a mode rather than a size. The platform remembers the size to go back to, and
    /// <see cref="Place()"/> says whether the window is maximized now.
    /// </remarks>
    /// <param name="maximized">Whether to maximize, or put it back.</param>
    /// <exception cref="BevyNativeException">There is no window, or this build has none.</exception>
    public static void SetMaximized(bool maximized) =>
        Native.Check(Native.bcs_window_set_maximized(Native.PrimaryWindow, maximized ? 1 : 0), "Window.SetMaximized");

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
        Native.Check(Native.bcs_window_place(Native.PrimaryWindow, &place), "Window.Place");
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
        Native.Check(Native.bcs_window_start_drag_move(Native.PrimaryWindow), "Window.StartDragMove");

    /// <summary>
    /// Hands the window to the platform to be resized by the pointer from one edge or corner, until
    /// the button held now is let go.
    /// </summary>
    /// <remarks>
    /// A window without the platform's border has no edge the platform resizes it by, so an app
    /// drawing its own frame calls this when the button goes down along the edges of it. Called on
    /// the press, like <see cref="StartDragMove()"/>.
    /// </remarks>
    /// <param name="edge">Which edge or corner moves.</param>
    /// <exception cref="BevyNativeException">There is no window, or this build has none.</exception>
    public static void StartDragResize(WindowEdge edge) =>
        Native.Check(Native.bcs_window_start_drag_resize(Native.PrimaryWindow, (int)edge), "Window.StartDragResize");

    /// <summary>
    /// Makes a window a game spawned fill a monitor, at the mode the monitor shows, and centers it
    /// there for when it leaves fullscreen. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The monitor is one of the entities Bevy keeps a <c>MonitorRef</c> on for each display it
    /// finds, as Bevy's monitor_info opens one window on each, and a run with no display has none.
    /// Bevy's <c>MonitorSelection::Entity</c> holds the entity where reflection cannot reach it, so
    /// this is a call of its own beside the window's other settings, which its <c>WindowRef</c>
    /// holds.
    /// </para>
    /// <para>
    /// The window is any entity carrying Bevy's <c>Window</c>, which a game spawns through
    /// <c>ecs.Insert&lt;WindowRef&gt;</c>, and <see cref="Config.PrimaryWindow"/> false leaves the
    /// run without one of its own for a game that opens all of them.
    /// </para>
    /// </remarks>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="monitor">The entity carrying the monitor, one Bevy found.</param>
    /// <exception cref="BevyNativeException">
    /// The window is no window, the monitor no monitor, or this build has no windows.
    /// </exception>
    public static void SetFullscreenOn(Entity window, Entity monitor) =>
        Native.Check(Native.bcs_window_fullscreen_on(window.Bits, monitor.Bits), $"filling monitor {monitor} with window {window}");

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
    /// Every video mode a monitor can run at, by an index below <see cref="MonitorCount"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a settings screen lists when it offers exclusive fullscreen. A monitor can only be
    /// run at the modes it reports, so the list is the choice rather than a validation of one
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
    /// <see cref="SetMode(WindowMode)"/> with <see cref="WindowMode.Fullscreen"/> takes the mode the monitor is
    /// already in, which avoids a switch the compositor has to undo on every alt-tab. This is the
    /// other case, where a game runs at a resolution the desktop is not in.
    /// </para>
    /// <para>
    /// The mode is named by its place in <see cref="MonitorModes"/> rather than by numbers, because
    /// a monitor can only run at the modes it offers.
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
            Native.bcs_window_set_video_mode(Native.PrimaryWindow, monitor, mode),
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
        Native.Check(Native.bcs_window_set_cursor(Native.PrimaryWindow, (int)grab, visible ? 1 : 0), "Window.SetCursor");
}
