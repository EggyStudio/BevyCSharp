using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class Native
{
    /// <summary>Converts four components in one of Bevy's color spaces to linear RGBA.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_color_from(int space, float* components, float* linear);

    /// <summary>Converts linear RGBA to four components in one of Bevy's color spaces.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_color_to(int space, float* linear, float* components);
}
