using System.Text.Json;
using Bevy.Interop;

namespace Bevy;

/// <summary>One field of one of Bevy's types as it lies in memory.</summary>
/// <param name="Path">Its reflect path from the type's start.</param>
/// <param name="Offset">Its first byte, counted from the type's start.</param>
/// <param name="Size">How many bytes it takes.</param>
/// <param name="Align">The alignment it is placed at.</param>
/// <param name="Kind">
/// What it is to reflection, <c>struct</c>, <c>tuple_struct</c> or <c>tuple</c> for one walked
/// into, <c>enum</c> for one none of whose variants holds a value, <c>value</c> for one with no
/// fields, as a number is, and <c>variant</c>, <c>list</c>, <c>array</c>, <c>map</c> or <c>set</c>
/// for one whose bytes say nothing a mirror could read.
/// </param>
/// <param name="Variants">For an <c>enum</c>, the number each variant is stored as, by name.</param>
internal readonly record struct LayoutField(
    string Path, int Offset, int Size, int Align, string Kind, IReadOnlyDictionary<string, ulong>? Variants = null);

/// <summary>Where the compiler put each field of one of Bevy's types, as the bridge read it from a value.</summary>
/// <param name="Size">The type's size, as the world stores it.</param>
/// <param name="Align">The type's alignment.</param>
/// <param name="Kind">What the type itself is to reflection, as a field's <see cref="LayoutField.Kind"/> is.</param>
/// <param name="Fields">Every field reflection reaches, walked into the structs and tuples among them.</param>
/// <param name="Variants">For a type that is itself an <c>enum</c>, the number each variant is stored as, by name.</param>
internal sealed record ReflectedLayout(
    int Size, int Align, string Kind, IReadOnlyList<LayoutField> Fields, IReadOnlyDictionary<string, ulong>? Variants = null)
{
    /// <summary>
    /// The field at a reflect path, the type itself at the empty one, or nothing where reflection
    /// reaches none there.
    /// </summary>
    public LayoutField? Field(string path)
    {
        if (path.Length == 0) return new LayoutField(string.Empty, 0, Size, Align, Kind, Variants);

        foreach (var field in Fields)
        {
            if (field.Path == path) return field;
        }

        return null;
    }
}

/// <summary>
/// Reads the layout of Bevy's types from the bridge, and says whether a byte mirror could hold one.
/// </summary>
/// <remarks>
/// <para>
/// A byte mirror reads and writes a component in place, so it has to agree with Rust's layout byte
/// for byte, which Rust promises nothing about for its default representation. The bridge reads
/// the layout from a value of the type, each reflected field's address less the value's, so it is
/// the layout the compiler chose, read rather than written down when the bridge is built, private
/// fields included, where <c>offset_of!</c> reaches only the fields the bridge can name.
/// </para>
/// <para>
/// A type a mirror can hold is plain data whose every byte is a reflected field or the padding
/// alignment puts before the next one. One left out has no default to make a value of, holds an
/// enum whose variants hold values, whose place depends on the variant held, or has bytes no
/// reflected field covers, as a field Bevy marks for reflection to ignore leaves.
/// </para>
/// </remarks>
internal static class ReflectedLayouts
{
    /// <summary>
    /// Reads where the compiler put each field of a reflected type, or <see langword="null"/>
    /// outside a frame.
    /// </summary>
    /// <exception cref="BevyNativeException">The type is not registered, or has no default to measure a value of.</exception>
    internal static ReflectedLayout? Measure(string typePath) =>
        EcsWorld.DescribeLayout(typePath) is { } json ? Parse(json) : null;

    /// <summary>A layout from the JSON <c>bcs_reflect_layout</c> answers.</summary>
    internal static ReflectedLayout Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var fields = root.GetProperty("fields").EnumerateArray()
            .Select(field => new LayoutField(
                field.GetProperty("path").GetString()!,
                field.GetProperty("offset").GetInt32(),
                field.GetProperty("size").GetInt32(),
                field.GetProperty("align").GetInt32(),
                field.GetProperty("kind").GetString()!,
                Numbers(field)))
            .ToList();
        return new ReflectedLayout(
            root.GetProperty("size").GetInt32(),
            root.GetProperty("align").GetInt32(),
            root.GetProperty("kind").GetString()!,
            fields,
            Numbers(root));

        static IReadOnlyDictionary<string, ulong>? Numbers(JsonElement owner) =>
            owner.TryGetProperty("variants", out var variants)
                ? variants.EnumerateObject().ToDictionary(variant => variant.Name, variant => variant.Value.GetUInt64(), StringComparer.Ordinal)
                : null;
    }

    /// <summary>
    /// The layout of a type at the paths a mirror reads, as one line of text, or <see langword="null"/>
    /// where reflection reaches no field at one of them.
    /// </summary>
    /// <remarks>
    /// The schema dump writes it into the description as a mirror's layout, the generator reads
    /// each field's offset from it, and the bridge's reading is turned into it again and compared
    /// whole, at the mirror's first use and in a test of the suite, so the two agree only where every
    /// number does. It is the size and the alignment, then for each path its offset and its size,
    /// and for an enum the number each variant is stored as, in the order of those numbers:
    /// <c>48/16;.translation@16:12;.rotation@0:16;.scale@28:12</c>, or
    /// <c>1/1;@0:1{Inherited=0,Hidden=1,Visible=2}</c> for a type that is itself an enum.
    /// </remarks>
    internal static string? Signature(ReflectedLayout layout, IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(paths);

        var text = new System.Text.StringBuilder(FormattableString.Invariant($"{layout.Size}/{layout.Align}"));
        foreach (var path in paths)
        {
            if (layout.Field(path) is not { } field) return null;

            text.Append(FormattableString.Invariant($";{path}@{field.Offset}:{field.Size}"));
            if (field.Variants is { } variants)
                text.Append('{')
                    .AppendJoin(',', variants.OrderBy(v => v.Value).ThenBy(v => v.Key, StringComparer.Ordinal)
                        .Select(v => FormattableString.Invariant($"{v.Key}={v.Value}")))
                    .Append('}');
        }

        return text.ToString();
    }

    /// <summary>The reflect paths a <see cref="Signature"/> names, in its order.</summary>
    internal static IEnumerable<string> PathsOf(string signature) =>
        signature.Split(';').Skip(1).Select(entry => entry.Substring(0, entry.IndexOf('@', StringComparison.Ordinal)));

    /// <summary>
    /// Why a byte mirror could not hold a type of this layout, or <see langword="null"/> where every
    /// byte is a field or the padding its alignment needs.
    /// </summary>
    internal static string? Unmirrored(ReflectedLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        // A type that is one value, as Visibility is an enum none of whose variants holds one, is
        // read whole.
        switch (layout.Kind)
        {
            case "enum" or "value":
                return null;
            case not ("struct" or "tuple_struct" or "tuple"):
                return $"it is a {layout.Kind}, whose bytes a mirror cannot read";
        }

        foreach (var field in layout.Fields)
        {
            switch (field.Kind)
            {
                case "variant":
                    return $"'{field.Path}' is an enum whose variants hold values, which lie where the variant held puts them";
                case "list" or "array" or "map" or "set" or "other":
                    return $"'{field.Path}' is a {field.Kind}, whose bytes are not its items";
            }
        }

        return Uncovered(layout, string.Empty, 0, layout.Size, layout.Align);
    }

    /// <summary>
    /// Why the bytes of a struct or tuple at <paramref name="path"/> are not all its fields' or
    /// padding, or <see langword="null"/> where they are.
    /// </summary>
    /// <remarks>
    /// Its fields in the order they lie in, each starting where the one before it ends or after the
    /// padding its own alignment needs, each struct among them read the same way inside it, and the
    /// whole ending after the last at the padding its own alignment needs, as a vector of three
    /// floats a SIMD register holds ends four bytes after its last.
    /// </remarks>
    private static string? Uncovered(ReflectedLayout layout, string path, int start, int size, int align)
    {
        var fields = layout.Fields
            .Where(field => field.Size > 0 && IsFieldOf(field.Path, path))
            .OrderBy(field => field.Offset)
            .ToList();

        var end = start;
        foreach (var field in fields)
        {
            if (field.Offset < end) return $"'{field.Path}' overlaps the field before it";
            if (field.Offset != AlignUp(end, field.Align))
                return $"bytes {end} to {field.Offset} hold no reflected field, as one reflection ignores leaves";
            if (field.Kind is "struct" or "tuple_struct" or "tuple"
                && Uncovered(layout, field.Path, field.Offset, field.Size, field.Align) is { } why)
                return why;
            end = field.Offset + field.Size;
        }

        return AlignUp(end, align) == start + size
            ? null
            : $"bytes {end} to {start + size} hold no reflected field, as one reflection ignores leaves";
    }

    /// <summary>Whether a reflect path names a field directly inside the one at <paramref name="parent"/>.</summary>
    private static bool IsFieldOf(string path, string parent) =>
        path.Length > parent.Length + 1
        && path.StartsWith(parent + ".", StringComparison.Ordinal)
        && path.IndexOf('.', parent.Length + 1) < 0;

    /// <summary>A byte count rounded up to an alignment.</summary>
    private static int AlignUp(int at, int align) => align <= 1 ? at : (at + align - 1) / align * align;
}
