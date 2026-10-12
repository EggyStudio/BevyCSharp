using Bevy.Interop;

namespace Bevy;

// The calls for a window a game spawns beside the primary one, which it addresses by the entity
// carrying Bevy's `Window`, as `ecs.Insert<WindowRef>` makes one. Each does what the call of the
// same name without the entity does to the primary window, and an entity carrying no window is
// refused as a missing one, rather than taken for the primary window, which a game that passes
// the wrong entity would never notice.
public static unsafe partial class Window
{
    /// <summary>Sets a window's title.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="title">The title.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void SetTitle(Entity window, string title)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        Native.Check(Native.bcs_window_set_title(window.Bits, title), $"setting the title of window {window}");
    }

    /// <summary>Resizes a window, in logical pixels.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="width">How wide.</param>
    /// <param name="height">How tall.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void SetSize(Entity window, uint width, uint height) =>
        Native.Check(Native.bcs_window_set_size(window.Bits, width, height), $"resizing window {window}");

    /// <summary>A window's current size, in logical pixels.</summary>
    /// <remarks>
    /// The size of the window itself, where the primary window's <see cref="Size()"/> answers with
    /// the image an offscreen run draws into instead.
    /// </remarks>
    /// <param name="window">The entity carrying the window.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static (uint Width, uint Height) Size(Entity window)
    {
        uint width;
        uint height;
        Native.Check(Native.bcs_window_size(window.Bits, &width, &height), $"reading the size of window {window}");
        return (width, height);
    }

    /// <summary>How many physical pixels a logical one is in a window, which may differ by monitor.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static float Scale(Entity window)
    {
        float scale;
        Native.Check(Native.bcs_window_scale(window.Bits, &scale), $"reading the scale of window {window}");
        return scale <= 0f ? 1f : scale;
    }

    /// <summary>Sets how a window fills the screen.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="mode">Windowed, or one of the fullscreen modes on the monitor the window is on.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void SetMode(Entity window, WindowMode mode) =>
        Native.Check(Native.bcs_window_set_mode(window.Bits, (int)mode), $"setting the mode of window {window}");

    /// <summary>Moves a window, in physical pixels from the desktop's top-left corner.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="x">How far across.</param>
    /// <param name="y">How far down.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void SetPosition(Entity window, int x, int y) =>
        Native.Check(Native.bcs_window_set_position(window.Bits, x, y), $"moving window {window}");

    /// <summary>
    /// Turns the platform's input method on or off for a window, and says where in it the text
    /// being composed is.
    /// </summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="enabled">Whether keys go to the input method.</param>
    /// <param name="x">Where the candidate list goes, in logical pixels across from the window's top left.</param>
    /// <param name="y">How far down.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void SetIme(Entity window, bool enabled, float x = 0f, float y = 0f) =>
        Native.Check(Native.bcs_window_set_ime(window.Bits, enabled ? 1 : 0, x, y), $"setting the input method of window {window}");

    /// <summary>
    /// Sets whether a window has a title bar and border, whether it can be resized by dragging, and
    /// whether it stays above other windows.
    /// </summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="decorations">Whether it has the platform's title bar and border.</param>
    /// <param name="resizable">Whether its edges resize it.</param>
    /// <param name="alwaysOnTop">Whether it stays above other windows.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void SetStyle(Entity window, bool decorations = true, bool resizable = true, bool alwaysOnTop = false) =>
        Native.Check(
            Native.bcs_window_set_style(window.Bits, decorations ? 1 : 0, resizable ? 1 : 0, alwaysOnTop ? 1 : 0),
            $"setting the style of window {window}");

    /// <summary>Sets the shape of the pointer while it is over a window.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="shape">Which shape.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void SetCursorShape(Entity window, CursorShape shape) =>
        Native.Check(Native.bcs_window_set_cursor_shape(window.Bits, (int)shape), $"setting the pointer's shape over window {window}");

    /// <summary>Sets the pointer over a window to an image, with its hotspot from the image's top left.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="image">The picture, loaded or made.</param>
    /// <param name="hotspotX">How far across the hotspot is.</param>
    /// <param name="hotspotY">How far down.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, the handle names no image, or this build has none.</exception>
    public static void SetCursorImage(Entity window, AssetHandle image, uint hotspotX = 0, uint hotspotY = 0) =>
        Native.Check(Native.bcs_window_set_cursor_image(window.Bits, image.Key, hotspotX, hotspotY), $"setting the pointer's image over window {window}");

    /// <summary>
    /// Cuts the pointer's image over a window to one frame of an atlas, or shows the whole image
    /// again with no layout.
    /// </summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="layout">The atlas's layout, or <see langword="null"/> for the whole image.</param>
    /// <param name="index">Which frame of the layout.</param>
    /// <param name="hotspotX">How far across the frame the hotspot is.</param>
    /// <param name="hotspotY">How far down.</param>
    /// <exception cref="BevyNativeException">
    /// The entity carries no window, its pointer is no image, the layout names no atlas layout, or
    /// this build has no windows.
    /// </exception>
    public static void SetCursorAtlas(Entity window, AssetHandle? layout, uint index = 0, uint hotspotX = 0, uint hotspotY = 0) =>
        Native.Check(
            Native.bcs_window_set_cursor_atlas(window.Bits, layout?.Key ?? -1, index, hotspotX, hotspotY),
            $"cutting the pointer's image over window {window}");

    /// <summary>Sets whether the cursor is confined to a window or hidden over it.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="grab">How to restrict the cursor.</param>
    /// <param name="visible">Whether the cursor is drawn.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void SetCursor(Entity window, CursorGrab grab, bool visible) =>
        Native.Check(Native.bcs_window_set_cursor(window.Bits, (int)grab, visible ? 1 : 0), $"setting the cursor over window {window}");

    /// <summary>Minimizes a window to the taskbar or dock.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void Minimize(Entity window) =>
        Native.Check(Native.bcs_window_minimize(window.Bits), $"minimizing window {window}");

    /// <summary>Maximizes a window, or puts it back to the size it had.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="maximized">Whether to maximize, or put it back.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void SetMaximized(Entity window, bool maximized) =>
        Native.Check(Native.bcs_window_set_maximized(window.Bits, maximized ? 1 : 0), $"maximizing window {window}");

    /// <summary>Where a window is, how large, and whether it is maximized.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static WindowPlace Place(Entity window)
    {
        NativeWindowPlace place;
        Native.Check(Native.bcs_window_place(window.Bits, &place), $"reading where window {window} is");
        return new WindowPlace(place.HasPosition != 0, place.X, place.Y, place.Width, place.Height, place.Maximized != 0);
    }

    /// <summary>
    /// Hands a window to the platform to be moved by the pointer, until the button held now is let go.
    /// </summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void StartDragMove(Entity window) =>
        Native.Check(Native.bcs_window_start_drag_move(window.Bits), $"dragging window {window}");

    /// <summary>
    /// Hands a window to the platform to be resized by the pointer from one edge or corner, until
    /// the button held now is let go.
    /// </summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="edge">Which edge or corner moves.</param>
    /// <exception cref="BevyNativeException">The entity carries no window, or this build has none.</exception>
    public static void StartDragResize(Entity window, WindowEdge edge) =>
        Native.Check(Native.bcs_window_start_drag_resize(window.Bits, (int)edge), $"resizing window {window} by its edge");

    /// <summary>Takes the screen over with a window in exclusive fullscreen at one of a monitor's own video modes.</summary>
    /// <param name="window">The entity carrying the window.</param>
    /// <param name="monitor">An index below <see cref="MonitorCount"/>.</param>
    /// <param name="mode">An index into that monitor's <see cref="MonitorModes"/>.</param>
    /// <exception cref="BevyNativeException">
    /// The entity carries no window, there is no such monitor, it has no such mode, or this build
    /// has no window.
    /// </exception>
    public static void SetVideoMode(Entity window, int monitor, int mode)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monitor);
        ArgumentOutOfRangeException.ThrowIfNegative(mode);

        Native.Check(
            Native.bcs_window_set_video_mode(window.Bits, monitor, mode),
            $"taking monitor {monitor} over with window {window} at video mode {mode}");
    }
}
