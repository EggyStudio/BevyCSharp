using System.Buffers;
using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// The storage runs matching a query, borrowed from Bevy for the length of a system call.
/// </summary>
/// <typeparam name="T">The component the chunks hold.</typeparam>
/// <remarks>
/// Dispose it (a <c>using</c> is enough) to return the pooled backing array. The chunks
/// themselves point into Bevy's memory and are invalidated by any structural change.
/// </remarks>
public readonly struct ChunkSet<T> : IDisposable, IEquatable<ChunkSet<T>> where T : unmanaged
{
    private readonly NativeChunk[]? _buffer;

    /// <summary>Number of chunks in the result.</summary>
    public int Count { get; }

    internal ChunkSet(NativeChunk[] buffer, int count)
    {
        _buffer = buffer;
        Count = count;
    }

    /// <summary>The chunk at <paramref name="index"/>.</summary>
    public NativeChunk this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _buffer![index];
        }
    }

    /// <summary>Total number of entities across every chunk.</summary>
    public int TotalLength
    {
        get
        {
            var total = 0;
            for (var i = 0; i < Count; i++) total += _buffer![i].Length;
            return total;
        }
    }

    /// <summary>True when the query matched nothing.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>Returns the pooled buffer.</summary>
    public void Dispose()
    {
        if (_buffer is not null) ArrayPool<NativeChunk>.Shared.Return(_buffer);
    }

    /// <inheritdoc/>
    public bool Equals(ChunkSet<T> other) =>
        ReferenceEquals(_buffer, other._buffer) && Count == other.Count;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ChunkSet<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_buffer, Count);

    /// <summary>Compares two chunk sets.</summary>
    public static bool operator ==(ChunkSet<T> left, ChunkSet<T> right) => left.Equals(right);

    /// <summary>Compares two chunk sets.</summary>
    public static bool operator !=(ChunkSet<T> left, ChunkSet<T> right) => !left.Equals(right);
}
