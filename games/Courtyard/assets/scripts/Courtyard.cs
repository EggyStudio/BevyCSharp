using Bevy;
using Bevy.Physics;

namespace Courtyard;

/// <summary>Where the game is, on its menu, being played, or won.</summary>
[InitialState(Menu)]
public enum Mode
{
    /// <summary>Waiting for Enter.</summary>
    Menu,

    /// <summary>Being played.</summary>
    Playing,

    /// <summary>Every coin brought to the goal.</summary>
    Won,
}

/// <summary>Whether play is held, which means something only while playing.</summary>
[SubStateOf(typeof(Mode), Mode.Playing)]
[InitialState(Off)]
public enum Pause
{
    /// <summary>Playing.</summary>
    Off,

    /// <summary>Held, with the pause menu up.</summary>
    On,
}

// The bodies are made on every frame of play for whatever has none yet, rather than once on entering
// play, so a level a load brings back mid-game is given them as the first one was.

/// <summary>A wall, which the player cannot walk through.</summary>
[Behavior]
public partial struct Wall
{
    /// <summary>Gives every wall a body while playing, sized to the cube it is drawn as.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public static void Build(BehaviorContext ctx)
    {
        if (!ctx.TryRes<PhysicsWorld>(out var physics)) return;

        foreach (var row in ctx.Ecs.Query<Wall>(markChanged: false))
        {
            if (physics.Has(row.Entity)) continue;

            // The editor's cube is two units a side, so a wall scaled to one is two units of it.
            var at = ctx.Ecs.GetOrDefault<Transform>(row.Entity);
            physics.Add(row.Entity, PhysicsShape.Box(at.Scale * 2f), BodyKind.Static, at);
        }
    }
}

/// <summary>The ground, which the runner stands on.</summary>
[Behavior]
public partial struct Floor
{
    /// <summary>Gives the ground a body while playing, a slab wider than the courtyard whose top is the ground's face.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public static void Build(BehaviorContext ctx)
    {
        if (!ctx.TryRes<PhysicsWorld>(out var physics)) return;

        foreach (var row in ctx.Ecs.Query<Floor>(markChanged: false))
        {
            if (physics.Has(row.Entity)) continue;

            // The editor's ground is a plane, which has no inside for a body to be, so the slab
            // hangs under it with its top where the plane is.
            var at = ctx.Ecs.GetOrDefault<Transform>(row.Entity);
            var slab = Transform.At(at.Translation.X, at.Translation.Y - 0.5f, at.Translation.Z);
            physics.Add(row.Entity, PhysicsShape.Box(new Vec3(100f, 1f, 100f)), BodyKind.Static, slab);
        }
    }
}

/// <summary>A coin, picked up by walking into it.</summary>
[Behavior]
public partial struct Coin
{
    /// <summary>Makes every coin a sensor while playing, which reports a touch and stops nothing.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public static void Build(BehaviorContext ctx)
    {
        if (!ctx.TryRes<PhysicsWorld>(out var physics)) return;

        foreach (var row in ctx.Ecs.Query<Coin>(markChanged: false))
        {
            if (physics.Has(row.Entity)) continue;

            var at = ctx.Ecs.GetOrDefault<Transform>(row.Entity);
            physics.Add(row.Entity, PhysicsShape.Sphere(at.Scale.X), BodyKind.Static, at, sensor: true);
        }
    }
}

/// <summary>The goal, where the coins are brought.</summary>
[Behavior]
public partial struct Goal
{
    /// <summary>Makes the goal a sensor while playing.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public static void Build(BehaviorContext ctx)
    {
        if (!ctx.TryRes<PhysicsWorld>(out var physics)) return;

        foreach (var row in ctx.Ecs.Query<Goal>(markChanged: false))
        {
            if (physics.Has(row.Entity)) continue;

            var at = ctx.Ecs.GetOrDefault<Transform>(row.Entity);
            physics.Add(row.Entity, PhysicsShape.Box(at.Scale * 2f), BodyKind.Static, at, sensor: true);
        }
    }
}

/// <summary>How many coins the player carries, kept in a save.</summary>
[Behavior, Persist]
public partial struct Wallet
{
    /// <summary>Coins picked up so far.</summary>
    public int Coins;
}

/// <summary>
/// The player, a model moved with WASD, whose body is a ball of its own so the model stays upright
/// while the ball rolls against the walls.
/// </summary>
[Behavior]
public partial struct Runner
{
    /// <summary>How fast it walks, in units a second.</summary>
    public float Speed;

    /// <summary>The ball it rides, made as play starts.</summary>
    public Entity Body;

    /// <summary>Whether it was walking last frame, so the walk starts and stops once.</summary>
    public bool Walking;

    private static AssetHandle _coin;

    /// <summary>Gives the runner its ball while playing, when it has none.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public void Begin(BehaviorContext ctx)
    {
        if (Speed <= 0f) Speed = 4f;
        if (!ctx.TryRes<PhysicsWorld>(out var physics) || ctx.Ecs.IsAlive(Body)) return;

        var at = ctx.Ecs.GetOrDefault<Transform>(ctx.Entity);
        Body = ctx.Ecs.Spawn();
        ctx.Ecs.SetName(Body, "Runner body");

        var ball = Transform.At(at.Translation.X, at.Translation.Y + 0.5f, at.Translation.Z);
        ctx.Ecs.Add(Body, ball);
        physics.Add(Body, PhysicsShape.Sphere(0.5f), BodyKind.Dynamic, ball, mass: 1f);
    }

    /// <summary>Walks while play is not held, and plays the walk while it does.</summary>
    [OnUpdate, InState(Pause.Off)]
    public void Walk(BehaviorContext ctx)
    {
        if (!ctx.TryRes<PhysicsWorld>(out var physics) || !physics.Has(Body)) return;

        var x = (ctx.Input.KeyDown(Key.D) ? 1f : 0f) - (ctx.Input.KeyDown(Key.A) ? 1f : 0f);
        var z = (ctx.Input.KeyDown(Key.S) ? 1f : 0f) - (ctx.Input.KeyDown(Key.W) ? 1f : 0f);

        var (linear, _) = physics.Velocity(Body);
        physics.SetVelocity(Body, new Vec3(x * Speed, linear.Y, z * Speed));

        // The model stands where the ball is, upright, facing the way it walks.
        var ball = ctx.Ecs.GetOrDefault<Transform>(Body).Translation;
        var place = ctx.Ecs.GetOrDefault<Transform>(ctx.Entity);
        place.Translation = new Vec3(ball.X, ball.Y - 0.5f, ball.Z);
        if (x != 0f || z != 0f) place.Rotation = Quat.FromAxisAngle(Vec3.UnitY, MathF.Atan2(x, z));
        ctx.Ecs.Set(ctx.Entity, place);

        var walking = x != 0f || z != 0f;
        if (walking == Walking) return;

        Walking = walking;
        if (walking) Animation.Play(ctx.Entity, "Bend", new AnimationSettings { Repeat = true, Speed = 2f, Blend = 0.15f });
        else Animation.Stop(ctx.Entity);
    }

    /// <summary>Stops the ball as the game is won, which would otherwise roll on with nothing steering it.</summary>
    [OnEnter(Mode.Won)]
    public void Finish(BehaviorContext ctx)
    {
        if (ctx.TryRes<PhysicsWorld>(out var physics) && physics.Has(Body))
        {
            var (linear, _) = physics.Velocity(Body);
            physics.SetVelocity(Body, new Vec3(0f, linear.Y, 0f));
        }

        if (!Walking) return;
        Walking = false;
        Animation.Stop(ctx.Entity);
    }

    /// <summary>Picks up a coin the ball touches, and wins at the goal with all of them.</summary>
    [OnUpdate, InState(Pause.Off)]
    public static void Touch(BehaviorContext ctx)
    {
        foreach (var contact in ctx.Read<ContactStarted>())
        {
            foreach (var row in ctx.Ecs.Query<Runner>(markChanged: false))
            {
                var other = contact.A == row.Component.Body ? contact.B : contact.B == row.Component.Body ? contact.A : Entity.None;
                if (other.IsNone || !ctx.Ecs.IsAlive(other)) continue;

                if (ctx.Ecs.Has<Coin>(other))
                {
                    if (ctx.TryRes<PhysicsWorld>(out var physics)) physics.Remove(other);
                    ctx.Ecs.Despawn(other);

                    var wallet = ctx.Ecs.GetOrDefault<Wallet>(row.Entity);
                    wallet.Coins++;
                    ctx.Ecs.Set(row.Entity, wallet);

                    if (!_coin.IsValid) _coin = AssetServer.Load(AssetKind.Audio, "sounds/coin.wav");
                    Audio.Play(_coin, AudioSettings.Effect);
                }
                else if (ctx.Ecs.Has<Goal>(other) && ctx.Ecs.Count<Coin>() == 0)
                {
                    Console.WriteLine($"[courtyard] won with {ctx.Ecs.GetOrDefault<Wallet>(row.Entity).Coins} coins");
                    ctx.SetState(Mode.Won);
                }
            }
        }
    }
}

/// <summary>The camera, which follows the runner from behind and above.</summary>
[Behavior]
public partial struct Follow
{
    /// <summary>Spawns the game's camera as the menu opens.</summary>
    /// <remarks>
    /// On entering the menu rather than at startup, since the editor loads these scripts while a
    /// level is edited and never enters the game's states, so a camera spawned at startup would
    /// draw over the editor's view and be saved into the level.
    /// </remarks>
    [OnEnter(Mode.Menu)]
    public static void Spawn(BehaviorContext ctx)
    {
        var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 55f });
        ctx.Ecs.SetName(camera, "Follow camera");
        ctx.Ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 9f, 12f), Vec3.Zero, Vec3.UnitY));
        ctx.Ecs.Add(camera, new Follow());
    }

    /// <summary>Keeps the runner in view.</summary>
    [OnUpdate]
    public void Keep(BehaviorContext ctx)
    {
        foreach (var row in ctx.Ecs.Query<Runner>(markChanged: false))
        {
            var target = ctx.Ecs.GetOrDefault<Transform>(row.Entity).Translation;
            ctx.Ecs.Set(ctx.Entity, Transform.LookingAt(target + new Vec3(0f, 9f, 10f), target, Vec3.UnitY));
            return;
        }
    }
}

/// <summary>The count of coins on the HUD, found by this rather than kept in a field of the script.</summary>
/// <remarks>
/// A component, so the text is found again after the script is reloaded while the game runs, which
/// starts the script's static fields over and moves its components onto the new types.
/// </remarks>
[Behavior]
public partial struct CoinsText
{
}

/// <summary>The words on screen, which are the menu, the coins carried, the pause menu and the win.</summary>
[Behavior]
public partial struct Screens
{

    /// <summary>The menu, until Enter starts the game.</summary>
    [OnEnter(Mode.Menu)]
    public static void Menu(BehaviorContext ctx) =>
        Banner(ctx, "Courtyard\nPress Enter to play", Mode.Menu);

    /// <summary>Starts the game from the menu.</summary>
    [OnUpdate, InState(Mode.Menu)]
    public static void Start(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Enter)) ctx.SetState(Mode.Playing);
    }

    /// <summary>The count of coins carried, in a corner, while playing.</summary>
    [OnEnter(Mode.Playing)]
    public static void Hud(BehaviorContext ctx)
    {
        var panel = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(16f),
            Top = Length.Px(16f),
            Padding = Length.Px(10f),
            Color = (0f, 0f, 0f, 0.45f),
        });

        var coins = Ui.SpawnText("Coins 0", new UiSettings { Color = (1f, 0.85f, 0.2f, 1f) }, 22f);
        ctx.Ecs.Add(coins, new CoinsText());
        ctx.Ecs.SetParent(coins, panel);
        ctx.Ecs.DespawnOnExit(panel, Mode.Playing);
    }

    /// <summary>Keeps the count up to date, and handles pausing, saving and loading.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public static void Play(BehaviorContext ctx)
    {
        foreach (var row in ctx.Ecs.Query<Wallet>(markChanged: false))
        {
            var text = $"Coins {row.Component.Coins} of {row.Component.Coins + ctx.Ecs.Count<Coin>()}";
            foreach (var shown in ctx.Ecs.Query<CoinsText>(markChanged: false)) Ui.SetText(shown.Entity, text);
            break;
        }

        if (ctx.Input.KeyPressed(Key.Escape))
            ctx.SetState(App.TryState<Pause>(out var paused) && paused == Pause.On ? Pause.Off : Pause.On);

        if (ctx.Input.KeyPressed(Key.F5)) Console.WriteLine($"[courtyard] saved {SaveGame.Save(ctx.Ecs)} entities");
        if (ctx.Input.KeyPressed(Key.F9))
        {
            // The ball is the game's own and not the level's, so the load leaves it, and the
            // runner the load brings back is given a new one.
            var balls = new List<Entity>();
            foreach (var row in ctx.Ecs.Query<Runner>(markChanged: false)) balls.Add(row.Component.Body);
            foreach (var ball in balls) ctx.Ecs.Despawn(ball);

            Console.WriteLine($"[courtyard] loaded {SaveGame.Load(ctx.Ecs).Entities.Count} entities");
        }
    }

    /// <summary>The pause menu while play is held, with the simulation held too.</summary>
    [OnEnter(Pause.On)]
    public static void Paused(BehaviorContext ctx)
    {
        if (ctx.TryRes<PhysicsWorld>(out var physics)) physics.Paused = true;
        Banner(ctx, "Paused\nEscape to go on, F5 to save, F9 to load", Pause.On);
    }

    /// <summary>Lets the simulation go on as play does.</summary>
    [OnExit(Pause.On)]
    public static void Resumed(BehaviorContext ctx)
    {
        if (ctx.TryRes<PhysicsWorld>(out var physics)) physics.Paused = false;
    }

    /// <summary>The win.</summary>
    [OnEnter(Mode.Won)]
    public static void Won(BehaviorContext ctx) =>
        Banner(ctx, "Every coin home!\nYou win", Mode.Won);

    /// <summary>Centered words on a dark panel, gone when the state they belong to is left.</summary>
    private static void Banner<TState>(BehaviorContext ctx, string text, TState state) where TState : struct, Enum
    {
        var panel = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Percent(30f),
            Top = Length.Percent(40f),
            Padding = Length.Px(24f),
            Color = (0f, 0f, 0f, 0.7f),
        });

        var words = Ui.SpawnText(text, new UiSettings { Color = (1f, 1f, 1f, 1f) }, 28f);
        ctx.Ecs.SetParent(words, panel);
        ctx.Ecs.DespawnOnExit(panel, state);
    }
}
