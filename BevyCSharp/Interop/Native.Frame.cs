using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class Native
{
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

    /// <summary>Gives an image that exists the sampler a config describes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_sampler(int key, NativeImageConfig* config);

    /// <summary>Starts loading an image with an explicit sampler.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_load_image(string path, NativeImageConfig* config);

    /// <summary>Builds an atlas layout over a grid of tiles and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_atlas_create(
        uint tileWidth, uint tileHeight, uint columns, uint rows,
        uint paddingX, uint paddingY, uint offsetX, uint offsetY);

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

    /// <summary>Writes what the bridge holds, as name and number pairs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_memory_describe(byte* buffer, int capacity);


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
    /// bridge reports the length, so one call covers anything short and only longer text pays for a
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
}
