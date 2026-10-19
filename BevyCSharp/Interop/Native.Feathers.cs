using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class Native
{
    /// <summary>Spawns a Feathers control of a kind under a parent, with its props and caption.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_spawn(int kind, ulong parent, NativeFeathersProps* props, byte* text, int length, ulong* entity);

    /// <summary>Colors a node from the theme by a token.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_theme_color(ulong entity, int slot, byte* token, int length);
}
