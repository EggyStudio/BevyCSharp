using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for the asset server, loading, the state and failures of a load,
/// folders, paths and releases.
/// </summary>
internal static unsafe partial class NativeAssets
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeAssets() => NativeLoader.Initialize(typeof(NativeAssets).Assembly);

    /// <summary>Collects the assets that failed to load, and reports how many.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_failures_drain();

    /// <summary>Writes the path of one drained failure.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_failure_path(int index, byte* buffer, int capacity);

    /// <summary>Writes why one drained failure failed.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_failure_reason(int index, byte* buffer, int capacity);

    /// <summary>Writes what kind of asset one drained failure was, returning its length.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_failure_kind(int index, byte* buffer, int capacity);

    /// <summary>Starts loading an asset and returns the key the engine knows it by.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_load(string kind, string path);

    /// <summary>Starts loading an asset of a glTF file with its meshes compressed as they load.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_load_compressed(string kind, string path);

    /// <summary>Starts loading every asset in a folder.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_load_folder(string path);

    /// <summary>Writes the keys of the assets a loaded folder holds.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_folder_handles(int folder, int* keys, int capacity);

    /// <summary>Starts loading an image with an explicit sampler.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_load_image(string path, NativeImageConfig* config);

    /// <summary>Writes the path an asset was loaded from into a buffer.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static unsafe partial int bcs_asset_path(int handle, byte* buffer, int capacity);

    /// <summary>Reports how far along a load is.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_load_state(int handle);

    /// <summary>Reports how far along a load is, counting everything the asset depends on.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_load_state_with_dependencies(int handle);

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

    /// <summary>Marks a handle to be released once nothing but the table holds its asset.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_release_when_unused(int handle);

    /// <summary>Releases the marked handles whose assets went unused, answering how many wait to be taken.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_sweep();

    /// <summary>Writes the handles a sweep released that have not been taken, answering how many.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_take_released(int* keys, int capacity);
}
