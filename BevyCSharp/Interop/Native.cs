using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The raw C ABI exported by <c>bevy_csharp</c>, the Rust bridge that owns the Bevy app.
/// </summary>
/// <remarks>
/// <para>
/// Every entry point that touches the ECS is <em>ambient</em>: it takes no world handle and
/// instead operates on the world Bevy has loaned to the currently running system callback on
/// this thread. That is why they are only valid from inside a system, on the main thread.
/// Calls from anywhere else fail with <see cref="NativeStatus.NoWorld"/> rather than corrupting
/// state, which is what steers parallel behavior methods onto
/// <see cref="EcsCommands"/> instead.
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
    internal const int ExpectedAbiVersion = 31;

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

    /// <summary>Registers a C# system callback into a Bevy schedule.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_add_system(
        IntPtr app,
        int stage,
        delegate* unmanaged[Cdecl]<IntPtr, void> callback,
        IntPtr user);

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
        string text, NativeUiNodeConfig* config, float fontSize);

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

    /// <summary>Counts the monitors the platform reports.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_monitor_count();

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

    /// <summary>Creates a state machine in a slot, before the app runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_add(IntPtr app, int slot, int initial);

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


    // -- Renderable assets

    /// <summary>Builds a mesh primitive and returns an asset key.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_create(string kind, float a, float b, float c);

    /// <summary>Spawns a scene asset under a new entity, returning it or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_scene_spawn(int asset);

    /// <summary>Builds a material and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_material_create(NativeMaterialConfig* config);

    /// <summary>Attaches an asset through a component that carries a handle.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_insert_asset(ulong entity, string component, int handle);

    /// <summary>Spawns a 3D camera and returns its entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_render_spawn_camera_3d(NativeCameraConfig* config);

    /// <summary>Spawns a light and returns its entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_render_spawn_light(NativeLightConfig* config);

    /// <summary>Drains what the window has reported since the last call.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_events(NativeWindowEvent* events, int capacity);

    /// <summary>Writes a monitor's name, returning its length in bytes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_monitor_name(int index, byte* buffer, int capacity);

    /// <summary>Collects the files dropped since the last call, returning how many.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_file_drops_drain();

    /// <summary>Writes one drained drop's path, returning its length in bytes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_file_drop_path(int index, int* kind, byte* buffer, int capacity);

    /// <summary>Plays a sound, returning the entity playing it or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_audio_play(int clip, NativeAudioConfig* config);

    /// <summary>Sets a playing sound's volume and pause state.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_control(ulong entity, float volume, int paused);

    /// <summary>Stops a sound and despawns its entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_stop(ulong entity);

    /// <summary>Records a debug shape to draw this frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_draw(NativeGizmoConfig* config);

    /// <summary>Spawns a 2D camera and returns its entity, or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_render_spawn_camera_2d(int order);

    /// <summary>Sets the shadow map size for each kind of light.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_shadow_maps(uint directional, uint point);

    /// <summary>Puts an entity on a set of render layers.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_layers(ulong entity, uint mask);

    /// <summary>Attaches a sprite to an entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_sprite(ulong entity, NativeSpriteConfig* config);


    // -- Renderer (render builds only)

    /// <summary>Describes the graphics adapter the renderer chose, as UTF-8.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_adapter(byte* buffer, int capacity);

    // -- Assets

    /// <summary>Starts loading an asset and returns the key the engine knows it by.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_load(string kind, string path);

    /// <summary>Starts loading an image with an explicit sampler.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_load_image(string path, NativeImageConfig* config);

    /// <summary>Reports how far along a load is.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_load_state(int handle);

    /// <summary>Reports whether the engine is still holding a handle.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_is_valid(int handle);

    /// <summary>Releases a handle.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_release(int handle);

    /// <summary>Counts the handles the engine is holding.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_live_count();


    // -- Frame state

    /// <summary>Copies this frame's time and input snapshot out of Bevy.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_frame_state(NativeFrameState* state);

    /// <summary>Writes text into a caller-owned buffer, returning its length in bytes.</summary>
    /// <returns>
    /// The length the text needs, whether or not it fitted, or a negative status code.
    /// </returns>
    internal delegate int TextWriter(byte* buffer, int capacity);

    /// <summary>
    /// Reads a string out of an entry point that follows the text convention.
    /// </summary>
    /// <remarks>
    /// Text cannot be handed back in a fixed struct field the way a number can, and returning a
    /// pointer would leave the question of who frees it. So the caller owns the buffer and the
    /// bridge reports the length: one call covers anything short, and only longer text pays for a
    /// second call against a buffer sized from the first answer.
    /// </remarks>
    internal static string ReadText(TextWriter write, string operation)
    {
        const int Probe = 256;

        byte* probe = stackalloc byte[Probe];
        var length = Check(write(probe, Probe), operation);

        if (length == 0) return string.Empty;
        if (length <= Probe) return System.Text.Encoding.UTF8.GetString(probe, length);

        var buffer = new byte[length];
        fixed (byte* target = buffer)
        {
            var written = Check(write(target, length), operation);
            return System.Text.Encoding.UTF8.GetString(target, Math.Min(written, length));
        }
    }

    /// <summary>Throws if <paramref name="status"/> is a failure code.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Check(int status, string operation)
    {
        if (status < 0) NativeStatus.Throw(status, operation);
        return status;
    }
}
