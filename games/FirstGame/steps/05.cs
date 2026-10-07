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
        ctx.Ecs.Add(camera, new Follow());
    }
}

/// <summary>The player, walked with WASD.</summary>
[Behavior]
public partial struct Player
{
    /// <summary>Where the player is, for the camera and the coins.</summary>
    public static Vec3 At;

    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
    {
        var look = Render.CreateMaterial((0.08f, 0.2f, 0.6f, 1f));
        var player = ctx.Ecs.SpawnMesh(Render.CreateMesh(MeshShape.Capsule, 0.35f, 1.1f), look, Transform.At(0f, 0.9f, 0f));
        ctx.Ecs.Add(player, new Player());
    }

    [OnUpdate]
    public void Walk(BehaviorContext ctx, ref Transform place)
    {
        var x = (ctx.Input.KeyDown(Key.D) ? 1f : 0f) - (ctx.Input.KeyDown(Key.A) ? 1f : 0f);
        var z = (ctx.Input.KeyDown(Key.S) ? 1f : 0f) - (ctx.Input.KeyDown(Key.W) ? 1f : 0f);
        var way = new Vec3(x, 0f, z);

        // Four units a second, and no faster slantwise.
        if (way != Vec3.Zero) place.Translation += way.Normalized * 4f * ctx.Time.Delta;
        At = place.Translation;
    }
}

/// <summary>The camera, which looks down at the player from behind and above it.</summary>
[Behavior]
public partial struct Follow
{
    [OnUpdate]
    public void Look(BehaviorContext ctx, ref Transform place) =>
        place = Transform.LookingAt(Player.At + new Vec3(0f, 9f, 8f), Player.At, Vec3.UnitY);
}
