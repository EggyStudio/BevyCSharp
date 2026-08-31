using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Spawns a handful of moving entities and integrates their positions each frame.
/// </summary>
/// <remarks>
/// Every method here is static, so this behavior is never stored on an entity. It holds two
/// systems that operate on other components, which is the right shape for global logic.
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
    /// <para>
    /// <c>Query</c> yields references into Bevy's storage, so assigning to
    /// <c>row.Component</c> writes the real component rather than a copy.
    /// </para>
    /// <para>
    /// On the fixed timestep rather than the frame, so where these entities end up does not
    /// depend on how fast the machine drew. It has to match <see cref="Gravity"/>: integrating
    /// position per frame while accelerating per step would be half a simulation.
    /// </para>
    /// </remarks>
    [OnFixedUpdate]
    public static void Integrate(BehaviorContext ctx)
    {
        var dt = ctx.Time.FixedDelta;

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
    /// <para>
    /// The queued <c>Add</c> matters: adding a component moves the entity to a different
    /// archetype, which would invalidate the references this loop is holding. Queuing it means
    /// it lands after every system has finished reading.
    /// </para>
    /// <para>
    /// Acceleration is where a per-frame step shows up worst: integrating gravity with a delta
    /// that varies gives a different fall on every machine, and a slow frame overshoots the
    /// floor. The fixed timestep is what makes the answer the same everywhere.
    /// </para>
    /// </remarks>
    [OnFixedUpdate]
    public static void Apply(BehaviorContext ctx)
    {
        var dt = ctx.Time.FixedDelta;

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
