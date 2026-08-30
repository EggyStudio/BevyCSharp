using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bevy;

/// <summary>
/// The body of a per-entity behavior method.
/// </summary>
/// <typeparam name="T">The behavior struct, which is also the component type.</typeparam>
/// <param name="component">
/// The entity's component, by reference. Assigning to its fields writes into Bevy's storage.
/// </param>
/// <param name="entity">The entity being processed.</param>
/// <param name="context">Resources for this invocation.</param>
public delegate void BehaviorRunner<T>(ref T component, Entity entity, BehaviorContext context)
    where T : unmanaged;

/// <summary>
/// Drives per-entity behavior methods over Bevy's storage.
/// </summary>
/// <remarks>
/// <para>
/// This is what the generated code calls. Keeping the iteration here rather than emitting it
/// per behavior means the pointer arithmetic, the parallel partitioning and the filtering
/// rules are written once, live in a project that has already opted into unsafe code, and can
/// be fixed without regenerating anything. The generated runner stays a handful of readable,
/// safe lines.
/// </para>
/// </remarks>
public static class BehaviorRunners
{
    /// <summary>
    /// Entity count from which iteration is worth splitting across threads.
    /// </summary>
    /// <remarks>
    /// Below this, the scheduling overhead costs more than the parallelism buys. The figure is
    /// deliberately high: most behaviors run over tens or hundreds of entities, where a plain
    /// loop over contiguous memory is already close to optimal.
    /// </remarks>
    public const int DefaultParallelThreshold = 4096;

    /// <summary>
    /// Runs <paramref name="body"/> for every entity carrying <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The behavior struct.</typeparam>
    /// <param name="world">The resource world.</param>
    /// <param name="body">The behavior method to invoke per entity.</param>
    /// <param name="with">Component ids the entity must also carry.</param>
    /// <param name="without">Component ids the entity must not carry.</param>
    /// <param name="changed">
    /// Component ids of which at least one must have changed this frame. Testing this is
    /// per entity and needs the main thread's world, so supplying any forces sequential
    /// iteration.
    /// </param>
    /// <param name="parallelThreshold">
    /// Entity count from which to parallelise, or 0 to always run sequentially.
    /// </param>
    public static unsafe void Run<T>(
        World world,
        BehaviorRunner<T> body,
        ReadOnlySpan<int> with = default,
        ReadOnlySpan<int> without = default,
        ReadOnlySpan<int> changed = default,
        int parallelThreshold = DefaultParallelThreshold) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(body);

        var ecs = world.Resource<EcsWorld>();
        using var chunks = ecs.Chunks<T>(with, without);
        if (chunks.IsEmpty) return;

        var commands = world.Resource<EcsCommands>();
        var time = world.Resource<Time>();
        var input = world.Resource<Input>();

        // A [Changed] filter reads Bevy's change ticks per entity, which only the main thread
        // may do, so it disables the parallel path rather than racing.
        var hasChangedFilter = changed.Length > 0;
        var effectiveThreshold = hasChangedFilter ? 0 : parallelThreshold;

        for (var chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
        {
            var chunk = chunks[chunkIndex];
            if (chunk.Length == 0) continue;

            if (effectiveThreshold > 0 && chunk.Length >= effectiveThreshold)
            {
                RunChunkParallel(world, ecs, commands, time, input, chunk, body);
            }
            else
            {
                RunChunkSequential(
                    new BehaviorContext(world, ecs, commands, time, input),
                    ecs, chunk, changed, body);
            }
        }
    }

    /// <summary>Walks one chunk on the calling thread, applying any change filter.</summary>
    private static void RunChunkSequential<T>(
        BehaviorContext context,
        EcsWorld ecs,
        Interop.NativeChunk chunk,
        ReadOnlySpan<int> changed,
        BehaviorRunner<T> body) where T : unmanaged
    {
        var components = chunk.Components<T>();
        var entities = chunk.Entities;

        for (var i = 0; i < components.Length; i++)
        {
            var entity = entities[i];
            if (changed.Length > 0 && !AnyChanged(ecs, entity, changed)) continue;

            context.Entity = entity;
            body(ref components[i], entity, context);
        }
    }

    /// <summary>
    /// Splits one chunk across the thread pool.
    /// </summary>
    /// <remarks>
    /// The spans are rebuilt inside each worker from the chunk's base addresses, because a
    /// <c>Span</c> cannot be captured by a lambda. Each partition owns a disjoint range of the
    /// same array, so the writes never overlap. Each worker also gets its own context, since
    /// <see cref="BehaviorContext.Entity"/> is per invocation.
    /// </remarks>
    private static unsafe void RunChunkParallel<T>(
        World world,
        EcsWorld ecs,
        EcsCommands commands,
        Time time,
        Input input,
        Interop.NativeChunk chunk,
        BehaviorRunner<T> body) where T : unmanaged
    {
        var data = chunk.DataPointer;
        var entityData = chunk.EntityPointer;
        var length = chunk.Length;
        var grain = Math.Max(256, length / (Environment.ProcessorCount * 4));

        Parallel.ForEach(
            Partitioner.Create(0, length, grain),
            range =>
            {
                var context = new BehaviorContext(world, ecs, commands, time, input);
                var components = new Span<T>((void*)data, length);
                var entities = new ReadOnlySpan<Entity>((void*)entityData, length);

                for (var i = range.Item1; i < range.Item2; i++)
                {
                    var entity = entities[i];
                    context.Entity = entity;
                    body(ref components[i], entity, context);
                }
            });
    }

    /// <summary>True when any of <paramref name="componentIds"/> changed on the entity.</summary>
    private static bool AnyChanged(EcsWorld ecs, Entity entity, ReadOnlySpan<int> componentIds)
    {
        foreach (var componentId in componentIds)
            if (ecs.ChangedById(entity, componentId))
                return true;

        return false;
    }
}
