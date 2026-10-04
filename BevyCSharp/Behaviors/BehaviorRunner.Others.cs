using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bevy;

/// <summary>The body of a per-entity behavior method that also takes another of the entity's components.</summary>
/// <typeparam name="T">The behavior struct, which is also the component type.</typeparam>
/// <typeparam name="T1">The other component.</typeparam>
public delegate void BehaviorRunner<T, T1>(ref T component, ref T1 first, Entity entity, BehaviorContext context)
    where T : unmanaged
    where T1 : unmanaged;

/// <summary>The body of a per-entity behavior method that also takes two other of the entity's components.</summary>
/// <typeparam name="T">The behavior struct, which is also the component type.</typeparam>
/// <typeparam name="T1">The first other component.</typeparam>
/// <typeparam name="T2">The second other component.</typeparam>
public delegate void BehaviorRunner<T, T1, T2>(ref T component, ref T1 first, ref T2 second, Entity entity, BehaviorContext context)
    where T : unmanaged
    where T1 : unmanaged
    where T2 : unmanaged;

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

        using var chunks = ecs.Chunks<T>(Except(all, own), without);
        if (chunks.IsEmpty) return;

        using var firsts = ecs.Chunks<T1>(Except(all, others[0]), without, writes[0]);
        var two = others.Length > 1;
        using var seconds = two ? ecs.Chunks<T2>(Except(all, others[1]), without, writes[1]) : default;

        var commands = world.Resource<EcsCommands>();
        var time = world.Resource<Time>();
        var input = world.Resource<Input>();

        // A [Changed] filter reads change ticks an entity at a time, which only the main thread
        // may do, so it keeps the loop on it, as the one-component form does.
        var effectiveThreshold = changed.Length > 0 ? 0 : parallelThreshold;

        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            var first = Matching(firsts, index, chunk);
            var second = two ? Matching(seconds, index, chunk) : first;
            if (chunk.Length == 0) continue;

            if (effectiveThreshold > 0 && chunk.Length >= effectiveThreshold)
            {
                RunTogetherParallel(world, ecs, commands, time, input, chunk, first, second, body);
                continue;
            }

            var context = new BehaviorContext(world, ecs, commands, time, input);
            var components = chunk.Components<T>();
            var firstComponents = first.Components<T1>();
            var secondComponents = second.Components<T2>();
            var entities = chunk.Entities;

            for (var i = 0; i < components.Length; i++)
            {
                var entity = entities[i];
                if (changed.Length > 0 && !AnyChanged(ecs, entity, changed)) continue;

                context.Entity = entity;
                body(ref components[i], ref firstComponents[i], ref secondComponents[i], entity, context);
            }
        }
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
