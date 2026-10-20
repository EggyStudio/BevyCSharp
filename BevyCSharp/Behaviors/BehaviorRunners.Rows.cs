using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

public static partial class BehaviorRunners
{
    /// <summary>This thread's row guard, asked of the bridge the first time a walk runs on it.</summary>
    [ThreadStatic]
    private static unsafe NativeRowGuard* _guard;

    /// <summary>
    /// Where a walk on the calling thread stands, kept so that a call a method makes that moves
    /// the row it holds is noticed, what the method wrote is put where its entity went, and the
    /// walk goes on from the row after the last it passed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A method is handed a copy of each component it takes, and the copy is written back to the
    /// row once it returns. A call it makes that inserts a table component on its own entity, or
    /// takes one off, moves the entity to another table and the old table's last row into its
    /// place, and one that adds an entity to the table can grow the table's columns into new
    /// memory. A reference into the row would then write into another entity's component, as the
    /// foxes of many_foxes did when each started its run, or into memory the table let go of. The
    /// bridge marks the guard where a call left the component elsewhere, and the copy goes to the
    /// entity by its id instead, then the runs are listed again, since every pointer into them
    /// may have gone with the move.
    /// </para>
    /// <para>
    /// The walk goes on from the row after the entity passed last, which is where it was when the
    /// running entity is the one that moved, as a call changing the running entity does. The
    /// entity that moved is passed over for the rest of the walk, where it may stand in a table
    /// further on. A call changing other entities directly, which a method queues instead, leaves
    /// the memory sound and may pass one of them twice or not at all.
    /// </para>
    /// <para>
    /// Only the walk on the calling thread is kept. A method spread across the thread pool reaches
    /// no world on the pool's threads, every call there answering that it has none, so a method
    /// that calls into the world is marked <c>[MainThread]</c> and walked here, and a walk across
    /// the pool runs on its rows as they are. A method that throws leaves its components as they
    /// were before it ran, since its copy is not written back.
    /// </para>
    /// <para>
    /// The copy is the cost. A walk on the calling thread of a hundred thousand entities, its
    /// method one addition, took per entity 1.5 nanoseconds by reference and 4.9 copied for a
    /// sixteen byte component, 3.8 and 12.4 with a forty byte transform taken beside it, and 6.5
    /// and 14.4 for a component of a hundred and twenty eight bytes, at best of a run's last thirty
    /// frames, and 2.4 and 7.0, 6.4 and 16.0, and 9.0 and 18.1 at their middle. A walk across the
    /// thread pool, as one of four thousand and ninety six entities and more is, pays nothing.
    /// </para>
    /// </remarks>
    private unsafe struct Rows : IDisposable
    {
        private readonly NativeRowGuard _outer;
        private HashSet<Entity>? _moved;
        private int _stoppedAt;

        /// <summary>This thread's guard, which the walks write before each method and read after.</summary>
        public readonly NativeRowGuard* Guard;

        /// <summary>The entity whose row the walk passed last, where <see cref="HasPrevious"/>.</summary>
        public Entity Previous;

        /// <summary>Whether the walk has passed a row.</summary>
        public bool HasPrevious;

        /// <summary>The row the next run walked starts at, after a move.</summary>
        public int From;

        /// <summary>Keeps the guard for a walk of a component, saving what a walk around this one kept in it.</summary>
        public Rows(int component)
        {
            if (_guard is null)
            {
                NativeRowGuard* guard;
                Native.Check(Native.bcs_ecs_row_guard(&guard), "reading the row guard");
                _guard = guard;
            }

            Guard = _guard;
            _outer = *Guard;
            Guard->Component = component;
            Guard->Running = 0;
        }

        /// <summary>Whether an entity has moved in this walk, which leaves the rest of it on this thread.</summary>
        public readonly bool AnyMoved => _moved is not null;

        /// <summary>Whether an entity moved in this walk and is passed over.</summary>
        public readonly bool Skips(Entity entity) => _moved is not null && _moved.Contains(entity);

        /// <summary>
        /// Has the bridge look after each call from here until a move ends the walk of the run.
        /// </summary>
        /// <remarks>
        /// Between two methods the guard still names the entity the first ran on, and a call the
        /// runner makes there, to read whether a row changed, leaves that row where it was.
        /// </remarks>
        public readonly void Watch()
        {
            Guard->Moved = 0;
            Guard->Running = 1;
        }

        /// <summary>
        /// Notes the entity that moved, the row it was at and the entity passed before it, and
        /// stops the bridge looking while its copy is put where it went and the runs are listed again.
        /// </summary>
        public void Stop(Entity entity, int row, Entity previous, bool hasPrevious)
        {
            Guard->Running = 0;
            (_moved ??= []).Add(entity);
            _stoppedAt = row;
            (Previous, HasPrevious) = (previous, hasPrevious);
        }

        /// <summary>
        /// The run to go on in, in the runs listed after a move, with <see cref="From"/> the row
        /// after the entity passed last.
        /// </summary>
        public int Resume<T>(ChunkSet<T> chunks, int chunk) where T : unmanaged
        {
            From = 0;
            if (!HasPrevious) return 0;

            // Where it stands when only the running entity moved, the row before in the same run,
            // or the last of the run before where the move was at a run's first row.
            if (_stoppedAt > 0 && chunk < chunks.Count && Holds(chunks[chunk], _stoppedAt - 1))
                return After(chunks, chunk, _stoppedAt - 1);
            if (_stoppedAt == 0 && chunk > 0 && chunk - 1 < chunks.Count && Holds(chunks[chunk - 1], chunks[chunk - 1].Length - 1))
                return After(chunks, chunk - 1, chunks[chunk - 1].Length - 1);

            for (var index = 0; index < chunks.Count; index++)
            {
                var row = chunks[index].Entities.IndexOf(Previous);
                if (row >= 0) return After(chunks, index, row);
            }

            // The entity passed last has gone too, which a call changing other entities can do, so
            // the walk goes on where it stood.
            From = chunk < chunks.Count ? Math.Min(_stoppedAt, chunks[chunk].Length) : 0;
            return chunk;
        }

        /// <summary>Puts back what a walk around this one kept in the guard, where this one kept it.</summary>
        public readonly void Dispose()
        {
            if (Guard is not null) *Guard = _outer;
        }

        private readonly bool Holds(NativeChunk chunk, int row) =>
            row >= 0 && row < chunk.Length && chunk.EntityAt(row) == Previous;

        private int After<T>(ChunkSet<T> chunks, int chunk, int row) where T : unmanaged
        {
            if (row + 1 < chunks[chunk].Length)
            {
                From = row + 1;
                return chunk;
            }

            From = 0;
            return chunk + 1;
        }
    }

    /// <summary>Writes a method's copy of a component back to the row it was copied from.</summary>
    /// <remarks>
    /// Four bytes at a time up to sixty four, the width of the fields a method writes, since the
    /// copy is read back the moment the method returns, while its last writes may still be on their
    /// way to memory, and a read wider than a write still on its way waits for it to land. Read
    /// whole, a sixteen byte component took 11 nanoseconds an entity, and four bytes at a time 3.3,
    /// with nothing else of the walk around it. A larger component is read whole, where four bytes
    /// at a time cost more than the wait, 20 nanoseconds an entity for a hundred and twenty eight
    /// bytes where read whole it took 15.
    /// </remarks>
    private static void WriteBack<T>(ref T row, in T copy) where T : unmanaged
    {
        if (Unsafe.SizeOf<T>() <= 64 && Unsafe.SizeOf<T>() % 4 == 0)
        {
            ref var to = ref Unsafe.As<T, int>(ref row);
            ref var from = ref Unsafe.As<T, int>(ref Unsafe.AsRef(in copy));
            for (var k = 0; k < Unsafe.SizeOf<T>() / 4; k++) Unsafe.Add(ref to, k) = Unsafe.Add(ref from, k);
        }
        else
        {
            row = copy;
        }
    }

    /// <summary>Writes a method's copy of a component to its entity, where the entity still carries it.</summary>
    private static unsafe void Put<T>(Entity entity, in T value) where T : unmanaged
    {
        var pointer = Native.bcs_ecs_get_ptr(entity.Bits, ComponentType<T>.ChunkId);
        if (pointer is not null) Unsafe.WriteUnaligned(pointer, value);
    }
}
