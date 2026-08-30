using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Spawns a handful of moving entities and integrates their positions each frame.
/// </summary>
/// <remarks>
/// Every method here is static, so this behavior is never stored on an entity - it is just a
/// place to hang two systems that operate on other components. That is the right shape for
/// global logic.
/// </remarks>
[Behavior]
public partial struct Movement
{
    /// <summary>How many entities to create at startup.</summary>
    private const int SpawnCount = 5;

    /// <summary>Creates the initial entities.</summary>
    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
    {
        for (var i = 0; i < SpawnCount; i++)
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Position { X = i, Y = 10f });
            ctx.Ecs.Add(entity, new Velocity { X = 1f + i, Y = 0f });
            ctx.Ecs.Add(entity, new Falls());
        }

        Console.WriteLine($"[Movement] spawned {SpawnCount} entities");
    }

    /// <summary>Integrates position from velocity.</summary>
    /// <remarks>
    /// <c>Query</c> yields references into Bevy's storage, so assigning to
    /// <c>row.Component</c> writes the real component rather than a copy.
    /// </remarks>
    [OnUpdate]
    public static void Integrate(BehaviorContext ctx)
    {
        var dt = ctx.Time.Delta;

        foreach (var row in ctx.Ecs.Query<Position>())
        {
            if (!ctx.Ecs.TryGet<Velocity>(row.Entity, out var velocity)) continue;

            row.Component.X += velocity.X * dt;
            row.Component.Y += velocity.Y * dt;
        }
    }
}

/// <summary>
/// Pulls falling entities downward, and grounds them when they reach the floor.
/// </summary>
[Behavior]
public partial struct Gravity
{
    /// <summary>Downward acceleration in units per second squared.</summary>
    private const float Acceleration = -9.81f;

    /// <summary>Height at which an entity is considered to have landed.</summary>
    private const float Floor = 0f;

    /// <summary>Accelerates everything that falls and has not landed yet.</summary>
    /// <remarks>
    /// The queued <c>Add</c> matters: adding a component moves the entity to a different
    /// archetype, which would invalidate the references this loop is holding. Queuing it means
    /// it lands after every system has finished reading.
    /// </remarks>
    [OnUpdate]
    public static void Apply(BehaviorContext ctx)
    {
        var dt = ctx.Time.Delta;

        foreach (var row in ctx.Ecs.Query<Velocity>())
        {
            if (!ctx.Ecs.Has<Falls>(row.Entity)) continue;
            if (ctx.Ecs.Has<Grounded>(row.Entity)) continue;

            row.Component.Y += Acceleration * dt;

            if (ctx.Ecs.TryGet<Position>(row.Entity, out var position) && position.Y <= Floor)
                ctx.Cmd.Add(row.Entity, new Grounded());
        }
    }
}
