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
/// state, which is what steers parallel behaviour methods onto
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
    internal const int ExpectedAbiVersion = 1;

    static Native() => NativeLoader.Initialize();

    // -- Diagnostics -------------------------------------------------------------------

    /// <summary>Returns the ABI revision the loaded native library implements.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_abi_version();

    /// <summary>Returns 1 if the native library was built with the renderer compiled in.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_has_render();

    // -- App lifecycle -----------------------------------------------------------------

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

    // -- Component registration --------------------------------------------------------

    /// <summary>Registers a component layout before the app starts running.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_register(IntPtr app, string name, uint size, uint align);

    /// <summary>Registers a component layout from inside a running system.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_register_live(string name, uint size, uint align);

    // -- ECS (ambient: requires an active world loan) -----------------------------------

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

    // -- Frame state -------------------------------------------------------------------

    /// <summary>Copies this frame's time and input snapshot out of Bevy.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_frame_state(NativeFrameState* state);

    /// <summary>Throws if <paramref name="status"/> is a failure code.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Check(int status, string operation)
    {
        if (status < 0) NativeStatus.Throw(status, operation);
        return status;
    }
}
