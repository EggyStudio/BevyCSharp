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

/// <summary>One entity and a writable reference to its component.</summary>
/// <typeparam name="T">The component type.</typeparam>
public readonly ref struct ComponentRow<T> where T : unmanaged
{
    private readonly ref T _component;

    /// <summary>The entity this row belongs to.</summary>
    public readonly Entity Entity;

    internal ComponentRow(Entity entity, ref T component)
    {
        Entity = entity;
        _component = ref component;
    }

    /// <summary>The component, by reference. Assigning to it writes into Bevy's storage.</summary>
    public ref T Component
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _component;
    }
}

/// <summary>
/// A <c>foreach</c>-able view over every entity carrying <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <remarks>
/// Iteration walks chunk by chunk and yields references rather than copies, so a loop body
/// that mutates <see cref="ComponentRow{T}.Component"/> updates the world directly. The
/// enumerator disposes the underlying <see cref="ChunkSet{T}"/> when iteration finishes, which
/// <c>foreach</c> does automatically for a ref struct enumerator.
/// </remarks>
public readonly ref struct ComponentQuery<T> where T : unmanaged
{
    private readonly ChunkSet<T> _chunks;

    internal ComponentQuery(ChunkSet<T> chunks) => _chunks = chunks;

    /// <summary>Number of matching entities.</summary>
    public int Count => _chunks.TotalLength;

    /// <summary>True when nothing matched.</summary>
    public bool IsEmpty => _chunks.IsEmpty;

    /// <summary>Returns the enumerator.</summary>
    public Enumerator GetEnumerator() => new(_chunks);

    /// <summary>Walks the chunks of a <see cref="ComponentQuery{T}"/>.</summary>
    public ref struct Enumerator : IDisposable
    {
        private readonly ChunkSet<T> _chunks;
        private int _chunkIndex;
        private int _rowIndex;
        private Span<T> _components;
        private ReadOnlySpan<Entity> _entities;

        internal Enumerator(ChunkSet<T> chunks)
        {
            _chunks = chunks;
            _chunkIndex = -1;
            _rowIndex = -1;
            _components = default;
            _entities = default;
        }

        /// <summary>The current entity and its component reference.</summary>
        public ComponentRow<T> Current => new(_entities[_rowIndex], ref _components[_rowIndex]);

        /// <summary>Advances to the next entity, crossing chunk boundaries as needed.</summary>
        public bool MoveNext()
        {
            while (true)
            {
                if (++_rowIndex < _components.Length) return true;

                if (++_chunkIndex >= _chunks.Count) return false;

                var chunk = _chunks[_chunkIndex];
                _components = chunk.Components<T>();
                _entities = chunk.Entities;
                _rowIndex = -1;
            }
        }

        /// <summary>Returns the pooled chunk buffer.</summary>
        public void Dispose() => _chunks.Dispose();
    }
}
