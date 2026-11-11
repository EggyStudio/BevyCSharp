using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class NativeCore
{
    // -- Bevy's components through its reflection

    /// <summary>Describes every reflected component and the types its fields reach, as JSON.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_types(byte* buffer, int capacity);

    /// <summary>
    /// Describes where the compiler put each field of a reflected type, read from a value made at
    /// its default, as JSON.
    /// </summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_layout(string typePath, byte* buffer, int capacity);

    /// <summary>Writes a component, or one field of it, as JSON.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_get(
        ulong entity, string typePath, string path, byte* buffer, int capacity);

    /// <summary>Writes the name of the variant an enum field holds.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_variant(
        ulong entity, string typePath, string path, byte* buffer, int capacity);

    /// <summary>Writes a value read from JSON over a component or one field of it.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_set(
        ulong entity, string typePath, string path, byte* json, int length);

    /// <summary>Switches an enum field to a variant, its fields at their defaults.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_set_variant(
        ulong entity, string typePath, string path, string variant);

    /// <summary>Inserts a component from JSON, or at its default when the length is zero.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_insert(
        ulong entity, string typePath, byte* json, int length);

    /// <summary>Removes a reflected component.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_remove(ulong entity, string typePath);

    /// <summary>Finds the entity holding one of Bevy's resources.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_resource_entity(string typePath, ulong* entity);

    /// <summary>Finds the entity Bevy keeps for one of its resources, holding it or not.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_resource_place(string typePath, ulong* entity);

    /// <summary>Lists the entities carrying a reflected component, answering how many there are.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_entities(string typePath, ulong* entities, int capacity);

    /// <summary>Whether a reflected type has a default to insert from nothing, as one or zero.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_has_default(string typePath);

    /// <summary>Reads an asset handle a component holds, as an asset key.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_get_asset(ulong entity, string typePath, string path);

    /// <summary>Points an asset handle a component holds at the asset behind a key.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_set_asset(
        ulong entity, string typePath, string path, int key);

    /// <summary>Reads a color field as four linear floats, whatever space it holds.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_get_color(
        ulong entity, string typePath, string path, float* rgba);

    /// <summary>Writes a color field from linear floats, in the space it already holds.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_set_color(
        ulong entity, string typePath, string path, float red, float green, float blue, float alpha);

    /// <summary>Reads how many items a list or an array a component holds has, or a negative status.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_list_len(ulong entity, string typePath, string path);

    /// <summary>
    /// Makes a list a component holds a length, taking items off its end or adding them at their
    /// default.
    /// </summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_list_resize(ulong entity, string typePath, string path, int length);

    /// <summary>Reads a float field, of either width, as a number.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_get_float(ulong entity, string typePath, string path, double* value);

    /// <summary>Writes a float field at the width it holds.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_set_float(ulong entity, string typePath, string path, double value);

    /// <summary>Reads a whole number or a flag field, of any width, as a number.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_get_integer(ulong entity, string typePath, string path, long* value);

    /// <summary>Writes a whole number or a flag field at the type it holds.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_set_integer(ulong entity, string typePath, string path, long value);

    /// <summary>Writes why the last reflected call on this thread failed.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_reflect_error(byte* buffer, int capacity);
}
