using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// The entries of a map field, as a schema reads them and takes them back.
/// </summary>
/// <remarks>
/// The counterpart of <see cref="ListValue"/> for a map. It holds every entry, boxed, in order, is
/// equal to another with the same entries in the same order, and is changed by making another and
/// writing it back whole.
/// Kept in order rather than as a dictionary, so the rows an inspector draws stay where they are
/// while one is being typed into.
/// </remarks>
public sealed class MapValue : IReadOnlyList<KeyValuePair<object, object?>>, IEquatable<MapValue>
{
    private readonly KeyValuePair<object, object?>[] _entries;

    /// <summary>Makes a value of the entries given, in order.</summary>
    public MapValue(IEnumerable<KeyValuePair<object, object?>> entries) => _entries = [.. entries];

    /// <summary>No entries.</summary>
    public static MapValue Empty { get; } = new([]);

    /// <summary>The entries of a dictionary, boxed, as a generated schema reads a stored map.</summary>
    public static MapValue From<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> entries) where TKey : notnull =>
        new(entries.Select(entry => new KeyValuePair<object, object?>(entry.Key, entry.Value)));

    /// <inheritdoc/>
    public int Count => _entries.Length;

    /// <inheritdoc/>
    public KeyValuePair<object, object?> this[int index] => _entries[index];

    /// <summary>The same entries with the one at <paramref name="index"/> given another key.</summary>
    public MapValue Rekeyed(int index, object key)
    {
        var entries = (KeyValuePair<object, object?>[])_entries.Clone();
        entries[index] = new KeyValuePair<object, object?>(key, entries[index].Value);
        return new MapValue(entries);
    }

    /// <summary>The same entries with the one at <paramref name="index"/> holding another value.</summary>
    public MapValue With(int index, object? value)
    {
        var entries = (KeyValuePair<object, object?>[])_entries.Clone();
        entries[index] = new KeyValuePair<object, object?>(entries[index].Key, value);
        return new MapValue(entries);
    }

    /// <summary>The same entries with one more at the end.</summary>
    public MapValue Adding(object key, object? value) => new([.. _entries, new(key, value)]);

    /// <summary>The same entries without the one at <paramref name="index"/>.</summary>
    public MapValue Without(int index) => new([.. _entries[..index], .. _entries[(index + 1)..]]);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<object, object?>> GetEnumerator() =>
        ((IEnumerable<KeyValuePair<object, object?>>)_entries).GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public bool Equals(MapValue? other) =>
        other is not null
        && _entries.Length == other._entries.Length
        && _entries.Zip(other._entries).All(pair =>
            Equals(pair.First.Key, pair.Second.Key) && Equals(pair.First.Value, pair.Second.Value));

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MapValue other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var entry in _entries)
        {
            hash.Add(entry.Key);
            hash.Add(entry.Value);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() =>
        "{" + string.Join(", ", _entries.Select(entry => $"{entry.Key}: {entry.Value ?? "none"}")) + "}";
}
