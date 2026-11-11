using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Bevy;

public static partial class BehaviorRunners
{
    /// <summary>
    /// Runs <paramref name="body"/> for every entity carrying <typeparamref name="T"/> and
    /// <typeparamref name="T1"/>, handing it both by reference from the same storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a method such as <c>void Tick(BehaviorContext ctx, ref Transform transform)</c>, which
    /// reaches a second component of its entity without a call into the bridge an entity. Each
    /// component is listed by storage run under the same filters, which gives the same runs of
    /// the same tables in the same order, so the two are walked together, a call each for the
    /// whole system rather than one an entity. That they are the same rows is checked, by the
    /// entities each run starts at, rather than assumed.
    /// </para>
    /// <para>
    /// A component taken <c>in</c> rather than <c>ref</c> is read and not written, so it is not
    /// marked changed, and a system watching for changes to it does not see every entity change.
    /// </para>
    /// </remarks>
    public static void Run<T, T1>(
        World world,
        BehaviorRunner<T, T1> body,
        ReadOnlySpan<int> with = default,
        ReadOnlySpan<int> without = default,
        ReadOnlySpan<int> changed = default,
        bool writesFirst = true,
        int parallelThreshold = DefaultParallelThreshold)
        where T : unmanaged
        where T1 : unmanaged
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(body);

        RunTogether<T, T1, T1>(
            world,
            (ref T component, ref T1 first, ref T1 _, Entity entity, BehaviorContext context) =>
                body(ref component, ref first, entity, context),
            with, without, changed, [ComponentType<T1>.ChunkId], [writesFirst], parallelThreshold);
    }

    /// <summary>
    /// Runs <paramref name="body"/> for every entity carrying <typeparamref name="T"/>,
    /// <typeparamref name="T1"/> and <typeparamref name="T2"/>, handing it all three by reference
    /// from the same storage, as the two-component form does.
    /// </summary>
    public static void Run<T, T1, T2>(
        World world,
        BehaviorRunner<T, T1, T2> body,
        ReadOnlySpan<int> with = default,
        ReadOnlySpan<int> without = default,
        ReadOnlySpan<int> changed = default,
        bool writesFirst = true,
        bool writesSecond = true,
        int parallelThreshold = DefaultParallelThreshold)
        where T : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(body);

        RunTogether(
            world, body, with, without, changed,
            [ComponentType<T1>.ChunkId, ComponentType<T2>.ChunkId], [writesFirst, writesSecond], parallelThreshold);
    }

    /// <summary>
    /// The loop both forms share, over one or two other components, the second the first again
    /// when there is one.
    /// </summary>
    private static unsafe void RunTogether<T, T1, T2>(
        World world,
        BehaviorRunner<T, T1, T2> body,
        ReadOnlySpan<int> with,
        ReadOnlySpan<int> without,
        ReadOnlySpan<int> changed,
        int[] others,
        bool[] writes,
        int parallelThreshold)
        where T : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
    {
        var ecs = world.Resource<EcsWorld>();
        var own = ComponentType<T>.ChunkId;

        // Every one of the components required of each list, so each lists the same entities.
        var all = new int[with.Length + 1 + others.Length];
        with.CopyTo(all);
        all[with.Length] = own;
        others.CopyTo(all, with.Length + 1);

        // Left unmarked under a [Changed] filter and marked a row at a time as the method runs on
        // it, as the one-component form does, so the filter sees what changed before this ran.
        var filtered = changed.Length > 0;
        var two = others.Length > 1;
        var withOwn = Except(all, own);
        var withFirst = Except(all, others[0]);
        var withSecond = two ? Except(all, others[1]) : withFirst;
        var chunks = ecs.Chunks<T>(withOwn, without, markChanged: !filtered);
        var firsts = default(ChunkSet<T1>);
        var seconds = default(ChunkSet<T2>);
        var rows = default(Rows);
        try
        {
            if (chunks.IsEmpty) return;

            firsts = ecs.Chunks<T1>(withFirst, without, writes[0] && !filtered);
            if (two) seconds = ecs.Chunks<T2>(withSecond, without, writes[1] && !filtered);

            var commands = world.Resource<EcsCommands>();
            var time = world.Resource<Time>();
            var input = world.Resource<Input>();
            var context = new BehaviorContext(world, ecs, commands, time, input);
            rows = new Rows(own);

            // A [Changed] filter reads change ticks an entity at a time, which only the main thread
            // may do, so it keeps the loop on it, as the one-component form does.
            var effectiveThreshold = changed.Length > 0 ? 0 : parallelThreshold;

            for (var index = 0; index < chunks.Count; index++)
            {
                var chunk = chunks[index];
                var first = Matching(firsts, index, chunk);
                var second = two ? Matching(seconds, index, chunk) : first;
                if (chunk.Length == 0) continue;

                if (effectiveThreshold > 0 && chunk.Length >= effectiveThreshold && rows.From == 0 && !rows.AnyMoved)
                {
                    RunTogetherParallel(world, ecs, commands, time, input, chunk, first, second, body);
                    (rows.Previous, rows.HasPrevious) = (chunk.EntityAt(chunk.Length - 1), true);
                    continue;
                }

                if (WalkTogether(context, ecs, chunk, first, second, changed, own, others, writes, body, ref rows)) continue;

                // A call moved the row the method held, so every component's runs are listed
                // again, as the one-component form lists its own.
                chunks.Dispose();
                firsts.Dispose();
                seconds.Dispose();
                (chunks, firsts, seconds) = (default, default, default);
                chunks = ecs.Chunks<T>(withOwn, without, markChanged: false);
                firsts = ecs.Chunks<T1>(withFirst, without, markChanged: false);
                if (two) seconds = ecs.Chunks<T2>(withSecond, without, markChanged: false);
                index = rows.Resume(chunks, index) - 1;
            }
        }
        finally
        {
            rows.Dispose();
            seconds.Dispose();
            firsts.Dispose();
            chunks.Dispose();
        }
    }

    /// <summary>
    /// Walks one run of all the components on the calling thread from <see cref="Rows.From"/>,
    /// and answers false where a call moved the row a method held, as the one-component walk does.
    /// </summary>
    /// <remarks>
    /// Each component is handed as a copy and written back, as the one-component walk hands its
    /// own, and one taken <c>in</c> is not written back, since the method does not write it.
    /// </remarks>
    private static unsafe bool WalkTogether<T, T1, T2>(
        BehaviorContext context,
        EcsWorld ecs,
        Interop.NativeChunk chunk,
        Interop.NativeChunk first,
        Interop.NativeChunk second,
        ReadOnlySpan<int> changed,
        int own,
        int[] others,
        bool[] writes,
        BehaviorRunner<T, T1, T2> body,
        ref Rows rows)
        where T : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
    {
        var components = chunk.Components<T>();
        var firstComponents = first.Components<T1>();
        var secondComponents = second.Components<T2>();
        var entities = chunk.Entities;
        var two = others.Length > 1;
        var writesFirst = writes[0];
        var writesSecond = two && writes[1];
        var guard = rows.Guard;
        var skipping = rows.AnyMoved;
        var (previous, hasPrevious) = (rows.Previous, rows.HasPrevious);
        var from = rows.From;
        rows.From = 0;
        rows.Watch();

        for (var i = from; i < components.Length; i++)
        {
            var entity = entities[i];
            if ((skipping && rows.Skips(entity)) || (changed.Length > 0 && !AnyChanged(ecs, entity, changed)))
            {
                (previous, hasPrevious) = (entity, true);
                continue;
            }

            if (changed.Length > 0)
            {
                ecs.MarkChanged(entity, own);
                if (writesFirst) ecs.MarkChanged(entity, others[0]);
                if (writesSecond) ecs.MarkChanged(entity, others[1]);
            }

            context.Entity = entity;
            ref var row = ref components[i];
            ref var firstRow = ref firstComponents[i];
            ref var secondRow = ref secondComponents[i];
            var copy = row;
            var firstCopy = firstRow;
            var secondCopy = secondRow;
            guard->Entity = entity.Bits;
            guard->Address = (nint)Unsafe.AsPointer(ref row);
            body(ref copy, ref firstCopy, ref secondCopy, entity, context);

            if (guard->Moved == 0)
            {
                WriteBack(ref row, in copy);
                if (writesFirst) WriteBack(ref firstRow, in firstCopy);
                if (writesSecond) WriteBack(ref secondRow, in secondCopy);
                (previous, hasPrevious) = (entity, true);
                continue;
            }

            rows.Stop(entity, i, previous, hasPrevious);
            Put(entity, copy);
            if (writesFirst) Put(entity, firstCopy);
            if (writesSecond) Put(entity, secondCopy);
            return false;
        }

        (rows.Previous, rows.HasPrevious) = (previous, hasPrevious);
        return true;
    }

    /// <summary>The ids but one, which is the component being listed rather than required.</summary>
    private static int[] Except(int[] ids, int listed) => [.. ids.Where(id => id != listed)];

    /// <summary>The run of another component at an index, checked to be the same rows as the behavior's.</summary>
    /// <exception cref="InvalidOperationException">The two lists do not line up, which would hand a method another entity's component.</exception>
    private static Interop.NativeChunk Matching<TOther>(ChunkSet<TOther> others, int index, Interop.NativeChunk chunk)
        where TOther : unmanaged
    {
        if (index >= others.Count)
            throw new InvalidOperationException($"The storage runs of {typeof(TOther).Name} do not line up with the behavior's.");

        var other = others[index];
        if (other.EntityPointer != chunk.EntityPointer || other.Length != chunk.Length)
            throw new InvalidOperationException($"The storage runs of {typeof(TOther).Name} do not line up with the behavior's.");

        return other;
    }

    /// <summary>Splits one run of all the components across the thread pool, as one component's is.</summary>
    private static unsafe void RunTogetherParallel<T, T1, T2>(
        World world,
        EcsWorld ecs,
        EcsCommands commands,
        Time time,
        Input input,
        Interop.NativeChunk chunk,
        Interop.NativeChunk first,
        Interop.NativeChunk second,
        BehaviorRunner<T, T1, T2> body)
        where T : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
    {
        Interlocked.Increment(ref ParallelChunkCount);

        var data = chunk.DataPointer;
        var firstData = first.DataPointer;
        var secondData = second.DataPointer;
        var entityData = chunk.EntityPointer;
        var length = chunk.Length;
        var grain = Math.Max(256, length / (Environment.ProcessorCount * 4));

        Parallel.ForEach(
            Partitioner.Create(0, length, grain),
            range =>
            {
                var context = new BehaviorContext(world, ecs, commands, time, input);
                var components = new Span<T>((void*)data, length);
                var firstComponents = new Span<T1>((void*)firstData, length);
                var secondComponents = new Span<T2>((void*)secondData, length);
                var entities = new ReadOnlySpan<Entity>((void*)entityData, length);

                for (var i = range.Item1; i < range.Item2; i++)
                {
                    var entity = entities[i];
                    context.Entity = entity;
                    body(ref components[i], ref firstComponents[i], ref secondComponents[i], entity, context);
                }
            });
    }
}
