using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// A behavior that is its own component: per-entity state plus the logic that drives it.
/// </summary>
/// <remarks>
/// This is the shape the whole package exists for. <see cref="Angle"/> and <see cref="Speed"/>
/// live in Bevy's tables, one copy per entity. <see cref="Tick"/> is an instance method, so it
/// runs once per entity with <c>this</c> bound by reference to that entity's row, no lookup,
/// no copy, no separate system class to keep in sync with the component.
/// </remarks>
[Behavior]
public partial struct Spinner
{
    /// <summary>Current rotation in radians.</summary>
    public float Angle;

    /// <summary>Rotation rate in radians per second.</summary>
    public float Speed;

    /// <summary>How many full turns have completed.</summary>
    public int Turns;

    /// <summary>Creates three spinners turning at different rates.</summary>
    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
    {
        for (var i = 1; i <= 3; i++)
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Spinner { Speed = i * 2f });
        }

        Console.WriteLine("[Spinner] spawned 3 spinners");
    }

    /// <summary>Advances this entity's rotation.</summary>
    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Angle += Speed * ctx.Time.Delta;

        if (Angle < MathF.Tau) return;

        Angle -= MathF.Tau;
        Turns++;
    }
}

/// <summary>
/// A behavior that removes its own entity once its timer runs out.
/// </summary>
[Behavior]
public partial struct Lifetime
{
    /// <summary>Seconds remaining before the entity is despawned.</summary>
    public float Remaining;

    /// <summary>Creates a few short-lived entities.</summary>
    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
    {
        for (var i = 1; i <= 4; i++)
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Lifetime { Remaining = i * 0.05f });
        }

        Console.WriteLine("[Lifetime] spawned 4 temporary entities");
    }

    /// <summary>Counts down and queues a despawn at zero.</summary>
    /// <remarks>
    /// The despawn goes through the command queue rather than <c>ctx.Ecs</c>, because removing
    /// an entity mid-iteration would move the rows this loop is walking.
    /// </remarks>
    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Remaining -= ctx.Time.Delta;
        if (Remaining <= 0f) ctx.Cmd.Despawn(ctx.Entity);
    }
}
