using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for global illumination, the meshes and lights it reads from the
/// frame, the scene's distance field's pool and the probes the leaf materials read.
/// </summary>
internal static unsafe partial class NativeGlobalIllumination
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeGlobalIllumination() => NativeLoader.Initialize();

    /// <summary>Writes the meshes in view and their triangles, the lights by kind and the camera the view is drawn by.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_scene_counts(NativeSceneCounts* counts);

    /// <summary>Writes the meshes drawn that cast shadows, as many as there is room for, how many there are, and the camera the scene's distance field lies around.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_scene_field_gather(NativeFieldDraw* draws, int capacity, NativeFieldEye* eye, int* count);

    /// <summary>Pools the meshes named, a number and a generation each, writing where each one's triangles start and how many, and answers the pool's buffer key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_scene_field_pool(uint* meshes, int count, uint* placed);

    /// <summary>Writes the bytes of the scene's distance field's pool of triangles.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_scene_field_pool_bytes(long* bytes);

    /// <summary>Writes the sun, the light from all around and the point and spot lights nearest the camera, which global illumination's probes are lit by.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gi_lights(NativeGiLights* lights);

    /// <summary>Hands the leaf materials global illumination's probes by their images' and buffers' keys, or turns their reading off where the faces' key is none.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_forward_probes(int cubes, int reach, int ways, int field, int lights, int air, float probes, float spacing, float cascades);

    /// <summary>A camera's viewport's size in physical pixels, its width then its height.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_viewport_size(ulong camera, uint* size);
}
