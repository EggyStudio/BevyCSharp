using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>A list of up to 32 items held inside a component's own bytes.</summary>
/// <typeparam name="T">What the list holds.</typeparam>
/// <remarks>
/// See <see cref="IInlineList{T}"/>. Adding past the capacity throws from
/// <see cref="Add"/> and is refused by <see cref="TryAdd"/>, rather than growing, because there is
/// nowhere in the component to grow into.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct InlineList32<T> : IInlineList<T>, IEquatable<InlineList32<T>> where T : unmanaged
{
    private int _count;
    private Buffer _items;

    /// <summary>How many items it can hold.</summary>
    public const int Size = 32;

    /// <inheritdoc/>
    public readonly int Count => _count;

    /// <inheritdoc/>
    public readonly int Capacity => Size;

    /// <summary>The items, in order.</summary>
    [UnscopedRef]
    public readonly ReadOnlySpan<T> Items => ((ReadOnlySpan<T>)_items)[.._count];

    /// <inheritdoc/>
    public readonly T ItemAt(int index) => Items[index];

    /// <summary>The items, in order, to write through.</summary>
    [UnscopedRef]
    public Span<T> AsSpan() => ((Span<T>)_items)[.._count];

    /// <summary>One item, by its place in the list.</summary>
    /// <exception cref="ArgumentOutOfRangeException">There is no item at that place.</exception>
    [UnscopedRef]
    public ref T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);
            return ref _items[index];
        }
    }

    /// <inheritdoc/>
    public bool TryAdd(T item)
    {
        if (_count == Size) return false;

        _items[_count++] = item;
        return true;
    }

    /// <summary>Adds an item at the end.</summary>
    /// <exception cref="InvalidOperationException">The list is full.</exception>
    public void Add(T item)
    {
        if (!TryAdd(item))
            throw new InvalidOperationException($"The list holds {Size} items and is full.");
    }

    /// <summary>Takes the item at a place out, moving the ones after it down.</summary>
    /// <exception cref="ArgumentOutOfRangeException">There is no item at that place.</exception>
    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);

        Span<T> items = _items;
        items[(index + 1).._count].CopyTo(items[index..]);

        // The freed slot is cleared, so two lists holding the same items are the same bytes, which
        // the component's equality and Bevy's change detection compare.
        items[--_count] = default;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        ((Span<T>)_items).Clear();
        _count = 0;
    }

    /// <summary>Walks the items in order.</summary>
    [UnscopedRef]
    public readonly ReadOnlySpan<T>.Enumerator GetEnumerator() => Items.GetEnumerator();

    /// <inheritdoc/>
    public readonly bool Equals(InlineList32<T> other) =>
        Items.SequenceEqual(other.Items, EqualityComparer<T>.Default);

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is InlineList32<T> other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Items) hash.Add(item);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public readonly override string ToString() => $"[{Count} of {Size}]";

    [InlineArray(32)]
    private struct Buffer
    {
        private T _element;
    }
}
