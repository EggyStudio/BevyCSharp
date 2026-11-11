using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>The bridge's entry point for the scenes, the world instances Bevy reported ready.</summary>
internal static unsafe partial class NativeScenes
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeScenes() => NativeLoader.Initialize(typeof(NativeScenes).Assembly);

    /// <summary>Moves up to <paramref name="capacity"/> world instances Bevy reported ready into a buffer.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_world_instances_ready(ulong* entities, int capacity);
}
