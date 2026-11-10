using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class NativeRendering
{
    // -- Images

    /// <summary>Asks for an image to be treated as a cubemap once it has loaded.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_make_cubemap(int image);

    /// <summary>The compressed texture formats the device decodes, as Bevy's bits.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_compressed_formats();

    /// <summary>Makes a cubemap out of six images, one a face, returning its key at once.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_cubemap_from_faces(int* faces);

    /// <summary>Asks for an image to be cut into layers or slices once it has loaded.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_reshape_image(int image, int count, int volume);

    /// <summary>Reads an image's width, height and depth in texels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_image_size(int image, uint* size);

    /// <summary>Reads an image's size and texels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_image_pixels(int image, uint* size, byte* output, int capacity);

    /// <summary>Writes texels over an image.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_image_set_pixels(int image, byte* data, int length);

    /// <summary>Makes an image out of pixels the caller holds, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_create_image(
        byte* pixels, uint width, uint height, int srgb);

    /// <summary>Makes an image out of pixels the caller holds with its smaller levels laid after it, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_create_image_levels(
        byte* pixels, uint width, uint height, int srgb, uint levels);

    /// <summary>Gives an image that exists the sampler a config describes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_sampler(int key, NativeImageConfig* config);

    /// <summary>
    /// Has the GPU make an image's mipmaps from its top level each frame while <paramref name="on"/>
    /// is non-zero.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_mip_generation(int key, int on);

    /// <summary>Creates an empty image a camera can draw into.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_create_target(uint width, uint height, int format, uint layers);

    // -- Captures

    /// <summary>Writes what a camera drew to a PNG file.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_screenshot(string path, int target);

    /// <summary>Asks for a picture to be read back into memory.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_capture(int target);

    /// <summary>Reads a capture, and forgets it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_capture_read(
        int id, uint* width, uint* height, byte* buffer, int capacity);

    /// <summary>Reads a capture in the format it arrived in, and forgets it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_capture_read_raw(
        int id, uint* width, uint* height, int* format, byte* buffer, int capacity);

    /// <summary>Forgets a capture that will not be read.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_capture_release(int id);

    /// <summary>Writes what a window a game spawned shows to a PNG file.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_screenshot(string path, ulong window);

    // -- Errors and what was drawn

    /// <summary>Copies the last frame's render timings out as text.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_timings(byte* output, int capacity);

    /// <summary>Writes how many pipelines the renderer is still compiling.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_pipelines_waiting(uint* waiting);

    /// <summary>Writes which of Bevy's shaders the bridge mended, 1 for anisotropy under a normal prepass and 2 where that was refused.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_shader_mends(uint* mends);

    /// <summary>Says what an error the renderer reports does, 0 to quit, 1 to ignore, 2 to stop rendering and 3 to recover.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_error_policy(int policy);

    /// <summary>What an error the renderer reports does now.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_error_policy();

    /// <summary>How many errors the renderer has reported.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_error_count();

    /// <summary>Causes an error on the GPU: 1 out of memory, 2 validation, 3 a lost device, 4 an endless loop.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_cause_error(int cause);

    /// <summary>Starts or stops reading back how many meshes the opaque pass draws.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_count_drawn(int on);

    /// <summary>Reads the meshes the opaque pass drew, 1 where known and 0 while pending.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_drawn_meshes(uint* drawn);

    // -- Picking

    /// <summary>Sets picking's debug mode, as <see cref="PickingDebugMode"/> numbers it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_dev_tools_set_picking_debug(int mode);

    /// <summary>Reads picking's debug mode, as <see cref="PickingDebugMode"/> numbers it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_dev_tools_picking_debug(int* mode);

    /// <summary>Copies the scene entities clicked since the last call.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pick_events(ulong* buffer, int capacity);

    /// <summary>Casts a ray at the scene's meshes and writes the nearest it meets.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pick_ray(float* origin, float* direction, ulong* entity, float* point, float* normal, float* uv);

    /// <summary>Spawns a pointer of the game's own and writes its number.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pointer_spawn(ulong* number);

    /// <summary>Moves, presses or releases a pointer of the game's own on an image.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pointer_input(ulong number, int image, float x, float y, int action, int button);

    /// <summary>Locks a pointer to an entity, as Bevy's <c>PointerCaptureMap::capture</c>, with the hit it reports.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pointer_capture(
        int kind, ulong number, ulong entity, ulong camera, float depth, float* position, float* normal);

    /// <summary>Releases what a pointer was locked to, as Bevy's <c>PointerCaptureMap::release</c>.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pointer_release_capture(int kind, ulong number);
}
