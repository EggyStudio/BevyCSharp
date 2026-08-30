using System.Collections.Concurrent;

namespace Bevy;

/// <summary>
/// A thread-safe queue of structural changes, applied once per frame.
/// </summary>
/// <remarks>
/// <para>
/// Spawning, despawning, adding and removing all move entities between archetypes, which
/// invalidates every outstanding component reference - including the ones a behavior is
/// iterating. Rather than forbid the operation mid-loop, commands buffer it: you queue the
/// change now, it lands during <see cref="Stage.CommandFlush"/> at the end of
/// <see cref="Stage.PostUpdate"/>, after every system has finished reading.
/// </para>
/// <para>
/// This is also the only ECS surface that is safe to touch from a parallel behavior method.
/// <see cref="EcsWorld"/> needs the main thread's world loan; this queue needs nothing but the
/// queue itself.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [OnUpdate]
/// public void Tick(BehaviorContext ctx)
/// {
///     Fuse -= (float)ctx.Time.DeltaSeconds;
///     if (Fuse &lt;= 0f) ctx.Cmd.Despawn(ctx.Entity);
/// }
/// </code>
/// </example>
public sealed class EcsCommands
{
    private readonly ConcurrentQueue<Action<EcsWorld>> _queue = new();

    /// <summary>Number of commands waiting to be applied.</summary>
    public int PendingCount => _queue.Count;

    /// <summary>Queues an entity spawn, passing the new entity to <paramref name="build"/>.</summary>
    public EcsCommands Spawn(Action<Entity, EcsWorld> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        _queue.Enqueue(world => build(world.Spawn(), world));
        return this;
    }

    /// <summary>Queues a spawn of <paramref name="count"/> entities.</summary>
    public EcsCommands SpawnBatch(int count, Action<Entity, EcsWorld> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        _queue.Enqueue(world =>
        {
            for (var i = 0; i < count; i++) build(world.Spawn(), world);
        });
        return this;
    }

    /// <summary>Queues a spawn of <paramref name="count"/> entities each carrying a component.</summary>
    public EcsCommands SpawnBatch<T>(int count, Func<int, T> factory) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(factory);
        _queue.Enqueue(world =>
        {
            for (var i = 0; i < count; i++) world.Add(world.Spawn(), factory(i));
        });
        return this;
    }

    /// <summary>Queues a despawn.</summary>
    public EcsCommands Despawn(Entity entity)
    {
        _queue.Enqueue(world => world.Despawn(entity));
        return this;
    }

    /// <summary>Queues adding or replacing a component.</summary>
    public EcsCommands Add<T>(Entity entity, T component) where T : unmanaged
    {
        _queue.Enqueue(world =>
        {
            if (world.IsAlive(entity)) world.Add(entity, component);
        });
        return this;
    }

    /// <summary>Queues removing a component.</summary>
    public EcsCommands Remove<T>(Entity entity) where T : unmanaged
    {
        _queue.Enqueue(world =>
        {
            if (world.IsAlive(entity)) world.Remove<T>(entity);
        });
        return this;
    }

    /// <summary>Queues an arbitrary action against the world.</summary>
    public EcsCommands Run(Action<EcsWorld> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _queue.Enqueue(action);
        return this;
    }

    /// <summary>
    /// Drains the queue against <paramref name="world"/>.
    /// </summary>
    /// <remarks>
    /// Commands are drained by snapshotting the current length first, so a command that queues
    /// further commands defers them to the next frame instead of spinning here forever.
    /// A command that throws is reported and skipped; one bad script should not strand every
    /// other queued change.
    /// </remarks>
    public void Apply(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var budget = _queue.Count;
        while (budget-- > 0 && _queue.TryDequeue(out var command))
        {
            try
            {
                command(world);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[BevyCSharp] Queued command failed: {ex.Message}");
            }
        }
    }

    /// <summary>Discards every queued command without applying it.</summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}
