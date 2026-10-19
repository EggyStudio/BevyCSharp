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

    /// <summary>Writes a number input's value of a kind.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_number_set(ulong entity, int kind, double value, long whole);

    /// <summary>Reads a number input's value and its kind.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_number_get(ulong entity, int* kind, double* value, long* whole);

    /// <summary>Bounds a number input's value, hard or soft.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_number_limit(ulong entity, int hard, int kind, double min, double max, long minWhole, long maxWhole);

    /// <summary>Has a number input show its value in units, by the units' id.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_number_units(ulong entity, byte* units, int length);

    /// <summary>Writes the node a control's contents go under.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_contents(ulong entity, ulong* contents);

    /// <summary>Replaces the theme with token colors alone, or with Bevy's dark theme for none.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_set_theme(byte* tokens, int length, float* colors, int count);
}
