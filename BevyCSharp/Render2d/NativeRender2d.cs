using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for 2D, the 2D camera, sprites and their atlases, color materials and
/// tilemaps.
/// </summary>
internal static unsafe partial class NativeRender2d
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeRender2d() => NativeLoader.Initialize(typeof(NativeRender2d).Assembly);

    /// <summary>Makes an entity a tilemap chunk with its tiles.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_tilemap_insert(ulong entity, NativeTilemapChunk* chunk, NativeTile* tiles, int count);

    /// <summary>Writes over a run of a chunk's tiles.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_tilemap_write(ulong entity, int start, NativeTile* tiles, int count);

    /// <summary>Reads a run of a chunk's tiles.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_tilemap_read(ulong entity, int start, NativeTile* tiles, int count);

    /// <summary>Spawns a 2D camera and returns its entity, or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_render_spawn_camera_2d(int order);

    /// <summary>Builds an atlas layout over a grid of tiles and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_atlas_create(
        uint tileWidth, uint tileHeight, uint columns, uint rows,
        uint paddingX, uint paddingY, uint offsetX, uint offsetY);

    /// <summary>Packs images into one atlas, writing the packed image, its layout and each image's frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static unsafe partial int bcs_atlas_build(
        int* images, int count, uint paddingX, uint paddingY, int* image, int* layout, int* indices);

    /// <summary>Attaches a sprite to an entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_sprite(ulong entity, NativeSpriteConfig* config);

    /// <summary>Moves each sprite or sprite mesh to the atlas frame beside it, answering how many it moved.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_sprite_frames(ulong* entities, uint* frames, int count);

    /// <summary>Builds a 2D mesh's color material and returns an asset key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_color_material_create(NativeColorMaterial* config);

    /// <summary>Writes over a 2D mesh's color material in place.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_color_material_write(int handle, NativeColorMaterial* config);
}
