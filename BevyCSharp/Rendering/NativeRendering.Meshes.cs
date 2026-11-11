using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class NativeRendering
{
    // -- Meshes

    /// <summary>Builds a mesh primitive and returns an asset key.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_create(string kind, float a, float b, float c);

    /// <summary>Builds a convex polygon pushed out into a solid and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_create_convex_extrusion(float* corners, int count, float depth);

    /// <summary>Builds a primitive again with new measures in place of an existing mesh.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_rebuild(int handle, string kind, float a, float b, float c);

    /// <summary>Reads a mesh's counts, attributes and bounds.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_mesh_info(int handle, NativeMeshInfo* info);

    /// <summary>Works out tangents for a mesh that has none, from its normals and texture coordinates.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_mesh_generate_tangents(int handle);

    /// <summary>Writes where an entity's mesh or material was loaded from.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_asset_path(
        ulong entity, int which, byte* buffer, int capacity);

    /// <summary>Writes vertices over a mesh.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_write(int handle, NativeMeshData* data);

    /// <summary>Builds a mesh from vertices and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_create_from(NativeMeshData* data);

    /// <summary>
    /// Reads the format and location a vertex stream's name was first given, for a refusal's
    /// message.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_stream_of(byte* name, int nameLength, int* format, uint* location);

    /// <summary>Says how an entity's mesh is treated beyond what it looks like.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_mesh_flags(ulong entity, uint flags);

    /// <summary>Attaches an asset through a component that carries a handle.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_insert_asset(ulong entity, string component, int handle);

    /// <summary>Compresses a mesh as Bevy's regular compression does, in place.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_compress_mesh(int mesh);

    /// <summary>Writes the names of a mesh's morph targets into a buffer, a line each.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static unsafe partial int bcs_render_morph_target_names(int mesh, byte* buffer, int capacity);

    /// <summary>Writes an entity's world-space bounds, min x, y, z then max x, y, z.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_bounds(ulong entity, float* bounds);

    /// <summary>Starts making a meshlet mesh from a mesh, answering its key at once.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_create_meshlet_mesh(int mesh, uint quantization, string? saveTo);

    /// <summary>Copies a mesh's positions and normals out, or answers how many vertices there are.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_mesh_normals(int mesh, float* positions, float* normals, int capacity, int* count);

    /// <summary>Copies a mesh's positions and triangle indices out, or answers their counts.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_mesh_triangles(
        int mesh, float* positions, int positionCapacity, uint* indices, int indexCapacity, int* counts);

    /// <summary>Gives a mesh the joints each vertex follows and their weights, four of each a vertex.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_set_joints(int mesh, ushort* joints, float* weights, int count);

    /// <summary>Makes a skin's inverse bindposes from transforms of ten floats each, writing its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_skin_create(float* transforms, int count, int* key);

    /// <summary>Skins an entity's mesh with a skin and the joint entities that move it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_skin_set(ulong entity, int skin, ulong* joints, int count);

    /// <summary>Sets the wind the trees' materials sway in.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_tree_wind(float x, float y, float z);

    // -- Materials

    /// <summary>Makes a material that colors each cluster of a meshlet mesh.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_cluster_material_create(uint seed);

    /// <summary>Builds a material and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_material_create(NativeMaterialConfig* config);

    /// <summary>Builds Bevy's forward decal material over a standard one and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_material_create_forward_decal(NativeMaterialConfig* config, float depthFadeFactor, float depthBias);

    /// <summary>Builds the bridge's leaf material over a standard one and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_material_create_leaf(NativeMaterialConfig* config, float trunk, float limb, float flutter, float frequency);

    /// <summary>Builds the bridge's bark material over a standard one and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_material_create_bark(NativeMaterialConfig* config, float trunk, float limb, float flutter, float frequency);

    /// <summary>Gives a material the share of global illumination's light that passes a unit of its depth.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_material_set_light_through(int material, float share);

    /// <summary>Reads the share of light a material lets pass a unit of its depth.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_material_light_through(int material, float* share);

    /// <summary>Gives a material the color the scene's distance field paints its base color times.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_material_set_field_tint(int material, float r, float g, float b);

    /// <summary>Builds the bridge's impostor material over a standard one, with its three atlases and the shape they picture, and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_material_create_impostor(NativeMaterialConfig* config, int colors, int normals, int faces, float middleX, float middleY, float middleZ, float extent, uint across, float trunk);

    /// <summary>Makes an entity a forward decal drawn with a forward decal material.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_forward_decal(ulong entity, int material);

    /// <summary>Writes over a standard material's settings in place.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_material_write(int handle, NativeMaterialConfig* config);

    /// <summary>Reads a standard material's settings back.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_material_read(int handle, NativeMaterialConfig* config);
}
