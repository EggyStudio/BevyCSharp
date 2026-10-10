using Bevy;

[Behavior]
public partial struct Spin
{
    [Range(0f, 5f)] public float Speed;
    private float _angle;

    // static: one plain system
    [OnStartup]
    public static void Scene(BehaviorContext ctx)
    {
        ctx.Ecs.Add(Render.SpawnCamera3d(), Transform.LookingAt(new Vec3(3f, 3f, 5f), Vec3.Zero, Vec3.UnitY));
        Render.SpawnLight(LightKind.Directional, 10_000f);

        var cube = ctx.Ecs.Spawn();
        ctx.Ecs.Add(cube, new Spin { Speed = 1.2f });
        Render.SetMesh(ctx.Ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
        Render.SetMaterial(ctx.Ecs, cube, Render.CreateMaterial(0.25f, 0.55f, 0.85f));
    }

    // instance: once per entity that has one, handed the entity's transform
    [OnUpdate]
    public void Tick(BehaviorContext ctx, ref Transform transform)
    {
        _angle += Speed * ctx.Time.Delta;
        transform.Rotation = Quat.FromAxisAngle(Vec3.UnitY, _angle);
    }
}
