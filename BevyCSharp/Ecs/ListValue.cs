using System.Collections;

namespace Bevy;

/// <summary>
/// The items of a list field, as a schema reads them and takes them back.
/// </summary>
/// <remarks>
/// <para>
/// What <see cref="ComponentField.Read"/> returns for a <see cref="FieldKind.List"/> field and what
/// its <see cref="ComponentField.Write"/> takes: every item, boxed, in order. A copy rather than a
/// view, because the list it was read from lives in a component that the next write replaces.
/// </para>
/// <para>
/// Two values with the same items are equal. A tool that reads a field twice and asks whether it
/// changed compares the two reads, and two arrays would never be equal, so every frame would look
/// like an edit. Changing one is done by making another (<see cref="With"/>, <see cref="Adding"/>,
/// <see cref="Without"/>, <see cref="Moving"/>) and writing that back, so an edit is one write of
/// the whole list, recorded and undone as one step.
/// </para>
/// </remarks>
public sealed class ListValue : IReadOnlyList<object?>, IEquatable<ListValue>
{
    private readonly object?[] _items;

    private ListValue(object?[] items) => _items = items;

    /// <summary>Makes a value of the items given, in order.</summary>
    public ListValue(IEnumerable<object?> items) => _items = [.. items];

    /// <summary>No items.</summary>
    public static ListValue Empty { get; } = new([]);

    /// <summary>The items of a span, boxed, as a generated schema reads an inline list.</summary>
    public static ListValue From<T>(ReadOnlySpan<T> items)
    {
        var boxed = new object?[items.Length];
        for (var i = 0; i < items.Length; i++) boxed[i] = items[i];
        return new ListValue(boxed);
    }

    /// <summary>The items of a list, boxed, as a generated schema reads a stored list.</summary>
    public static ListValue From<T>(IReadOnlyList<T> items)
    {
        var boxed = new object?[items.Count];
        for (var i = 0; i < items.Count; i++) boxed[i] = items[i];
        return new ListValue(boxed);
    }

    /// <inheritdoc/>
    public int Count => _items.Length;

    /// <inheritdoc/>
    public object? this[int index] => _items[index];

    /// <summary>The same items with the one at <paramref name="index"/> replaced.</summary>
    public ListValue With(int index, object? item)
    {
        var items = (object?[])_items.Clone();
        items[index] = item;
        return new ListValue(items);
    }

    /// <summary>The same items with one more at the end.</summary>
    public ListValue Adding(object? item) => new([.. _items, item]);

    /// <summary>The same items without the one at <paramref name="index"/>.</summary>
    public ListValue Without(int index) => new([.. _items[..index], .. _items[(index + 1)..]]);

    /// <summary>The same items with one moved from one place to another.</summary>
    public ListValue Moving(int from, int to)
    {
        var items = _items.ToList();
        var moved = items[from];
        items.RemoveAt(from);
        items.Insert(to, moved);
        return new ListValue([.. items]);
    }

    /// <inheritdoc/>
    public IEnumerator<object?> GetEnumerator() => ((IEnumerable<object?>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public bool Equals(ListValue? other) =>
        other is not null && _items.AsSpan().SequenceEqual(other._items, EqualityComparer<object?>.Default);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ListValue other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _items) hash.Add(item);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() =>
        "[" + string.Join(", ", _items.Select(item => item?.ToString() ?? "none")) + "]";
}
