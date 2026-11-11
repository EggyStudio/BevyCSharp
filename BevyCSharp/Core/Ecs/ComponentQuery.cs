using System.Buffers;
using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

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
