using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Builds a visible scene: a camera, a light, and cubes orbiting a sphere.
/// </summary>
/// <remarks>
/// Everything here is skipped on a headless build, so the sample runs unchanged either way. The
/// rest of the sample's behaviors do not know or care whether this ran.
/// </remarks>
[Behavior]
public partial struct Scene
{
    /// <summary>Radians per second about the world origin.</summary>
    public float Speed;

    /// <summary>Distance from the origin.</summary>
    public float Radius;

    /// <summary>Current angle in radians.</summary>
    public float Angle;

    /// <summary>Height above the ground plane.</summary>
    public float Height;

    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var camera = Render.SpawnCamera3d();
        ctx.Ecs.AddNative(camera, NativeComponents.Transform,
            Transform.LookingAt(new Vec3(0f, 6f, 12f), Vec3.Zero, Vec3.UnitY));

        var sun = Render.SpawnLight(LightKind.Directional, 10_000f);
        ctx.Ecs.AddNative(sun, NativeComponents.Transform,
            Transform.LookingAt(new Vec3(4f, 8f, 4f), Vec3.Zero, Vec3.UnitY));

        var ground = Render.CreateMesh(MeshShape.Plane, 40f, 40f);
        var groundMaterial = Render.CreateMaterial(0.10f, 0.11f, 0.13f, roughness: 0.9f);
        Spawn(ctx, ground, groundMaterial, Transform.At(0f, -1.5f, 0f));

        var core = Render.CreateMesh(MeshShape.Sphere, 1.6f);
        var coreMaterial = Render.CreateMaterial(0.85f, 0.35f, 0.15f, metallic: 0.9f,
            roughness: 0.25f);
        Spawn(ctx, core, coreMaterial, Transform.Identity);

        // One mesh and one material, shared by every cube. Handles are references, so this is
        // eight entities drawing the same two assets rather than eight copies of each.
        var cube = Render.CreateMesh(MeshShape.Cuboid, 0.7f, 0.7f, 0.7f);
        var cubeMaterial = Render.CreateMaterial(0.25f, 0.55f, 0.85f, roughness: 0.35f);

        for (var i = 0; i < 8; i++)
        {
            var entity = Spawn(ctx, cube, cubeMaterial, Transform.Identity);
            ctx.Ecs.Add(entity, new Scene
            {
                Speed = 0.6f,
                Radius = 5f,
                Angle = MathF.Tau * i / 8f,
                Height = MathF.Sin(i) * 0.8f,
            });
        }

        Console.WriteLine("[Scene] camera, light and 10 drawable entities");
    }

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Angle += Speed * ctx.Time.Delta;

        ref var transform = ref ctx.Ecs.GetNativeRef<Transform>(
            ctx.Entity, NativeComponents.Transform);

        transform.Translation = new Vec3(
            MathF.Cos(Angle) * Radius,
            Height,
            MathF.Sin(Angle) * Radius);
        transform.Rotation = Quat.FromRotationY(Angle * 2f);
    }

    /// <summary>Creates an entity that draws <paramref name="mesh"/> with a material.</summary>
    private static Entity Spawn(
        BehaviorContext ctx,
        AssetHandle mesh,
        AssetHandle material,
        Transform placement)
    {
        var entity = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, entity, mesh);
        Render.SetMaterial(ctx.Ecs, entity, material);
        ctx.Ecs.AddNative(entity, NativeComponents.Transform, placement);
        return entity;
    }
}
