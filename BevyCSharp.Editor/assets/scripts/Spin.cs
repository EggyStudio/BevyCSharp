using Bevy;

[Behavior]
public partial struct Spin
{
    [Range(0f, 5f)] public float Speed;
    private float _angle;

    // static: one plain system. In a script, startup means whenever the script is loaded, so this
    // runs again on every save, and the cube the last save made is taken away first.
    [OnStartup]
    public static void Scene(BehaviorContext ctx)
    {
        // Found by its name rather than by Spin, because every reload is a new Spin type as far
        // as the engine is concerned, and the last one's cube does not carry this one's.
        foreach (var entity in ctx.Ecs.All())
        {
            if (ctx.Ecs.NameOf(entity) == "Spinner") ctx.Ecs.Despawn(entity);
        }

        var cube = ctx.Ecs.Spawn();

        // Named, so the world list shows it as what it is and a world save keeps it, and placed
        // beside the editor's own cube rather than at the origin, where that cube hides it.
        ctx.Ecs.SetName(cube, "Spinner");
        ctx.Ecs.Add(cube, Transform.At(3f, 0f, 0f));
        ctx.Ecs.Add(cube, new Spin { Speed = 1.2f });

        Render.SetMesh(ctx.Ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
        Render.SetMaterial(ctx.Ecs, cube, Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = (1f, 0.6f, 0.2f, 1f),
            Emissive = (6f, 2.5f, 0.5f, 1f),
        }));

        Console.WriteLine("[script] Spin is running");
    }

    // instance: once per entity that has one
    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        _angle += Speed * ctx.Time.Delta;
        ctx.Ecs.GetRef<Transform>(ctx.Entity).Rotation = Quat.FromAxisAngle(Vec3.UnitY, _angle);
    }
}
