using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class Native
{
    // -- Renderable assets

    /// <summary>Builds a mesh primitive and returns an asset key.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_create(string kind, float a, float b, float c);

    /// <summary>Builds a primitive again with new measures in place of an existing mesh.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_rebuild(int handle, string kind, float a, float b, float c);

    /// <summary>Spawns a scene asset under a new entity, returning it or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_scene_spawn(int asset);

    /// <summary>Builds a 2D mesh's color material and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_color_material_create(NativeColorMaterial* config);

    /// <summary>Writes over a 2D mesh's color material in place.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_color_material_write(int handle, NativeColorMaterial* config);

    /// <summary>Builds a material and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_material_create(NativeMaterialConfig* config);

    /// <summary>Reads a mesh's counts, attributes and bounds.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_mesh_info(int handle, NativeMeshInfo* info);

    /// <summary>Reads a standard material's settings back.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_material_write(int handle, NativeMaterialConfig* config);

    /// <summary>Reads a standard material's settings back.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_material_read(int handle, NativeMaterialConfig* config);

    /// <summary>Writes where an entity's mesh or material was loaded from.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_asset_path(
        ulong entity, int which, byte* buffer, int capacity);

    /// <summary>Makes a program from the shaders named and returns its number.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_program_create(NativeShaderProgramConfig* config);

    /// <summary>Reports whether a program is compiling, ready or failed.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_program_state(int program);

    /// <summary>Reports how many times a program's shaders have been replaced.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_program_generation(int program);

    /// <summary>Writes what a program's compilers said.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_program_diagnostics(int program, byte* buffer, int capacity);

    /// <summary>Writes which files a program is made of.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_program_describe(int program, byte* buffer, int capacity);

    /// <summary>Reports how many programs the app has made.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_program_count();

    /// <summary>Compiles or reloads every stage of a program now.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_program_reload(int program);

    /// <summary>Reports whether there is a slangc to compile Slang with.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_slang_available();

    /// <summary>Says whether a validation error closes the app or is survived.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_keep_rendering_after_errors(int keep);

    /// <summary>Writes the last error the renderer reported.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_last_render_error(byte* buffer, int capacity);

    /// <summary>Writes why the last value was refused.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_last_error(byte* buffer, int capacity);

    /// <summary>Writes what a program's shaders declare.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_program_layout(int program, byte* buffer, int capacity);

    /// <summary>Makes a material drawn by a program, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_material_create(int program, int alpha, float cutoff, int cull, float depthBias);

    /// <summary>Changes a material's program, alpha, culling and depth bias.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_material_configure(int material, int program, int alpha, float cutoff, int cull, float depthBias);

    /// <summary>Reports which program a target runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_target_program(int kind, long id);

    /// <summary>Makes a shader instance for a pass or a dispatch.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_instance_create(int program);

    /// <summary>Has a different program run an instance.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_instance_set_program(int instance, int program);

    /// <summary>Runs an instance's compute shader once, this frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_dispatch(int instance, uint x, uint y, uint z);

    /// <summary>Replaces the passes a camera runs over its picture.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_shader_passes(ulong camera, int* instances, int* afterTonemapping, int count);

    /// <summary>Sets numbers under a name.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_set_numbers(int kind, long id, byte* name, int scalar, int components, byte* data, int count);

    /// <summary>Sets bytes under a name.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_set_bytes(int kind, long id, byte* name, byte* data, int length);

    /// <summary>Puts an image under a name.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_set_image(int kind, long id, byte* name, int index, int image, int mip);

    /// <summary>Puts a buffer under a name.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_set_buffer(int kind, long id, byte* name, int buffer);

    /// <summary>Sets how the sampler under a name reads.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_set_sampler(int kind, long id, byte* name, int index, NativeSamplerConfig* config);

    /// <summary>Takes the value under a name off a target.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_unset(int kind, long id, byte* name);

    /// <summary>Reads the numbers set under a name.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_get_numbers(int kind, long id, byte* name, byte* buffer, int capacity);

    /// <summary>Writes the names a target's program declares.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_target_names(int kind, long id, byte* buffer, int capacity);

    /// <summary>Makes an image a compute shader writes, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_image_create(uint width, uint height, uint depth, int format, uint mips);

    /// <summary>Runs an instance's compute shader with workgroup counts read from a buffer.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_dispatch_indirect(int instance, int buffer, uint offset);

    /// <summary>Gives a camera the images its shaders keep.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_view_images(ulong camera, NativeViewImage* images, int count);

    /// <summary>Replaces the dispatches a camera runs every frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_view_dispatches(ulong camera, NativeViewDispatch* dispatches, int count);

    /// <summary>Makes an image a compute shader writes, starting with the texels given.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_image_create_from(uint width, uint height, uint depth, int format, byte* texels, int length);

    /// <summary>Writes texels into a region of an image on the GPU.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_image_write(
        int image, uint x, uint y, uint z, uint width, uint height, uint depth, uint mip, byte* texels, int length);

    /// <summary>Sets the ambient light, everywhere or on one camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_ambient_light(ulong camera, float r, float g, float b, float brightness);

    /// <summary>Turns Bevy's screen-space ambient occlusion on or off for a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_ambient_occlusion(ulong camera, int quality, float thickness);

    /// <summary>Makes a buffer bigger, keeping what it holds.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_buffer_grow(int buffer, int size);

    /// <summary>Makes a buffer the engine fills with entities' transforms.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_instance_buffer_create(int capacity);

    /// <summary>Puts an entity in a slot of an instance buffer.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_instance_buffer_set(int buffer, int slot, ulong entity);

    /// <summary>Replaces the draws a camera makes every frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_view_draws(ulong camera, NativeViewDraw* draws, int count);

    /// <summary>Starts drawing one of a camera's images into an image every frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_watch_view_image(ulong camera, byte* name, uint width, uint height, float scale, float offset);

    /// <summary>Stops watching one of a camera's images.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_unwatch_view_image(ulong camera, byte* name);

    /// <summary>Draws Bevy's own materials deferred or forward.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_deferred(int on);

    /// <summary>Draws contact shadows on a camera, or with zero steps stops.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_contact_shadows(ulong camera, uint steps, float thickness, float length);

    /// <summary>Turns screen-space reflections on or off for a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_screen_space_reflections(ulong camera, NativeReflectionConfig* config);

    /// <summary>Asks for an image to be treated as a cubemap once it has loaded.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_make_cubemap(int image);

    /// <summary>Makes a cubemap out of six images, one a face, returning its key at once.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_cubemap_from_faces(int* faces);

    /// <summary>Asks for an image to be cut into layers or slices once it has loaded.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_reshape_image(int image, int count, int volume);

    /// <summary>Makes a buffer shaders can read and write, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_buffer_create(byte* bytes, int length, int size);

    /// <summary>Replaces a buffer's contents.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_buffer_write(int buffer, byte* bytes, int length);

    /// <summary>Reports a buffer's size in bytes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_buffer_size(int buffer);

    /// <summary>Starts copying a buffer back from the GPU.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_buffer_read(int buffer);

    /// <summary>Starts copying an image back from the GPU.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_image_read(int image);

    /// <summary>Takes the bytes a read brought back.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_buffer_take(int ticket, byte* buffer, int capacity);

    /// <summary>Writes vertices over a mesh.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_write(int handle, NativeMeshData* data);

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

    /// <summary>Builds a mesh from vertices and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_create_from(NativeMeshData* data);

    /// <summary>Says how an entity's mesh is treated beyond what it looks like.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_mesh_flags(ulong entity, uint flags);

    /// <summary>Asks a camera to draw depth, normals or both before the scene.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_prepass(ulong camera, uint flags);

    /// <summary>Reports which program draws an entity's material.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_entity_program(ulong entity);

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

    /// <summary>Makes an image out of pixels the caller holds, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_create_image(
        byte* pixels, uint width, uint height, int srgb);

    /// <summary>Sets how a directional light divides its shadows across the distance.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_shadow_cascades(
        ulong light, int cascades, float minimum, float maximum, float firstBound, float overlap);

    /// <summary>Shapes a spot light's beam with a picture.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_light_cookie(ulong light, int image);

    /// <summary>Softens a light's shadows as a light of a size, or hardens them with zero.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_soft_shadows(ulong light, float size);

    /// <summary>Sets how a camera filters the shadow maps it reads.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_shadow_filtering(ulong camera, int method);

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

    /// <summary>Collects what the input method said since the last call, and reports how many.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ime_drain();

    /// <summary>Reads one drained input method message: its kind, its caret and its text.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ime_read(int index, int* kind, int* caret, byte* output, int capacity);

    /// <summary>Turns the input method on or off, and places its candidate list.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_set_ime(int enabled, float x, float y);

    /// <summary>Says something as the input method would.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_input_ime(int kind, byte* text, uint length, int start, int end);

    /// <summary>Moves up to <paramref name="capacity"/> world instances Bevy reported ready into a buffer.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_world_instances_ready(ulong* entities, int capacity);

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

    /// <summary>Writes one drained drop's path, returning its length in bytes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_file_drop_path(int index, int* kind, byte* buffer, int capacity);

    /// <summary>Plays a sound, returning the entity playing it or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_audio_play(int clip, NativeAudioConfig* config);

    /// <summary>Reads a playing sound's volume and whether it is paused.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_state(ulong entity, float* volume, int* paused);

    /// <summary>Sets a playing sound's volume and pause state.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_control(ulong entity, float volume, int paused);

    /// <summary>Stops a sound and despawns its entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_stop(ulong entity);

    /// <summary>Makes an entity the ear, with each ear placed exactly.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_listener_ears(ulong entity, float* left, float* right);

    /// <summary>Makes an entity the ear spatial sound is heard from.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_listener(ulong entity, float gap);

    /// <summary>Writes how far into its clip a sound has played, in seconds.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_position(ulong entity, float* seconds);

    /// <summary>Moves playback to a point in the clip.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_seek(ulong entity, float seconds);

    /// <summary>Scales every sound at once.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_global_volume(float volume);

    /// <summary>Makes a UI node a text field.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_editable_text(ulong entity, NativeEditableTextConfig* config, string text, string? allowed);

    /// <summary>Writes what a text field holds.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_editable_text(ulong entity, byte* output, int capacity);

    /// <summary>Replaces what a text field holds.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_editable_value(ulong entity, string text);

    /// <summary>Makes one of Bevy's widgets keep its own state.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_widget_self_update(ulong entity, int kind);

    /// <summary>Records a run of gizmo text to draw this frame.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_text(string text, NativeGizmoText* settings);

    /// <summary>Records a debug shape to draw this frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_draw(NativeGizmoConfig* config);

    /// <summary>Records a whole array of shapes to draw this frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_draw_many(NativeGizmoConfig* configs, int count);

    /// <summary>Records a run of lines to draw this frame, all in front of the scene or all behind it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_lines(NativeGizmoSegment* segments, int count, int inFront);

    /// <summary>Sets how gizmos are drawn, for the groups named.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_configure(
        float width, uint layers, int enabled, int which);

    /// <summary>Sets what a gizmo line looks like, for the groups named.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_style(
        int style,
        float gapScale,
        float lineScale,
        int joint,
        uint jointResolution,
        int perspective,
        int which);

    /// <summary>Moves the named groups' gizmos toward the camera or away before depth testing.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_depth_bias(float bias, int which);

    /// <summary>Sets whether every light's gizmo is drawn and how they are colored.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_lights(int drawAll, int mode, float* color);

    /// <summary>Sets whether every bounding box is drawn and in what color.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_bounds(int drawAll, float* color);

    /// <summary>Starts keeping the gizmo shapes asked for rather than drawing them.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_record_begin();

    /// <summary>Makes the shapes kept since the recording began into a gizmo asset.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_record_end(int* handle);

    /// <summary>Spawns a 2D camera and returns its entity, or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_render_spawn_camera_2d(int order);

    /// <summary>Sets what a camera does to the picture after the scene is drawn.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_post(ulong entity, NativePostConfig* config);
}
