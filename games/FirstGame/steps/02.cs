using Bevy;

// A window, and everything in it is the behaviors below.
var config = Config.Windowed("Coins", 1280, 720);
return BevyApp.Run(config);

/// <summary>The field, its sun and the camera, made once as the game starts.</summary>
[Behavior]
public partial struct Field
{
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        var grass = Render.CreateMaterial((0.12f, 0.3f, 0.1f, 1f));
        ctx.Ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 20f, 1f, 20f), grass, Transform.At(0f, -0.5f, 0f));

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 8000f });
        ctx.Ecs.Set(sun, Transform.LookingAt(new Vec3(4f, 10f, 3f), Vec3.Zero, Vec3.UnitY));

        var camera = Render.SpawnCamera3d();
        ctx.Ecs.Set(camera, Transform.LookingAt(new Vec3(0f, 12f, 10f), Vec3.Zero, Vec3.UnitY));
    }
}
