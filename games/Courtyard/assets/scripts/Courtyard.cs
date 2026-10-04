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

// The walls, the ground, the coins and the goal are bodies the level holds as RigidBody and
// Collider components, which the physics plugin makes, so a coin and the goal are only what the
// runner tells apart by.

/// <summary>A coin, picked up by walking into it.</summary>
[Behavior]
public partial struct Coin
{
}

/// <summary>The goal, where the coins are brought.</summary>
[Behavior]
public partial struct Goal
{
}

/// <summary>How many coins the player carries, kept in a save.</summary>
[Behavior, Persist]
public partial struct Wallet
{
    /// <summary>Coins picked up so far.</summary>
    public int Coins;
}

/// <summary>
/// The player, a model walked with WASD as a character, which the walls stop and the ground holds,
/// facing the way it walks.
/// </summary>
[Behavior]
public partial struct Runner
{
    /// <summary>How fast it walks, in units a second.</summary>
    public float Speed;

    /// <summary>Whether it was walking last frame, so the walk starts and stops once.</summary>
    public bool Walking;

    private static AssetHandle _coin;

    /// <summary>Makes the runner a character as play starts.</summary>
    [OnEnter(Mode.Playing)]
    public void Begin(BehaviorContext ctx) => Body(ctx);

    /// <summary>Makes the runner a character again after a load, which brings it back as the level has it.</summary>
    [OnUpdate, InState(Mode.Playing)]
    public void Loaded(BehaviorContext ctx)
    {
        foreach (var _ in ctx.Read<SaveLoaded>())
        {
            Body(ctx);
            return;
        }
    }

    /// <summary>
    /// A capsule as tall as the model, standing on its feet, which the physics plugin makes a
    /// character from its components.
    /// </summary>
    /// <remarks>
    /// As play starts rather than in the level, since the editor runs this script while the level
    /// is edited, and a runner with a body there would walk off while it was being placed. Added
    /// by command, since this runs over the runners and adding to one moves it in the world's
    /// storage.
    /// </remarks>
    private void Body(BehaviorContext ctx)
    {
        if (Speed <= 0f) Speed = 4f;
        if (ctx.Ecs.Has<CharacterController>(ctx.Entity)) return;

        ctx.Cmd.Add(ctx.Entity, new RigidBody { Kind = BodyKind.Dynamic, Mass = 70f });
        ctx.Cmd.Add(ctx.Entity, new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(0.7f, 1.8f, 0.7f), Offset = new Vec3(0f, 0.9f, 0f) });
        ctx.Cmd.Add(ctx.Entity, new CharacterController());
    }

    /// <summary>Walks while play is not held, facing the way it walks, and plays the walk while it does.</summary>
    [OnUpdate, InState(Pause.Off)]
    public void Walk(BehaviorContext ctx, ref CharacterController body, ref Transform place)
    {
        var x = (ctx.Input.KeyDown(Key.D) ? 1f : 0f) - (ctx.Input.KeyDown(Key.A) ? 1f : 0f);
        var z = (ctx.Input.KeyDown(Key.S) ? 1f : 0f) - (ctx.Input.KeyDown(Key.W) ? 1f : 0f);

        body.Move = new Vec3(x * Speed, 0f, z * Speed);

        // The body stays upright whatever the rotation, so turning the model is the game's alone.
        var walking = x != 0f || z != 0f;
        if (walking) place.Rotation = Quat.FromAxisAngle(Vec3.UnitY, MathF.Atan2(x, z));

        if (walking == Walking) return;

        Walking = walking;
        if (walking) Animation.Play(ctx.Entity, "Bend", new AnimationSettings { Repeat = true, Speed = 2f, Blend = 0.15f });
        else Animation.Stop(ctx.Entity);
    }

    /// <summary>Stops the runner as the game is won, which would otherwise walk on with nothing steering it.</summary>
    [OnEnter(Mode.Won)]
    public void Finish(BehaviorContext ctx, ref CharacterController body)
    {
        body.Move = Vec3.Zero;

        if (!Walking) return;
        Walking = false;
        Animation.Stop(ctx.Entity);
    }

    /// <summary>Picks up a coin the runner touches, and wins at the goal with all of them.</summary>
    [OnUpdate, InState(Pause.Off)]
    public static void Touch(BehaviorContext ctx)
    {
        foreach (var contact in ctx.Read<ContactStarted>())
        {
            foreach (var row in ctx.Ecs.Query<Runner>(markChanged: false))
            {
                var other = contact.A == row.Entity ? contact.B : contact.B == row.Entity ? contact.A : Entity.None;
                if (other.IsNone || !ctx.Ecs.IsAlive(other)) continue;

                if (ctx.Ecs.Has<Coin>(other))
                {
                    // Its body goes with it.
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

/// <summary>The level's camera, which follows the runner from behind and above.</summary>
[Behavior]
public partial struct Follow
{
    /// <summary>Keeps the runner in view while playing.</summary>
    /// <remarks>
    /// Only while playing, since the editor runs this script while the level is edited and the
    /// camera stays where the level put it there, which is also where the menu shows it.
    /// </remarks>
    [OnUpdate, InState(Mode.Playing)]
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
        if (ctx.Input.KeyPressed(Key.F9)) Console.WriteLine($"[courtyard] loaded {SaveGame.Load(ctx.Ecs).Entities.Count} entities");
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
