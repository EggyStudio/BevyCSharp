using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class Native
{
    /// <summary>Sets picking's debug mode, as <see cref="PickingDebugMode"/> numbers it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_dev_tools_set_picking_debug(int mode);

    /// <summary>Reads picking's debug mode, as <see cref="PickingDebugMode"/> numbers it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_dev_tools_picking_debug(int* mode);
}
