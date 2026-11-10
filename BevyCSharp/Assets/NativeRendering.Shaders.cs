using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class NativeRendering
{
    // -- Programs

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

    /// <summary>Reports which program a target runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_target_program(int kind, long id);

    /// <summary>Writes the names a target's program declares.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_target_names(int kind, long id, byte* buffer, int capacity);

    /// <summary>Whether the device can build ray scenes and trace rays through them.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_ray_queries_supported();

    /// <summary>Whether this device runs mesh shaders, one or zero.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_mesh_shaders_supported();

    /// <summary>How many sampled textures one stage of a shader may bind on this device.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_sampled_textures_per_stage();

    // -- Buffers and images

    /// <summary>Makes an image a compute shader writes, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_image_create(uint width, uint height, uint depth, int format, uint mips);

    /// <summary>Makes an image a compute shader writes, starting with the texels given.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_image_create_from(uint width, uint height, uint depth, int format, byte* texels, int length);

    /// <summary>Writes texels into a region of an image on the GPU.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_image_write(
        int image, uint x, uint y, uint z, uint width, uint height, uint depth, uint mip, byte* texels, int length);

    /// <summary>Makes a buffer bigger, keeping what it holds.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_buffer_grow(int buffer, int size);

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

    /// <summary>Makes an empty geometry pool, writing its three buffer keys.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_geometry_pool_create(int* keys);

    /// <summary>Adds a mesh to a geometry pool, answering its number there.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_geometry_pool_add(int pool, int mesh);

    // -- Instances and materials

    /// <summary>Makes a material drawn by a program, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_material_create(int program, int alpha, float cutoff, int cull, float depthBias);

    /// <summary>
    /// Makes a material a program draws a 2D mesh or a sprite with, returning its key, an
    /// <paramref name="alpha"/> of -1 leaving the alpha mode to the sprite.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_material_2d_create(int program, int alpha, float cutoff);

    /// <summary>Makes a material a program draws an interface node with, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_material_ui_create(int program);

    /// <summary>Changes a material's program, alpha, culling and depth bias.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_material_configure(int material, int program, int alpha, float cutoff, int cull, float depthBias);

    /// <summary>Makes a shader instance for a pass or a dispatch.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_instance_create(int program);

    /// <summary>Has a different program run an instance.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_instance_set_program(int instance, int program);

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

    /// <summary>Makes a buffer the engine fills with entities' transforms.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_instance_buffer_create(int capacity);

    /// <summary>Puts an entity in a slot of an instance buffer.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_instance_buffer_set(int buffer, int slot, ulong entity);

    /// <summary>Reports which program draws an entity's material.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_entity_program(ulong entity);

    /// <summary>Puts a ray scene under a name, or takes it off with a key of zero.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_set_ray_scene(int kind, long id, byte* name, int scene);

    /// <summary>Makes a buffer the engine fills with the materials of entities put in its slots.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_material_buffer_create(int capacity);

    /// <summary>The asset key of a material buffer's mask, the base color texture of a cut-out material its slots hold.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_material_buffer_mask(int buffer, int index);

    /// <summary>How many cut-out textures a material buffer's slots name past its masks.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_material_buffer_unmasked(int buffer);

    // -- Dispatches

    /// <summary>Runs an instance's compute shader once, this frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_dispatch(int instance, uint x, uint y, uint z);

    /// <summary>Runs an instance's compute shader with workgroup counts read from a buffer.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_dispatch_indirect(int instance, int buffer, uint offset);

    // -- Ray scenes

    /// <summary>Makes a ray scene over a geometry pool, answering its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_ray_scene_create(int pool, int capacity);

    /// <summary>Puts an entity made of a pool mesh in a slot of a ray scene, or empties the slot.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_ray_scene_set(int scene, int slot, ulong entity, int mesh);

    /// <summary>Builds a ray scene's pool mesh again from what the pool holds, or all with a negative mesh.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_ray_scene_rebuild(int scene, int mesh);

    // -- Views and passes

    /// <summary>Replaces the passes a camera runs over its picture.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_shader_passes(ulong camera, int* instances, int* afterTonemapping, int count);

    /// <summary>Gives a camera the images its shaders keep.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_view_images(ulong camera, NativeViewImage* images, int count);

    /// <summary>Replaces the dispatches a camera runs every frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_view_dispatches(ulong camera, NativeViewDispatch* dispatches, int count);

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

    /// <summary>Asks a camera to draw depth, normals or both before the scene.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_prepass(ulong camera, uint flags);

    /// <summary>Copies out the names a watch on a camera would find, one a line.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_drawn_view_image_names(ulong camera, byte* output, int capacity);
}
