using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The raw C ABI exported by <c>bevy_csharp</c>, the Rust bridge that owns the Bevy app.
/// </summary>
/// <remarks>
/// <para>
/// Every entry point that touches the ECS is <em>ambient</em>, meaning it takes no world handle and
/// instead operates on the world Bevy has loaned to the currently running system callback on this
/// thread. That is why they are only valid from inside a system, on the main thread. Calls from
/// anywhere else fail with <see cref="NativeStatus.NoWorld"/> rather than corrupting state, which
/// steers parallel behavior methods onto <see cref="EcsCommands"/> instead.
/// </para>
/// <para>
/// These are deliberately not public. <see cref="EcsWorld"/> and <see cref="App"/> are the
/// supported surface; this type may change with any release.
/// </para>
/// </remarks>
internal static unsafe partial class Native
{
    /// <summary>Base name of the native library, resolved by <see cref="NativeLoader"/>.</summary>
    internal const string Library = "bevy_csharp";

    /// <summary>ABI revision this assembly was built against.</summary>
    internal const int ExpectedAbiVersion = 307;

    /// <summary>
    /// The window the <c>bcs_window_*</c> calls address for the primary one, which no entity's
    /// bits can be, where <see cref="Entity.None"/>'s zero could be Bevy's first entity.
    /// </summary>
    internal const ulong PrimaryWindow = ulong.MaxValue;

    static Native() => NativeLoader.Initialize();

    // -- Diagnostics

    /// <summary>Returns the ABI revision the loaded native library implements.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_abi_version();

    /// <summary>Returns 1 if the native library was built with the renderer compiled in.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_has_render();

    /// <summary>Reports whether the caller is on the process main thread.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_is_main_thread();

    // -- App lifecycle

    /// <summary>Creates the Bevy app. Returns <see cref="IntPtr.Zero"/> on failure.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial IntPtr bcs_app_create(NativeConfig* config);

    /// <summary>Destroys the app and everything it owns.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void bcs_app_destroy(IntPtr app);

    /// <summary>Runs the app, blocking until exit, then runs the cleanup systems.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_run(IntPtr app);

    /// <summary>Asks the running app to shut down after the current frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_request_exit();

    /// <summary>Asks the running app to shut down after the current frame, ending the run with a code.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_request_exit_code(int code);

    /// <summary>Writes a line into Bevy's log at a level, from trace at zero to error at four.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_log_write(int level, string message);

    /// <summary>
    /// Takes the oldest line Bevy logged at the error level and answers its length, or 0 when none
    /// is kept. A buffer too small for the line leaves it kept and answers the length it needs.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_log_take_error(byte* output, int capacity);

    /// <summary>
    /// Starts or stops keeping every line Bevy logs for <see cref="bcs_log_take_line"/>.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_log_collect(int on);

    /// <summary>
    /// Takes the oldest line kept since <see cref="bcs_log_collect"/> with its level, from trace at
    /// zero to error at four, and answers its length, or 0 when none is kept.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_log_take_line(byte* output, int capacity, int* level);

    /// <summary>
    /// Gives the bridge the writer for a panic on a thread outside the guard, or none.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_crash_writer(delegate* unmanaged[Cdecl]<byte*, int, void> writer);

    /// <summary>
    /// Writes what the most recent panic in the bridge said, or nothing where none has happened.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_last_panic(byte* output, int capacity);

    /// <summary>
    /// Panics on purpose, inside the guard where <paramref name="guarded"/> is not zero and on a
    /// thread of its own otherwise, for a crash log to be tried.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_panic_on_purpose(int guarded);

    /// <summary>Registers a C# system callback into a Bevy schedule, under the name Bevy reports it by.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_add_system(
        IntPtr app,
        int stage,
        string name,
        delegate* unmanaged[Cdecl]<IntPtr, void> callback,
        IntPtr user);

    /// <summary>Steps the systems of a stage from the next frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_stepping_add(int stage);

    /// <summary>Stops stepping the systems of a stage.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_stepping_remove(int stage);

    /// <summary>Turns stepping off or on, or steps a system or the rest of the frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_stepping_act(int action);

    /// <summary>Answers 1 while stepping is on.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_stepping_enabled();

    /// <summary>Sets how stepping treats every system of a stage with a name.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_stepping_set_behavior(int stage, string name, int behavior);

    /// <summary>Writes the stepped systems into a buffer, a stage and a name a line.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_stepping_systems(byte* buffer, int capacity);

    /// <summary>Answers the place in the stepped systems of the one stepping runs next.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_stepping_cursor();

    /// <summary>Writes Bevy's account of the stepping into a buffer.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_stepping_describe(byte* buffer, int capacity);

    /// <summary>Orders one C# system of a stage before another, by the numbers registration gave them.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_order_systems(IntPtr handle, int stage, int first, int then);

    // -- Component registration

    /// <summary>Registers a component layout before the app starts running.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_register(
        IntPtr app, string name, uint size, uint align, int storage);

    /// <summary>Registers a component layout from inside a running system.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_register_live(
        string name, uint size, uint align, int storage);

    /// <summary>
    /// Calls back whenever a component leaves an entity. Through the app handle before the run, or
    /// with a null handle through the loaned world during it.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_on_remove(
        IntPtr app, int component, delegate* unmanaged[Cdecl]<ulong, int, byte*, void> callback);

    /// <summary>
    /// Calls back as a component is added to an entity, inserted on it, or about to be overwritten or
    /// taken off it, by kind, through the app handle before the run or the loaned world during it.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_on_hook(
        IntPtr app, int component, int kind, delegate* unmanaged[Cdecl]<int, ulong, int, byte*, void> callback);

    /// <summary>Asks Bevy to report one kind of thing a pointer does, through an observer it spawns.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_observe_pointer(
        IntPtr app,
        int kind,
        delegate* unmanaged[Cdecl]<NativePointerEvent*, IntPtr, void> callback,
        IntPtr user,
        ulong* observer);

    /// <summary>Asks Bevy to report each key that reaches the focused entity, through an observer it spawns.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_observe_focused_keys(
        IntPtr app,
        delegate* unmanaged[Cdecl]<NativeFocusedKey*, IntPtr, void> callback,
        IntPtr user,
        ulong* observer);

    /// <summary>Asks Bevy to report one kind of thing its widgets report, through an observer it spawns.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_observe_widget(
        IntPtr app,
        int kind,
        delegate* unmanaged[Cdecl]<NativeWidgetEvent*, IntPtr, void> callback,
        IntPtr user,
        ulong* observer);

    /// <summary>Asks Bevy to report one kind of change to a component, through an observer it spawns.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_observe_component(
        IntPtr app,
        int kind,
        int component,
        delegate* unmanaged[Cdecl]<int, int, ulong, byte*, nuint, IntPtr, void> callback,
        IntPtr user,
        ulong* observer);

    /// <summary>
    /// Calls back with the copy of a component an entity clone is about to receive, so its handles
    /// can be replaced before it is written.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_on_clone(
        int component, delegate* unmanaged[Cdecl]<int, byte*, void> callback);

    /// <summary>Spawns a copy of an entity with everything on it, returning the copy or zero.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_ecs_clone(ulong entity);

    /// <summary>Resolves one of Bevy's own components to an id, by name.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_id_of(string name);

    /// <summary>Reports the size and alignment Bevy uses for a component.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_layout(int component, uint* size, uint* align);

    // -- UI (render builds only)

    /// <summary>Spawns a UI rectangle, returning its entity or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_ui_spawn_node(NativeUiNodeConfig* config);

    /// <summary>Spawns a run of UI text, returning its entity or 0.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_ui_spawn_text(
        string text, NativeUiNodeConfig* config, NativeUiTextConfig* style);

    /// <summary>Adds a run of text to an existing one, returning its entity or 0.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_ui_spawn_text_span(
        ulong parent, string text, NativeUiTextConfig* style, float* color);

    /// <summary>Lays a node's children out on a grid.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_grid(ulong entity, NativeUiGridConfig* config);

    /// <summary>Places one child on its parent's grid.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_grid_placement(
        ulong entity, int row, int rowSpan, int column, int columnSpan, int justifySelf);

    /// <summary>Replaces what a text entity says.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_text(ulong entity, string text);

    /// <summary>Writes how many font atlases there are, then the bytes their images hold.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_font_atlases(ulong* counts);

    /// <summary>Writes each font atlas's picture key and font size, and answers how many there are.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_font_atlas_pictures(int* keys, float* sizes, int capacity);

    /// <summary>Empties the font atlases.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_clear_font_atlases();

    /// <summary>Reads a node's interaction, 0 for none, 1 for hovered and 2 for pressed.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_interaction(ulong entity);

    /// <summary>Draws a picture inside a node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_image(ulong entity, NativeUiImageConfig* config);

    /// <summary>Moves a scrolling node's contents inside it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_scroll(ulong entity, float x, float y);

    // -- Window (render builds only)

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

    /// <summary>Takes the screen over at one of a monitor's own video modes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_video_mode(ulong window, int monitor, int mode);

    /// <summary>Describes one monitor by index.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_monitor_info(int index, NativeMonitor* monitor);

    /// <summary>Sets whether the cursor is confined or hidden.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_cursor(ulong window, int grab, int visible);

    // -- App states

    /// <summary>Reports how many independent state slots the bridge provides.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_slots();

    /// <summary>Reports how many sub-states one state slot can carry.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_subs_per_slot();

    /// <summary>Reports how many computed states one state slot can carry.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_computed_per_slot();

    /// <summary>Reports how many sub-states exist in total.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_sub_count();

    /// <summary>Creates a computed state, working its value out from a table.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_computed_add(
        IntPtr app, int slot, int* from, int* to, int count);

    /// <summary>
    /// Hands the bridge a reader over the assets the game's assembly carries and the paths it
    /// carries, newline-joined, or takes them back with a null reader. Read by the next app built.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_assets_carried(delegate* unmanaged[Cdecl]<byte*, nuint, byte*, nuint, long> read, byte* paths, nuint pathsLength);

    /// <summary>How many sub-states of several states the bridge has.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_joint_sub_count();

    /// <summary>Creates a sub-state of several states, by its number among those, with one wanted value a state slot.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_joint_substate_add(IntPtr handle, int slot, int* wants, int count, int initial);

    /// <summary>How many joint states the bridge has.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_joint_count();

    /// <summary>How many computed states the bridge has in total, which is where joints start.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_computed_count();

    /// <summary>Sets the function joint states are worked out by.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_joint_rule(delegate* unmanaged[Cdecl]<int, int*, uint, int*, int> rule);

    /// <summary>Creates a joint state, by its number among joints.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_joint_add(IntPtr handle, int slot);

    /// <summary>Sets the function computed states added with a rule are worked out by.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_computed_rule(delegate* unmanaged[Cdecl]<int, int, int*, int> rule);

    /// <summary>Creates a computed state worked out by that function rather than a table.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_computed_add_rule(IntPtr handle, int slot);

    /// <summary>Creates a state machine in a slot, before the app runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_add(IntPtr app, int slot, int initial);

    /// <summary>Creates a sub-state under a slot, existing while that slot holds a value.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_substate_add(IntPtr app, int slot, int parent, int initial);

    /// <summary>Marks an entity to be despawned when a slot leaves a value.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_despawn_on_exit(ulong entity, int slot, int value);

    /// <summary>Marks an entity to be despawned as a slot enters a value.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_despawn_on_enter(ulong entity, int slot, int value);

    /// <summary>Copies the transitions every slot made since the last call, answering how many.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_transitions(NativeStateTransition* transitions, int capacity);

    /// <summary>Registers a system on a state's enter or exit edge.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_add_system(
        IntPtr app,
        int slot,
        int value,
        int edge,
        delegate* unmanaged[Cdecl]<IntPtr, void> callback,
        IntPtr user);

    /// <summary>Registers a system on a state's move from one value to a particular other.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_add_transition(
        IntPtr app,
        int slot,
        int from,
        int to,
        delegate* unmanaged[Cdecl]<IntPtr, void> callback,
        IntPtr user);

    /// <summary>Registers a system among a state's exits, moves or entries, run with the move it made.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_add_step(
        IntPtr app,
        int slot,
        int step,
        delegate* unmanaged[Cdecl]<IntPtr, void> callback,
        IntPtr user);

    /// <summary>Reads the move a state made, while a system among its exits, moves or entries runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_step_transition(NativeStateTransition* transition);

    /// <summary>Places a schedule of the game's own in Bevy's main order, after a stage.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_place_schedule(IntPtr app, string schedule, int after, uint singleThreaded);

    /// <summary>Registers a C# system in a schedule of the game's own, by the schedule's name.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_add_schedule_system(
        IntPtr app,
        string schedule,
        string name,
        delegate* unmanaged[Cdecl]<IntPtr, void> callback,
        IntPtr user);

    /// <summary>Runs a schedule of the game's own, answering 1 where it ran.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_schedule_run(string schedule);

    /// <summary>Reads a slot's current value.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_get(int slot, int* value);

    /// <summary>Queues a transition of a slot.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_set(int slot, int value);

    // -- ECS, ambient, each needing an active world loan

    /// <summary>Splits an entity handle into its logical index and generation.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void bcs_entity_parts(ulong entity, uint* index, uint* generation);

    /// <summary>Spawns an empty entity, returning its handle, or 0 on failure.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_ecs_spawn();

    /// <summary>Despawns an entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_despawn(ulong entity);

    /// <summary>Reports whether an entity handle is still live.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_alive(ulong entity);

    /// <summary>Inserts or replaces a component from raw bytes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_insert(ulong entity, int component, void* data);

    /// <summary>Removes a component.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_remove(ulong entity, int component);

    /// <summary>Reports whether an entity carries a component.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_has(ulong entity, int component);

    /// <summary>Returns a writable pointer to a component, or null, marking it changed.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void* bcs_ecs_get_ptr(ulong entity, int component);

    /// <summary>Returns a pointer to a component for reading, or null, marking nothing.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void* bcs_ecs_read_ptr(ulong entity, int component);

    /// <summary>Reports whether a component changed since the previous frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_changed(ulong entity, int component);

    /// <summary>How many relationship slots the bridge provides.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_relation_slots();

    /// <summary>Makes an entity relate to another in a slot.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_relation_set(int slot, ulong source, ulong target);

    /// <summary>Takes an entity's relation in a slot away.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_relation_remove(int slot, ulong source);

    /// <summary>Writes what an entity relates to in a slot.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_relation_target(int slot, ulong source, ulong* target);

    /// <summary>Writes the entities relating to an entity in a slot, answering how many.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_relation_sources(int slot, ulong target, ulong* sources, int capacity);

    /// <summary>Writes when a component was added and last changed, and whether either was since the running system last ran.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_change_ticks(ulong entity, int component, NativeChangeTicks* ticks);

    /// <summary>Counts entities carrying a component.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_count(int component);

    /// <summary>Collects the storage runs matching a component and its filters.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_chunks(
        int component,
        int* with,
        int withLength,
        int* without,
        int withoutLength,
        int markChanged,
        NativeChunk* output,
        int capacity);

    /// <summary>Writes the address of this thread's row guard, which the behavior runner writes before each method and reads after.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_row_guard(NativeRowGuard** guard);

    // -- Hierarchy

    /// <summary>Makes one entity a child of another.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_set_parent(ulong child, ulong parent);

    /// <summary>Detaches an entity from its parent.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_clear_parent(ulong child);

    /// <summary>Returns an entity's parent, or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_ecs_parent_of(ulong entity);

    /// <summary>Writes an entity's children out and returns how many it has.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_children(ulong entity, ulong* output, int capacity);
}
