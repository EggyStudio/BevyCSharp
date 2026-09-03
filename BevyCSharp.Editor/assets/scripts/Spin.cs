using Bevy;

// A behavior script. Edit this file while the editor runs and it is recompiled and swapped in.
//
// Nothing here is special. It is an ordinary [Behavior] struct compiled with the same source
// generator the rest of the project uses, so it gets the same runner and the same scheduling.
// The only difference is when it was compiled.
[Behavior]
public partial struct Spin
{
    /// <summary>
    /// Spawns what this generation acts on, and clears out what the last one left.
    /// </summary>
    /// <remarks>
    /// Startup for a script means when the script arrives, not when the app began, so this runs
    /// again on every reload. Each generation is a different type as far as the engine is
    /// concerned, so the previous one's cubes are not this one's and have to go.
    /// </remarks>
    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
    {
        foreach (var previous in ctx.Ecs.EntitiesWith<Spin>())
            ctx.Ecs.Despawn(previous);

        var cube = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1.4f, 1.4f, 1.4f));
        Render.SetMaterial(ctx.Ecs, cube, Render.CreateMaterial(0.9f, 0.5f, 0.2f));
        ctx.Ecs.Add(cube, Transform.At(3f, 0f, 0f));
        ctx.Ecs.Add(cube, new Spin { Speed = 1.2f });

        Console.WriteLine($"[script] Spin is running, {ctx.Ecs.Count<Spin>()} cube(s)");
    }

    /// <summary>Radians per second. Change this and save to see it take effect.</summary>
    public float Speed;

    /// <summary>Current angle.</summary>
    public float Angle;

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Angle += Speed * ctx.Time.Delta;

        ref var transform = ref ctx.Ecs.GetRef<Transform>(ctx.Entity);
        transform.Rotation = Quat.FromRotationY(Angle);
    }
}
