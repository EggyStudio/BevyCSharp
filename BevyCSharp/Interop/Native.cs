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
    internal const int ExpectedAbiVersion = 205;

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

    /// <summary>
    /// Takes the oldest line Bevy logged at the error level and answers its length, or 0 when none
    /// is kept. A buffer too small for the line leaves it kept and answers the length it needs.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_log_take_error(byte* output, int capacity);

    /// <summary>Registers a C# system callback into a Bevy schedule.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_add_system(
        IntPtr app,
        int stage,
        delegate* unmanaged[Cdecl]<IntPtr, void> callback,
        IntPtr user);

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

    /// <summary>Reports where Bevy places each field of Transform.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_transform_layout(
        uint* size, uint* rotation, uint* translation, uint* scale);

    /// <summary>Reports where Bevy places each part of GlobalTransform.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_global_transform_layout(
        uint* size, uint* xAxis, uint* yAxis, uint* zAxis, uint* translation);

    /// <summary>Reports the size and variant numbering of Visibility. Render builds only.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_visibility_layout(
        uint* size, uint* inherited, uint* hidden, uint* visible);

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

    /// <summary>Reads a node's interaction: 0 none, 1 hovered, 2 pressed.</summary>
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
    internal static partial int bcs_window_set_title(string title);

    /// <summary>Resizes the primary window, in logical pixels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_size(uint width, uint height);

    /// <summary>Reads the primary window's size, in logical pixels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_size(uint* width, uint* height);

    /// <summary>Writes the primary window's entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_entity(ulong* entity);

    /// <summary>Switches between windowed and borderless fullscreen.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_mode(int mode);

    /// <summary>Moves the window, in physical pixels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_position(int x, int y);

    /// <summary>Sets decorations, resizability and always-on-top together.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_style(int decorations, int resizable, int alwaysOnTop);

    /// <summary>Sets the shape of the pointer over the window.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_cursor_shape(int shape);

    /// <summary>Minimizes the window.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_minimize();

    /// <summary>Maximizes the window, or puts it back.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_maximized(int maximized);

    /// <summary>Writes where the window is, how large, and whether it is maximized.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_place(NativeWindowPlace* place);

    /// <summary>Hands the window to the platform to be moved by the pointer.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_start_drag_move();

    /// <summary>Hands the window to the platform to be resized from an edge.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_start_drag_resize(int edge);

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
    internal static partial int bcs_window_set_video_mode(int monitor, int mode);

    /// <summary>Describes one monitor by index.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_monitor_info(int index, NativeMonitor* monitor);

    /// <summary>Sets whether the cursor is confined or hidden.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_cursor(int grab, int visible);

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

    /// <summary>Reads a slot's current value.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_get(int slot, int* value);

    /// <summary>Queues a transition of a slot.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_set(int slot, int value);

    // -- ECS (ambient: requires an active world loan)

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

    /// <summary>Returns a writable pointer to a component, or null.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void* bcs_ecs_get_ptr(ulong entity, int component);

    /// <summary>Reports whether a component changed since the previous frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_changed(ulong entity, int component);

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
