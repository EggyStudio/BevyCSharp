using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

// The materials' entry points, the standard material's and the bridge's own built over it, and the
// 2D meshes' color material.
internal static unsafe partial class Native
{
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

    /// <summary>Sets the wind the trees' materials sway in.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_tree_wind(float x, float y, float z);

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
