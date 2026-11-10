using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class NativeCore
{
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

    /// <summary>
    /// Calls back with the copy of a component an entity clone is about to receive, so its handles
    /// can be replaced before it is written.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_on_clone(
        int component, delegate* unmanaged[Cdecl]<int, byte*, void> callback);

    /// <summary>Resolves one of Bevy's own components to an id, by name.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_id_of(string name);

    /// <summary>Reports the size and alignment Bevy uses for a component.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_layout(int component, uint* size, uint* align);

    /// <summary>Writes a component's name into a buffer, returning the bytes it needs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_component_name(int component, byte* buffer, int capacity);

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

    // -- ECS, ambient, each needing an active world loan

    /// <summary>Spawns a copy of an entity with everything on it, returning the copy or zero.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_ecs_clone(ulong entity);

    /// <summary>Runs a schedule of the game's own, answering 1 where it ran.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_schedule_run(string schedule);

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

    /// <summary>Spawns a scene asset under a new entity, returning it or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_scene_spawn(int asset);

    /// <summary>The entities whose component changed after a tick, and the tick it was read at.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_changed_since(int component, uint since, ulong* entities, int capacity, uint* now);

    /// <summary>The entities that lost a component since the running system last asked.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_removed(int component, ulong* entities, int capacity);

    /// <summary>For each of a list of entities, whether it carries a component, or changed it after a tick.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_has_many(int component, uint since, ulong* entities, int count, byte* answers);

    /// <summary>Copies every live entity into a buffer, returning how many exist.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_entities(ulong* buffer, int capacity);

    /// <summary>Copies the components an entity carries, returning how many it has.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_components_of(ulong entity, int* buffer, int capacity);

    /// <summary>Writes an entity's name into a buffer, returning the bytes it needs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_entity_name(ulong entity, byte* buffer, int capacity);

    /// <summary>Gives an entity a name, or removes it when the name is empty.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_set_entity_name(ulong entity, string name);

    // -- Relations

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

    // -- Observers

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

    /// <summary>Has the clips' events call back into C#, through the handle given.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_observe_clip_events(IntPtr app, delegate* unmanaged[Cdecl]<uint, ulong, IntPtr, void> callback, IntPtr user);
}
