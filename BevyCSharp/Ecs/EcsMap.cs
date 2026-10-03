using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// A dictionary a component holds a handle to, kept in the same managed store as
/// <see cref="EcsList{T}"/>.
/// </summary>
/// <typeparam name="TKey">What the entries are found by.</typeparam>
/// <typeparam name="TValue">What they hold, which may be a string or any other managed type.</typeparam>
/// <remarks>
/// Everything <see cref="EcsList{T}"/> says holds here: two numbers in the component, the
/// dictionary made on its first write, copies of the handle naming the same dictionary, freed by
/// the remove hook the generator registers when the component leaves its entity, and a handle used
/// after that throwing rather than reading another entity's entries.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct EcsMap<TKey, TValue> : IEquatable<EcsMap<TKey, TValue>> where TKey : notnull
{
    private int _slot;
    private int _generation;

    /// <summary>Makes an empty dictionary now, rather than when its first entry is written.</summary>
    public static EcsMap<TKey, TValue> New()
    {
        var (slot, generation) = EcsStore.Allocate(new Dictionary<TKey, TValue>());
        return new EcsMap<TKey, TValue> { _slot = slot, _generation = generation };
    }

    /// <summary>Whether a dictionary has been made for this handle.</summary>
    public readonly bool IsCreated => _generation != 0;

    /// <summary>How many entries it holds, which is none for a dictionary never made.</summary>
    public readonly int Count => Existing?.Count ?? 0;

    /// <summary>The entries, which are none for a dictionary never made.</summary>
    public readonly IReadOnlyDictionary<TKey, TValue> Entries =>
        (IReadOnlyDictionary<TKey, TValue>?)Existing ?? EmptyEntries;

    /// <summary>The value under a key.</summary>
    /// <exception cref="KeyNotFoundException">There is no entry under that key.</exception>
    public readonly TValue this[TKey key] =>
        Existing is { } entries ? entries[key] : throw new KeyNotFoundException($"No entry under '{key}'.");

    /// <summary>Writes a value under a key, making the dictionary if this is its first entry.</summary>
    /// <remarks>
    /// As <see cref="EcsList{T}.Add"/>, making the dictionary writes the new handle into this value,
    /// so it is called on the component's own field for the component to keep it.
    /// </remarks>
    public void Set(TKey key, TValue value) => Made()[key] = value;

    /// <summary>Reads the value under a key, reporting whether there was one.</summary>
    public readonly bool TryGetValue(TKey key, out TValue value)
    {
        if (Existing is { } entries && entries.TryGetValue(key, out var found))
        {
            value = found;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>Whether there is an entry under a key.</summary>
    public readonly bool ContainsKey(TKey key) => Existing?.ContainsKey(key) ?? false;

    /// <summary>Takes the entry under a key out, reporting whether there was one.</summary>
    public readonly bool Remove(TKey key) => Existing?.Remove(key) ?? false;

    /// <summary>Empties the dictionary, keeping it.</summary>
    public readonly void Clear() => Existing?.Clear();

    /// <summary>
    /// A handle to a new dictionary holding the same entries, or a handle to nothing when this names
    /// none.
    /// </summary>
    /// <remarks>As <see cref="EcsList{T}.Copy"/>.</remarks>
    public readonly EcsMap<TKey, TValue> Copy()
    {
        if (Existing is not { } entries) return default;

        var (slot, generation) = EcsStore.Allocate(new Dictionary<TKey, TValue>(entries));
        return new EcsMap<TKey, TValue> { _slot = slot, _generation = generation };
    }

    /// <summary>Frees the dictionary, after which this handle and every copy of it name nothing.</summary>
    /// <returns>Whether there was a dictionary to free.</returns>
    public bool Free()
    {
        var freed = EcsStore.Release(_slot, _generation);
        _slot = 0;
        _generation = 0;
        return freed;
    }

    /// <summary>Walks the entries.</summary>
    public readonly IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Entries.GetEnumerator();

    private static readonly Dictionary<TKey, TValue> EmptyEntries = [];

    /// <summary>The dictionary, or nothing for one never made, throwing for one freed since.</summary>
    private readonly Dictionary<TKey, TValue>? Existing
    {
        get
        {
            if (_generation == 0) return null;

            return EcsStore.Get(_slot, _generation) as Dictionary<TKey, TValue>
                ?? throw new ObjectDisposedException(
                    nameof(EcsMap<TKey, TValue>),
                    "The dictionary was freed with the component that held it, and its slot may hold "
                    + "another one now.");
        }
    }

    /// <summary>The dictionary, made now if this handle has none yet.</summary>
    private Dictionary<TKey, TValue> Made()
    {
        if (Existing is { } entries) return entries;

        this = New();
        return Existing!;
    }

    /// <inheritdoc/>
    public readonly bool Equals(EcsMap<TKey, TValue> other) =>
        _slot == other._slot && _generation == other._generation;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is EcsMap<TKey, TValue> other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => HashCode.Combine(_slot, _generation);

    /// <inheritdoc/>
    public readonly override string ToString() => IsCreated ? $"{{{Count} entries}}" : "{}";
}

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
