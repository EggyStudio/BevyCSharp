using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class NativeCore
{
    /// <summary>
    /// The window the <c>bcs_window_*</c> calls address for the primary one, which no entity's
    /// bits can be, where <see cref="Entity.None"/>'s zero could be Bevy's first entity.
    /// </summary>
    internal const ulong PrimaryWindow = ulong.MaxValue;

    // -- Windows

    /// <summary>Sets the primary window's title.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_title(ulong window, string title);

    /// <summary>Resizes the primary window, in logical pixels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_size(ulong window, uint width, uint height);

    /// <summary>Reads the primary window's size, in logical pixels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_size(ulong window, uint* width, uint* height);

    /// <summary>Writes the primary window's entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_entity(ulong* entity);

    /// <summary>Switches between windowed and borderless fullscreen.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_mode(ulong window, int mode);

    /// <summary>Moves the window, in physical pixels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_position(ulong window, int x, int y);

    /// <summary>Sets decorations, resizability and always-on-top together.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_style(ulong window, int decorations, int resizable, int alwaysOnTop);

    /// <summary>Sets the shape of the pointer over the window.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_cursor_shape(ulong window, int shape);

    /// <summary>Sets the pointer over the window to an image.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_cursor_image(ulong window, int image, uint x, uint y);

    /// <summary>Asks for a window to be shown by an image as its icon, put on once both have arrived.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_icon(ulong window, int image);

    /// <summary>Cuts the pointer's image to one frame of an atlas, with that frame's hotspot.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_cursor_atlas(ulong window, int layout, uint index, uint x, uint y);

    /// <summary>Sets how often the app updates with its window focused and without.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_update_modes(
        uint focusedContinuous, double focusedWait, uint focusedReacts,
        uint unfocusedContinuous, double unfocusedWait, uint unfocusedReacts);

    /// <summary>Asks for one more update, as Bevy's <c>RequestRedraw</c>.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_request_redraw();

    /// <summary>Wakes winit's loop for one more update.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_wake_up();

    /// <summary>Makes a window a game spawned fill a monitor named by its entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_fullscreen_on(ulong window, ulong monitor);

    /// <summary>Minimizes the window.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_minimize(ulong window);

    /// <summary>Maximizes the window, or puts it back.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_maximized(ulong window, int maximized);

    /// <summary>Writes where the window is, how large, and whether it is maximized.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_place(ulong window, NativeWindowPlace* place);

    /// <summary>Hands the window to the platform to be moved by the pointer.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_start_drag_move(ulong window);

    /// <summary>Hands the window to the platform to be resized from an edge.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_start_drag_resize(ulong window, int edge);

    /// <summary>Takes the screen over at one of a monitor's own video modes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_video_mode(ulong window, int monitor, int mode);

    /// <summary>Sets whether the cursor is confined or hidden.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_cursor(ulong window, int grab, int visible);

    /// <summary>Drains what the window has reported since the last call.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_events(NativeWindowEvent* events, int capacity);

    /// <summary>Turns the input method on or off, and places its candidate list.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_ime(ulong window, int enabled, float x, float y);

    /// <summary>Writes how many physical pixels a logical one is.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_scale(ulong window, float* scale);

    // -- Monitors

    /// <summary>Counts the monitors the platform reports.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_monitor_count();

    /// <summary>Reports how many video modes a monitor offers.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_monitor_mode_count(int index);

    /// <summary>Describes one video mode of one monitor.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_monitor_mode(int index, int mode, NativeVideoMode* output);

    /// <summary>Describes one monitor by index.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_monitor_info(int index, NativeMonitor* monitor);

    /// <summary>Writes a monitor's name, returning its length in bytes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_monitor_name(int index, byte* buffer, int capacity);

    // -- Text input and dropped files

    /// <summary>Collects the files dropped since the last call, returning how many.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_file_drops_drain();

    /// <summary>Collects what the input method said since the last call, and reports how many.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ime_drain();

    /// <summary>Reads one drained input method message, its kind, its caret and its text.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ime_read(int index, int* kind, int* caret, byte* output, int capacity);

    /// <summary>Writes one drained drop's path, returning its length in bytes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_file_drop_path(int index, int* kind, byte* buffer, int capacity);
}
