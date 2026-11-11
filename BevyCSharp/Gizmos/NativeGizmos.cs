using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for gizmos, what they draw, their style and text, and the bounds,
/// frusta and lights they show of the scene.
/// </summary>
internal static unsafe partial class NativeGizmos
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeGizmos() => NativeLoader.Initialize();

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

    /// <summary>Sets whether every frustum is drawn and in what color.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_frusta(int drawAll, float* color);

    /// <summary>Sets whether every skinned mesh's joint bounds are drawn and in what color.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_skinned_bounds(int drawAll, float* color);

    /// <summary>Starts keeping the gizmo shapes asked for rather than drawing them.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_record_begin();

    /// <summary>Makes the shapes kept since the recording began into a gizmo asset.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gizmo_record_end(int* handle);
}
