using Bevy;
using Bevy.Physics;

// A window, and physics for the crates. Everything in it is the behaviors below.
var config = Config.Windowed("Coins", 1280, 720);
return BevyApp.Run(app => app.AddPlugin(new PhysicsPlugin()), config);

/// <summary>The field, its sun, the camera and the crates, made once as the game starts.</summary>
[Behavior]
public partial struct Field
{
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        var grass = Render.CreateMaterial((0.12f, 0.3f, 0.1f, 1f));
        var ground = ctx.Ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 20f, 1f, 20f), grass, Transform.At(0f, -0.5f, 0f));
        ctx.Ecs.Add(ground, new RigidBody { Kind = BodyKind.Static });
        ctx.Ecs.Add(ground, new Collider { Shape = ColliderShape.Box });

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 8000f });
        ctx.Ecs.Set(sun, Transform.LookingAt(new Vec3(4f, 10f, 3f), Vec3.Zero, Vec3.UnitY));

        var camera = Render.SpawnCamera3d();
        ctx.Ecs.Add(camera, new Follow());

        // Crates to walk around, boxes that do not move.
        var crate = Render.CreateMesh(MeshShape.Cuboid, 1.5f, 1.5f, 1.5f);
        var wood = Render.CreateMaterial((0.35f, 0.2f, 0.08f, 1f));
        foreach (var (x, z) in new[] { (2f, -2f), (-3f, -1f), (1f, 4f), (-5f, 4f) })
        {
            var box = ctx.Ecs.SpawnMesh(crate, wood, Transform.At(x, 0.75f, z));
            ctx.Ecs.Add(box, new RigidBody { Kind = BodyKind.Static });
            ctx.Ecs.Add(box, new Collider { Shape = ColliderShape.Box });
        }
    }
}

/// <summary>The player, walked with WASD, which takes the coins it walks into.</summary>
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

        // A body the crates stop, walked by what it is told rather than pushed.
        ctx.Ecs.Add(player, new RigidBody { Kind = BodyKind.Dynamic, Mass = 70f });
        ctx.Ecs.Add(player, new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(0.7f, 1.8f, 0.7f) });
        ctx.Ecs.Add(player, new CharacterController());
    }

    [OnUpdate]
    public void Walk(BehaviorContext ctx, ref CharacterController body, in Transform place)
    {
        var x = (ctx.Input.KeyDown(Key.D) ? 1f : 0f) - (ctx.Input.KeyDown(Key.A) ? 1f : 0f);
        var z = (ctx.Input.KeyDown(Key.S) ? 1f : 0f) - (ctx.Input.KeyDown(Key.W) ? 1f : 0f);
        var way = new Vec3(x, 0f, z);

        // Four units a second, and no faster slantwise.
        body.Move = way == Vec3.Zero ? Vec3.Zero : way.Normalized * 4f;
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

/// <summary>A coin, floating over the field, taken when the player walks into it.</summary>
[Behavior]
public partial struct Coin
{
    /// <summary>Where it floats about.</summary>
    public Vec3 Home;

    /// <summary>How many there are, and how many have been taken.</summary>
    public static int All, Taken;

    private static AssetHandle _chime;

    [OnStartup]
    public static void Scatter(BehaviorContext ctx)
    {
        var ball = Render.CreateMesh(MeshShape.Sphere, 0.35f);
        var gold = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0.75f, 0.15f, 1f), Metallic = 0.9f, Roughness = 0.3f });
        var places = new[] { new Vec3(4f, 0.9f, 0f), new Vec3(-4f, 0.9f, 2f), new Vec3(0f, 0.9f, -5f), new Vec3(6f, 0.9f, -6f), new Vec3(-7f, 0.9f, -3f) };
        foreach (var home in places)
        {
            var coin = ctx.Ecs.SpawnMesh(ball, gold, new Transform(home));
            ctx.Ecs.Add(coin, new Coin { Home = home });
        }

        (All, Taken) = (places.Length, 0);
        _chime = AssetServer.Load(AssetKind.Audio, "sounds/coin.wav");
    }

    [OnUpdate]
    public void Float(BehaviorContext ctx, ref Transform place)
    {
        // Up and down by the sine of the time, each a little apart from the others by its place.
        place.Translation = Home + new Vec3(0f, MathF.Sin(ctx.Time.Elapsed * 3f + Home.X) * 0.15f, 0f);

        if ((place.Translation - Player.At).Length > 1f) return;

        ctx.Cmd.Despawn(ctx.Entity);
        Audio.Play(_chime, AudioSettings.Effect);
        Taken++;
    }
}

/// <summary>How many coins have been taken, in the corner of the window.</summary>
[Behavior]
public partial struct Count
{
    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
    {
        var text = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Left = Length.Px(16f), Top = Length.Px(16f) }, 28f);
        ctx.Ecs.Add(text, new Count());
    }

    [OnUpdate]
    public void Show(BehaviorContext ctx) => Ui.SetText(ctx.Entity, $"Coins {Coin.Taken} of {Coin.All}");
}
