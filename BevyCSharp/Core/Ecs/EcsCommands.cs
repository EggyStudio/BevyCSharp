using System.Collections.Concurrent;

namespace Bevy;

/// <summary>
/// A thread-safe queue of structural changes, applied once per frame.
/// </summary>
/// <remarks>
/// <para>
/// Spawning, despawning, adding and removing all move entities between archetypes, which
/// invalidates every outstanding component reference, including the ones a behavior is
/// iterating. Rather than forbid the operation mid-loop, commands buffer it. You queue the
/// change now, and it lands during <see cref="Stage.CommandFlush"/> at the end of
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

    // Queues given a delay, waiting for this queue to be applied, which starts their delays.
    private readonly ConcurrentQueue<(double Delay, EcsCommands Queue)> _delaying = new();

    // Queues whose delays have started, with the elapsed time each is due at, in the order queued.
    private readonly List<(double Due, EcsCommands Queue)> _delayed = [];


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
    /// A queue whose commands land once <paramref name="seconds"/> have passed after this queue is
    /// applied, Bevy's <c>commands.delayed().secs</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The delay starts as this queue is applied, at the end of the frame's systems, and counts the
    /// app's own time (<see cref="Time.ElapsedSeconds"/>), so a game paused by time pauses its
    /// delays. A queue whose time has come lands where this one does, ahead of what this frame
    /// queued, so a delay of nothing lands the next frame, as Bevy's does. Queues of one delay land
    /// in the order they were made.
    /// </para>
    /// <para>
    /// For something to happen a while after a cause without a timer to keep, a ripple across a
    /// grid from where it was clicked, a light switched off a moment after it went on.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// ctx.Cmd.Delayed(0.5f).Despawn(spark);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentOutOfRangeException">The delay is negative or not a number.</exception>
    public EcsCommands Delayed(float seconds)
    {
        if (!(seconds >= 0f)) throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A delay is zero or more seconds.");

        var queue = new EcsCommands();
        _delaying.Enqueue((seconds, queue));
        return queue;
    }

    /// <summary>
    /// Drains the queue against <paramref name="world"/> at the app's time, landing the delayed
    /// queues whose time has come first and starting the delays queued since.
    /// </summary>
    /// <remarks>What the app does at <see cref="Stage.CommandFlush"/>, with the time its clock has reached.</remarks>
    /// <param name="world">The world the commands change.</param>
    /// <param name="elapsedSeconds">The app's elapsed time, which delays are counted in.</param>
    public void Apply(EcsWorld world, double elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(world);

        // Due ones first, taken out before they run, since a landed queue may queue further delays.
        List<EcsCommands>? due = null;
        lock (_delayed)
        {
            for (var i = 0; i < _delayed.Count; i++)
            {
                if (_delayed[i].Due > elapsedSeconds) continue;
                (due ??= []).Add(_delayed[i].Queue);
                _delayed.RemoveAt(i--);
            }
        }

        foreach (var queue in due ?? [])
        {
            // What a landed queue delays in turn is kept here, since the landed queue is dropped.
            queue.Apply(world, elapsedSeconds);
            lock (queue._delayed)
            lock (_delayed)
            {
                _delayed.AddRange(queue._delayed);
                queue._delayed.Clear();
            }
        }

        Apply(world);

        lock (_delayed)
        {
            while (_delaying.TryDequeue(out var waiting)) _delayed.Add((elapsedSeconds + waiting.Delay, waiting.Queue));
        }
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
                EngineLog.Error(null, "command", $"[BevyCSharp] Queued command failed: {ex.Message}", ex);
            }
        }
    }

    /// <summary>Discards every queued command without applying it, delayed ones included.</summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
        while (_delaying.TryDequeue(out _)) { }
        lock (_delayed) _delayed.Clear();
    }
}
