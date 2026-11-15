using System.Buffers;
using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// A <c>foreach</c>-able view over every entity carrying <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <remarks>
/// <para>
/// Iteration walks chunk by chunk and yields references rather than copies, so a loop body
/// that mutates <see cref="ComponentRow{T}.Component"/> updates the world directly. The
/// enumerator disposes the underlying <see cref="ChunkSet{T}"/> when iteration finishes, which
/// <c>foreach</c> does automatically for a ref struct enumerator.
/// </para>
/// <para>
/// Each walk takes the chunks as it starts, in a pooled buffer of its own that its enumerator
/// gives back, so one query walked twice gives back two buffers once each. One buffer shared by
/// both walks would go back to the pool twice, and the pool would then hand that array to two
/// later listings at once, one overwriting the other. A walk sees the entities there are as it
/// starts, and so does <see cref="Count"/>, as it is read.
/// </para>
/// </remarks>
public readonly ref struct ComponentQuery<T> where T : unmanaged
{
    private readonly EcsWorld? _world;
    private readonly bool _markChanged;

    internal ComponentQuery(EcsWorld world, bool markChanged)
    {
        _world = world;
        _markChanged = markChanged;
    }

    /// <summary>Number of matching entities.</summary>
    public int Count
    {
        get
        {
            using var chunks = Chunks(markChanged: false);
            return chunks.TotalLength;
        }
    }

    /// <summary>True when nothing matched.</summary>
    public bool IsEmpty
    {
        get
        {
            using var chunks = Chunks(markChanged: false);
            return chunks.IsEmpty;
        }
    }

    /// <summary>Returns the enumerator.</summary>
    public Enumerator GetEnumerator() => new(Chunks(_markChanged));

    /// <summary>The chunks matching as this is called, or none for a query made by no world.</summary>
    private ChunkSet<T> Chunks(bool markChanged) => _world?.Chunks<T>(markChanged: markChanged) ?? default;

    /// <summary>Walks the chunks of a <see cref="ComponentQuery{T}"/>.</summary>
    public ref struct Enumerator : IDisposable
    {
        private ChunkSet<T> _chunks;
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

        /// <summary>Returns the pooled chunk buffer, once however often it is called.</summary>
        public void Dispose()
        {
            _chunks.Dispose();
            _chunks = default;
        }
    }
}
