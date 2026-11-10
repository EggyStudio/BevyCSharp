using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class NativeCore
{
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

    /// <summary>Reads a slot's current value.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_get(int slot, int* value);

    /// <summary>Queues a transition of a slot.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_state_set(int slot, int value);

    // -- Stepping

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
}
