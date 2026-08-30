using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Moves entities by writing Bevy's own <see cref="Transform"/>, and parents one to another.
/// </summary>
/// <remarks>
/// <para>
/// The transforms here are not a copy kept in sync. They are the components Bevy's own systems
/// read, so writing one is picked up by transform propagation and, in a windowed run, by
/// rendering.
/// </para>
/// <para>
/// The moon is parented to the planet, so it inherits the planet's motion and only has to
/// describe its own orbit. That is the whole point of a hierarchy, and it works because the
/// parent link goes through Bevy's relationship API rather than a raw component write.
/// </para>
/// </remarks>
[Behavior]
public partial struct Orbit
{
    /// <summary>Distance from whatever this orbits.</summary>
    public float Radius;

    /// <summary>Radians per second.</summary>
    public float Speed;

    /// <summary>Current angle in radians.</summary>
    public float Angle;

    /// <summary>Builds a planet with a moon parented to it.</summary>
    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
    {
        var planet = ctx.Ecs.Spawn();
        ctx.Ecs.AddNative(planet, NativeComponents.Transform, Transform.Identity);
        ctx.Ecs.Add(planet, new Orbit { Radius = 8f, Speed = 0.5f });

        var moon = ctx.Ecs.Spawn();
        ctx.Ecs.AddNative(moon, NativeComponents.Transform, Transform.Identity);
        ctx.Ecs.Add(moon, new Orbit { Radius = 2f, Speed = 2f });
        ctx.Ecs.SetParent(moon, planet);

        Console.WriteLine($"[Orbit] planet {planet} with moon {moon} parented to it");
    }

    /// <summary>Advances this entity's orbit and writes its transform.</summary>
    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Angle += Speed * ctx.Time.Delta;

        // The moon's transform is relative to the planet, so this is its own orbit only. Bevy
        // combines the two into a GlobalTransform during propagation.
        ref var transform = ref ctx.Ecs.GetNativeRef<Transform>(
            ctx.Entity, NativeComponents.Transform);

        transform.Translation = new Vec3(
            MathF.Cos(Angle) * Radius,
            0f,
            MathF.Sin(Angle) * Radius);
        transform.Rotation = Quat.FromRotationY(Angle);
    }

    /// <summary>Reports where everything ended up.</summary>
    [OnCleanup]
    public static void Report(BehaviorContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("=== transforms ===");

        foreach (var row in ctx.Ecs.Query<Orbit>(markChanged: false))
        {
            if (!ctx.Ecs.TryGetNative<Transform>(
                    row.Entity, NativeComponents.Transform, out var transform))
                continue;

            var parent = ctx.Ecs.ParentOf(row.Entity);
            var children = ctx.Ecs.ChildrenOf(row.Entity);

            Console.WriteLine(
                $"  local {transform.Translation}"
                + $"  parent {(parent.IsNone ? "none" : parent.ToString())}"
                + $"  children {children.Length}");
        }
    }
}
