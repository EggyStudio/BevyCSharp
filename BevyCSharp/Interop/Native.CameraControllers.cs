using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class Native
{
    /// <summary>Writes Bevy's default free camera settings.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_default(NativeFreeCamera* settings);

    /// <summary>Puts the free camera on a camera, or changes its settings.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_set(ulong camera, NativeFreeCamera* settings);

    /// <summary>Reads the free camera's settings off a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_get(ulong camera, NativeFreeCamera* settings);

    /// <summary>Takes the free camera off a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_remove(ulong camera);

    /// <summary>Reads what the free camera holds as it runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_state(ulong camera, NativeFreeCameraState* state);

    /// <summary>Writes what the free camera holds as it runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_set_state(ulong camera, NativeFreeCameraState* state);

    /// <summary>Writes Bevy's description of the free camera's controls.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_text(ulong camera, byte* buffer, int capacity);

    /// <summary>Writes Bevy's default pan camera settings.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_camera_default(NativePanCamera* settings);

    /// <summary>Puts the pan camera on a camera, or changes its settings.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_camera_set(ulong camera, NativePanCamera* settings);

    /// <summary>Reads the pan camera's settings off a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_camera_get(ulong camera, NativePanCamera* settings);

    /// <summary>Takes the pan camera off a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_camera_remove(ulong camera);

    /// <summary>Writes Bevy's description of the pan camera's controls.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_camera_text(ulong camera, byte* buffer, int capacity);

    /// <summary>Moves a pan-orbit camera to another projection over Bevy's dolly zoom.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_orbit_dolly_zoom(ulong camera, int orthographic);

    /// <summary>Turns a pan-orbit camera to face a direction over Bevy's look-to animation.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_orbit_look_to(ulong camera, float x, float y, float z);
}
