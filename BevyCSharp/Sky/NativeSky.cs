using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for the sky and the weather, the picture over a directional light, the
/// roofs' camera and the cube the sky's lighting is filtered from.
/// </summary>
internal static unsafe partial class NativeSky
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeSky() => NativeLoader.Initialize(typeof(NativeSky).Assembly);

    /// <summary>Lays a picture over a directional light across a square of the world about a point, or with an image under zero takes it off.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_directional_cookie(ulong light, int image, float half, float x, float y, float z);

    /// <summary>
    /// The cube a camera's sky lighting is filtered from, which Bevy's atmosphere draws after the
    /// prepass, as an asset key, or <c>NOT_PRESENT</c> where it has none yet.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_sky_lighting_cube(ulong camera);

    /// <summary>Makes a camera one of the rain's roofs' cameras, drawing their layer alone.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_roof_camera(ulong camera, uint mask);
}
