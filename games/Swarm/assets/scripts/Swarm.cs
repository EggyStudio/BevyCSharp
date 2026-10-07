using Bevy;
using Bevy.Physics;

namespace Swarm;

/// <summary>Where the game is, on its menu, being played, or over.</summary>
[InitialState(Menu)]
public enum Mode
{
    /// <summary>Waiting for Enter.</summary>
    Menu,

    /// <summary>Being played.</summary>
    Playing,

    /// <summary>The player has fallen or cleared the last wave.</summary>
    Over,
}

/// <summary>Whether a wave is coming or the arena rests between two, which means something only while playing.</summary>
[SubStateOf(typeof(Mode), Mode.Playing)]
[InitialState(Wave)]
public enum Phase
{
    /// <summary>Creatures are coming.</summary>
    Wave,

    /// <summary>The wave is cleared and the next is a moment away.</summary>
    Rest,
}

/// <summary>The two kinds of creature, the one quick and frail and the other slow and hard to stop.</summary>
public enum Kind
{
    /// <summary>Small and quick, one shot to fell.</summary>
    Crawler,

    /// <summary>Large and slow, four shots to fell, and twice the harm.</summary>
    Brute,
}

/// <summary>
/// The waves and what they come to, held by one entity rather than in the script's static fields,
/// so a script compiled again while the game runs keeps the count.
/// </summary>
[Behavior]
public partial struct Waves
{
    /// <summary>The waves this game has come to, the current one counted.</summary>
    public int Wave;

    /// <summary>The creatures the current wave brings.</summary>
    public int Coming;

    /// <summary>The creatures of the current wave spawned so far.</summary>
    public int Spawned;

    /// <summary>The creatures felled this game.</summary>
    public int Felled;

    /// <summary>Seconds to the next wave, while resting.</summary>
    public float Rest;

    /// <summary>The most creatures alive at once this game, which play.sh reads the load from.</summary>
    public int Most;
}

/// <summary>The player, walked with WASD as a character, firing at the nearest creature by itself.</summary>
[Behavior]
public partial struct Player
{
    /// <summary>What it can take before it falls.</summary>
    public float Health;

    /// <summary>Seconds to its next shot.</summary>
    public float Reload;

    /// <summary>Harm taken this frame, added up by the creatures touching it and taken at once.</summary>
    internal static float Harm;

    /// <summary>Where it stands, which every creature walks toward.</summary>
    internal static Vec3 At;

    /// <summary>Walks while playing, and remembers where it stands for the creatures.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public void Walk(BehaviorContext ctx, ref CharacterController body, in Transform place)
    {
        var x = (ctx.Input.KeyDown(Key.D) ? 1f : 0f) - (ctx.Input.KeyDown(Key.A) ? 1f : 0f);
        var z = (ctx.Input.KeyDown(Key.S) ? 1f : 0f) - (ctx.Input.KeyDown(Key.W) ? 1f : 0f);
        body.Move = new Vec3(x, 0f, z) * 6f;
        At = place.Translation;

        Health -= Harm;
        Harm = 0f;
        if (Health > 0f) return;

        Console.WriteLine($"[swarm] fell in wave {Arena.Current(ctx).Wave} with {Arena.Current(ctx).Felled} felled");
        ctx.SetState(Mode.Over);
    }

    /// <summary>
    /// Fires at the nearest creature in range whenever the gun is ready, after the creatures have
    /// said where they are, and from a static method, since a shot spawned while the players'
    /// storage is walked would move what the walk holds.
    /// </summary>
    [OnPostUpdate, InState(Mode.Playing)]
    public static void Fire(BehaviorContext ctx)
    {
        foreach (var row in ctx.Ecs.Query<Player>(markChanged: false))
        {
            ref var player = ref ctx.Ecs.GetRef<Player>(row.Entity);
            player.Reload -= ctx.Time.Delta;
            if (player.Reload > 0f) return;

            var nearest = Vec3.Zero;
            var best = 18f * 18f;
            foreach (var at in Creature.Seen)
            {
                var toward = at - At;
                if (toward.LengthSquared >= best) continue;
                (best, nearest) = (toward.LengthSquared, toward);
            }

            if (nearest == Vec3.Zero) return;

            player.Reload = 0.08f;
            Bullet.Fire(ctx, At + new Vec3(0f, 0.8f, 0f), new Vec3(nearest.X, 0f, nearest.Z).Normalized * 30f);
            return;
        }
    }
}

/// <summary>A creature, which walks at the player and harms it while it touches it.</summary>
[Behavior]
public partial struct Creature
{
    /// <summary>Which kind it is.</summary>
    public Kind Kind;

    /// <summary>Shots it can take before it falls.</summary>
    public int Health;

    /// <summary>Where every creature stood this frame, which the player aims from.</summary>
    internal static readonly List<Vec3> Seen = [];

    /// <summary>Forgets where the creatures stood, before they say it again.</summary>
    [OnFirst]
    public static void Forget(BehaviorContext ctx) => Seen.Clear();

    /// <summary>
    /// Walks at the player, and adds its harm to what the player takes while it touches it, on the
    /// main thread for every creature since the harm is the player's to add up.
    /// </summary>
    [OnUpdate, InState(Phase.Wave), MainThread]
    public void Chase(BehaviorContext ctx, ref CharacterController body, in Transform place)
    {
        Seen.Add(place.Translation);
        var toward = Player.At - place.Translation;
        toward = new Vec3(toward.X, 0f, toward.Z);

        var speed = Kind == Kind.Crawler ? 4.2f : 2.4f;
        body.Move = toward.LengthSquared > 0.01f ? toward.Normalized * speed : Vec3.Zero;

        var reach = Kind == Kind.Crawler ? 1.0f : 1.5f;
        if (toward.LengthSquared < reach * reach) Player.Harm += (Kind == Kind.Crawler ? 2f : 4f) * ctx.Time.Delta;
    }
}

/// <summary>A shot, a sensor flying straight until it meets a creature or its time is up.</summary>
[Behavior]
public partial struct Bullet
{
    /// <summary>Where it flies, in units a second.</summary>
    public Vec3 Velocity;

    /// <summary>Seconds left before it is gone.</summary>
    public float Life;

    private static AssetHandle _mesh, _material;

    /// <summary>Flies on, and is gone once its time is up.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public void Fly(BehaviorContext ctx, ref Transform place)
    {
        place.Translation += Velocity * ctx.Time.Delta;
        Life -= ctx.Time.Delta;
        if (Life <= 0f) ctx.Cmd.Despawn(ctx.Entity);
    }

    /// <summary>A shot from a point, flying at a velocity, a kinematic sensor its transform moves.</summary>
    internal static void Fire(BehaviorContext ctx, Vec3 from, Vec3 velocity)
    {
        if (!_mesh.IsValid)
        {
            _mesh = Render.CreateMesh(MeshShape.Sphere, 0.15f);
            _material = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0.9f, 0.3f, 1f), Emissive = (4f, 3f, 0.5f, 1f) });
        }

        var shot = ctx.Ecs.Spawn();
        ctx.Ecs.Add(shot, Transform.At(from.X, from.Y, from.Z));
        Render.SetMesh(ctx.Ecs, shot, _mesh);
        Render.SetMaterial(ctx.Ecs, shot, _material);
        ctx.Ecs.Add(shot, new RigidBody { Kind = BodyKind.Kinematic, Sensor = true });
        ctx.Ecs.Add(shot, new Collider { Shape = ColliderShape.Sphere, Size = new Vec3(0.3f) });
        ctx.Ecs.Add(shot, new Bullet { Velocity = velocity, Life = 0.8f });
        ctx.Ecs.DespawnOnExit(shot, Mode.Playing);
    }

    /// <summary>A shot meeting a creature harms it and is spent, and a creature with no health left falls.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public static void Hit(BehaviorContext ctx)
    {
        foreach (var contact in ctx.Read<ContactStarted>())
        {
            var (shot, other) = ctx.Ecs.Has<Bullet>(contact.A) ? (contact.A, contact.B) : (contact.B, contact.A);
            if (!ctx.Ecs.IsAlive(shot) || !ctx.Ecs.Has<Bullet>(shot) || !ctx.Ecs.IsAlive(other) || !ctx.Ecs.Has<Creature>(other)) continue;

            ctx.Ecs.Despawn(shot);
            ref var creature = ref ctx.Ecs.GetRef<Creature>(other);
            if (--creature.Health > 0) continue;

            ctx.Ecs.Despawn(other);
            Arena.Current(ctx).Felled++;
            Sounds.Hit(ctx);
        }
    }
}

/// <summary>The arena, its waves and its creatures, built and run by static systems.</summary>
[Behavior]
public partial struct Arena
{
    private const float Size = 36f;
    private const int LastWave = 5;

    private static AssetHandle _crawler, _brute, _crawlerLook, _bruteLook, _player, _playerLook;

    /// <summary>The waves' entity, made with the arena.</summary>
    internal static ref Waves Current(BehaviorContext ctx)
    {
        foreach (var row in ctx.Ecs.Query<Waves>(markChanged: false)) return ref ctx.Ecs.GetRef<Waves>(row.Entity);
        throw new InvalidOperationException("The arena has no waves.");
    }

    /// <summary>The ground, the walls, a light and a camera, and the waves' entity.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        var ground = ctx.Ecs.Spawn();
        ctx.Ecs.Add(ground, Transform.At(0f, -0.5f, 0f));
        Render.SetMesh(ctx.Ecs, ground, Render.CreateMesh(MeshShape.Cuboid, Size, 1f, Size));
        Render.SetMaterial(ctx.Ecs, ground, Render.CreateMaterial((0.18f, 0.22f, 0.16f, 1f)));
        ctx.Ecs.Add(ground, new RigidBody { Kind = BodyKind.Static });
        ctx.Ecs.Add(ground, new Collider { Shape = ColliderShape.Box });

        var wall = Render.CreateMesh(MeshShape.Cuboid, Size, 2f, 1f);
        var stone = Render.CreateMaterial((0.35f, 0.33f, 0.3f, 1f));
        foreach (var (x, z, turned) in new[] { (0f, -Size / 2f, false), (0f, Size / 2f, false), (-Size / 2f, 0f, true), (Size / 2f, 0f, true) })
        {
            var side = ctx.Ecs.Spawn();
            ctx.Ecs.Add(side, new Transform(new Vec3(x, 1f, z), turned ? Quat.FromAxisAngle(Vec3.UnitY, MathF.PI / 2f) : Quat.Identity, Vec3.One));
            Render.SetMesh(ctx.Ecs, side, wall);
            Render.SetMaterial(ctx.Ecs, side, stone);
            ctx.Ecs.Add(side, new RigidBody { Kind = BodyKind.Static });
            ctx.Ecs.Add(side, new Collider { Shape = ColliderShape.Box });
        }

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 8000f });
        ctx.Ecs.Set(sun, Transform.LookingAt(new Vec3(10f, 20f, 5f), Vec3.Zero, Vec3.UnitY));

        var camera = Render.SpawnCamera3d();
        ctx.Ecs.Set(camera, Transform.LookingAt(new Vec3(0f, 34f, 22f), Vec3.Zero, Vec3.UnitY));

        ctx.Ecs.Add(ctx.Ecs.Spawn(), new Waves());

        _crawler = Render.CreateMesh(MeshShape.Capsule, 0.3f, 0.4f);
        _brute = Render.CreateMesh(MeshShape.Cuboid, 1f, 1.6f, 1f);
        _crawlerLook = Render.CreateMaterial((0.8f, 0.25f, 0.2f, 1f));
        _bruteLook = Render.CreateMaterial((0.45f, 0.2f, 0.6f, 1f));

        // Made here once, as the creatures' are, since a mesh or a material made as each game
        // starts would be held for good by a handle nothing releases, one more of each a game.
        _player = Render.CreateMesh(MeshShape.Capsule, 0.35f, 1.1f);
        _playerLook = Render.CreateMaterial((0.3f, 0.7f, 1f, 1f));
    }

    /// <summary>The player, in the middle, and the count started over, as play starts.</summary>
    [OnEnter(Mode.Playing)]
    public static void Begin(BehaviorContext ctx)
    {
        Current(ctx) = new Waves();

        var player = ctx.Ecs.Spawn();
        ctx.Ecs.Add(player, Transform.At(0f, 0f, 0f));
        Render.SetMesh(ctx.Ecs, player, _player);
        Render.SetMaterial(ctx.Ecs, player, _playerLook);
        ctx.Ecs.Add(player, new RigidBody { Kind = BodyKind.Dynamic, Mass = 70f });
        ctx.Ecs.Add(player, new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(0.7f, 1.8f, 0.7f), Offset = new Vec3(0f, 0.9f, 0f) });
        ctx.Ecs.Add(player, new CharacterController());
        ctx.Ecs.Add(player, new Player { Health = 600f });
        ctx.Ecs.DespawnOnExit(player, Mode.Playing);
    }

    /// <summary>A wave, bigger than the last.</summary>
    [OnEnter(Phase.Wave)]
    public static void Next(BehaviorContext ctx)
    {
        ref var waves = ref Current(ctx);
        waves.Wave++;
        waves.Coming = 40 + 60 * waves.Wave;
        waves.Spawned = 0;
        Console.WriteLine($"[swarm] wave {waves.Wave}, {waves.Coming} coming");
    }

    /// <summary>
    /// Spawns the wave a few at a time around the walls, and rests once every creature of it has
    /// come and fallen.
    /// </summary>
    [OnUpdate, InState(Phase.Wave)]
    public static void Spawn(BehaviorContext ctx)
    {
        ref var waves = ref Current(ctx);
        var alive = ctx.Ecs.Count<Creature>();
        waves.Most = Math.Max(waves.Most, alive);

        if (waves.Spawned >= waves.Coming)
        {
            if (alive > 0) return;
            if (waves.Wave >= LastWave)
            {
                Console.WriteLine($"[swarm] held, {waves.Felled} felled");
                ctx.SetState(Mode.Over);
                return;
            }

            ctx.SetState(Phase.Rest);
            return;
        }

        for (var i = 0; i < 6 && waves.Spawned < waves.Coming; i++, waves.Spawned++)
        {
            // Around the walls, a brute one in every five.
            var angle = waves.Spawned * 2.399963f;
            var at = new Vec3(MathF.Cos(angle), 0f, MathF.Sin(angle)) * (Size / 2f - 2f);
            var kind = waves.Spawned % 5 == 4 ? Kind.Brute : Kind.Crawler;

            var creature = ctx.Ecs.Spawn();
            ctx.Ecs.Add(creature, Transform.At(at.X, 0.1f, at.Z));
            Render.SetMesh(ctx.Ecs, creature, kind == Kind.Crawler ? _crawler : _brute);
            Render.SetMaterial(ctx.Ecs, creature, kind == Kind.Crawler ? _crawlerLook : _bruteLook);
            ctx.Ecs.Add(creature, new RigidBody { Kind = BodyKind.Dynamic, Mass = kind == Kind.Crawler ? 20f : 90f });
            ctx.Ecs.Add(creature, kind == Kind.Crawler
                ? new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(0.6f, 1f, 0.6f), Offset = new Vec3(0f, 0.5f, 0f) }
                : new Collider { Shape = ColliderShape.Box, Size = new Vec3(1f, 1.6f, 1f), Offset = new Vec3(0f, 0.8f, 0f) });
            ctx.Ecs.Add(creature, new CharacterController());
            ctx.Ecs.Add(creature, new Creature { Kind = kind, Health = kind == Kind.Crawler ? 1 : 4 });
            ctx.Ecs.DespawnOnExit(creature, Mode.Playing);
        }
    }

    /// <summary>A moment between waves.</summary>
    [OnEnter(Phase.Rest)]
    public static void Breathe(BehaviorContext ctx) => Current(ctx).Rest = 2f;

    /// <summary>The next wave once the moment is over.</summary>
    [OnUpdate, InState(Phase.Rest)]
    public static void Wait(BehaviorContext ctx)
    {
        ref var waves = ref Current(ctx);
        waves.Rest -= ctx.Time.Delta;
        if (waves.Rest <= 0f) ctx.SetState(Phase.Wave);
    }
}

/// <summary>The sound of a creature falling, played no more often than a listener tells two apart.</summary>
internal static class Sounds
{
    private static AssetHandle _hit;
    private static double _last;

    internal static void Hit(BehaviorContext ctx)
    {
        if (ctx.Time.ElapsedSeconds - _last < 0.05) return;
        _last = ctx.Time.ElapsedSeconds;

        if (!_hit.IsValid) _hit = AssetServer.Load(AssetKind.Audio, "sounds/hit.wav");
        Audio.Play(_hit, AudioSettings.Effect);
    }
}

/// <summary>The words on screen, the menu, the count while playing, and how it ended.</summary>
[Behavior]
public partial struct Screens
{
    /// <summary>The count on the HUD, found by this rather than kept in a field of the script.</summary>
    [Behavior]
    public partial struct Count
    {
    }

    /// <summary>The menu, until Enter starts the game.</summary>
    [OnEnter(Mode.Menu)]
    public static void Menu(BehaviorContext ctx) => Banner(ctx, "Swarm\nWASD to move, the gun fires itself\nPress Enter to play", Mode.Menu);

    /// <summary>Starts the game from the menu.</summary>
    [OnUpdate, InState(Mode.Menu)]
    public static void Start(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Enter)) ctx.SetState(Mode.Playing);
    }

    /// <summary>The count in a corner while playing.</summary>
    [OnEnter(Mode.Playing)]
    public static void Hud(BehaviorContext ctx)
    {
        var count = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Left = Length.Px(16f), Top = Length.Px(16f), Color = (1f, 1f, 1f, 1f) }, 22f);
        ctx.Ecs.Add(count, new Count());
        ctx.Ecs.DespawnOnExit(count, Mode.Playing);
    }

    /// <summary>Keeps the count up to date, the creatures alive and the player's health changing every frame.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public static void Keep(BehaviorContext ctx)
    {
        var waves = Arena.Current(ctx);
        var health = 0f;
        foreach (var row in ctx.Ecs.Query<Player>(markChanged: false)) health = row.Component.Health;

        var text = $"Wave {waves.Wave}   Creatures {ctx.Ecs.Count<Creature>()}   Felled {waves.Felled}   Health {MathF.Max(0f, health):0}";
        foreach (var row in ctx.Ecs.Query<Count>(markChanged: false)) Ui.SetText(row.Entity, text);
    }

    /// <summary>How it ended.</summary>
    [OnEnter(Mode.Over)]
    public static void Over(BehaviorContext ctx)
    {
        var waves = Arena.Current(ctx);
        Banner(ctx, $"Wave {waves.Wave}, {waves.Felled} felled\nPress Enter to play again", Mode.Over);
    }

    /// <summary>Plays again from the end.</summary>
    [OnUpdate, InState(Mode.Over)]
    public static void Again(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Enter)) ctx.SetState(Mode.Playing);
    }

    /// <summary>Centered words on a dark panel, gone when the state they belong to is left.</summary>
    private static void Banner<TState>(BehaviorContext ctx, string text, TState state) where TState : struct, Enum
    {
        var panel = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Percent(30f), Top = Length.Percent(38f), Padding = Length.Px(24f), Color = (0f, 0f, 0f, 0.7f) });
        var words = Ui.SpawnText(text, new UiSettings { Color = (1f, 1f, 1f, 1f) }, 28f);
        ctx.Ecs.SetParent(words, panel);
        ctx.Ecs.DespawnOnExit(panel, state);
    }
}

/// <summary>What play.sh asks the game, as a line of numbers.</summary>
internal static class Status
{
    /// <summary>The mode, the wave, the creatures alive, the most at once, the felled and the player's health.</summary>
    [Command("swarm.status", "The game as numbers: mode, wave, alive, most, felled and health")]
    internal static string Now()
    {
        var ecs = ConsoleHost.Ecs;
        var waves = default(Waves);
        foreach (var row in ecs.Query<Waves>(markChanged: false)) waves = row.Component;

        var health = 0f;
        foreach (var row in ecs.Query<Player>(markChanged: false)) health = row.Component.Health;

        var mode = App.TryState<Mode>(out var now) ? now : Mode.Menu;
        return FormattableString.Invariant(
            $"mode={mode} wave={waves.Wave} alive={ecs.Count<Creature>()} most={waves.Most} felled={waves.Felled} health={health:0}");
    }
}
